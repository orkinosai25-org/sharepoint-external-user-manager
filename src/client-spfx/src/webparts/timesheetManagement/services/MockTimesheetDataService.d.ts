import { ITimesheetEntry } from '../models/ITimesheetEntry';
export declare class MockTimesheetDataService {
    static getTimesheets(): ITimesheetEntry[];
    static createTimesheet(timesheet: Partial<ITimesheetEntry>): Promise<ITimesheetEntry>;
    static updateTimesheet(id: string, updates: Partial<ITimesheetEntry>): Promise<void>;
    static deleteTimesheet(id: string): Promise<void>;
}
//# sourceMappingURL=MockTimesheetDataService.d.ts.map