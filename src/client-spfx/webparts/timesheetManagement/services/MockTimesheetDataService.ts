import { ITimesheetEntry } from '../models/ITimesheetEntry';

export class MockTimesheetDataService {
  public static getTimesheets(): ITimesheetEntry[] {
    return [
      {
        id: '1',
        employee: 'John Smith',
        weekEnding: new Date('2024-01-12'),
        totalHours: 40,
        status: 'Approved',
        submittedDate: new Date('2024-01-13'),
        approvedBy: 'Jane Manager',
        approvedDate: new Date('2024-01-14'),
        entries: [
          {
            id: '1-1',
            date: new Date('2024-01-08'),
            project: 'Project Alpha',
            task: 'Development',
            hours: 8,
            description: 'Frontend development'
          },
          {
            id: '1-2',
            date: new Date('2024-01-09'),
            project: 'Project Alpha',
            task: 'Testing',
            hours: 8,
            description: 'Unit testing'
          }
        ]
      },
      {
        id: '2',
        employee: 'Sarah Johnson',
        weekEnding: new Date('2024-01-12'),
        totalHours: 35,
        status: 'Submitted',
        submittedDate: new Date('2024-01-13'),
        entries: [
          {
            id: '2-1',
            date: new Date('2024-01-08'),
            project: 'Project Beta',
            task: 'Analysis',
            hours: 7,
            description: 'Requirements analysis'
          }
        ]
      },
      {
        id: '3',
        employee: 'Mike Wilson',
        weekEnding: new Date('2024-01-12'),
        totalHours: 42,
        status: 'Draft',
        entries: [
          {
            id: '3-1',
            date: new Date('2024-01-08'),
            project: 'Project Gamma',
            task: 'Design',
            hours: 8,
            description: 'UI/UX design'
          }
        ]
      }
    ];
  }

  public static async createTimesheet(timesheet: Partial<ITimesheetEntry>): Promise<ITimesheetEntry> {
    // Mock implementation - would integrate with SharePoint Lists or external API
    return {
      id: Math.random().toString(36).substr(2, 9),
      employee: timesheet.employee || '',
      weekEnding: timesheet.weekEnding || new Date(),
      totalHours: timesheet.totalHours || 0,
      status: 'Draft',
      entries: timesheet.entries || []
    };
  }

  public static async updateTimesheet(id: string, updates: Partial<ITimesheetEntry>): Promise<void> {
    // Mock implementation - would integrate with SharePoint Lists or external API
    console.log(`Updating timesheet ${id}`, updates);
  }

  public static async deleteTimesheet(id: string): Promise<void> {
    // Mock implementation - would integrate with SharePoint Lists or external API
    console.log(`Deleting timesheet ${id}`);
  }
}