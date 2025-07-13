package com.aegis.messenger.db

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "messages")
data class Message(
    @PrimaryKey(autoGenerate = true) val id: Long = 0,
    val senderId: String,
    val receiverId: String,
    val content: ByteArray,
    val timestamp: Long,
    val isGroup: Boolean = false
)
