package com.aegis.messenger.network

import javax.crypto.Cipher
import javax.crypto.spec.SecretKeySpec

object SealedSender {
    fun sealMessage(senderId: String, message: ByteArray, key: ByteArray): ByteArray {
        val cipher = Cipher.getInstance("AES/GCM/NoPadding")
        val iv = ByteArray(12).apply { java.security.SecureRandom().nextBytes(this) }
        cipher.init(Cipher.ENCRYPT_MODE, SecretKeySpec(key, "AES"), javax.crypto.spec.GCMParameterSpec(128, iv))
        val sealedSender = cipher.doFinal(senderId.toByteArray())
        val sealedPayload = cipher.doFinal(message)
        return iv + sealedSender + sealedPayload
    }

    fun unsealMessage(sealed: ByteArray, key: ByteArray): Pair<String, ByteArray> {
        val iv = sealed.sliceArray(0 until 12)
        val cipher = Cipher.getInstance("AES/GCM/NoPadding")
        cipher.init(Cipher.DECRYPT_MODE, SecretKeySpec(key, "AES"), javax.crypto.spec.GCMParameterSpec(128, iv))
        // Rozdziel sealedSender i sealedPayload według długości senderId (wymaga protokołu)
        // Placeholder: zwraca całość jako payload
        val payload = cipher.doFinal(sealed.sliceArray(12 until sealed.size))
        return Pair("unknown", payload)
    }
}
