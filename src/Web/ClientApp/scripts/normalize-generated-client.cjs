const fs = require('node:fs');
const path = require('node:path');

const clientPath = path.resolve(__dirname, '../src/web-api-client.ts');
const source = fs.readFileSync(clientPath, 'utf8');
fs.writeFileSync(clientPath, source.replace(/[\t ]+$/gm, ''));
