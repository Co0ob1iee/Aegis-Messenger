package com.aegis.messenger.security

import android.os.Debug
import java.io.File

object AntiDebug {
    fun isDebuggerConnected(): Boolean = Debug.isDebuggerConnected()

    fun isTracerPidPresent(): Boolean {
        try {
            val statusFile = File("/proc/self/status")
            val lines = statusFile.readLines()
            for (line in lines) {
                if (line.startsWith("TracerPid:")) {
                    val pid = line.split(":")[1].trim().toInt()
                    if (pid > 0) return true
                }
            }
        } catch (e: Exception) {
            // Obsługa błędów
        }
        return false
    }
}
