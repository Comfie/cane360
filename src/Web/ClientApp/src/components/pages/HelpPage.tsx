import {BookOpen, Download, ExternalLink} from 'lucide-react';
import {PageHeader} from '../PageHeader';

export function HelpPage() {
    return (
        <div className="page-stack">
            <PageHeader eyebrow="User guide" title="Help and user manual"
                        description="Learn the system module by module, from account activation to payroll settlement and crop closure.">
                <div className="page-actions">
                    <a className="secondary-action" href="/help/Cane360-User-Manual.pdf" download>
                        <Download size={16}/> Printable PDF
                    </a>
                    <a className="secondary-action" href="/help/index.html" target="_blank" rel="noopener noreferrer">
                        <ExternalLink size={16}/> Open full manual
                    </a>
                </div>
            </PageHeader>
            <p><BookOpen size={16} aria-hidden="true"/> Use the contents or search below to find a procedure. The manual includes permissions, examples, troubleshooting and current screen limitations.</p>
            <iframe className="user-manual-frame" src="/help/index.html" title="Cane360 user manual"/>
        </div>
    );
}
