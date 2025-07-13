package com.aegis.messenger.security

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import org.junit.Assert.*
import org.junit.Test

class JwtManagerTest {
    private val context: Context = ApplicationProvider.getApplicationContext()

    @Test
    fun testStoreAndValidateJwt() {
        val fakeJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjI0MDAwMDAwMDB9.signature"
        JwtManager.storeJwt(context, fakeJwt)
        val stored = JwtManager.getJwt(context)
        assertEquals(fakeJwt, stored)
        // Walidacja JWT (przykład, prawdziwy JWT powinien być generowany przez serwer)
        assertTrue(JwtManager.isJwtValid(fakeJwt))
    }
}
