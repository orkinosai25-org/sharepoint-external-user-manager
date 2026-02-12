import * as React from 'react';
import { IExternalLibrary } from '../models/IExternalLibrary';
export interface IDeleteLibraryModalProps {
    isOpen: boolean;
    libraries: IExternalLibrary[];
    onClose: () => void;
    onLibrariesDeleted: (deletedLibraryIds: string[]) => void;
    onDeleteLibrary: (libraryId: string) => Promise<void>;
}
export declare const DeleteLibraryModal: React.FC<IDeleteLibraryModalProps>;
//# sourceMappingURL=DeleteLibraryModal.d.ts.map