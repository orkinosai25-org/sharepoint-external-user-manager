import * as React from 'react';
export interface IAddListPanelProps {
    isOpen: boolean;
    clientName: string;
    onDismiss: () => void;
    onListCreated: (listName: string, listType: string, description: string) => Promise<void>;
}
declare const AddListPanel: React.FC<IAddListPanelProps>;
export default AddListPanel;
//# sourceMappingURL=AddListPanel.d.ts.map