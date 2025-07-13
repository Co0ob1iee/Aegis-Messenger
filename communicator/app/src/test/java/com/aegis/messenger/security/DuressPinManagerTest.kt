package com.aegis.messenger.security

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import org.junit.Assert.*
import org.junit.Test

class DuressPinManagerTest {
    private val context: Context = ApplicationProvider.getApplicationContext()

    @Test
    fun testPinTypes() {
        DuressPinManager.setPins(context, "1234", "9999")
        assertEquals(DuressPinManager.PinType.TRUE, DuressPinManager.checkPin(context, "1234"))
        assertEquals(DuressPinManager.PinType.DURESS, DuressPinManager.checkPin(context, "9999"))
        assertEquals(DuressPinManager.PinType.INVALID, DuressPinManager.checkPin(context, "0000"))
    }
}
