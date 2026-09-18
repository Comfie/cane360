const fs = require('node:fs');
const path = require('node:path');

const clientPath = process.argv[2] ?? path.resolve(__dirname, '../src/web-api-client.ts');
const allowlistPath = path.resolve(__dirname, 'client-method-digit-allowlist.json');
const allowlist = new Set(JSON.parse(fs.readFileSync(allowlistPath, 'utf8')));
const source = fs.readFileSync(clientPath, 'utf8');
let client = '';
const violations = [];

for (const line of source.split(/\r?\n/)) {
    const declaration = /^export class (\w+Client)\b/.exec(line);
    if (declaration) client = declaration[1];

    const method = /^    ([A-Za-z_]\w*)\([^\n]*\): Promise</.exec(line);
    if (client && method && /\d$/.test(method[1]) && !allowlist.has(`${client}.${method[1]}`)) {
        violations.push(`${client}.${method[1]}`);
    }
}

if (violations.length) {
    console.error('Generated API methods ending in a digit need explicit endpoint names:');
    for (const violation of violations) console.error(`  ${violation}`);
    process.exitCode = 1;
}
