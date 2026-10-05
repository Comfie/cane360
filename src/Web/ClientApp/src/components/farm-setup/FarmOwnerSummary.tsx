import {useState} from 'react';
import type {GrowerDto} from '../../web-api-client';
import {useAuth} from '../api-authorization/AuthContext';
import {farmSetupClient, getApiError} from './farmSetupApi';

export function FarmOwnerSummary({owner}: {owner?: GrowerDto}) {
    const canReveal = useAuth().session.role === 'Grower';
    const [revealed, setRevealed] = useState('');
    const [error, setError] = useState('');
    const [busy, setBusy] = useState(false);
    if (!owner) return null;
    const reveal = async () => {
        if (busy) return;
        setBusy(true); setError('');
        try {setRevealed((await farmSetupClient.revealFarmOwnerNationalId()).nationalId);}
        catch (error) {setError(getApiError(error));} finally {setBusy(false);}
    };
    return <section className="record-panel farm-summary farm-owner-summary"><header><h2>Farm Owner profile</h2>
        <span>{owner.active ? 'Active' : 'Inactive'}</span></header>
        <dl className="record-details">
            {Object.entries({Title: owner.title, 'First name': owner.firstName, Surname: owner.surname, Sex: owner.sex,
                'Grower number / ID': owner.growerNumber, Association: owner.association, 'Membership number': owner.membershipNumber,
                'Registered address': owner.registeredAddress, 'Contact number': owner.phone, Email: owner.email,
                'Photograph reference': owner.photoReference}).map(([label, value]) =>
                <div key={label}><dt>{label}</dt><dd>{value || 'Not recorded'}</dd></div>)}
            <div><dt>National ID</dt><dd>{revealed || owner.nationalIdMask || 'Not recorded'}</dd></div>
        </dl>
        {canReveal && owner.nationalIdMask && <button type="button" className="secondary" disabled={busy}
            onClick={() => revealed ? setRevealed('') : void reveal()}>{revealed ? 'Hide National ID' : 'Reveal National ID'}</button>}
        {error && <p className="form-error" role="alert">{error}</p>}
    </section>;
}
