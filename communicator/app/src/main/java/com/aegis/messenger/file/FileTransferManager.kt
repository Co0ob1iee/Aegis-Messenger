package com.aegis.messenger.file

import android.content.Context
import java.io.File
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.spec.IvParameterSpec
import javax.crypto.spec.SecretKeySpec

object FileTransferManager {
    fun encryptFile(context: Context, file: File): Pair<File, ByteArray> {
        val keyGen = KeyGenerator.getInstance("AES")
        keyGen.init(256)
        val key = keyGen.generateKey().encoded
        val iv = ByteArray(16).apply { java.security.SecureRandom().nextBytes(this) }
        val cipher = Cipher.getInstance("AES/CBC/PKCS7Padding")
        cipher.init(Cipher.ENCRYPT_MODE, SecretKeySpec(key, "AES"), IvParameterSpec(iv))
        val encryptedFile = File(file.parent, file.name + ".enc")
        file.inputStream().use { input ->
            encryptedFile.outputStream().use { output ->
                val buffer = ByteArray(4096)
                var bytesRead: Int
                while (input.read(buffer).also { bytesRead = it } != -1) {
                    output.write(cipher.update(buffer, 0, bytesRead))
                }
                output.write(cipher.doFinal())
            }
        }
        return Pair(encryptedFile, key + iv)
    }

    fun decryptFile(encryptedFile: File, keyIv: ByteArray): File {
        val key = keyIv.sliceArray(0 until 32)
        val iv = keyIv.sliceArray(32 until 48)
        val cipher = Cipher.getInstance("AES/CBC/PKCS7Padding")
        cipher.init(Cipher.DECRYPT_MODE, SecretKeySpec(key, "AES"), IvParameterSpec(iv))
        val decryptedFile = File(encryptedFile.parent, encryptedFile.name.removeSuffix(".enc"))
        encryptedFile.inputStream().use { input ->
            decryptedFile.outputStream().use { output ->
                val buffer = ByteArray(4096)
                var bytesRead: Int
                while (input.read(buffer).also { bytesRead = it } != -1) {
                    output.write(cipher.update(buffer, 0, bytesRead))
                }
                output.write(cipher.doFinal())
            }
        }
        return decryptedFile
    }
}
