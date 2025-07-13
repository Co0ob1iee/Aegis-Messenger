package com.aegis.messenger.crypto

import org.whispersystems.libsignal.state.PreKeyBundle
import org.whispersystems.libsignal.state.SignalProtocolStore
import org.whispersystems.libsignal.protocol.SignalProtocolAddress
import org.whispersystems.libsignal.SessionBuilder
import org.whispersystems.libsignal.SessionCipher
import org.whispersystems.libsignal.InvalidKeyException

class SignalSessionManager(private val store: SignalProtocolStore) {
    // Inicjalizacja sesji X3DH
    fun initializeSession(remoteAddress: SignalProtocolAddress, preKeyBundle: PreKeyBundle) {
        val sessionBuilder = SessionBuilder(store, remoteAddress)
        try {
            sessionBuilder.process(preKeyBundle)
        } catch (e: InvalidKeyException) {
            // Obsługa błędów, logowanie, walidacja
            throw e
        }
    }

    // Szyfrowanie wiadomości Double Ratchet
    fun encryptMessage(remoteAddress: SignalProtocolAddress, plaintext: ByteArray): ByteArray {
        val sessionCipher = SessionCipher(store, remoteAddress)
        return sessionCipher.encrypt(plaintext).serialize()
    }

    // Odszyfrowywanie wiadomości Double Ratchet
    fun decryptMessage(remoteAddress: SignalProtocolAddress, ciphertext: ByteArray): ByteArray {
        val sessionCipher = SessionCipher(store, remoteAddress)
        val message = sessionCipher.decrypt(ciphertext)
        return message
    }

    // Obsługa wiadomości poza kolejnością
    // Libsignal automatycznie zarządza stanem zapadek
}
