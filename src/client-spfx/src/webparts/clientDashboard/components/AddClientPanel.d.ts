import * as React from 'react';
export interface IAddClientPanelProps {
    isOpen: boolean;
    onDismiss: () => void;
    onClientAdded: (clientName: string) => Promise<void>;
}
declare const AddClientPanel: React.FC<IAddClientPanelProps>;
export default AddClientPanel;
//# sourceMappingURL=AddClientPanel.d.ts.map