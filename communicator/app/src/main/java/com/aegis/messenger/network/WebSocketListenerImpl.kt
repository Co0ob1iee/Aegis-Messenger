package com.aegis.messenger.network

import okhttp3.Response
import okhttp3.WebSocket
import okhttp3.WebSocketListener
import okio.ByteString
import org.json.JSONObject
import android.util.Log
import com.aegis.messenger.crypto.SignalSessionManager
import org.whispersystems.libsignal.protocol.SignalProtocolAddress

class WebSocketListenerImpl(
    private val signalSessionManager: SignalSessionManager,
    private val localAddress: SignalProtocolAddress
) : WebSocketListener() {
    override fun onOpen(webSocket: WebSocket, response: Response) {
        // Połączono z serwerem
        Log.d("WebSocket", "Połączono z serwerem")
    }

    override fun onMessage(webSocket: WebSocket, text: String) {
        // Otrzymano wiadomość tekstową
        try {
            val json = JSONObject(text)
            val type = json.optString("type")
            when (type) {
                "chat" -> {
                    val sender = json.optString("sender")
                    val message = json.optString("message")
                    Log.d("WebSocket", "Wiadomość od $sender: $message")
                    // Tu można dodać logikę wyświetlania w UI
                }
                "info" -> {
                    val info = json.optString("info")
                    Log.d("WebSocket", "Info: $info")
                }
                else -> {
                    Log.d("WebSocket", "Nieznany typ wiadomości: $type")
                }
            }
        } catch (e: Exception) {
            Log.e("WebSocket", "Błąd parsowania JSON: ${e.message}")
        }
    }

    override fun onMessage(webSocket: WebSocket, bytes: ByteString) {
        // Otrzymano wiadomość binarną
        try {
            val encryptedBytes = bytes.toByteArray()
            // Przykład: adres nadawcy powinien być przekazany w wiadomości lub ustalony z kontekstu
            // val remoteAddress = ...
            // val decrypted = signalSessionManager.decryptMessage(remoteAddress, encryptedBytes)
            // Log.d("WebSocket", "Odszyfrowana wiadomość: ${String(decrypted)}")
        } catch (e: Exception) {
            Log.e("WebSocket", "Błąd odszyfrowania: ${e.message}")
        }
    }

    override fun onClosing(webSocket: WebSocket, code: Int, reason: String) {
        webSocket.close(code, reason)
        Log.d("WebSocket", "Zamykanie połączenia: $reason ($code)")
    }

    override fun onFailure(webSocket: WebSocket, t: Throwable, response: Response?) {
        Log.e("WebSocket", "Błąd połączenia: ${t.message}")
    }
}
