import * as React from 'react';
import { IExternalLibrary } from '../models/IExternalLibrary';
export interface ICreateLibraryModalProps {
    isOpen: boolean;
    onClose: () => void;
    onLibraryCreated: (library: IExternalLibrary) => void;
    onCreateLibrary: (config: {
        title: string;
        description?: string;
        enableExternalSharing?: boolean;
    }) => Promise<IExternalLibrary>;
}
export interface ICreateLibraryFormData {
    title: string;
    description: string;
    enableExternalSharing: boolean;
}
export declare const CreateLibraryModal: React.FC<ICreateLibraryModalProps>;
//# sourceMappingURL=CreateLibraryModal.d.ts.map