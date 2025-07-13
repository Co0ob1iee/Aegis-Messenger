package com.aegis.messenger.ui

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity

class MainActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // ...existing code...
        // Sprawdź root i debug
        if (com.aegis.messenger.security.RootDetection.isDeviceRooted(this) ||
            com.aegis.messenger.security.AntiDebug.isDebuggerConnected() ||
            com.aegis.messenger.security.AntiDebug.isTracerPidPresent()) {
            finishAffinity()
            return
        }
        // Sprawdź JWT
        val jwt = com.aegis.messenger.security.JwtManager.getJwt(this)
        if (jwt == null || !com.aegis.messenger.security.JwtManager.isJwtValid(jwt)) {
            // TODO: Przekieruj do ekranu logowania
        }
        // Przykład ukrywania ikony
        // com.aegis.messenger.ui.LauncherHider.hideLauncherIcon(this)
        // Przykład obsługi PIN
        // val pinInput = findViewById<android.widget.EditText>(R.id.pinInput)
        // val submitBtn = findViewById<android.widget.Button>(R.id.submitBtn)
        // submitBtn.setOnClickListener {
        //     val pin = pinInput.text.toString()
        //     when (com.aegis.messenger.security.DuressPinManager.checkPin(this, pin)) {
        //         com.aegis.messenger.security.DuressPinManager.PinType.TRUE -> {/* dostęp */}
        //         com.aegis.messenger.security.DuressPinManager.PinType.DURESS -> {/* tryb przynęty */}
        //         com.aegis.messenger.security.DuressPinManager.PinType.INVALID -> {/* błąd */}
        //     }
        // }
    }
}
