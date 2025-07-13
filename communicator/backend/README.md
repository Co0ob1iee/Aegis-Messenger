# Aegis Messenger Backend

Minimalny backend dla komunikatora Signal/REST/WebSocket/JWT.

## Endpointy REST
- POST /api/keys – publikacja pre-kluczy
- GET /api/keys/:userId – pobieranie pre-kluczy
- POST /api/messages – wysyłanie zaszyfrowanej wiadomości

## WebSocket
- Port 3001, autoryzacja przez JWT w query string (?token=...)
- Push wiadomości do odbiorcy w czasie rzeczywistym

## JWT
- Użyj klucza `supersecret` do generowania tokenów JWT (pole `sub` = userId)

## Uruchomienie
```
npm install
npm start
```

## Uwaga
- Przechowywanie kluczy i wiadomości jest in-memory (do testów/demo)
- Brak obsługi Signal Protocol po stronie serwera (tylko przekazywanie kluczy i wiadomości)
- Wdrożenie produkcyjne wymaga bazy danych, obsługi Signal, HTTPS, SGX itd.
