# Sealed Sender Implementation Guide

## Overview

**Sealed Sender** (Unidentified Sender) is an advanced privacy feature that hides the sender's identity from the server while still allowing the recipient to verify the sender's authenticity. This prevents metadata leakage where the server knows "who is talking to whom."

### Why Sealed Sender?

**Normal Messaging:**
```
Server sees: FROM Alice TO Bob + encrypted payload
```
The server knows Alice is sending to Bob (metadata leakage).

**Sealed Sender:**
```
Server sees: TO Bob + double-encrypted payload
```
The server only knows Bob is receiving a message, but **NOT** who sent it!

## Architecture

### Components

1. **SenderCertificate** - Certificate issued by server proving sender is authorized
2. **UnidentifiedSenderMessage** - Multi-layer encrypted message envelope
3. **SenderCertificateService** - Certificate generation and verification
4. **SealedSenderService** - Multi-layer encryption/decryption logic
5. **SealedSenderFallbackService** - Automatic fallback to normal messages

### Multi-Layer Encryption

Sealed sender uses **three layers of encryption**:

```
┌─────────────────────────────────────────────────────────────┐
│ Layer 3: Outer Envelope (ECDH + AES-256-GCM)               │
│ Server CANNOT decrypt this - only recipient can            │
│ ┌───────────────────────────────────────────────────────┐   │
│ │ Layer 2: Inner Content                               │   │
│ │ Contains: Sender Certificate + Encrypted Payload     │   │
│ │ ┌─────────────────────────────────────────────────┐   │   │
│ │ │ Layer 1: Signal Protocol (Double Ratchet)      │   │   │
│ │ │ Normal E2EE message encryption                 │   │   │
│ │ │ Plaintext: "Hello Bob!"                        │   │   │
│ │ └─────────────────────────────────────────────────┘   │   │
│ └───────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Encryption Flow

**Sender (Alice) sends to Recipient (Bob):**

1. **Signal Protocol Encryption** (Layer 1)
   ```csharp
   var signalPayload = await _signalProtocol.EncryptMessageAsync(bobId, "Hello Bob!");
   ```

2. **Create Inner Content** (Layer 2)
   ```csharp
   var innerContent = new UnidentifiedSenderMessageContent
   {
       SenderCertificate = aliceCertificate,
       EncryptedPayload = signalPayload,
       Type = MessageType.Normal
   };
   ```

3. **Outer Envelope Encryption** (Layer 3)
   - Generate ephemeral ECDH key pair
   - Perform ECDH with recipient's public key → shared secret
   - Derive encryption key using HKDF(shared_secret)
   - Encrypt inner content with AES-256-GCM
   ```csharp
   var sealedMessage = await _sealedSenderService.EncryptAsync(
       bobId,
       bobIdentityKey,
       plaintext,
       aliceCertificate,
       signalPayload
   );
   ```

### Decryption Flow

**Recipient (Bob) receives:**

1. **Decrypt Outer Envelope** (Layer 3)
   - Perform ECDH with ephemeral public key → shared secret
   - Derive decryption key using HKDF(shared_secret)
   - Decrypt with AES-256-GCM → inner content

2. **Verify Sender Certificate** (Layer 2)
   - Extract sender certificate from inner content
   - Verify server signature using server's public key
   - Check expiration
   - Extract sender ID (Alice)

3. **Decrypt Signal Protocol Payload** (Layer 1)
   ```csharp
   var result = await _sealedSenderService.DecryptAsync(
       sealedMessage,
       bobPrivateKey,
       serverPublicKey
   );
   // result.SenderId = Alice's ID
   // result.Plaintext = "Hello Bob!"
   ```

## Sender Certificates

### What is a Sender Certificate?

A sender certificate is a cryptographically signed document that proves:
- The sender is a registered user (authenticated by server)
- The sender is authorized to send messages
- The certificate is not expired

**Certificate Structure:**
```csharp
{
    CertificateId: Guid,
    SenderId: Guid,                    // User ID
    DeviceId: uint,                    // Device ID (default: 1)
    SenderIdentityKey: string,         // Sender's public key
    ExpiresAt: DateTime,               // Certificate expiration (24h default)
    IssuedAt: DateTime,                // When issued
    ServerSignature: string            // RSA signature by server
}
```

### Certificate Lifecycle

**1. Request Certificate**
```http
POST /api/sealed-sender/certificate
Authorization: Bearer {jwt_token}

