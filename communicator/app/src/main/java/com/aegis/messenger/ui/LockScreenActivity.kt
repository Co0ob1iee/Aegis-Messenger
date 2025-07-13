package com.aegis.messenger.ui

import android.os.Bundle
import android.widget.EditText
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import com.aegis.messenger.security.DuressPinManager

class LockScreenActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // setContentView(R.layout.activity_lock_screen)
        // val pinInput = findViewById<EditText>(R.id.pinInput)
        // val submitBtn = findViewById<Button>(R.id.submitBtn)
        // submitBtn.setOnClickListener {
        //     val pin = pinInput.text.toString()
        //     when (DuressPinManager.checkPin(this, pin)) {
        //         DuressPinManager.PinType.TRUE -> unlockApp()
        //         DuressPinManager.PinType.DURESS -> handleDuress()
        //         DuressPinManager.PinType.INVALID -> showError()
        //     }
        // }
    }

    private fun unlockApp() {
        Toast.makeText(this, "Dostęp do prawdziwych danych", Toast.LENGTH_SHORT).show()
        // TODO: Przejdź do głównej aktywności
    }

    private fun handleDuress() {
        Toast.makeText(this, "Tryb przynęty lub bezpieczne wyczyszczenie", Toast.LENGTH_SHORT).show()
        // TODO: Otwórz fałszywą instancję lub wyczyść dane
    }

    private fun showError() {
        Toast.makeText(this, "Nieprawidłowy PIN", Toast.LENGTH_SHORT).show()
    }
}
