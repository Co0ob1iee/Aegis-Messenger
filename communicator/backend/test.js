import fetch from 'node-fetch';
import jwt from 'jsonwebtoken';

const token = jwt.sign({ sub: 'userA' }, 'supersecret');
const challenge = Math.random().toString(36).substring(2);

fetch('http://localhost:3000/api/sgx-attestation', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({ challenge })
})
  .then(res => res.json())
  .then(data => console.log('SGX attestation response:', data));
