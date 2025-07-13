package com.aegis.messenger.network

import android.content.Context
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.WebSocketListener
import org.whispersystems.libsignal.state.PreKeyBundle
import org.whispersystems.libsignal.protocol.SignalProtocolAddress
import com.aegis.messenger.crypto.SignalKeyManager
import com.aegis.messenger.crypto.SignalSessionManager
import com.aegis.messenger.security.JwtManager
import retrofit2.Response

class ServerCommunicator(private val context: Context) {
    private val api = NetworkClient.api

    suspend fun publishPreKeys(preKeyBundle: Map<String, Any>) = withContext(Dispatchers.IO) {
        val jwt = JwtManager.getJwt(context)
        val headers = mapOf("Authorization" to "Bearer $jwt")
        api.publishPreKeys(preKeyBundle, headers)
    }

    suspend fun getPreKeys(userId: String): PreKeyBundle? = withContext(Dispatchers.IO) {
        val jwt = JwtManager.getJwt(context)
        val headers = mapOf("Authorization" to "Bearer $jwt")
        val response: Response<Map<String, Any>> = api.getPreKeys(userId, headers)
        if (response.isSuccessful) {
            val body = response.body()
            if (body != null) {
                // Parsowanie odpowiedzi na PreKeyBundle
                val preKeyBundle = PreKeyBundle(
                    body["registrationId"] as Int,
                    body["deviceId"] as Int,
                    body["preKeyId"] as Int,
                    body["preKeyPublic"] as ByteArray,
                    body["signedPreKeyId"] as Int,
                    body["signedPreKeyPublic"] as ByteArray,
                    body["signedPreKeySignature"] as ByteArray,
                    body["identityKey"] as ByteArray
                )
                return@withContext preKeyBundle
            }
        }
        null
    }

    suspend fun sendMessage(payload: Map<String, Any>) = withContext(Dispatchers.IO) {
        val jwt = JwtManager.getJwt(context)
        val headers = mapOf("Authorization" to "Bearer $jwt")
        api.sendMessage(payload, headers)
    }

    fun connectWebSocket(listener: WebSocketListener) {
        NetworkClient.createWebSocket(listener)
    }

    // Sealed Sender
    fun sendSealedMessage(receiverId: String, message: ByteArray, senderId: String, key: ByteArray) {
        val sealedPayload = SealedSender.sealMessage(senderId, message, key)
        val payload = mapOf(
            "receiverId" to receiverId,
            "sealedPayload" to sealedPayload
        )
        sendMessage(payload)
    }

    // SGX Discovery
    fun discoverContactsSGX(hashes: List<String>, key: ByteArray) {
        val encryptedHashes = SgxDiscoveryClient.encryptHashes(hashes, key)
        // Przykład: wysyłka i odszyfrowanie odpowiedzi
        val payload = mapOf("encryptedHashes" to encryptedHashes)
        sendMessage(payload)
        // Odbiór odpowiedzi i odszyfrowanie (do zaimplementowania wg backendu)
    }
}
