import { Check, Circle } from 'lucide-react';

/** @param {{ setup: import('../../web-api-client').FarmSetupDto }} props */
export function FarmSetupProgress({ setup }) {
  const fields = setup.farm?.fields ?? [];
  const steps = [
    { label: 'Farm', complete: setup.isConfigured },
    { label: 'Field', complete: fields.length > 0 },
    { label: 'Current crop', complete: fields.some((field) => field.currentCropCycle) },
  ];

  return (
    <section className="setup-progress" aria-labelledby="setup-progress-title">
      <div>
        <span className="eyebrow">Setup trail</span>
        <h2 id="setup-progress-title">Build your working farm record</h2>
      </div>
      <ol>
        {steps.map((step, index) => (
          <li key={step.label} className={step.complete ? 'is-complete' : ''}>
            <span aria-hidden="true">
              {step.complete ? <Check size={14} /> : <Circle size={10} />}
            </span>
            <small>Step {index + 1}</small>
            <strong>{step.label}</strong>
          </li>
        ))}
      </ol>
    </section>
  );
}
