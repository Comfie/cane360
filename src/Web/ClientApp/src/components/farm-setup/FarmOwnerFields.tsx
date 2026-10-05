import type {GrowerDto} from '../../web-api-client';

export function FarmOwnerFields({owner}: {owner?: GrowerDto}) {
    return <>
        <fieldset className="form-grid"><legend>Identity</legend>
            <label>Title<input name="title" maxLength={40} defaultValue={owner?.title}/></label>
            <label>First name<input name="firstName" autoComplete="given-name" maxLength={100} defaultValue={owner?.firstName}/></label>
            <label>Surname<input name="surname" autoComplete="family-name" maxLength={100} defaultValue={owner?.surname}/></label>
            <label>Sex<select name="sex" defaultValue={owner?.sex ?? ''}><option value="">Not recorded</option>
                {['Female', 'Male', 'Other', 'Prefer not to say'].map(value => <option key={value}>{value}</option>)}
            </select></label>
            <label>Farm Owner display name<input name="growerDisplayName" maxLength={120} required defaultValue={owner?.displayName}/></label>
            <label>Grower number / ID<input name="growerNumber" maxLength={60} defaultValue={owner?.growerNumber}/></label>
            <label>National ID number<input name="nationalId" maxLength={80} autoComplete="off" placeholder={owner?.nationalIdMask ?? 'Not recorded'}/>
                <small>Leave blank to keep the current protected ID.</small></label>
        </fieldset>
        <fieldset className="form-grid"><legend>Association</legend>
            <label>Association<input name="association" maxLength={120} defaultValue={owner?.association}/></label>
            <label>Membership number<input name="membershipNumber" maxLength={60} defaultValue={owner?.membershipNumber}/></label>
        </fieldset>
        <fieldset className="form-grid"><legend>Contact</legend>
            <label>Primary contact number<input name="growerPhone" type="tel" autoComplete="tel" maxLength={30} defaultValue={owner?.phone}/></label>
            <label>Email address<input name="email" type="email" autoComplete="email" maxLength={254} defaultValue={owner?.email}/></label>
            <label className="is-wide">Registered address<textarea name="registeredAddress" maxLength={240} rows={2} defaultValue={owner?.registeredAddress}/></label>
        </fieldset>
        <fieldset className="form-grid"><legend>Profile</legend>
            <label>Photograph reference<input name="photoReference" maxLength={240} defaultValue={owner?.photoReference}/>
                <small>Reference to an existing photograph. Uploads are not available yet.</small></label>
            <label>Status<select name="ownerStatus" defaultValue={owner?.active === false ? 'Inactive' : 'Active'}>
                <option>Active</option><option>Inactive</option></select></label>
        </fieldset>
    </>;
}
