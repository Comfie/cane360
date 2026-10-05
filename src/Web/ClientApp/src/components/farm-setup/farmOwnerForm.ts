import {FarmOwnerProfileInput} from '../../web-api-client.ts';

export function farmOwnerInput(data: FormData): FarmOwnerProfileInput {
    const value = (name: string) => String(data.get(name) ?? '').trim() || undefined;
    return new FarmOwnerProfileInput({
        title: value('title'), firstName: value('firstName'), surname: value('surname'), sex: value('sex'),
        growerNumber: value('growerNumber'), association: value('association'), membershipNumber: value('membershipNumber'),
        registeredAddress: value('registeredAddress'), email: value('email'), photoReference: value('photoReference'),
        active: data.get('ownerStatus') !== 'Inactive', nationalId: value('nationalId'),
    });
}
