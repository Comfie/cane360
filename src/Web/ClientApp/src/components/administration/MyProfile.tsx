import {type FormEvent, useEffect, useState} from 'react';
import {type AccountProfileDto, MyProfileClient, UpdateMyProfileCommand} from '../../web-api-client';
import {roleLabel} from '../api-authorization/activationView';
import {useAuth} from '../api-authorization/AuthContext';
import {getApiError} from '../apiError';
import {LoadingState} from '../LoadingState';
import {ValidationError} from '../ValidationError';

const profiles = new MyProfileClient();

export function MyProfile() {
    const {session} = useAuth();
    const [profile, setProfile] = useState<AccountProfileDto | null>(null);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');
    const [displayName, setDisplayName] = useState('');
    const [phoneNumber, setPhoneNumber] = useState('');

    useEffect(() => {
        let active = true;
        profiles.getMyProfile().then((result) => {
            if (!active) return;
            setProfile(result);
            setDisplayName(result.displayName ?? '');
            setPhoneNumber(result.phoneNumber ?? '');
        }).catch((cause) => {
            if (active) setError(getApiError(cause));
        }).finally(() => {
            if (active) setLoading(false);
        });
        return () => {active = false;};
    }, []);

    const save = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (saving) return;
        setSaving(true);
        setError('');
        setSuccess('');
        try {
            const result = await profiles.updateMyProfile(new UpdateMyProfileCommand({displayName, phoneNumber}));
            setProfile(result);
            setDisplayName(result.displayName ?? '');
            setPhoneNumber(result.phoneNumber ?? '');
            setSuccess('Your profile has been saved.');
        } catch (cause) {
            setError(getApiError(cause));
        } finally {
            setSaving(false);
        }
    };

    if (loading) return <LoadingState label="Loading your profile"/>;
    if (!profile) return <ValidationError message={error || 'Your profile could not be loaded.'}/>;

    return <section className="record-panel my-profile">
        <header className="section-heading">
            <div><span className="eyebrow">Signed-in account</span><h2>My profile</h2></div>
            <span className="signed-in-role">{roleLabel(session.role)}</span>
        </header>
        <p>Manage the name and contact number for your Cane360 account. Farm personnel and the Farm Owner record are managed under Farm.</p>
        <dl className="record-details">
            <div><dt>Sign-in email</dt><dd>{profile.email}</dd></div>
            <div><dt>Email verification</dt><dd>{profile.isEmailConfirmed ? 'Verified' : 'Not verified'}</dd></div>
            <div><dt>Assigned role</dt><dd>{roleLabel(session.role)}</dd></div>
        </dl>
        <p className="profile-help">Your sign-in email and assigned role are read-only here.</p>
        <ValidationError message={error}/>
        <div role="status">{success && <p className="success-banner">{success}</p>}</div>
        <form onSubmit={save}>
            <fieldset disabled={saving} className="form-grid">
                <legend>Personal details</legend>
                <label>Display name<input name="displayName" autoComplete="name" required maxLength={120}
                    value={displayName} onChange={(event) => {setDisplayName(event.target.value); setSuccess('');}}/></label>
                <label>Contact number<input name="phoneNumber" type="tel" autoComplete="tel" maxLength={40}
                    value={phoneNumber} onChange={(event) => {setPhoneNumber(event.target.value); setSuccess('');}}/></label>
            </fieldset>
            <footer className="form-actions">
                <button type="submit" disabled={saving || !displayName.trim()}>{saving ? 'Saving…' : 'Save profile'}</button>
            </footer>
        </form>
    </section>;
}
