package com.aegis.messenger.crypto

import android.content.Context
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import java.security.KeyPairGenerator
import java.security.KeyStore
import java.security.PrivateKey
import java.security.PublicKey
import org.whispersystems.libsignal.IdentityKeyPair
import org.whispersystems.libsignal.state.PreKeyRecord
import org.whispersystems.libsignal.state.SignedPreKeyRecord
import org.whispersystems.libsignal.state.PreKeyBundle
import org.whispersystems.libsignal.util.KeyHelper

class SignalKeyManager(private val context: Context) {
    private val keystoreAliasIK = "aegis_ik"
    private val keystoreAliasSPK = "aegis_spk"
    private val keystoreAliasOPK = "aegis_opk"
    private val keyStore: KeyStore = KeyStore.getInstance("AndroidKeyStore").apply { load(null) }

    // Generowanie klucza tożsamości (IK)
    fun generateIdentityKeyPair(): IdentityKeyPair {
        val generator = KeyPairGenerator.getInstance(KeyProperties.KEY_ALGORITHM_EC, "AndroidKeyStore")
        val spec = KeyGenParameterSpec.Builder(
            keystoreAliasIK,
            KeyProperties.PURPOSE_SIGN or KeyProperties.PURPOSE_VERIFY
        )
            .setDigests(KeyProperties.DIGEST_SHA256)
            .setUserAuthenticationRequired(false)
            .setIsStrongBoxBacked(true)
            .setKeySize(256)
            .build()
        generator.initialize(spec)
        val keyPair = generator.generateKeyPair()
        // Użyj kluczy do utworzenia IdentityKeyPair
        return IdentityKeyPair(
            org.whispersystems.libsignal.IdentityKey(keyPair.public.encoded, 0),
            keyPair.private.encoded
        )
    }

    // Generowanie podpisanego pre-klucza (SPK)
    fun generateSignedPreKey(identityKeyPair: IdentityKeyPair, keyId: Int): SignedPreKeyRecord {
        val signedPreKey = KeyHelper.generateSignedPreKey(identityKeyPair, keyId)
        // Zapisz w Keystore lub bazie
        return signedPreKey
    }

    // Generowanie jednorazowych pre-kluczy (OPK)
    fun generateOneTimePreKeys(startId: Int, count: Int): List<PreKeyRecord> {
        return KeyHelper.generatePreKeys(startId, count)
    }

    // Pobieranie kluczy z Keystore
    fun getPrivateKey(alias: String): PrivateKey? =
        (keyStore.getEntry(alias, null) as? KeyStore.PrivateKeyEntry)?.privateKey

    fun getPublicKey(alias: String): PublicKey? =
        (keyStore.getEntry(alias, null) as? KeyStore.PrivateKeyEntry)?.certificate?.publicKey

    // TODO: Rotacja SPK, uzupełnianie OPK, walidacja hardware security
    // TODO: Integracja z bazą danych i serwerem
}
