export interface ITimesheetEntry {
    id: string;
    employee: string;
    weekEnding: Date;
    totalHours: number;
    status: 'Draft' | 'Submitted' | 'Approved' | 'Rejected';
    submittedDate?: Date;
    approvedBy?: string;
    approvedDate?: Date;
    entries: ITimeEntry[];
}
export interface ITimeEntry {
    id: string;
    date: Date;
    project: string;
    task: string;
    hours: number;
    description?: string;
}
export declare type TimesheetStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected';
//# sourceMappingURL=ITimesheetEntry.d.ts.map