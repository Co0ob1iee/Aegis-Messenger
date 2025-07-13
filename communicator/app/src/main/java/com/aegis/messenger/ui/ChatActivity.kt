package com.aegis.messenger.ui

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.aegis.messenger.db.EncryptedDatabase
import com.aegis.messenger.db.Message
import com.aegis.messenger.db.MessageDao
import kotlinx.coroutines.launch

class ChatActivity : AppCompatActivity() {
    private lateinit var messageDao: MessageDao
    private lateinit var userId: String
    private lateinit var messagesAdapter: androidx.recyclerview.widget.RecyclerView.Adapter<*>
    private lateinit var recyclerView: androidx.recyclerview.widget.RecyclerView
    private lateinit var sendButton: android.widget.Button
    private lateinit var inputField: android.widget.EditText

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_chat)
        val db = com.aegis.messenger.security.EncryptedDatabase.getInstance(this)
        messageDao = db.messageDao()
        // Pobierz userId z JWT
        val jwt = com.aegis.messenger.security.JwtManager.getJwt(this)
        userId = jwt?.let { com.auth0.android.jwt.JWT(it).subject ?: "" } ?: ""
        // Inicjalizuj UI czatu
        recyclerView = findViewById(R.id.recyclerView)
        sendButton = findViewById(R.id.sendButton)
        inputField = findViewById(R.id.inputField)
        messagesAdapter = ChatMessagesAdapter(emptyList())
        recyclerView.adapter = messagesAdapter
        recyclerView.layoutManager = androidx.recyclerview.widget.LinearLayoutManager(this)
        // Obsługa wysyłania wiadomości
        sendButton.setOnClickListener {
            val text = inputField.text.toString()
            if (text.isNotBlank()) {
                sendMessage(receiverId = "receiverId", content = text.toByteArray())
                inputField.text.clear()
            }
        }
        loadMessages()
    }

    private fun loadMessages() {
        lifecycleScope.launch {
            val messages = messageDao.getMessagesForUser(userId)
            (messagesAdapter as ChatMessagesAdapter).updateMessages(messages)
        }
    }

    private fun sendMessage(receiverId: String, content: ByteArray, isGroup: Boolean = false) {
        val message = com.aegis.messenger.db.Message(
            senderId = userId,
            receiverId = receiverId,
            content = content,
            timestamp = System.currentTimeMillis(),
            isGroup = isGroup
        )
        lifecycleScope.launch {
            messageDao.insertMessage(message)
            (messagesAdapter as ChatMessagesAdapter).addMessage(message)
            // Wyślij wiadomość przez NetworkClient
            val payload = mapOf(
                "receiverId" to receiverId,
                "sealedPayload" to content
            )
            com.aegis.messenger.network.NetworkClient.api.sendMessage(payload)
        }
// Adapter do wyświetlania wiadomości
class ChatMessagesAdapter(private var messages: List<com.aegis.messenger.db.Message>) : androidx.recyclerview.widget.RecyclerView.Adapter<ChatMessagesAdapter.MessageViewHolder>() {
    class MessageViewHolder(val view: android.view.View) : androidx.recyclerview.widget.RecyclerView.ViewHolder(view) {
        val textView: android.widget.TextView = view.findViewById(R.id.messageText)
    }
    override fun onCreateViewHolder(parent: android.view.ViewGroup, viewType: Int): MessageViewHolder {
        val view = android.view.LayoutInflater.from(parent.context).inflate(R.layout.item_message, parent, false)
        return MessageViewHolder(view)
    }
    override fun onBindViewHolder(holder: MessageViewHolder, position: Int) {
        val msg = messages[position]
        holder.textView.text = String(msg.content)
    }
    override fun getItemCount(): Int = messages.size
    fun updateMessages(newMessages: List<com.aegis.messenger.db.Message>) {
        messages = newMessages
        notifyDataSetChanged()
    }
    fun addMessage(message: com.aegis.messenger.db.Message) {
        messages = messages + message
        notifyItemInserted(messages.size - 1)
    }
}
    }
}
