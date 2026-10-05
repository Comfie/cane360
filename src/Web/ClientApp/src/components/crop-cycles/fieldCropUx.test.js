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
const {FieldForm} = await import('../farm-setup/FieldForm.tsx');
const {FieldRecord} = await import('../farm-setup/FieldRecord.tsx');
const {CropPlanFields} = await import('./CropPlanFields.tsx');
const {CropCycleEditForm} = await import('./CropCycleEditForm.tsx');
const {CropCyclesClient, UpdateActualYieldRequest} = await import('../../web-api-client.ts');

const field = {id: 'field', code: 'A-01', name: 'North', reportingHectares: 5,
    declaredHectares: 5, reportingAreaSource: 'Declared', irrigationMethod: 'Furrow'};
/** @param {import('react').ComponentType<any>} component @param {any} props */
const render = (component, props) => renderToStaticMarkup(createElement(component, props));

test('Add Field renders only physical properties with required validation and accessible labels', () => {
    const html = render(FieldForm, {onSaved() {}, onError() {}});
    const names = [...html.matchAll(/name="([^"]+)"/g)].map(match => match[1]);
    assert.deepEqual(names, ['code', 'name', 'declaredHectares', 'mappedHectares', 'reportingAreaSource', 'irrigationMethod', 'soilNotes']);
    assert.match(html, /Field name<input[^>]+required/);
    assert.match(html, /Declared area.*min="0.01"/);
    assert.doesNotMatch(html, /name="(startDate|ratoonNumber|actualTonnes|expectedYieldTonnes)"/);
});

test('Field without a cycle renders an honest empty state', () => {
    const html = render(FieldRecord, {field});
    assert.match(html, /No crop cycle configured/);
    assert.doesNotMatch(html, /Expected yield|Harvest window/);
});

test('Field current cycle and draft remain distinct contexts', () => {
    const cycle = {id: 'active', variety: 'N14', status: 'Active', cycleType: 'PlantCane', expectedYieldTonnes: 50,
        startDate: '2026-01-31', expectedHarvestStart: '2027-03-31', expectedHarvestEnd: '2027-03-31'};
    const html = render(FieldRecord, {field: {...field, currentCropCycle: cycle}, draftCycle: {...cycle, id: 'draft', status: 'Draft'}});
    assert.match(html, /Current crop cycle for North/);
    assert.match(html, /Draft crop cycle for North/);
    assert.match(html, /50/);
});

test('Crop planning renders derived maturity with no manually entered age or actual yield', () => {
    const html = render(CropPlanFields, {fieldId: 'field'});
    assert.match(html, /Choose a planting date/);
    assert.match(html, /Use calculated maturity/);
    assert.doesNotMatch(html, /name="(cropAge|actualTonnes|expectedHarvestStart)"/);
});

test('Existing draft window renders editable overrides instead of replacing historical values', () => {
    const html = render(CropPlanFields, {fieldId: 'field', startDate: '2026-01-31',
        harvestStart: '2027-03-31', harvestEnd: '2027-04-30', expectedYield: 50});
    assert.match(html, /name="expectedHarvestStart"[^>]+value="2027-03-31"/);
    assert.match(html, /name="expectedHarvestEnd"[^>]+value="2027-04-30"/);
});

test('Manual yield edit is available only for harvested cycles and closed history stays read-only', () => {
    for (const status of ['Active', 'ReadyForHarvest', 'Closed', 'Cancelled']) {
        assert.equal(render(CropCycleEditForm, {details: {field, cropCycle: {status}}, onSaved() {}}), '');
    }
    assert.match(render(CropCycleEditForm, {details: {field, cropCycle: {status: 'Harvested'}}, onSaved() {}}), /Edit actual yield/);
    assert.match(render(CropCycleEditForm, {details: {field, cropCycle: {status: 'Draft'}}, onSaved() {}}), /Edit crop plan/);
});

test('Manual yield client sends only crop-cycle identity, version and manual tonnes to the correct endpoint', async () => {
    /** @type {{url: RequestInfo | URL, init?: RequestInit} | undefined} */
    let captured;
    const client = new CropCyclesClient('', {fetch: async (url, init) => {
        captured = {url, init};
        return new Response(JSON.stringify({field, cropCycle: {id: 'cycle', harvestResult: {actualTonnes: 55.125}}}),
            {status: 200, headers: {'Content-Type': 'application/json'}});
    }});
    const result = await client.updateCropCycleActualYield('field', 'cycle', new UpdateActualYieldRequest({expectedVersion: 3, actualTonnes: 55.125}));
    assert.ok(captured);
    assert.ok(captured.init);
    assert.equal(captured.url, '/api/fields/field/crop-cycles/cycle/actual-yield');
    assert.equal(captured.init.method, 'PUT');
    assert.deepEqual(JSON.parse(String(captured.init.body)), {expectedVersion: 3, actualTonnes: 55.125});
    assert.ok(result.cropCycle.harvestResult);
    assert.equal(result.cropCycle.harvestResult.actualTonnes, 55.125);
});


test('Maturity preview sends the selected calendar date without a UTC timezone shift', async () => {
    let requestedUrl = '';
    const client = new CropCyclesClient('', {fetch: async url => {
        requestedUrl = String(url);
        return new Response(JSON.stringify({defaultCropMaturityMonths: 14, expectedMaturityDate: '2027-03-31'}),
            {status: 200, headers: {'Content-Type': 'application/json'}});
    }});
    const result = await client.calculateCropMaturity('field', new Date(2026, 0, 31));
    assert.equal(new URL(requestedUrl, 'https://example.invalid').searchParams.get('plantingDate'), '2026-01-31');
    assert.equal(result.expectedMaturityDate, '2027-03-31');
});
