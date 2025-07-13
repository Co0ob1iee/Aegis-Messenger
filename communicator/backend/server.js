import express from 'express';
import bodyParser from 'body-parser';
import jwt from 'jsonwebtoken';
import { WebSocketServer } from 'ws';
import { randomUUID } from 'crypto';
import swaggerUi from 'swagger-ui-express';
import fs from 'fs';

const app = express();
app.use(bodyParser.json());
const PORT = 3000;
const JWT_SECRET = 'supersecret';

// In-memory stores
const keysStore = {};
const messagesStore = [];
const wsClients = new Map();

// In-memory store for groups
const groupsStore = {};

// In-memory store for files
const filesStore = {};

// In-memory audit log
const auditLog = {};

// JWT middleware
function authenticate(req, res, next) {
  const auth = req.headers['authorization'];
  if (!auth) return res.status(401).json({ error: 'No token' });
  const token = auth.split(' ')[1];
  try {
    req.user = jwt.verify(token, JWT_SECRET);
    next();
  } catch {
    res.status(401).json({ error: 'Invalid token' });
  }
}

function logAudit(action, user, details) {
  auditLog.push({ timestamp: Date.now(), action, user, details });
}

// Middleware do autoryzacji admina dla Swagger UI
function adminOnly(req, res, next) {
  const auth = req.headers['authorization'];
  if (!auth) return res.status(401).send('No token');
  const token = auth.split(' ')[1];
  try {
    const user = jwt.verify(token, JWT_SECRET);
    if (user.sub !== 'admin') return res.status(403).send('Forbidden');
    next();
  } catch {
    res.status(401).send('Invalid token');
  }
}

// POST /keys - publikacja pre-kluczy
app.post('/api/keys', authenticate, (req, res) => {
  const { userId, identityKey, preKeys, signedPreKey } = req.body;
  if (!userId || !identityKey || !preKeys || !signedPreKey) return res.status(400).json({ error: 'Missing fields' });
  keysStore[userId] = { identityKey, preKeys, signedPreKey };
  logAudit('publishPreKeys', req.user.sub, { userId, identityKey });
  res.json({ success: true });
});

// GET /keys/:userId - pobieranie pre-kluczy
app.get('/api/keys/:userId', authenticate, (req, res) => {
  const userId = req.params.userId;
  const bundle = keysStore[userId];
  if (!bundle) return res.status(404).json({ error: 'Not found' });
  logAudit('getPreKeys', req.user.sub, { userId });
  res.json({ success: true, bundle });
});

// POST /messages - wysyłanie zaszyfrowanej wiadomości
app.post('/api/messages', authenticate, (req, res) => {
  const { receiverId, sealedPayload } = req.body;
  if (!receiverId || !sealedPayload) return res.status(400).json({ error: 'Missing fields' });
  messagesStore.push({ receiverId, sealedPayload });
  logAudit('sendMessage', req.user.sub, { receiverId });
  // Push to WebSocket if receiver online
  if (wsClients.has(receiverId)) {
    wsClients.get(receiverId).send(JSON.stringify({ sealedPayload }));
  }
  res.json({ success: true });
});

// POST /sgx-attestation - zdalna atestacja SGX (mock)
app.post('/api/sgx-attestation', authenticate, (req, res) => {
  const { challenge } = req.body;
  if (!challenge) return res.status(400).json({ error: 'Missing challenge' });
  // Mock: zawsze OK
  res.json({ status: 'SGX_OK', challenge });
});

// Tworzenie grupy
app.post('/api/groups', authenticate, (req, res) => {
  const { groupId, members } = req.body;
  if (!groupId || !Array.isArray(members) || members.length === 0) return res.status(400).json({ error: 'Missing fields' });
  if (groupsStore[groupId]) return res.status(409).json({ error: 'Group already exists' });
  groupsStore[groupId] = { members, messages: [] };
  res.json({ success: true, group: groupsStore[groupId] });
});

