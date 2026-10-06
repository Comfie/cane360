import {useState, type FormEvent} from 'react';
import {ArchiveWorkerRequest, CorrectWorkerNationalIdRequest, RevealNationalIdRequest,
    UpdateWorkerProfileRequest, type WorkerDetailsDto} from '../../web-api-client';
import {workersClient} from './labourApi';
import {employeeProfileInput} from './employeeForm';
import {employmentTypes, harareToday, label} from './labourView';
import {DatePicker} from '../DatePicker';
import {getApiError} from '../apiError';

export function EmployeeFields({details}: {details: WorkerDetailsDto}) {
    const p = details.profile;
    const w = details.worker;
    return <>
        <fieldset className="form-grid"><legend>Identity</legend>
            <label>Employee number<input name="employeeNumber" maxLength={60} defaultValue={p?.employeeNumber}/></label>
            <label>Title<input name="title" maxLength={40} defaultValue={p?.title}/></label>
            <label>First name<input name="firstName" maxLength={60} autoComplete="given-name" defaultValue={p?.firstName}/></label>
            <label>Surname<input name="surname" maxLength={59} autoComplete="family-name" defaultValue={p?.surname}/></label>
            <label>Display name<input name="displayName" maxLength={120} required defaultValue={w.displayName}/>
                <small>First name and surname, when supplied together, update this name.</small></label>
            <label>Sex<select name="sex" defaultValue={p?.sex ?? ''}><option value="">Not recorded</option>
                {['Female', 'Male', 'Other', 'Prefer not to say'].map(value => <option key={value}>{value}</option>)}
            </select></label>
            <label>Date of birth<DatePicker name="dateOfBirth" max={harareToday()} defaultValue={p?.dateOfBirth ? isoDate(p.dateOfBirth) : undefined}/></label>
            <label>National ID<input value={w.nationalIdMask} readOnly aria-label="Masked National ID"/></label>
        </fieldset>
        <fieldset className="form-grid"><legend>Employment</legend>
            <label>Employee type<select name="employmentType" defaultValue={w.employmentType}>
                {employmentTypes.map(value => <option key={value} value={value}>{label(value)}</option>)}
            </select></label>
            <label>Employment date<input readOnly value={isoDate(w.activeFrom)}/>
                <small>Retained to protect historical attendance and eligibility.</small></label>
            <label>Status<input readOnly value={w.status}/></label>
        </fieldset>
        <fieldset className="form-grid"><legend>Contact</legend>
            <label>Primary contact number<input name="phone" type="tel" maxLength={30} autoComplete="tel" defaultValue={w.phone}/></label>
            <label className="is-wide">Residential / contact address<textarea name="address" maxLength={240} rows={2} defaultValue={p?.address}/></label>
        </fieldset>
        <fieldset className="form-grid"><legend>Next of kin</legend>
            <label>Full name<input name="nextOfKinName" maxLength={120} defaultValue={p?.nextOfKinName}/></label>
            <label>Relationship<input name="nextOfKinRelationship" maxLength={60} defaultValue={p?.nextOfKinRelationship}/></label>
            <label>Contact number<input name="nextOfKinPhone" type="tel" maxLength={30} defaultValue={p?.nextOfKinPhone}/></label>
            <label className="is-wide">Address<textarea name="nextOfKinAddress" maxLength={240} rows={2} defaultValue={p?.nextOfKinAddress}/></label>
        </fieldset>
        <fieldset className="form-grid"><legend>Profile photograph</legend>
            <label className="is-wide">Photograph reference<input name="photoReference" maxLength={240} defaultValue={p?.photoReference}/>
                <small>Reference to an existing photograph. Uploads are not available yet.</small></label>
        </fieldset>
    </>;
}

