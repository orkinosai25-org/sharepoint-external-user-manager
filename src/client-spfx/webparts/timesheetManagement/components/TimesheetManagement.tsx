import * as React from 'react';
import { useState, useEffect } from 'react';
import { Stack, CommandBar, DetailsList, IColumn, SelectionMode, Spinner, SpinnerSize } from '@fluentui/react';
import { ITimesheetManagementProps } from './ITimesheetManagementProps';
import { ITimesheetEntry } from '../models/ITimesheetEntry';
import { MockTimesheetDataService } from '../services/MockTimesheetDataService';
import styles from './TimesheetManagement.module.scss';

const TimesheetManagement: React.FC<ITimesheetManagementProps> = (props) => {
  const [timesheets, setTimesheets] = useState<ITimesheetEntry[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    loadTimesheets();
  }, []);

  const loadTimesheets = async (): Promise<void> => {
    setLoading(true);
    try {
      // Simulate API call
      setTimeout(() => {
        const mockTimesheets = MockTimesheetDataService.getTimesheets();
        setTimesheets(mockTimesheets);
        setLoading(false);
      }, 1000);
    } catch (error) {
      console.error('Error loading timesheets:', error);
      setLoading(false);
    }
  };

  const columns: IColumn[] = [
    {
      key: 'employee',
      name: 'Employee',
      fieldName: 'employee',
      minWidth: 150,
      maxWidth: 200,
      isResizable: true
    },
    {
      key: 'weekEnding',
      name: 'Week Ending',
      fieldName: 'weekEnding',
      minWidth: 120,
      maxWidth: 150,
      isResizable: true,
      onRender: (item: ITimesheetEntry) => new Date(item.weekEnding).toLocaleDateString()
    },
    {
      key: 'totalHours',
      name: 'Total Hours',
      fieldName: 'totalHours',
      minWidth: 100,
      maxWidth: 120,
      isResizable: true
    },
    {
      key: 'status',
      name: 'Status',
      fieldName: 'status',
      minWidth: 100,
      maxWidth: 120,
      isResizable: true
    }
  ];

  const commandBarItems = [
    {
      key: 'newTimesheet',
      text: 'New Timesheet',
      iconProps: { iconName: 'Add' },
      onClick: () => alert('New Timesheet functionality will be implemented')
    },
    {
      key: 'approve',
      text: 'Approve',
      iconProps: { iconName: 'CheckMark' },
      onClick: () => alert('Approve functionality will be implemented')
    },
    {
      key: 'reject',
      text: 'Reject',
      iconProps: { iconName: 'Cancel' },
      onClick: () => alert('Reject functionality will be implemented')
    },
    {
      key: 'refresh',
      text: 'Refresh',
      iconProps: { iconName: 'Refresh' },
      onClick: loadTimesheets
    }
  ];

  return (
    <div className={styles.timesheetManagement}>
      <Stack tokens={{ childrenGap: 20 }}>
        <h2>Timesheet Management</h2>
        <CommandBar items={commandBarItems} />
        {loading ? (
          <Spinner size={SpinnerSize.large} label="Loading timesheets..." />
        ) : (
          <DetailsList
            items={timesheets}
            columns={columns}
            selectionMode={SelectionMode.multiple}
            setKey="set"
            layoutMode={0}
            isHeaderVisible={true}
          />
        )}
      </Stack>
    </div>
  );
};

export default TimesheetManagement;