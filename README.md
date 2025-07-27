# Aegis Messenger 📡🔐

Secure Android messenger powered by Signal Protocol, SQLCipher, JWT, WebSocket, and advanced client protection mechanisms.

## ✨ Key Features
- End-to-end encryption (Signal Protocol: X3DH, Double Ratchet)
- Encrypted database (SQLCipher + Android Keystore)
- Secure key storage (hardware-backed Keystore)
- Server communication via REST & WebSocket
- Private contact discovery (SGX)
- Sealed Sender (sender anonymity)
- Anti-analysis mechanisms (root, debug, ptrace detection)
- Duress PIN (fake environment / secure wipe)
- App icon hiding mode
- Safety Numbers (key verification with QR)
- Encrypted group chats & file sharing
- Threat modeling using STRIDE

## 🛠️ Project Architecture
- `crypto/` – Signal Protocol logic
- `network/` – server comms, SGX, Sealed Sender
- `security/` – Keystore, SQLCipher, root/debug detection, JWT, Duress PIN
- `ui/` – activities, views, Safety Numbers, LockScreen
- `group/` – group chat logic
- `file/` – file sharing
- `db/` – Room encrypted database

## ⚡ Quick Start
1. Open the project in Android Studio.
2. Install dependencies (Gradle).
3. Build and run on Android device (minSdk 23).
4. Run unit tests: `./gradlew test`

## 📂 Backend (demo)
- Sample backend lives in `backend/` folder
- Run: `cd backend && npm install && npm start`
- REST: publish/retrieve keys, send messages
- WebSocket: push messages in real-time, JWT auth
- Data persistence: in-memory (for testing only)

## 🔒 Security
- Keys always stored in hardware-backed Keystore
- SQLCipher for all local data
- Code obfuscation (R8), root/debug/ptrace detection
- Duress PIN – secure wipe & reset functionality
- STRIDE threat model in `ui/ThreatModeling.kt`

## 🔄 Message Flow Example
1. Message composed in UI (`ChatActivity.kt`)
2. Encrypted with Signal (`SignalSessionManager.kt`)
3. Sent via REST/WebSocket (`ServerCommunicator.kt`)
4. Stored in encrypted Room database (`MessageDao.kt`)

## 📊 Requirements
- Android 6.0+ (minSdk 23)
- Node.js (for demo backend)

## ⚖️ License
Demo project – open for further development and security audit.

---

**📧 Contact:**
If you have questions or want to contribute, open an issue or reach out via GitHub.

