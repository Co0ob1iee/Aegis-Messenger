package com.aegis.messenger.security

import android.content.Context
import android.content.SharedPreferences
import android.util.Base64
import javax.crypto.Cipher
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec

object DatabasePasswordManager {
    private const val PREF_NAME = "secure_prefs"
    private const val KEY_ENCRYPTED_DB_PASS = "encrypted_db_pass"
    private const val IV_SIZE = 12
    private const val TAG_SIZE = 128

    fun storeEncryptedPassword(context: Context, password: ByteArray, key: SecretKey) {
        val cipher = Cipher.getInstance("AES/GCM/NoPadding")
        val iv = ByteArray(IV_SIZE).apply { java.security.SecureRandom().nextBytes(this) }
        val spec = GCMParameterSpec(TAG_SIZE, iv)
        cipher.init(Cipher.ENCRYPT_MODE, key, spec)
        val encrypted = cipher.doFinal(password)
        val prefs = context.getSharedPreferences(PREF_NAME, Context.MODE_PRIVATE)
        prefs.edit().putString(KEY_ENCRYPTED_DB_PASS, Base64.encodeToString(iv + encrypted, Base64.DEFAULT)).apply()
    }

    fun getDecryptedPassword(context: Context, key: SecretKey): ByteArray? {
        val prefs = context.getSharedPreferences(PREF_NAME, Context.MODE_PRIVATE)
        val enc = prefs.getString(KEY_ENCRYPTED_DB_PASS, null) ?: return null
        val data = Base64.decode(enc, Base64.DEFAULT)
        val iv = data.sliceArray(0 until IV_SIZE)
        val encrypted = data.sliceArray(IV_SIZE until data.size)
        val cipher = Cipher.getInstance("AES/GCM/NoPadding")
        val spec = GCMParameterSpec(TAG_SIZE, iv)
        cipher.init(Cipher.DECRYPT_MODE, key, spec)
        return cipher.doFinal(encrypted)
    }
}
