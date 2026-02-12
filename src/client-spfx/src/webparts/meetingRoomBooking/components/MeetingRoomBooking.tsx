import * as React from 'react';
import { useState, useEffect } from 'react';
import { 
  Stack, 
  CommandBar, 
  DetailsList, 
  IColumn, 
  SelectionMode, 
  Spinner, 
  SpinnerSize,
  DatePicker,
  Dropdown,
  IDropdownOption,
  MessageBar,
  MessageBarType,
  Panel,
  PanelType,
  TextField,
  PrimaryButton,
  DefaultButton,
  Calendar,
  Text,
  Icon,
  Separator,
  Toggle
} from '@fluentui/react';
import { IMeetingRoomBookingProps } from './IMeetingRoomBookingProps';
import { IMeetingRoom, IBooking, IAvailabilitySlot } from '../models/IBookingModels';
import { MockBookingDataService } from '../services/MockBookingDataService';
import { TeamsIntegrationService } from '../services/TeamsIntegrationService';
import styles from './MeetingRoomBooking.module.scss';

const MeetingRoomBooking: React.FC<IMeetingRoomBookingProps> = (props) => {
  const [rooms, setRooms] = useState<IMeetingRoom[]>([]);
  const [bookings, setBookings] = useState<IBooking[]>([]);
  const [selectedDate, setSelectedDate] = useState<Date>(new Date());
  const [selectedRoom, setSelectedRoom] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(true);
  const [showBookingPanel, setShowBookingPanel] = useState<boolean>(false);
  const [newBooking, setNewBooking] = useState<Partial<IBooking>>({});
  const [availabilitySlots, setAvailabilitySlots] = useState<IAvailabilitySlot[]>([]);

  useEffect(() => {
    loadRoomsAndBookings();
  }, []);

  useEffect(() => {
    if (selectedDate && selectedRoom) {
      loadAvailability();
    }
  }, [selectedDate, selectedRoom]);

  const loadRoomsAndBookings = async (): Promise<void> => {
    setLoading(true);
    try {
      setTimeout(() => {
        const mockRooms = MockBookingDataService.getMeetingRooms();
        const mockBookings = MockBookingDataService.getBookings();
        setRooms(mockRooms);
        setBookings(mockBookings);
        setLoading(false);
      }, 1000);
    } catch (error) {
      console.error('Error loading rooms and bookings:', error);
      setLoading(false);
    }
  };

  const loadAvailability = async (): Promise<void> => {
    try {
      const slots = await MockBookingDataService.getAvailabilitySlots(selectedRoom, selectedDate);
      setAvailabilitySlots(slots);
    } catch (error) {
      console.error('Error loading availability:', error);
    }
  };

  const handleCreateBooking = async (): Promise<void> => {
    try {
      let teamsLink: string | undefined;
      
      if (props.enableTeamsIntegration && newBooking.title) {
        try {
          const teamsIntegration = await TeamsIntegrationService.createTeamsMeeting({
            subject: newBooking.title,
            startTime: newBooking.startTime!,
            endTime: newBooking.endTime!,
            attendees: newBooking.attendees || []
          });
          teamsLink = teamsIntegration.joinUrl;
        } catch (teamsError) {
          console.error('Teams integration error:', teamsError);
          // Continue with booking even if Teams integration fails
        }
      }

      const booking: Partial<IBooking> = {
        ...newBooking,
        roomId: selectedRoom,
        roomName: rooms.find(r => r.id === selectedRoom)?.name || '',
        status: props.requireApproval ? 'Requested' : 'Confirmed',
        createdDate: new Date(),
        lastModified: new Date(),
        organizer: props.context.pageContext.user.email,
        teamsLink
      };

      await MockBookingDataService.createBooking(booking);
      setShowBookingPanel(false);
      setNewBooking({});
      loadRoomsAndBookings();
    } catch (error) {
      console.error('Error creating booking:', error);
    }
  };

  const roomOptions: IDropdownOption[] = rooms.map(room => ({
    key: room.id,
    text: `${room.name} (${room.location}) - Capacity: ${room.capacity}`
  }));

  const todaysBookings = bookings.filter(booking => {
    const bookingDate = new Date(booking.startTime);
    return bookingDate.toDateString() === selectedDate.toDateString();
  });

  const selectedRoomBookings = todaysBookings.filter(booking => booking.roomId === selectedRoom);

  const columns: IColumn[] = [
    {
      key: 'title',
      name: 'Meeting Title',
      fieldName: 'title',
      minWidth: 150,
      maxWidth: 250,
      isResizable: true
    },
    {
      key: 'time',
      name: 'Time',
      fieldName: 'startTime',
      minWidth: 120,
      maxWidth: 150,
      isResizable: true,
      onRender: (item: IBooking) => 
        `${new Date(item.startTime).toLocaleTimeString()} - ${new Date(item.endTime).toLocaleTimeString()}`
    },
    {
      key: 'organizer',
      name: 'Organizer',
      fieldName: 'organizer',
      minWidth: 120,
      maxWidth: 180,
      isResizable: true
    },
    {
      key: 'status',
      name: 'Status',
      fieldName: 'status',
      minWidth: 80,
      maxWidth: 100,
      isResizable: true
    },
    {
      key: 'teamsLink',
      name: 'Teams',
      fieldName: 'teamsLink',
      minWidth: 60,
      maxWidth: 80,
      isResizable: true,
      onRender: (item: IBooking) => 
        item.teamsLink ? <Icon iconName="TeamsLogo" style={{ color: '#464EB8' }} /> : null
    }
  ];

  const commandBarItems = [
    {
      key: 'newBooking',
      text: 'New Booking',
      iconProps: { iconName: 'Add' },
      onClick: () => setShowBookingPanel(true)
    },
    {
      key: 'viewCalendar',
      text: 'Calendar View',
      iconProps: { iconName: 'Calendar' },
      onClick: () => alert('Calendar view will be implemented')
    },
    {
      key: 'myBookings',
      text: 'My Bookings',
      iconProps: { iconName: 'Contact' },
      onClick: () => alert('My bookings view will be implemented')
    },
    {
      key: 'reports',
      text: 'Reports',
      iconProps: { iconName: 'BarChart4' },
      onClick: () => alert('Reports functionality will be implemented')
    },
    {
      key: 'refresh',
      text: 'Refresh',
      iconProps: { iconName: 'Refresh' },
      onClick: loadRoomsAndBookings
    }
  ];

  const renderAvailabilitySlots = () => (
    <Stack tokens={{ childrenGap: 10 }}>
      <Text variant="mediumPlus">Available Time Slots</Text>
      {availabilitySlots.length === 0 ? (
        <Text variant="medium">No availability data for selected room and date.</Text>
      ) : (
        availabilitySlots.map((slot, index) => (
          <div 
            key={index} 
            className={slot.isAvailable ? styles.availableSlot : styles.unavailableSlot}
            onClick={() => {
              if (slot.isAvailable) {
                setNewBooking({
                  ...newBooking,
                  startTime: slot.start,
                  endTime: slot.end
                });
                setShowBookingPanel(true);
              }
            }}
          >
            <Text variant="medium">
              {slot.start.toLocaleTimeString()} - {slot.end.toLocaleTimeString()}
              {slot.isAvailable ? ' (Available)' : ' (Booked)'}
            </Text>
          </div>
        ))
      )}
    </Stack>
  );

  const renderBookingPanel = () => (
    <Panel
      isOpen={showBookingPanel}
      type={PanelType.medium}
      onDismiss={() => setShowBookingPanel(false)}
      headerText="Create New Booking"
    >
      <Stack tokens={{ childrenGap: 15 }}>
        <TextField
          label="Meeting Title"
          value={newBooking.title || ''}
          onChange={(_, value) => setNewBooking({ ...newBooking, title: value })}
          required
        />
        
        <TextField
          label="Description"
          multiline
          rows={3}
          value={newBooking.description || ''}
          onChange={(_, value) => setNewBooking({ ...newBooking, description: value })}
        />
        
        <Dropdown
          label="Meeting Room"
          options={roomOptions}
          selectedKey={selectedRoom}
          onChange={(_, option) => setSelectedRoom(option?.key as string)}
          required
        />
        
        <DatePicker
          label="Date"
          value={selectedDate}
          onSelectDate={(date) => date && setSelectedDate(date)}
        />
        
        <Stack horizontal tokens={{ childrenGap: 10 }}>
          <TextField
            label="Start Time"
            type="time"
            value={newBooking.startTime ? 
              new Date(newBooking.startTime).toTimeString().slice(0, 5) : ''}
            onChange={(_, value) => {
              if (value) {
                const [hours, minutes] = value.split(':');
                const startTime = new Date(selectedDate);
                startTime.setHours(parseInt(hours), parseInt(minutes));
                setNewBooking({ ...newBooking, startTime });
              }
            }}
          />
          
          <TextField
            label="End Time"
            type="time"
            value={newBooking.endTime ? 
              new Date(newBooking.endTime).toTimeString().slice(0, 5) : ''}
            onChange={(_, value) => {
              if (value) {
                const [hours, minutes] = value.split(':');
                const endTime = new Date(selectedDate);
                endTime.setHours(parseInt(hours), parseInt(minutes));
                setNewBooking({ ...newBooking, endTime });
              }
            }}
          />
        </Stack>
        
        {props.enableTeamsIntegration && (
          <Toggle
            label="Create Teams Meeting"
            checked={true}
            onText="Teams meeting will be created"
            offText="No Teams meeting"
          />
        )}
        
        <Stack horizontal tokens={{ childrenGap: 10 }}>
          <PrimaryButton text="Book Room" onClick={handleCreateBooking} />
          <DefaultButton text="Cancel" onClick={() => setShowBookingPanel(false)} />
        </Stack>
      </Stack>
    </Panel>
  );

  return (
    <div className={styles.meetingRoomBooking}>
      <Stack tokens={{ childrenGap: 20 }}>
        <h2>Meeting Room & Resource Booking</h2>
        
        {props.requireApproval && (
          <MessageBar messageBarType={MessageBarType.info}>
            Bookings require approval. You will receive a notification once your booking is reviewed.
          </MessageBar>
        )}
        
        <CommandBar items={commandBarItems} />
        
        <Stack horizontal tokens={{ childrenGap: 20 }}>
          <Stack tokens={{ childrenGap: 15 }} styles={{ root: { width: '300px' } }}>
            <DatePicker
              label="Select Date"
              value={selectedDate}
              onSelectDate={(date) => date && setSelectedDate(date)}
            />
            
            <Dropdown
              label="Select Room"
              placeholder="Choose a meeting room"
              options={roomOptions}
              selectedKey={selectedRoom}
              onChange={(_, option) => setSelectedRoom(option?.key as string)}
            />
            
            {selectedRoom && renderAvailabilitySlots()}
          </Stack>
          
          <Stack tokens={{ childrenGap: 15 }} styles={{ root: { flex: 1 } }}>
            <Text variant="xLarge">
              Bookings for {selectedDate.toLocaleDateString()}
            </Text>
            
            {loading ? (
              <Spinner size={SpinnerSize.large} label="Loading bookings..." />
            ) : (
              <DetailsList
                items={selectedRoomBookings}
                columns={columns}
                selectionMode={SelectionMode.none}
                setKey="set"
                layoutMode={0}
                isHeaderVisible={true}
              />
            )}
          </Stack>
        </Stack>
        
        {renderBookingPanel()}
      </Stack>
    </div>
  );
};

export default MeetingRoomBooking;