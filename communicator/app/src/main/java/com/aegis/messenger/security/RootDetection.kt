package com.aegis.messenger.security

import android.content.Context
import java.io.File

object RootDetection {
    private val knownRootApps = listOf("com.topjohnwu.magisk", "eu.chainfire.supersu", "com.koushikdutta.superuser")
    private val suPaths = listOf("/system/bin/su", "/system/xbin/su", "/sbin/su")

    fun isDeviceRooted(context: Context): Boolean {
        if (checkSuExists() || checkRootAppsInstalled(context) || checkSystemWritable()) {
            return true
        }
        return false
    }

    private fun checkSuExists(): Boolean = suPaths.any { File(it).exists() }

    private fun checkRootAppsInstalled(context: Context): Boolean = knownRootApps.any {
        try {
            context.packageManager.getPackageInfo(it, 0)
            true
        } catch (e: Exception) {
            false
        }
    }

    private fun checkSystemWritable(): Boolean = try {
        File("/system").canWrite()
    } catch (e: Exception) {
        false
    }
}
