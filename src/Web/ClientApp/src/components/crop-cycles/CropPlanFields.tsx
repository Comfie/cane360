import {useEffect, useState} from 'react';
import {DatePicker} from '../DatePicker';
import {cropCyclesClient, localDate} from './cropCycleApi';
import {getApiError} from '../farm-setup/farmSetupApi';
import {ValidationError} from '../ValidationError';

interface CropPlanFieldsProps {
    fieldId: string;
    startDate?: string;
    harvestStart?: string;
    harvestEnd?: string;
    expectedYield?: number;
}

export function CropPlanFields({fieldId, startDate = '', harvestStart = '', harvestEnd = '', expectedYield}: CropPlanFieldsProps) {
    const [plantingDate, setPlantingDate] = useState(startDate);
    const [useDefault, setUseDefault] = useState(!harvestStart);
    const [maturity, setMaturity] = useState<string>();
    const [months, setMonths] = useState<number>();
    const [error, setError] = useState('');

    useEffect(() => {
        let current = true;
        cropCyclesClient.calculateCropMaturity(fieldId, plantingDate ? localDate(plantingDate) : undefined)
            .then(result => {
                if (!current) return;
                setMaturity(result.expectedMaturityDate);
                setMonths(result.defaultCropMaturityMonths);
                setError('');
            })
            .catch(requestError => {if (current) setError(getApiError(requestError));});
        return () => {current = false;};
    }, [fieldId, plantingDate]);

    return <>
        <label>Planting / cycle start date<DatePicker name="startDate" value={plantingDate} onChange={setPlantingDate} required/></label>
        <label className="inline-choice"><input type="checkbox" checked={useDefault} onChange={event => setUseDefault(event.target.checked)}/>
            Use calculated maturity{months !== undefined ? ` (${months} months)` : ''}</label>
        {useDefault ? <p className="form-guidance" role="status">Expected maturity: {plantingDate && maturity ? maturity : 'Choose a planting date'}.
            This becomes the expected harvest window when saved.</p> : <>
            <label>Expected harvest from<DatePicker name="expectedHarvestStart" defaultValue={harvestStart} required/></label>
            <label>Expected harvest to<DatePicker name="expectedHarvestEnd" defaultValue={harvestEnd} required/></label>
        </>}
        <label>Expected yield (tonnes)<input name="expectedYieldTonnes" type="number" min="0.001" max="1000000"
            step="0.001" inputMode="decimal" defaultValue={expectedYield} required/></label>
        <ValidationError message={error}/>
    </>;
}
