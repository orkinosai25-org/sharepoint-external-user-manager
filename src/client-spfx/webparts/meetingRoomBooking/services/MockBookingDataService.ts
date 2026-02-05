import { IMeetingRoom, IBooking, IAvailabilitySlot, IResource } from '../models/IBookingModels';

export class MockBookingDataService {
  public static getMeetingRooms(): IMeetingRoom[] {
    return [
      {
        id: '1',
        name: 'Executive Boardroom',
        location: 'Building A, Floor 10',
        capacity: 20,
        description: 'Premium boardroom with city views and high-end AV equipment',
        amenities: ['Video Conferencing', 'Whiteboard', 'Coffee Station', 'Projector'],
        isAvailable: true,
        floor: '10',
        building: 'Building A',
        equipment: [
          {
            id: 'eq1',
            name: '4K Projector',
            type: 'AV',
            isAvailable: true,
            requiresSetup: true,
            setupTime: 15
          },
          {
            id: 'eq2',
            name: 'Conference Phone',
            type: 'AV',
            isAvailable: true,
            requiresSetup: false,
            setupTime: 0
          }
        ],
        accessibility: {
          wheelchairAccessible: true,
          hearingLoopAvailable: true,
          visualAidsSupport: true,
          accessibleParking: true
        },
        hourlyRate: 150,
        bookingPolicy: {
          maxBookingDuration: 8,
          advanceBookingLimit: 30,
          cancellationDeadline: 24,
          requiresApproval: true,
          allowRecurring: true
        }
      },
      {
        id: '2',
        name: 'Innovation Lab',
        location: 'Building B, Floor 3',
        capacity: 12,
        description: 'Modern collaboration space with interactive displays and flexible seating',
        amenities: ['Interactive Display', 'Moveable Furniture', 'High-speed WiFi', 'Phone Booth'],
        isAvailable: true,
        floor: '3',
        building: 'Building B',
        equipment: [
          {
            id: 'eq3',
            name: 'Interactive Whiteboard',
            type: 'AV',
            isAvailable: true,
            requiresSetup: false,
            setupTime: 0
          }
        ],
        accessibility: {
          wheelchairAccessible: true,
          hearingLoopAvailable: false,
          visualAidsSupport: true,
          accessibleParking: true
        },
        hourlyRate: 75,
        bookingPolicy: {
          maxBookingDuration: 4,
          advanceBookingLimit: 14,
          cancellationDeadline: 4,
          requiresApproval: false,
          allowRecurring: true
        }
      },
      {
        id: '3',
        name: 'Team Huddle Room',
        location: 'Building A, Floor 5',
        capacity: 6,
        description: 'Cozy space perfect for small team meetings and brainstorming',
        amenities: ['TV Display', 'Whiteboard', 'Conference Phone'],
        isAvailable: true,
        floor: '5',
        building: 'Building A',
        equipment: [
          {
            id: 'eq4',
            name: '55" TV Display',
            type: 'AV',
            isAvailable: true,
            requiresSetup: false,
            setupTime: 0
          }
        ],
        accessibility: {
          wheelchairAccessible: true,
          hearingLoopAvailable: false,
          visualAidsSupport: false,
          accessibleParking: false
        },
        hourlyRate: 25,
        bookingPolicy: {
          maxBookingDuration: 2,
          advanceBookingLimit: 7,
          cancellationDeadline: 2,
          requiresApproval: false,
          allowRecurring: true
        }
      },
      {
        id: '4',
        name: 'Training Center',
        location: 'Building C, Floor 1',
        capacity: 50,
        description: 'Large training facility with theater-style seating and full AV setup',
        amenities: ['Theater Seating', 'Sound System', 'Stage Area', 'Recording Equipment'],
        isAvailable: true,
        floor: '1',
        building: 'Building C',
        equipment: [
          {
            id: 'eq5',
            name: 'Professional Sound System',
            type: 'AV',
            isAvailable: true,
            requiresSetup: true,
            setupTime: 30
          },
          {
            id: 'eq6',
            name: 'Recording Equipment',
            type: 'AV',
            isAvailable: true,
            requiresSetup: true,
            setupTime: 45
          }
        ],
        accessibility: {
          wheelchairAccessible: true,
          hearingLoopAvailable: true,
          visualAidsSupport: true,
          accessibleParking: true
        },
        hourlyRate: 200,
        bookingPolicy: {
          maxBookingDuration: 8,
          advanceBookingLimit: 60,
          cancellationDeadline: 48,
          requiresApproval: true,
          allowRecurring: false
        }
      }
    ];
  }

  public static getBookings(): IBooking[] {
    const today = new Date();
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);