{
  "deviceId": 1,
  "identityKey": "base64_public_key"
}
```

**Response:**
```json
{
  "certificateId": "abc123...",
  "senderId": "user-guid",
  "deviceId": 1,
  "senderIdentityKey": "base64_public_key",
  "expiresAt": "2025-11-17T14:30:00Z",
  "issuedAt": "2025-11-16T14:30:00Z",
  "serverSignature": "base64_rsa_signature"
}
```

**2. Certificate Validation**
- ✅ Verify server signature (RSA-2048 + SHA-256)
- ✅ Check expiration (24 hours default)
- ✅ Verify not revoked

**3. Certificate Caching**
- Certificates are cached for their validity period
- Automatically renewed when < 6 hours remaining
- Invalidated on identity key change

## Usage Examples

### Send Sealed Sender Message

```csharp
public class MessageService
{
    private readonly SealedSenderFallbackService _sealedSender;
    private readonly ISenderCertificateService _certificateService;

    public async Task<Guid> SendPrivateMessageAsync(
        Guid senderId,
        Guid recipientId,
        string plaintext,
        string senderIdentityKey)
    {
        // Get or create sender certificate
        var certificate = await _certificateService.GetOrCreateCertificateAsync(
            senderId,
            deviceId: 1,
            senderIdentityKey);

        // Send with sealed sender (auto-fallback to normal if fails)
        var (encryptedMessage, usedSealedSender) = await _sealedSender.SendMessageAsync(
            senderId,
            recipientId,
            plaintext,
            senderIdentityKey,
            useSealedSender: true);

        if (usedSealedSender)
        {
            _logger.LogInformation("Message sent with sealed sender (anonymous)");
        }
        else
        {
            _logger.LogInformation("Message sent with normal encryption (fallback)");
        }

        // Send to server
        await SendToServer(recipientId, encryptedMessage);
    }
}
```

### Receive Sealed Sender Message

```csharp
public async Task<(string plaintext, Guid senderId)> ReceiveMessageAsync(
    Guid recipientId,
    byte[] encryptedMessage)
{
    // Automatically detects sealed sender vs normal
    var (plaintext, senderId) = await _sealedSender.ReceiveMessageAsync(
        recipientId,
        encryptedMessage);

    _logger.LogInformation("Message received from {SenderId}", senderId);
    return (plaintext, senderId);
}
```

### Manual Sealed Sender Encryption

```csharp
public async Task<UnidentifiedSenderMessage> EncryptManuallyAsync()
{
    // 1. Encrypt with Signal Protocol first
    var signalPayload = await _signalProtocol.EncryptMessageAsync(
        recipientId,
        "Hello!");

    // 2. Get sender certificate
    var certificate = await _certificateService.GetOrCreateCertificateAsync(
        senderId,
        deviceId: 1,
        senderIdentityKey);

    // 3. Encrypt with sealed sender
    var sealedMessage = await _sealedSenderService.EncryptAsync(
        recipientId,
        recipientIdentityKey,
        "Hello!",
        certificate,
        signalPayload);

    // 4. Serialize for transmission
    var messageBytes = sealedMessage.Serialize();
    return sealedMessage;
}
```

### Manual Sealed Sender Decryption

```csharp
public async Task<SealedSenderDecryptionResult> DecryptManuallyAsync(
    byte[] encryptedBytes)
{
    // 1. Deserialize message
    var sealedMessage = UnidentifiedSenderMessage.Deserialize(encryptedBytes);

    // 2. Decrypt (verifies certificate automatically)
    var result = await _sealedSenderService.DecryptAsync(
        sealedMessage,
        recipientPrivateKey,
        serverPublicKey);

    // 3. Use decrypted data
    Console.WriteLine($"From: {result.SenderId}");
    Console.WriteLine($"Message: {result.Plaintext}");
    Console.WriteLine($"Verified: {result.SenderIdentityKey}");

    return result;
}
```

## API Endpoints

### 1. Request Sender Certificate

**Endpoint:** `POST /api/sealed-sender/certificate`

**Auth:** Required (JWT)

**Request:**
```json
{
  "deviceId": 1,
  "identityKey": "base64_encoded_public_key"
}
```

**Response:** `200 OK`
```json
{
  "certificateId": "abc123-...",
  "senderId": "user-guid",
  "deviceId": 1,
  "senderIdentityKey": "base64_public_key",
  "expiresAt": "2025-11-17T14:30:00Z",
  "issuedAt": "2025-11-16T14:30:00Z",
  "serverSignature": "base64_rsa_signature"
}
```

### 2. Verify Certificate

**Endpoint:** `POST /api/sealed-sender/certificate/verify`

**Auth:** None (public endpoint)

**Request:**
```json
{
  "certificateId": "abc123...",
  "senderId": "user-guid",
  "deviceId": 1,
  "senderIdentityKey": "base64_key",
  "expiresAt": "2025-11-17T14:30:00Z",
  "issuedAt": "2025-11-16T14:30:00Z",
  "serverSignature": "base64_signature",
  "serverPublicKey": "base64_server_public_key"
}
```

**Response:** `200 OK`
```json
{
  "isValid": true,
  "isExpired": false,
  "expiresAt": "2025-11-17T14:30:00Z"
}
```

### 3. Get Server Public Key

**Endpoint:** `GET /api/sealed-sender/server-key`

**Auth:** None (public endpoint)

**Response:** `200 OK`
```json
{
  "publicKey": "base64_encoded_rsa_public_key",
  "algorithm": "RSA-2048",
  "usage": "Certificate signing and verification"
}
```

### 4. Revoke Certificate

**Endpoint:** `DELETE /api/sealed-sender/certificate/{certificateId}`

**Auth:** Required (JWT)

**Response:** `204 No Content`

## Fallback Mechanism

Sealed sender includes automatic fallback to normal Signal Protocol messages when:
- Certificate generation fails
- Recipient doesn't support sealed sender
- Server rejects sealed sender message
- Network issues with certificate service

**Fallback is transparent:**
```csharp
var (encryptedMessage, usedSealedSender) = await _fallbackService.SendMessageAsync(
    senderId,
    recipientId,
    plaintext,
    senderIdentityKey,
    useSealedSender: true  // Try sealed sender first
);