// Pobieranie grupy
app.get('/api/groups/:groupId', authenticate, (req, res) => {
  const group = groupsStore[req.params.groupId];
  if (!group) return res.status(404).json({ error: 'Group not found' });
  res.json({ success: true, group });
});

// Dodawanie użytkownika do grupy
app.post('/api/groups/:groupId/add', authenticate, (req, res) => {
  const group = groupsStore[req.params.groupId];
  const { userId } = req.body;
  if (!group || !userId) return res.status(400).json({ error: 'Missing fields' });
  if (group.members.includes(userId)) return res.status(409).json({ error: 'User already in group' });
  group.members.push(userId);
  res.json({ success: true, group });
});

// Usuwanie użytkownika z grupy
app.post('/api/groups/:groupId/remove', authenticate, (req, res) => {
  const group = groupsStore[req.params.groupId];
  const { userId } = req.body;
  if (!group || !userId) return res.status(400).json({ error: 'Missing fields' });
  group.members = group.members.filter(id => id !== userId);
  res.json({ success: true, group });
});

// Pobieranie wiadomości z grupy
app.get('/api/groups/:groupId/messages', authenticate, (req, res) => {
  const group = groupsStore[req.params.groupId];
  if (!group) return res.status(404).json({ error: 'Group not found' });
  res.json({ success: true, messages: group.messages });
});

// Dodawanie wiadomości do grupy
app.post('/api/groups/:groupId/messages', authenticate, (req, res) => {
  const group = groupsStore[req.params.groupId];
  const { senderId, sealedPayload } = req.body;
  if (!group || !senderId || !sealedPayload) return res.status(400).json({ error: 'Missing fields' });
  group.messages.push({ senderId, sealedPayload });
  res.json({ success: true });
});

// Upload pliku
app.post('/api/files', authenticate, (req, res) => {
  const { filename, filedata } = req.body;
  if (!filename || !filedata) return res.status(400).json({ error: 'Missing fields' });
  const fileId = randomUUID();
  filesStore[fileId] = { filename, filedata, owner: req.user.sub };
  logAudit('uploadFile', req.user.sub, { filename });
  res.json({ success: true, fileId });
});

// Download pliku
app.get('/api/files/:fileId', authenticate, (req, res) => {
  const file = filesStore[req.params.fileId];
  if (!file) return res.status(404).json({ error: 'File not found' });
  // Tylko właściciel może pobrać plik (do demo)
  if (file.owner !== req.user.sub) return res.status(403).json({ error: 'Forbidden' });
  logAudit('downloadFile', req.user.sub, { fileId });
  res.json({ success: true, filename: file.filename, filedata: file.filedata });
});

// Endpoint do pobierania logów audytowych
app.get('/api/audit', authenticate, (req, res) => {
  // Tylko admin (np. sub === 'admin')
  if (req.user.sub !== 'admin') return res.status(403).json({ error: 'Forbidden' });
  res.json({ success: true, auditLog });
});

// Swagger UI z zabezpieczeniem
const swaggerDocument = JSON.parse(fs.readFileSync('./swagger.json', 'utf8'));
app.use('/api-docs', adminOnly, swaggerUi.serve, swaggerUi.setup(swaggerDocument));

// WebSocket serwer
const wss = new WebSocketServer({ port: 3001 });
wss.on('connection', (ws, req) => {
  // JWT w query string
  const params = new URLSearchParams(req.url.replace('/?', ''));
  const token = params.get('token');
  let userId = null;
  try {
    const payload = jwt.verify(token, JWT_SECRET);
    userId = payload.sub;
    wsClients.set(userId, ws);
    ws.on('close', () => wsClients.delete(userId));
  } catch {
    ws.close();
  }
});

app.listen(PORT, () => console.log(`REST API listening on port ${PORT}`));
console.log('WebSocket listening on port 3001');