export function EmployeeProfile({details, canReveal, onChanged, onError}: {
    details: WorkerDetailsDto; canReveal: boolean; onChanged: (details: WorkerDetailsDto) => void;
    onError: (message: string) => void;
}) {
    const [editing, setEditing] = useState(false);
    const [busy, setBusy] = useState(false);
    const [revealed, setRevealed] = useState('');
    const [revealing, setRevealing] = useState(false);
    const [correcting, setCorrecting] = useState(false);
    const [archiving, setArchiving] = useState(false);
    const w = details.worker;
    const p = details.profile;
    const perform = async (action: () => Promise<void>) => {
        if (busy) return;
        setBusy(true); onError('');
        try {await action();} catch (error) {onError(getApiError(error));} finally {setBusy(false);}
    };
    const save = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        void perform(async () => {
            onChanged(await workersClient.updateWorkerProfile(w.id, new UpdateWorkerProfileRequest({
                expectedVersion: w.version, expectedPersonVersion: details.personVersion ?? 0,
                displayName: String(data.get('displayName')).trim(), phone: String(data.get('phone')).trim() || undefined,
                employmentType: String(data.get('employmentType')), profile: employeeProfileInput(data)})));
            setEditing(false);
        });
    };
    return <section className="employee-profile">
        <header className="section-heading"><h3>Employee profile</h3>
            <button type="button" className="secondary-action" disabled={busy} onClick={() => setEditing(!editing)}>
                {editing ? 'Cancel edit' : 'Edit Employee'}</button></header>
        {editing ? <form className="labour-form" onSubmit={save}><EmployeeFields key={`${w.version}-${details.personVersion}`} details={details}/>
            <footer className="form-actions"><button disabled={busy}>{busy ? 'Saving…' : 'Save Employee'}</button></footer></form> :
            <dl className="record-details">
                {Object.entries({'Employee number': p?.employeeNumber, Title: p?.title, 'First name': p?.firstName,
                    Surname: p?.surname, Sex: p?.sex, 'Date of birth': p?.dateOfBirth ? isoDate(p.dateOfBirth) : undefined, 'Contact number': w.phone,
                    Address: p?.address, 'Next of kin': p?.nextOfKinName, Relationship: p?.nextOfKinRelationship,
                    'Next of kin contact': p?.nextOfKinPhone, 'Next of kin address': p?.nextOfKinAddress,
                    'Photograph reference': p?.photoReference}).map(([name, value]) =>
                    <div key={name}><dt>{name}</dt><dd>{value || 'Not recorded'}</dd></div>)}
                <div><dt>National ID</dt><dd>{revealed || w.nationalIdMask}</dd></div>
            </dl>}
        {canReveal && <div className="form-actions">
            <button type="button" disabled={busy} onClick={() => {
                if (revealed) setRevealed(''); else setRevealing(!revealing);
            }}>{revealed ? 'Hide National ID' : 'Reveal National ID'}</button>
            <button type="button" disabled={busy} onClick={() => setCorrecting(!correcting)}>Correct National ID</button>
        </div>}
        {canReveal && revealing && <form className="labour-form" onSubmit={event => {
            event.preventDefault(); const data = new FormData(event.currentTarget);
            void perform(async () => {setRevealed((await workersClient.revealWorkerNationalId(w.id,
                new RevealNationalIdRequest({reason: String(data.get('reason'))}))).nationalId); setRevealing(false);});
        }}><label>Reason for reveal<input name="reason" required maxLength={500}/></label>
            <button disabled={busy}>Reveal protected ID</button></form>}
        {canReveal && correcting && <form className="labour-form" onSubmit={event => {
            event.preventDefault(); const data = new FormData(event.currentTarget);
            void perform(async () => {onChanged(await workersClient.correctWorkerNationalId(w.id,
                new CorrectWorkerNationalIdRequest({expectedVersion: w.version,
                    nationalId: String(data.get('nationalId')), reason: String(data.get('reason'))})));
                setRevealed(''); setCorrecting(false);});
        }}><label>Corrected National ID<input name="nationalId" type="password" autoComplete="off" required maxLength={80}/></label>
            <label>Reason for correction<input name="reason" required maxLength={500}/></label>
            <button disabled={busy}>Save protected ID</button></form>}
        {w.status === 'Active' && <button type="button" className="secondary-action" disabled={busy}
            onClick={() => setArchiving(!archiving)}>{archiving ? 'Cancel archive' : 'Archive Employee'}</button>}
        {archiving && <form className="labour-form" onSubmit={event => {
            event.preventDefault(); const data = new FormData(event.currentTarget);
            void perform(async () => {onChanged(await workersClient.archiveWorkers(w.id,
                new ArchiveWorkerRequest({activeTo: String(data.get('activeTo')), expectedVersion: w.version})));
                setArchiving(false);});
        }}><p>Archiving ends active employment and retains historical evidence. This action cannot be reversed.</p>
            <label>Employment end date<DatePicker name="activeTo" min={isoDate(w.activeFrom)} required/></label>
            <button disabled={busy}>Confirm archive</button></form>}
    </section>;
}

function isoDate(value: Date | string): string {
    return value instanceof Date ? value.toISOString().slice(0, 10) : String(value).slice(0, 10);
}
