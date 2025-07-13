package com.aegis.messenger.ui

import android.content.Context
import android.graphics.Bitmap
import android.util.AttributeSet
import android.view.View
import android.widget.ImageView
import android.widget.TextView
import androidx.constraintlayout.widget.ConstraintLayout
import com.google.zxing.BarcodeFormat
import com.google.zxing.qrcode.QRCodeWriter

class SafetyNumberView @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null,
    defStyleAttr: Int = 0
) : ConstraintLayout(context, attrs, defStyleAttr) {

    private val safetyNumberText: TextView
    private val qrImageView: ImageView

    init {
        inflate(context, R.layout.view_safety_number, this)
        safetyNumberText = findViewById(R.id.safetyNumberText)
        qrImageView = findViewById(R.id.qrImageView)
    }

    fun setSafetyNumber(safetyNumber: String) {
        safetyNumberText.text = safetyNumber
        qrImageView.setImageBitmap(generateQrCode(safetyNumber))
    }

    private fun generateQrCode(data: String): Bitmap {
        val writer = QRCodeWriter()
        val bitMatrix = writer.encode(data, BarcodeFormat.QR_CODE, 400, 400)
        val bmp = Bitmap.createBitmap(400, 400, Bitmap.Config.RGB_565)
        for (x in 0 until 400) {
            for (y in 0 until 400) {
                bmp.setPixel(x, y, if (bitMatrix[x, y]) android.graphics.Color.BLACK else android.graphics.Color.WHITE)
            }
        }
        return bmp
    }
}
