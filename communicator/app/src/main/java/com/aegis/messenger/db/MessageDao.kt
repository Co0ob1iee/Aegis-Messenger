package com.aegis.messenger.db

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.Query
import androidx.room.Delete

@Dao
interface MessageDao {
    @Insert
    suspend fun insertMessage(message: Message)

    @Query("SELECT * FROM messages WHERE receiverId = :userId ORDER BY timestamp DESC")
    suspend fun getMessagesForUser(userId: String): List<Message>

    @Delete
    suspend fun deleteMessage(message: Message)
}
