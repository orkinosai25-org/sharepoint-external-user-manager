import { IMeetingRoom, IBooking, IAvailabilitySlot } from '../models/IBookingModels';
export declare class MockBookingDataService {
    static getMeetingRooms(): IMeetingRoom[];
    static getBookings(): IBooking[];
    static getAvailabilitySlots(roomId: string, date: Date): Promise<IAvailabilitySlot[]>;
    static createBooking(booking: Partial<IBooking>): Promise<IBooking>;
    static updateBooking(id: string, updates: Partial<IBooking>): Promise<void>;
    static cancelBooking(id: string, reason?: string): Promise<void>;
    static getMyBookings(userId: string): Promise<IBooking[]>;
    static getResourceUtilization(resourceId: string, startDate: Date, endDate: Date): Promise<any>;
}
//# sourceMappingURL=MockBookingDataService.d.ts.map