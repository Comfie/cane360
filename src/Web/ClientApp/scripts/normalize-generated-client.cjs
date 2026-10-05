const fs = require('node:fs');
const path = require('node:path');

const clientPath = path.resolve(__dirname, '../src/web-api-client.ts');
let source = fs.readFileSync(clientPath, 'utf8').replace(/[\t ]+$/gm, '');
// These previously generated methods retain their original NSwag formatting.
const retainedMethods = [...source.matchAll(/    protected process(?:ListFarmModels|SaveFarmModel|RevealFarmOwnerNationalId)\(response: Response\)[\s\S]*?(?=\n    \/\*\*|\n})/g)].map(match => match[0]);
// Keep the checked-in client formatting stable across NSwag regeneration.
source = source.replace('/* eslint-disable */\n// ReSharper', '/* eslint-disable */\n\n// ReSharper');
source = source.replace(/^( +)let _headers: any = {}; if \(response.headers && response.headers.forEach\) { response.headers.forEach\(\(v: any, k: any\) => _headers\[k\] = v\); };$/gm,
    (_, indent) => `${indent}let _headers: any = {};\n${indent}if (response.headers && response.headers.forEach) {\n${indent}    response.headers.forEach((v: any, k: any) => _headers[k] = v);\n${indent}}`);
source = source.replace(/(return response.text\(\).then\(\(_responseText\) => {\n)([\s\S]*?)(^ +}\);)/gm,
    (_, start, body, end) => {
        const closingIndent = end.match(/^ +/)[0].length;
        const bodyIndent = body.match(/^ +(?=\S)/m)?.[0].length ?? 0;
        return start + (bodyIndent <= closingIndent
            ? body.split('\n').map(line => line ? `    ${line}` : line).join('\n') : body) + end;
    });
source = source.replace(/^( +)}\n\1else {/gm, '$1} else {');
source = source.replace(/\(d\.getMonth\(\)\+1\)/g, '(d.getMonth() + 1)');
source = source.replace('headers: { [key: string]: any; }, result?: any): any {',
    'headers: {\n    [key: string]: any;\n}, result?: any): any {');
source = source.replace(/headers: {\n +}/g, 'headers: {}');
// NSwag serializes DateOnly query values as UTC timestamps; preserve the selected calendar date.
source = source.replace('plantingDate ? "" + plantingDate.toISOString() : ""',
    'plantingDate ? formatDate(plantingDate) : ""');
source = source.replace(/^( +)(result\d+ = resultData\d+ !== undefined[^\n]+)$/gm,
    (_, indent, line) => `${indent.length > 16 ? ' '.repeat(16) : indent}${line}`);
for (const method of retainedMethods) {
    const name = method.match(/protected (\w+)\(/)[1];
    const pattern = new RegExp(`    protected ${name}\\(response: Response\\)[\\s\\S]*?(?=\\n    /\\*\\*|\\n})`);
    source = source.replace(pattern, () => method);
}
fs.writeFileSync(clientPath, source);
