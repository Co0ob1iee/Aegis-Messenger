package com.aegis.messenger.ui

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.aegis.messenger.file.FileTransferManager
import java.io.File

class FileTransferActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // setContentView(R.layout.activity_file_transfer)
        // TODO: Inicjalizuj UI przesyłania plików
    }

    private fun encryptAndSendFile(file: File) {
        val (encryptedFile, keyIv) = FileTransferManager.encryptFile(this, file)
        // TODO: Prześlij zaszyfrowany plik na serwer
        // TODO: Wyślij klucz i IV w wiadomości E2EE
    }

    private fun receiveAndDecryptFile(encryptedFile: File, keyIv: ByteArray) {
        val decryptedFile = FileTransferManager.decryptFile(encryptedFile, keyIv)
        // TODO: Wyświetl lub zapisz odszyfrowany plik
    }
}
