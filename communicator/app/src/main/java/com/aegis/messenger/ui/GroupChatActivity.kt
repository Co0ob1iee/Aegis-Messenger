package com.aegis.messenger.ui

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.aegis.messenger.group.GroupChatManager
import com.aegis.messenger.crypto.SignalSessionManager
import org.whispersystems.libsignal.protocol.SignalProtocolAddress

class GroupChatActivity : AppCompatActivity() {
    private lateinit var groupChatManager: GroupChatManager
    private val members: List<SignalProtocolAddress> = listOf() // TODO: Pobierz z modelu grupy

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // setContentView(R.layout.activity_group_chat)
        // TODO: Inicjalizuj UI grupy, listę członków, obsługę wysyłania
        val sessionManager = SignalSessionManager(/* TODO: przekaz store */)
        groupChatManager = GroupChatManager(sessionManager)
    }

    private fun sendGroupMessage(senderKey: ByteArray, message: ByteArray) {
        groupChatManager.sendGroupMessage(senderKey, message, members)
        // TODO: Wyślij zaszyfrowaną wiadomość na serwer
    }
}
