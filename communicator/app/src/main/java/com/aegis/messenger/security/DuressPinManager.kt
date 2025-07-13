package com.aegis.messenger.security

import android.content.Context
import android.content.SharedPreferences

object DuressPinManager {
    private const val PREF_NAME = "duress_prefs"
    private const val KEY_TRUE_PIN = "true_pin"
    private const val KEY_DURESS_PIN = "duress_pin"

    fun setPins(context: Context, truePin: String, duressPin: String) {
        val prefs = context.getSharedPreferences(PREF_NAME, Context.MODE_PRIVATE)
        prefs.edit().putString(KEY_TRUE_PIN, truePin).putString(KEY_DURESS_PIN, duressPin).apply()
    }

    fun checkPin(context: Context, pin: String): PinType {
        val prefs = context.getSharedPreferences(PREF_NAME, Context.MODE_PRIVATE)
        return when (pin) {
            prefs.getString(KEY_TRUE_PIN, null) -> PinType.TRUE
            prefs.getString(KEY_DURESS_PIN, null) -> PinType.DURESS
            else -> PinType.INVALID
        }
    }

    enum class PinType {
        TRUE, DURESS, INVALID
    }
}
