package com.aegis.messenger.network

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import kotlinx.coroutines.runBlocking
import org.junit.Assert.*
import org.junit.Test

class ServerCommunicatorTest {
    private val context: Context = ApplicationProvider.getApplicationContext()
    private val communicator = ServerCommunicator(context)

    @Test
    fun testPublishPreKeys() = runBlocking {
        val bundle = mapOf("identityKey" to "test", "preKeys" to listOf("pk1", "pk2"))
        val response = communicator.publishPreKeys(bundle)
        assertNotNull(response)
    }

    @Test
    fun testSendMessage() = runBlocking {
        val payload = mapOf("receiverId" to "userB", "sealedPayload" to byteArrayOf(1,2,3))
        val response = communicator.sendMessage(payload)
        assertNotNull(response)
    }
}
