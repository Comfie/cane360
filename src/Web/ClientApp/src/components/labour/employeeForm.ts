import {WorkerProfileInput} from '../../web-api-client';
import {dateOnly} from './labourView';

export function employeeProfileInput(data: FormData): WorkerProfileInput {
    const text = (name: string) => String(data.get(name) ?? '').trim() || undefined;
    return new WorkerProfileInput({employeeNumber: text('employeeNumber'), title: text('title'),
        firstName: text('firstName'), surname: text('surname'), sex: text('sex'),
        dateOfBirth: text('dateOfBirth') ? new Date(`${dateOnly(text('dateOfBirth')!)}T00:00:00`) : undefined,
        address: text('address'), photoReference: text('photoReference'), nextOfKinName: text('nextOfKinName'),
        nextOfKinRelationship: text('nextOfKinRelationship'), nextOfKinPhone: text('nextOfKinPhone'),
        nextOfKinAddress: text('nextOfKinAddress')});
}
