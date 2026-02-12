import * as React from 'react';
import { IClient } from '../models/IClient';
import { ClientDataService } from '../services/ClientDataService';
export interface IClientSpaceDetailPanelProps {
    isOpen: boolean;
    client: IClient | null;
    dataService: ClientDataService;
    onDismiss: () => void;
}
declare const ClientSpaceDetailPanel: React.FC<IClientSpaceDetailPanelProps>;
export default ClientSpaceDetailPanel;
//# sourceMappingURL=ClientSpaceDetailPanel.d.ts.map