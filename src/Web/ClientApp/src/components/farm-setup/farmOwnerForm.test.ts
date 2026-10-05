import test from 'node:test';
import assert from 'node:assert/strict';
import {farmOwnerInput} from './farmOwnerForm.ts';
import {FarmOwnerProfileInput, UpdateFarmInformationRequest, GrowerDto, FarmDto} from '../../web-api-client.ts';

test('owner form trims grouped profile data and includes selected status', () => {
    const data = new FormData();
    data.set('firstName', ' Tariro '); data.set('growerNumber', ' G-1 ');
    data.set('nationalId', '63-123456-A-12'); data.set('ownerStatus', 'Inactive');
    const input = farmOwnerInput(data);
    assert.equal(input.firstName, 'Tariro'); assert.equal(input.growerNumber, 'G-1');
    assert.equal(input.active, false); assert.equal(input.nationalId, '63-123456-A-12');
});

test('blank ID is omitted to preserve existing encrypted identity', () => {
    const input = farmOwnerInput(new FormData());
    assert.equal(input.nationalId, undefined); assert.equal(input.active, true);
    assert.equal(JSON.stringify(input).includes('nationalId'), false);
});

test('generated enhanced update contract serializes nested profile and explicit category clear', () => {
    const request = new UpdateFarmInformationRequest({growerDisplayName: 'Owner', growerPhone: undefined, farmCode: 'TEST', farmName: 'Synthetic', address: 'Synthetic', location: 'Synthetic', tenure: 'Owned', declaredHectares: 10, irrigationContext: 'Synthetic', ownerProfile: new FarmOwnerProfileInput({
        association: 'Synthetic', membershipNumber: 'M1', photoReference: 'asset:photo', active: true}), updateFarmModel: true});
    const json = JSON.parse(JSON.stringify(request));
    assert.equal(json.ownerProfile.membershipNumber, 'M1'); assert.equal(json.updateFarmModel, true);
    assert.equal(json.farmModelId, undefined);
});

test('legacy response remains readable with optional new fields', () => {
    const owner = GrowerDto.fromJS({displayName: 'Legacy', phone: '123'});
    assert.equal(owner.displayName, 'Legacy'); assert.equal(owner.firstName, undefined);
    assert.equal(FarmDto.fromJS({id: 'synthetic', fields: []}).farmModelId, undefined);
});
