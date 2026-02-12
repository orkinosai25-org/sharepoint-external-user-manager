import * as React from 'react';
export interface IAddLibraryPanelProps {
    isOpen: boolean;
    clientName: string;
    onDismiss: () => void;
    onLibraryCreated: (libraryName: string, description: string) => Promise<void>;
}
declare const AddLibraryPanel: React.FC<IAddLibraryPanelProps>;
export default AddLibraryPanel;
//# sourceMappingURL=AddLibraryPanel.d.ts.map