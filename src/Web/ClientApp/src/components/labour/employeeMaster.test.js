import assert from 'node:assert/strict';
import {readFileSync, existsSync} from 'node:fs';
import {registerHooks} from 'node:module';
import {fileURLToPath} from 'node:url';
import test from 'node:test';
import ts from 'typescript';
import {createElement} from 'react';
import {renderToStaticMarkup} from 'react-dom/server';

// Render production TSX with the pinned TypeScript compiler, without adding a test dependency.
registerHooks({
    resolve(specifier, context, nextResolve) {
        if (specifier.startsWith('.') && !/\.(tsx?|js)$/.test(specifier)) {
            for (const extension of ['.tsx', '.ts']) {
                const url = new URL(specifier + extension, context.parentURL);
                if (existsSync(fileURLToPath(url))) return {url: url.href, shortCircuit: true};
            }
        }
        return nextResolve(specifier, context);
    },
    load(url, context, nextLoad) {
        if (/\.tsx?$/.test(url)) return {format: 'module', shortCircuit: true,
            source: ts.transpileModule(readFileSync(fileURLToPath(url), 'utf8'), {compilerOptions: {
                jsx: ts.JsxEmit.ReactJSX, module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022,
            }}).outputText};
        return nextLoad(url, context);
    },
});
Object.defineProperty(globalThis, 'window', {value: {fetch: () => {throw new Error('Rendering must not issue API requests.');}}});
const {WorkerForm, WorkerRegister} = await import('../pages/LabourPage.tsx');
const {EmployeeFields, EmployeeProfile} = await import('./EmployeeProfile.tsx');
const {employeeProfileInput} = await import('./employeeForm.ts');
const {WorkersClient, WorkerDetailsDto, UpdateWorkerProfileRequest, WorkerProfileInput} = await import('../../web-api-client.ts');
const worker = {id: 'worker', personId: 'person', displayName: 'Legacy employee', employmentType: 'Casual',
    activeFrom: '2026-01-01', status: 'Active', nationalIdMask: '••••••12', version: 0};
const details = {worker, rates: [], personVersion: 0, profile: {}};
/** @param {import('react').ComponentType<any>} component @param {any} props */
const render = (component, props) => renderToStaticMarkup(createElement(component, props));

test('Create Employee stays compact with five core fields and no optional enrichment', () => {
    const html = render(WorkerForm, {onSaved() {}, onError() {}});
    assert.deepEqual([...html.matchAll(/name="([^"]+)"/g)].map(match => match[1]),
        ['firstName', 'surname', 'employmentType', 'activeFrom', 'nationalId']);
    assert.match(html, /First name<input[^>]+required/);
    assert.match(html, /Surname<input[^>]+required/);
    assert.match(html, /type="password"/);
    assert.match(html, /Create Employee/);
});

test('Employee list identifies number and masked ID without private profile fields', () => {
    const html = render(WorkerRegister, {workers: [{...worker, employeeNumber: 'EMP-1'}], onOpen() {}});
    assert.match(html, /Employee register/); assert.match(html, /EMP-1/); assert.match(html, /••••••12/);
    assert.doesNotMatch(html, /Date of birth|Next of kin/);
    assert.match(render(WorkerRegister, {workers: [], onOpen() {}}), /No employees/);
});

test('Full Edit groups extended fields and legacy identity without requiring new personal data', () => {
    const html = render(EmployeeFields, {details});
    for (const group of ['Identity', 'Employment', 'Contact', 'Next of kin', 'Profile photograph'])
        assert.ok(html.includes(`<legend>${group}</legend>`));
    for (const name of ['employeeNumber', 'title', 'firstName', 'surname', 'sex', 'dateOfBirth', 'address',
        'photoReference', 'nextOfKinName', 'nextOfKinRelationship', 'nextOfKinPhone', 'nextOfKinAddress'])
        assert.ok(html.includes(`name="${name}"`));
    assert.doesNotMatch(html, /name="firstName"[^>]+required/);
    assert.match(html, /Masked National ID/); assert.doesNotMatch(html, /type="file"/);
});

test('Profile shows enrichment and sensitive controls only for existing authorized role', () => {
    const props = {details: {...details, profile: {nextOfKinName: 'Kin', photoReference: 'asset:photo'}}, onChanged() {}, onError() {}};
    const manager = render(EmployeeProfile, {...props, canReveal: false});
    assert.match(manager, /Kin/); assert.match(manager, /asset:photo/); assert.match(manager, /••••••12/);
    assert.match(manager, /Edit Employee/); assert.match(manager, /Archive Employee/);
    assert.doesNotMatch(manager, /Reveal National ID|Correct National ID/);
    assert.match(render(EmployeeProfile, {...props, canReveal: true}), /Reveal National ID/);
});

test('Profile form trims metadata and clears blanks without creating plaintext national ID', () => {
    const data = new FormData(); data.set('employeeNumber', ' EMP-1 '); data.set('nextOfKinName', ' Kin ');
    data.set('dateOfBirth', '1990-01-02'); data.set('photoReference', ' asset:photo '); data.set('nationalId', 'must-not-copy');
    const profile = employeeProfileInput(data);
    assert.equal(profile.employeeNumber, 'EMP-1'); assert.equal(profile.nextOfKinName, 'Kin');
    assert.equal(JSON.parse(JSON.stringify(profile)).dateOfBirth, '1990-01-02'); assert.equal(profile.photoReference, 'asset:photo');
    assert.equal(profile.address, undefined); assert.equal(JSON.stringify(profile).includes('must-not-copy'), false);
});

test('Legacy generated detail remains readable with no added fields', () => {
    const result = WorkerDetailsDto.fromJS({worker, rates: []});
    assert.equal(result.worker.id, 'worker'); assert.equal(result.worker.displayName, 'Legacy employee');
    assert.equal(result.profile, undefined); assert.equal(result.worker.employeeNumber, undefined);
});

test('Generated edit API preserves worker route identity, calendar DOB and both versions', async () => {
    let captured = {url: '', options: /** @type {RequestInit} */ ({})};
    const client = new WorkersClient('', {fetch: async (url, options) => {
        captured = {url: String(url), options: options ?? {}}; return new Response(JSON.stringify(details), {status: 200});
    }});
    await client.updateWorkerProfile('worker', new UpdateWorkerProfileRequest({expectedVersion: 2,
        expectedPersonVersion: 3, displayName: 'Name', phone: undefined, employmentType: 'Casual',
        profile: new WorkerProfileInput({dateOfBirth: new Date('1990-01-02T00:00:00'), nextOfKinName: 'Kin', photoReference: 'asset:photo'})}));
    assert.equal(captured.url, '/api/workers/worker/profile'); assert.equal(captured.options.method, 'PUT');
    const body = JSON.parse(String(captured.options.body));
    assert.equal(body.expectedVersion, 2); assert.equal(body.expectedPersonVersion, 3);
    assert.equal(body.profile.dateOfBirth, '1990-01-02'); assert.equal(body.profile.nextOfKinName, 'Kin');
    assert.equal(body.profile.photoReference, 'asset:photo');
});
