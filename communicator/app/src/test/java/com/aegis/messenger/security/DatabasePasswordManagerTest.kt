package com.aegis.messenger.security

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import org.junit.Assert.*
import org.junit.Test
import javax.crypto.SecretKey

class DatabasePasswordManagerTest {
    private val context: Context = ApplicationProvider.getApplicationContext()
    private val key: SecretKey = KeystoreHelper.generateDatabaseKey()

    @Test
    fun testStoreAndRetrievePassword() {
        val password = ByteArray(32) { it.toByte() }
        DatabasePasswordManager.storeEncryptedPassword(context, password, key)
        val decrypted = DatabasePasswordManager.getDecryptedPassword(context, key)
        assertNotNull(decrypted)
        assertArrayEquals(password, decrypted)
    }
}