if (!usedSealedSender)
{
    _logger.LogInformation("Fell back to normal encryption");
}
```

## Security Considerations

### ✅ Sealed Sender Provides

1. **Sender Anonymity** - Server doesn't know who sent the message
2. **Authentication** - Recipient can verify sender via certificate
3. **Forward Secrecy** - Each message uses ephemeral keys (ECDH)
4. **Integrity** - AEAD encryption (AES-GCM) prevents tampering

### ⚠️ Sealed Sender Does NOT Provide

1. **Recipient Anonymity** - Server still knows who receives the message
2. **Traffic Analysis Protection** - Server can still see message timing/size
3. **IP Anonymity** - Server can still see sender's IP address (use Tor/VPN)

### 🔒 Best Practices

1. **Always verify certificates** before accepting messages
2. **Use sealed sender for sensitive conversations** where metadata privacy matters
3. **Combine with Tor/VPN** for full anonymity (hide IP address)
4. **Rotate certificates regularly** (24h expiration enforced)
5. **Revoke certificates** when identity keys change

## Message Format

### Sealed Sender Message Structure

```
┌─────────────────────────────────────────────────────┐
│ Version (1 byte)            │ 0x01                  │
├─────────────────────────────────────────────────────┤
│ Ephemeral Public Key Length │ 2 bytes (uint16)      │
├─────────────────────────────────────────────────────┤
│ Ephemeral Public Key        │ Variable (91 bytes)   │
├─────────────────────────────────────────────────────┤
│ Auth Tag Length             │ 1 byte                │
├─────────────────────────────────────────────────────┤
│ Authentication Tag          │ 16 bytes (AES-GCM)    │
├─────────────────────────────────────────────────────┤
│ Encrypted Content           │ Variable              │
│ (Inner content + padding)   │                       │
└─────────────────────────────────────────────────────┘
```

### Inner Content Structure

```
┌─────────────────────────────────────────────────────┐
│ Message Type (1 byte)       │ 0=Normal, 1=PreKey    │
├─────────────────────────────────────────────────────┤
│ Certificate Length          │ 4 bytes (int32)       │
├─────────────────────────────────────────────────────┤
│ Sender Certificate          │ Variable (JSON)       │
├─────────────────────────────────────────────────────┤
│ Signature Length            │ 2 bytes (uint16)      │
├─────────────────────────────────────────────────────┤
│ Server Signature            │ 256 bytes (RSA-2048)  │
├─────────────────────────────────────────────────────┤
│ Encrypted Payload           │ Variable (Signal msg) │
│ (Signal Protocol message)   │                       │
└─────────────────────────────────────────────────────┘
```

## Performance Considerations

### Overhead

- **Certificate Request**: ~100ms (cached for 24h)
- **Encryption Overhead**: +5-10ms vs normal Signal Protocol
- **Message Size**: +~400 bytes (certificate + ephemeral key)

### Optimization Tips

1. **Cache certificates** - reduces API calls
2. **Reuse certificates** - valid for 24 hours
3. **Batch certificate renewals** - renew before expiration
4. **Use normal mode for bulk/non-sensitive** messages

### Scalability

- **Certificate cache**: 10,000 certificates in memory
- **Cleanup interval**: Every 1 hour (remove expired)
- **Concurrent encryption**: Thread-safe, uses concurrent dictionaries

## Testing

### Unit Tests

```csharp
[Fact]
public async Task SealedSender_EncryptDecrypt_Success()
{
    // Arrange
    var sealedSenderService = new SealedSenderService(logger, signalProtocol, certService);
    var certificate = await certService.GenerateCertificateAsync(senderId, 1, identityKey);

    // Act
    var sealed = await sealedSenderService.EncryptAsync(
        recipientId, recipientKey, "Hello!", certificate, signalPayload);

    var result = await sealedSenderService.DecryptAsync(
        sealed, recipientPrivateKey, serverPublicKey);

    // Assert
    Assert.Equal("Hello!", result.Plaintext);
    Assert.Equal(senderId, result.SenderId);
}
```

### Integration Tests

```bash
# Test certificate request
curl -X POST https://api.aegis.com/api/sealed-sender/certificate \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"deviceId": 1, "identityKey": "base64_key"}'

