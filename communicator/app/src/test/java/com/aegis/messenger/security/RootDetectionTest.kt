package com.aegis.messenger.security

import android.content.Context
import androidx.test.core.app.ApplicationProvider
import org.junit.Assert.*
import org.junit.Test

class RootDetectionTest {
    private val context: Context = ApplicationProvider.getApplicationContext()

    @Test
    fun testIsDeviceRooted() {
        // Test na emulatorze powinien zwracać false
        assertFalse(RootDetection.isDeviceRooted(context))
    }
}
