import {FieldForm} from '../farm-setup/FieldForm';
import {useEffect, useState} from 'react';
import {Eye, Plus, Sprout} from 'lucide-react';
import {Link, useNavigate} from 'react-router-dom';
import {type CropCycleCollectionDto} from '../../web-api-client';
import {CropCycleForm} from '../crop-cycles/CropCycleForm';
import {cropCyclesClient} from '../crop-cycles/cropCycleApi';
import {CropCycleRegister} from '../crop-cycles/CropCycleRegister';
import {EmptyState} from '../EmptyState';
import {FieldDetailsForm} from '../farm-setup/FieldDetailsForm';
import {FieldRecord} from '../farm-setup/FieldRecord';
import {FarmSetupProgress} from '../farm-setup/FarmSetupProgress';
import {getApiError, useFarmSetup} from '../farm-setup/farmSetupApi';
import {LoadingState} from '../LoadingState';
import {PageHeader} from '../PageHeader';
import {ValidationError} from '../ValidationError';
import {LineProfileForm} from '../farm-setup/LineProfileForm';
import {useAuth} from '../api-authorization/AuthContext';

export function FieldsPage() {
    const navigate = useNavigate();
    const isSupervisor = useAuth().session.role === 'Supervisor';
    const {setup, setSetup, error, setError, isLoading} = useFarmSetup();
    const [isAddingField, setIsAddingField] = useState(false);
    const [activeCycleField, setActiveCycleField] = useState<string | null>(null);
    const [cycleCollections, setCycleCollections] = useState<CropCycleCollectionDto[]>([]);
    const [cycleFilter, setCycleFilter] = useState('all');
    const [loadedFieldKey, setLoadedFieldKey] = useState('');
    const fieldIds = (setup?.farm?.fields ?? []).map((field) => field.id);
    const fieldKey = fieldIds.join(',');
    const areCyclesLoading = Boolean(fieldKey) && loadedFieldKey !== fieldKey;

    useEffect(() => {
        let isCurrent = true;
        if (!fieldKey) {
            return () => {
                isCurrent = false;
            };
        }

        Promise.all(fieldKey.split(',').map((fieldId) => cropCyclesClient.getCropCycles(fieldId)))
            .then((collections) => {
                if (isCurrent) setCycleCollections(collections);
            })
            .catch((requestError) => {
                if (isCurrent) setError(getApiError(requestError));
            })
            .finally(() => {
                if (isCurrent) setLoadedFieldKey(fieldKey);
            });

        return () => {
            isCurrent = false;
        };
    }, [fieldKey, setError]);

    if (isLoading) return <LoadingState label="Loading fields and crop cycles"/>;
    if (!setup) return <ValidationError title="Field records unavailable" message={error} persistent/>;

    if (!setup.isConfigured) {
        return (
            <div className="page-stack">
                <PageHeader eyebrow="Crop records" title="Fields and crop cycles"
                            description="Create your farm before adding its fields."/>
                <FarmSetupProgress setup={setup}/>
                <EmptyState title="Your farm record comes first"
                            description="Fields belong to your active farm and use its grower workspace for secure data isolation."
                            nextStep="Create the farm, then return here to add its first field."
                            action={<Link className="primary-action" to="/farm">Create farm</Link>}/>
            </div>
        );
    }

    const fields = setup.farm?.fields ?? [];
    const showFieldForm = !isSupervisor && (fields.length === 0 || isAddingField);

    return (
        <div className="page-stack">
            <PageHeader eyebrow="Crop records" title="Fields and crop cycles"
                        description={`${isSupervisor ? 'View' : 'Manage'} field plans, current crops and chronological history on ${setup.farm?.name}.`}>
                {!isSupervisor && !showFieldForm &&
                    <button type="button" className="primary-action" onClick={() => setIsAddingField(true)}><Plus
                        size={17}/> Add field</button>}
            </PageHeader>

            <FarmSetupProgress setup={setup}/>
            <ValidationError message={error}/>

            {showFieldForm && (
                <FieldForm
                    onSaved={(result) => {
                        setSetup(result);
                        setIsAddingField(false);
                        setActiveCycleField(null);
                    }}
                    onCancel={fields.length > 0 ? () => setIsAddingField(false) : undefined}
                    onError={setError}
                />
            )}

            {fields.length > 0 && (
                <section aria-labelledby="field-list-title">
                    <div className="section-heading">
                        <div><span className="eyebrow">Farm fields</span><h2
                            id="field-list-title">{fields.length} {fields.length === 1 ? 'field' : 'fields'} recorded</h2>
                        </div>
                        <p>Each field keeps its own current crop and full cycle register.</p>
                    </div>
                    <div className="field-record-list">
                        {fields.map((field) => {
                            const draftCycle = cycleCollections
                                .find((collection) => collection.field.id === field.id)
                                ?.cropCycles.find((cycle) => cycle.status === 'Draft');

                            return <FieldRecord key={field.id} field={field} draftCycle={draftCycle}>
                                {!isSupervisor && <FieldDetailsForm field={field} onSaved={setSetup}/>}
                                {!isSupervisor && <LineProfileForm fieldId={field.id}/>}
                                {!isSupervisor && <div className="field-cycle-actions">
                                    {field.currentCropCycle && <Link className="secondary-action"
                                                                     to={`/fields/${field.id}/crop-cycles/${field.currentCropCycle.id}`}><Eye
                                        size={16}/> View current cycle</Link>}
                                    {draftCycle
                                        ?
                                        <Link className={field.currentCropCycle ? 'secondary-action' : 'primary-action'}
                                              to={`/fields/${field.id}/crop-cycles/${draftCycle.id}`}><Sprout
                                            size={17}/> {field.currentCropCycle ? 'Review draft' : 'Review and activate'}
                                        </Link>
                                        : activeCycleField !== field.id && <button type="button"
                                                                                   className={field.currentCropCycle ? 'secondary-action' : 'primary-action'}
                                                                                   onClick={() => setActiveCycleField(field.id)}>
                                        <Sprout
                                            size={17}/> {field.currentCropCycle ? 'Plan next crop' : 'Set up first crop'}
                                    </button>}
                                </div>}
                                {!isSupervisor && activeCycleField === field.id && (
                                    <CropCycleForm
                                        field={field}
                                        onSaved={(details) => navigate(`/fields/${field.id}/crop-cycles/${details.cropCycle.id}`)}
                                        onCancel={() => setActiveCycleField(null)}
                                    />
                                )}
                            </FieldRecord>;
                        })}
                    </div>
                </section>
            )}

            {fields.length > 0 && (areCyclesLoading
                ? <LoadingState label="Loading crop-cycle register"/>
                : <CropCycleRegister collections={cycleCollections} filter={cycleFilter} onFilterChange={setCycleFilter}
                                     readOnly={isSupervisor}/>)}
        </div>
    );
}
