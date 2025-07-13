package com.aegis.messenger.db

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import androidx.room.Room
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.aegis.messenger.security.EncryptedDatabase
import org.junit.After
import org.junit.Assert.*
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith

@RunWith(AndroidJUnit4::class)
class MessageDaoTest {
    private lateinit var db: EncryptedDatabase
    private lateinit var dao: MessageDao
    private val context: Context = ApplicationProvider.getApplicationContext()

    @Before
    fun setup() {
        db = EncryptedDatabase.getInstance(context)
        dao = db.messageDao()
    }

    @After
    fun teardown() {
        db.close()
    }

    @Test
    fun testInsertAndQueryMessage() {
        val message = Message(senderId = "A", receiverId = "B", content = byteArrayOf(1,2,3), timestamp = System.currentTimeMillis())
        dao.insertMessage(message)
        val messages = dao.getMessagesForUser("B")
        assertTrue(messages.isNotEmpty())
        assertEquals("A", messages.first().senderId)
    }
}
