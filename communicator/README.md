# Aegis Messenger

Bezpieczny komunikator na Androida, oparty o Signal Protocol, SQLCipher, JWT, WebSocket oraz zaawansowane mechanizmy ochrony klienta.

## Kluczowe funkcje
- Szyfrowanie end-to-end (Signal Protocol: X3DH, Double Ratchet)
- Szyfrowana baza danych (SQLCipher + Android Keystore)
- Bezpieczne przechowywanie kluczy
- Komunikacja z serwerem przez REST i WebSocket
- Prywatne odkrywanie kontaktów (SGX)
- Sealed Sender (ukrywanie tożsamości nadawcy)
- Mechanizmy antyanalizy (root, debug, ptrace)
- Duress PIN (przynęta/bezpieczne wyczyszczenie)
- Ukrywanie ikony aplikacji
- Safety Numbers (weryfikacja kluczy, QR)
- Szyfrowane czaty grupowe i przesyłanie plików
- Modelowanie zagrożeń STRIDE

## Architektura projektu
- `crypto/` – logika Signal Protocol
- `network/` – komunikacja z serwerem, SGX, Sealed Sender
- `security/` – Keystore, SQLCipher, root/debug detection, JWT, duress PIN
- `ui/` – aktywności, widoki, Safety Numbers, LockScreen
- `group/` – czaty grupowe
- `file/` – przesyłanie plików
- `db/` – baza danych Room

## Szybki start
1. Otwórz projekt w Android Studio.
2. Zainstaluj zależności (Gradle).
3. Zbuduj i uruchom aplikację na urządzeniu z Androidem (minSdk 23).
4. Uruchom testy jednostkowe: `./gradlew test`

## Backend (demo)
- Przykładowy backend znajduje się w folderze `backend/`.
- Uruchomienie: `cd backend && npm install && npm start`
- REST: publikacja/pobieranie kluczy, wysyłka wiadomości
- WebSocket: push wiadomości w czasie rzeczywistym, autoryzacja JWT
- Przechowywanie danych: in-memory (tylko do testów)

## Bezpieczeństwo
- Klucze zawsze w hardware-backed Keystore
- SQLCipher dla wszystkich danych lokalnych
- Zaciemnianie kodu (R8), wykrywanie root/debug/ptrace
- Duress PIN – bezpieczne czyszczenie danych i reset aplikacji
- STRIDE – modelowanie zagrożeń w `ui/ThreatModeling.kt`

## Przykład przepływu wiadomości
1. Kompozycja wiadomości w UI (`ChatActivity.kt`)
2. Szyfrowanie przez Signal (`SignalSessionManager.kt`)
3. Wysyłka przez REST/WebSocket (`ServerCommunicator.kt`)
4. Zapis w szyfrowanej bazie Room (`MessageDao.kt`)

## Wymagania
- Android 6.0+ (minSdk 23)
- Node.js (do backendu demo)

## Licencja
Projekt demonstracyjny – do dalszego rozwoju i audytu bezpieczeństwa.

---

**Kontakt:**
Jeśli masz pytania lub chcesz współtworzyć projekt, napisz na GitHubie lub otwórz issue.
