import * as React from 'react';
import { IExternalLibrary, IExternalUser } from '../models/IExternalLibrary';
export interface IManageUsersModalProps {
    isOpen: boolean;
    library: IExternalLibrary | null;
    onClose: () => void;
    onAddUser: (libraryId: string, email: string, permission: 'Read' | 'Edit', company?: string, project?: string) => Promise<void>;
    onBulkAddUsers: (libraryId: string, emails: string[], permission: 'Read' | 'Edit', company?: string, project?: string) => Promise<any>;
    onRemoveUser: (libraryId: string, userId: string) => Promise<void>;
    onGetUsers: (libraryId: string) => Promise<IExternalUser[]>;
    onSearchUsers: (query: string) => Promise<IExternalUser[]>;
    onUpdateUserMetadata: (libraryId: string, userId: string, company: string, project: string) => Promise<void>;
}
export interface IAddUserFormData {
    email: string;
    emails: string;
    permission: 'Read' | 'Edit';
    isBulkMode: boolean;
    company: string;
    project: string;
}
export declare const ManageUsersModal: React.FC<IManageUsersModalProps>;
//# sourceMappingURL=ManageUsersModal.d.ts.map