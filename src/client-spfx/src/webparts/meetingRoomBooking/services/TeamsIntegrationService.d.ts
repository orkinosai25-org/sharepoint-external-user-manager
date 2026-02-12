export interface ITeamsMeetingRequest {
    subject: string;
    startTime: Date;
    endTime: Date;
    attendees: string[];
    description?: string;
    isOnlineMeeting?: boolean;
}
export interface ITeamsMeetingResponse {
    meetingId: string;
    joinUrl: string;
    dialInNumbers: string[];
    conferenceId: string;
    organizerUrl?: string;
}
export declare class TeamsIntegrationService {
    /**
     * Create a Teams meeting for a room booking
     */
    static createTeamsMeeting(request: ITeamsMeetingRequest): Promise<ITeamsMeetingResponse>;
    /**
     * Update an existing Teams meeting
     */
    static updateTeamsMeeting(meetingId: string, updates: Partial<ITeamsMeetingRequest>): Promise<ITeamsMeetingResponse>;
    /**
     * Cancel a Teams meeting
     */
    static cancelTeamsMeeting(meetingId: string): Promise<void>;
    /**
     * Get Teams meeting details
     */
    static getTeamsMeetingDetails(meetingId: string): Promise<ITeamsMeetingResponse>;
    /**
     * Add attendees to an existing Teams meeting
     */
    static addAttendees(meetingId: string, attendees: string[]): Promise<void>;
    /**
     * Get Teams meeting recording details
     */
    static getMeetingRecording(meetingId: string): Promise<any>;
    /**
     * Configure Teams room for hybrid meetings
     */
    static configureTeamsRoom(roomId: string, configuration: any): Promise<void>;
    /**
     * Generate a mock Teams meeting for development
     */
    private static getMockTeamsMeeting;
    /**
     * Process real Microsoft Graph meeting response
     */
    private static processMeetingResponse;
    /**
     * Validate Teams meeting permissions
     */
    static validatePermissions(): Promise<boolean>;
    /**
     * Get Teams application status for a room
     */
    static getTeamsRoomStatus(roomId: string): Promise<any>;
}
//# sourceMappingURL=TeamsIntegrationService.d.ts.map