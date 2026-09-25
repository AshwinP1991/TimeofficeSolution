const crypto = require('crypto');
const express = require('express');

const app = express();
app.use(express.json());

const DefaultKey = 'YOUR_SECRET_KEY_HERE';

function getKeyBytes(passphrase) {
  const bytes = Buffer.from(passphrase, 'utf8');
  if (bytes.length === 16 || bytes.length === 24 || bytes.length === 32) {
    return bytes;
  }
  return crypto.createHash('sha256').update(bytes).digest();
}

function getAlgorithm(key) {
  return `aes-${key.length * 8}-cbc`;
}

function encrypt(plainText, key) {
  const iv = Buffer.alloc(16, 0);
  const cipher = crypto.createCipheriv(getAlgorithm(key), key, iv);
  const encrypted = Buffer.concat([cipher.update(plainText, 'utf8'), cipher.final()]);
  return encrypted.toString('base64');
}

function decrypt(cipherText, key) {
  const iv = Buffer.alloc(16, 0);
  const buffer = Buffer.from(cipherText, 'base64');
  const decipher = crypto.createDecipheriv(getAlgorithm(key), key, iv);
  const decrypted = Buffer.concat([decipher.update(buffer), decipher.final()]);
  return decrypted.toString('utf8');
}

function pad2(n) {
  return String(n).padStart(2, '0');
}

function formatDate(d) {
  return (
    `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())} ` +
    `${pad2(d.getHours())}:${pad2(d.getMinutes())}:${pad2(d.getSeconds())}`
  );
}

function parseDate(str) {
  // Accepts "yyyy-MM-dd HH:mm:ss" or ISO strings (matches DateTime.Parse usage)
  const normalized = str.includes('T') ? str : str.replace(' ', 'T');
  const d = new Date(normalized);
  if (isNaN(d.getTime())) {
    throw new Error(`The value '${str}' is not a valid date.`);
  }
  return d;
}

app.post('/api/encrypt', (req, res) => {
  const { text, key } = req.body || {};
  if (text === undefined || text === null) {
    return res.status(400).json({ error: 'text is required' });
  }
  try {
    const keyBytes = getKeyBytes(key ?? DefaultKey);
    const encrypted = encrypt(String(text), keyBytes);
    return res.json({ encrypted });
  } catch (ex) {
    return res.status(400).json({ error: ex.message });
  }
});

app.post('/api/decrypt', (req, res) => {
  const { text, key } = req.body || {};
  if (text === undefined || text === null) {
    return res.status(400).json({ error: 'text is required' });
  }
  try {
    const keyBytes = getKeyBytes(key ?? DefaultKey);
    const decrypted = decrypt(String(text), keyBytes);
    return res.json({ decrypted });
  } catch (ex) {
    return res.status(400).json({ error: ex.message });
  }
});

app.post('/api/license/generate', (req, res) => {
  const { expiry, key } = req.body || {};
  if (!expiry) {
    return res.status(400).json({ error: 'expiry is required' });
  }
  try {
    const keyBytes = getKeyBytes(key ?? DefaultKey);
    const date = parseDate(String(expiry));
    const license = encrypt(formatDate(date), keyBytes);
    return res.json({ license, expiry: date.toISOString() });
  } catch (ex) {
    return res.status(400).json({ error: ex.message });
  }
});

app.post('/api/license/decrypt', (req, res) => {
  const { license, key } = req.body || {};
  if (!license) {
    return res.status(400).json({ error: 'license is required' });
  }
  try {
    const keyBytes = getKeyBytes(key ?? DefaultKey);
    const plain = decrypt(String(license), keyBytes);
    // Parse exact "yyyy-MM-dd HH:mm:ss" (invariant/local like DateTime.ParseExact + DateTime.Now)
    const expiry = parseDate(plain);
    const remaining = Math.trunc((expiry.getTime() - Date.now()) / 86400000);
    return res.json({
      expiry: formatDate(expiry),
      valid: remaining >= 0,
      remainingDays: remaining,
      status: remaining >= 0 ? 'VALID' : 'EXPIRED'
    });
  } catch (ex) {
    return res.status(400).json({ error: ex.message });
  }
});

app.get('/health', (req, res) => {
  res.json({ status: 'healthy', time: new Date().toISOString() });
});

const port = process.env.PORT || 8080;
app.listen(port, () => {
  console.log(`LicenseGen listening on http://localhost:${port}`);
});
