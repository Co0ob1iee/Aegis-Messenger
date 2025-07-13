package com.aegis.messenger.ui

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.aegis.messenger.network.SgxDiscoveryClient

class ContactDiscoveryActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // setContentView(R.layout.activity_contact_discovery)
        // TODO: Pobierz kontakty z książki adresowej
        // TODO: Zahaszuj numery i wyślij do SGX
        // TODO: Odbierz i odszyfruj odpowiedź
    }

    private fun discoverContacts(phoneNumbers: List<String>, key: ByteArray) {
        val hashes = SgxDiscoveryClient.hashPhoneNumbers(phoneNumbers)
        if (SgxDiscoveryClient.remoteAttestation()) {
            val encryptedHashes = SgxDiscoveryClient.encryptHashes(hashes, key)
            // TODO: Wyślij encryptedHashes do serwera
            // TODO: Odbierz odpowiedź i odszyfruj
        }
    }
}
