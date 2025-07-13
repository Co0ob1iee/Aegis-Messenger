package com.aegis.messenger.security

import android.content.Context
import androidx.room.Database
import androidx.room.Room
import androidx.room.RoomDatabase
import net.sqlcipher.database.SupportFactory
import com.aegis.messenger.security.DatabasePasswordManager
import com.aegis.messenger.security.KeystoreHelper
import com.aegis.messenger.db.Message
import com.aegis.messenger.db.MessageDao

@Database(entities = [Message::class], version = 1)
abstract class EncryptedDatabase : RoomDatabase() {
    abstract fun messageDao(): MessageDao

    companion object {
        @Volatile private var INSTANCE: EncryptedDatabase? = null

        fun getInstance(context: Context): EncryptedDatabase {
            return INSTANCE ?: synchronized(this) {
                val key = KeystoreHelper.getDatabaseKey() ?: KeystoreHelper.generateDatabaseKey()
                val password = ByteArray(32).apply { java.security.SecureRandom().nextBytes(this) }
                if (DatabasePasswordManager.getDecryptedPassword(context, key) == null) {
                    DatabasePasswordManager.storeEncryptedPassword(context, password, key)
                }
                val dbPass = DatabasePasswordManager.getDecryptedPassword(context, key) ?: password
                val factory = SupportFactory(dbPass)
                val instance = Room.databaseBuilder(
                    context.applicationContext,
                    EncryptedDatabase::class.java,
                    "aegis_encrypted.db"
                ).openHelperFactory(factory).build()
                INSTANCE = instance
                instance
            }
        }
    }
}
