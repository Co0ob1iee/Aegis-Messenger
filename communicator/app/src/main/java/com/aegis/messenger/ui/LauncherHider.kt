package com.aegis.messenger.ui

import android.content.Context
import android.content.pm.PackageManager

object LauncherHider {
    fun hideLauncherIcon(context: Context) {
        val pm = context.packageManager
        pm.setComponentEnabledSetting(
            context.packageManager.getLaunchIntentForPackage(context.packageName)?.component!!,
            PackageManager.COMPONENT_ENABLED_STATE_DISABLED,
            PackageManager.DONT_KILL_APP
        )
    }

    fun showLauncherIcon(context: Context) {
        val pm = context.packageManager
        pm.setComponentEnabledSetting(
            context.packageManager.getLaunchIntentForPackage(context.packageName)?.component!!,
            PackageManager.COMPONENT_ENABLED_STATE_ENABLED,
            PackageManager.DONT_KILL_APP
        )
    }
}