    return [
      {
        id: '1',
        roomId: '1',
        roomName: 'Executive Boardroom',
        title: 'Board Meeting',
        description: 'Monthly board meeting with all executives',
        startTime: new Date(today.getFullYear(), today.getMonth(), today.getDate(), 9, 0),
        endTime: new Date(today.getFullYear(), today.getMonth(), today.getDate(), 11, 0),
        organizer: 'ceo@company.com',
        attendees: ['cfo@company.com', 'cto@company.com', 'coo@company.com'],
        status: 'Confirmed',
        isRecurring: true,
        teamsLink: 'https://teams.microsoft.com/l/meetup-join/...',
        equipmentBooked: ['eq1', 'eq2'],
        createdDate: new Date('2024-01-01'),
        lastModified: new Date('2024-01-01')
      },
      {
        id: '2',
        roomId: '2',
        roomName: 'Innovation Lab',
        title: 'Product Design Workshop',
        description: 'Collaborative design session for new product features',
        startTime: new Date(today.getFullYear(), today.getMonth(), today.getDate(), 14, 0),
        endTime: new Date(today.getFullYear(), today.getMonth(), today.getDate(), 16, 0),
        organizer: 'designer@company.com',
        attendees: ['pm@company.com', 'dev1@company.com', 'dev2@company.com'],
        status: 'Confirmed',
        isRecurring: false,
        equipmentBooked: ['eq3'],
        createdDate: new Date('2024-01-10'),
        lastModified: new Date('2024-01-10')
      },
      {
        id: '3',
        roomId: '3',
        roomName: 'Team Huddle Room',
        title: 'Daily Standup',
        description: 'Daily team synchronization meeting',
        startTime: new Date(tomorrow.getFullYear(), tomorrow.getMonth(), tomorrow.getDate(), 9, 30),
        endTime: new Date(tomorrow.getFullYear(), tomorrow.getMonth(), tomorrow.getDate(), 10, 0),
        organizer: 'scrum@company.com',
        attendees: ['dev1@company.com', 'dev2@company.com', 'qa@company.com'],
        status: 'Confirmed',
        isRecurring: true,
        recurrencePattern: {
          type: 'Daily',
          interval: 1,
          daysOfWeek: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday']
        },
        equipmentBooked: ['eq4'],
        createdDate: new Date('2024-01-05'),
        lastModified: new Date('2024-01-05')
      }
    ];
  }

  public static async getAvailabilitySlots(roomId: string, date: Date): Promise<IAvailabilitySlot[]> {
    // Mock implementation - would check against actual bookings
    const slots: IAvailabilitySlot[] = [];
    const startHour = 8;
    const endHour = 18;
    
    for (let hour = startHour; hour < endHour; hour++) {
      const slotStart = new Date(date.getFullYear(), date.getMonth(), date.getDate(), hour, 0);
      const slotEnd = new Date(date.getFullYear(), date.getMonth(), date.getDate(), hour + 1, 0);
      
      // Check if slot conflicts with existing bookings
      const bookings = this.getBookings();
      const hasConflict = bookings.some(booking => {
        if (booking.roomId !== roomId) return false;
        const bookingStart = new Date(booking.startTime);
        const bookingEnd = new Date(booking.endTime);
        return slotStart < bookingEnd && slotEnd > bookingStart;
      });
      
      slots.push({
        start: slotStart,
        end: slotEnd,
        isAvailable: !hasConflict,
        isRestricted: hour < 9 || hour > 17 // Business hours restriction
      });
    }
    
    return slots;
  }

  public static async createBooking(booking: Partial<IBooking>): Promise<IBooking> {
    // Mock implementation - would integrate with SharePoint Lists or external API
    const newBooking: IBooking = {
      id: Math.random().toString(36).substr(2, 9),
      roomId: booking.roomId || '',
      roomName: booking.roomName || '',
      title: booking.title || '',
      description: booking.description,
      startTime: booking.startTime || new Date(),
      endTime: booking.endTime || new Date(),
      organizer: booking.organizer || 'user@company.com',
      attendees: booking.attendees || [],
      status: booking.status || 'Confirmed',
      isRecurring: booking.isRecurring || false,
      equipmentBooked: booking.equipmentBooked || [],
      createdDate: new Date(),
      lastModified: new Date(),
      teamsLink: booking.teamsLink
    };
    
    console.log('Creating booking:', newBooking);
    return newBooking;
  }

  public static async updateBooking(id: string, updates: Partial<IBooking>): Promise<void> {
    // Mock implementation - would integrate with SharePoint Lists or external API
    console.log(`Updating booking ${id}`, updates);
  }

  public static async cancelBooking(id: string, reason?: string): Promise<void> {
    // Mock implementation - would integrate with SharePoint Lists or external API
    console.log(`Cancelling booking ${id}`, reason);
  }

  public static async getMyBookings(userId: string): Promise<IBooking[]> {
    // Mock implementation - would filter by user
    return this.getBookings().filter(booking => booking.organizer === userId);
  }

  public static async getResourceUtilization(resourceId: string, startDate: Date, endDate: Date): Promise<any> {
    // Mock implementation - would calculate utilization metrics
    return {
      totalHours: 40,
      bookedHours: 28,
      utilizationRate: 0.7,
      peakHours: ['9:00-10:00', '14:00-15:00'],
      averageBookingDuration: 1.5
    };
  }
}