# Test certificate verification
curl -X POST https://api.aegis.com/api/sealed-sender/certificate/verify \
  -H "Content-Type: application/json" \
  -d @certificate.json

# Get server public key
curl https://api.aegis.com/api/sealed-sender/server-key
```

## Troubleshooting

### Certificate Verification Failed

**Symptom:** "Sender certificate is invalid or expired"

**Solutions:**
1. Check certificate expiration (`expiresAt` field)
2. Verify server public key is correct
3. Check if certificate was revoked
4. Ensure clock synchronization (NTP)

### Decryption Failed

**Symptom:** "Failed to decrypt sealed sender message"

**Solutions:**
1. Verify message format (version byte = 0x01)
2. Check ephemeral key derivation
3. Ensure recipient private key is correct
4. Verify AEAD authentication tag

### Fallback to Normal Mode

**Symptom:** Messages sent as normal instead of sealed sender

**Check logs for:**
```
"Sealed sender failed ... falling back to normal"
```

**Common causes:**
1. Certificate generation failure
2. Network issues
3. Recipient identity key not available
4. Sealed sender disabled in config

## Configuration

### appsettings.json

```json
{
  "SealedSender": {
    "Enabled": true,
    "AllowFallback": true,
    "CertificateValidityPeriod": "24:00:00",
    "CertificateRenewalThreshold": "06:00:00",
    "MaxCachedCertificates": 10000,
    "CleanupInterval": "01:00:00"
  }
}
```

### Dependency Injection

```csharp
// In Program.cs / Startup.cs
builder.Services.AddCryptographyServices();  // Includes sealed sender

// Services registered:
// - ISenderCertificateService
// - ISealedSenderService
// - SealedSenderFallbackService
```

## References

- [Signal Protocol Specifications](https://signal.org/docs/)
- [Sealed Sender Whitepaper](https://signal.org/blog/sealed-sender/)
- [ECDH Key Agreement](https://en.wikipedia.org/wiki/Elliptic-curve_Diffie%E2%80%93Hellman)
- [HKDF Key Derivation](https://tools.ietf.org/html/rfc5869)
- [AES-GCM AEAD](https://en.wikipedia.org/wiki/Galois/Counter_Mode)

## License

Part of Aegis Messenger - see main repository LICENSE.
