package com.aegis.messenger.security

import android.content.Context
import android.content.SharedPreferences
import com.auth0.android.jwt.JWT

object JwtManager {
    private const val PREF_NAME = "jwt_prefs"
    private const val KEY_JWT = "jwt_token"

    fun storeJwt(context: Context, token: String) {
        val prefs = context.getSharedPreferences(PREF_NAME, Context.MODE_PRIVATE)
        prefs.edit().putString(KEY_JWT, token).apply()
    }

    fun getJwt(context: Context): String? {
        val prefs = context.getSharedPreferences(PREF_NAME, Context.MODE_PRIVATE)
        return prefs.getString(KEY_JWT, null)
    }

    fun isJwtValid(token: String): Boolean {
        return try {
            val jwt = JWT(token)
            !jwt.isExpired(10)
        } catch (e: Exception) {
            false
        }
    }

    // Mechanizm odświeżania tokenu
    fun refreshJwt(context: Context, refreshToken: String): String? {
        // TODO: Wywołanie API do odświeżenia tokenu
        return null // Placeholder
    }
}
