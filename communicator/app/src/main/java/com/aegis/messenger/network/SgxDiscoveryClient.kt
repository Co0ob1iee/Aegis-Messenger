package com.aegis.messenger.network

import android.content.Context
import java.security.MessageDigest
import javax.crypto.Cipher
import javax.crypto.spec.SecretKeySpec
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.Response

object SgxDiscoveryClient {
    fun hashPhoneNumbers(phoneNumbers: List<String>): List<String> {
        val md = MessageDigest.getInstance("SHA-256")
        return phoneNumbers.map { number ->
            md.digest(number.toByteArray()).joinToString(separator = "") { "%02x".format(it) }
        }
    }

    fun remoteAttestation(serverUrl: String): Boolean {
        // Przykładowa implementacja zdalnej atestacji SGX przez REST
        return try {
            val client = OkHttpClient()
            val request = Request.Builder()
                .url("$serverUrl/api/sgx-attestation")
                .get()
                .build()
            val response: Response = client.newCall(request).execute()
            response.isSuccessful && response.body?.string()?.contains("SGX_OK") == true
        } catch (e: Exception) {
            false
        }
    }

    fun encryptHashes(hashes: List<String>, key: ByteArray): ByteArray {
        val cipher = Cipher.getInstance("AES/ECB/PKCS7Padding")
        cipher.init(Cipher.ENCRYPT_MODE, SecretKeySpec(key, "AES"))
        val data = hashes.joinToString(",").toByteArray()
        return cipher.doFinal(data)
    }

    fun decryptResponse(response: ByteArray, key: ByteArray): List<String> {
        val cipher = Cipher.getInstance("AES/ECB/PKCS7Padding")
        cipher.init(Cipher.DECRYPT_MODE, SecretKeySpec(key, "AES"))
        val decrypted = cipher.doFinal(response)
        return String(decrypted).split(",")
    }
}
