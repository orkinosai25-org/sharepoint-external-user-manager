/**
 * Tests for inviteUser function
 */

import { InviteUserRequest, ExternalUserResponse, PermissionLevel } from '../../models/user';
import { inviteUserSchema } from '../../utils/validation';
import Joi from 'joi';

describe('InviteUser Function', () => {
  describe('Request Validation', () => {
    it('should accept valid invite user request', () => {
      const validRequest: InviteUserRequest = {
        email: 'external@partner.com',
        displayName: 'John External',
        library: 'https://contoso.sharepoint.com/sites/client1/Shared%20Documents',
        permissions: 'Read',
        message: 'Welcome to the client space',
        metadata: {
          company: 'Partner Corp',
          project: 'Q1 Campaign'
        }
      };

      const { error, value } = inviteUserSchema.validate(validRequest);
      expect(error).toBeUndefined();
      expect(value.email).toBe(validRequest.email);
      expect(value.displayName).toBe(validRequest.displayName);
      expect(value.library).toBe(validRequest.library);
      expect(value.permissions).toBe(validRequest.permissions);
    });

    it('should reject request with invalid email', () => {
      const invalidRequest = {
        email: 'not-an-email',
        displayName: 'John External',
        library: 'https://contoso.sharepoint.com/sites/client1/Shared%20Documents',
        permissions: 'Read'
      };

      const { error } = inviteUserSchema.validate(invalidRequest);
      expect(error).toBeDefined();
      expect(error?.message).toContain('email');
    });

    it('should reject request with invalid library URL', () => {
      const invalidRequest = {
        email: 'external@partner.com',
        displayName: 'John External',
        library: 'not-a-url',
        permissions: 'Read'
      };

      const { error } = inviteUserSchema.validate(invalidRequest);
      expect(error).toBeDefined();
      expect(error?.message).toContain('uri');
    });

    it('should reject request with invalid permission level', () => {
      const invalidRequest = {
        email: 'external@partner.com',
        displayName: 'John External',
        library: 'https://contoso.sharepoint.com/sites/client1/Shared%20Documents',
        permissions: 'InvalidPermission'
      };

      const { error } = inviteUserSchema.validate(invalidRequest);
      expect(error).toBeDefined();
      expect(error?.message).toContain('must be one of');
    });

    it('should accept valid permission levels', () => {
      const permissionLevels: PermissionLevel[] = ['Read', 'Contribute', 'Edit', 'FullControl'];

      permissionLevels.forEach(permission => {
        const request = {
          email: 'external@partner.com',
          displayName: 'John External',
          library: 'https://contoso.sharepoint.com/sites/client1/Shared%20Documents',
          permissions: permission
        };

        const { error } = inviteUserSchema.validate(request);
        expect(error).toBeUndefined();
      });
    });

    it('should accept request without optional message and metadata', () => {
      const minimalRequest = {
        email: 'external@partner.com',
        displayName: 'John External',
        library: 'https://contoso.sharepoint.com/sites/client1/Shared%20Documents',
        permissions: 'Read'
      };

      const { error, value } = inviteUserSchema.validate(minimalRequest);
      expect(error).toBeUndefined();
      expect(value.message).toBeUndefined();
      expect(value.metadata).toBeUndefined();
    });

    it('should validate metadata structure when provided', () => {
      const requestWithMetadata = {
        email: 'external@partner.com',
        displayName: 'John External',
        library: 'https://contoso.sharepoint.com/sites/client1/Shared%20Documents',
        permissions: 'Read',
        metadata: {
          company: 'Partner Corp',
          project: 'Q1 Campaign',
          department: 'Sales',
          notes: 'Special access for Q1 project'
        }
      };

      const { error, value } = inviteUserSchema.validate(requestWithMetadata);
      expect(error).toBeUndefined();
      expect(value.metadata).toBeDefined();
      expect(value.metadata?.company).toBe('Partner Corp');
      expect(value.metadata?.project).toBe('Q1 Campaign');
    });

    it('should reject message that is too long', () => {
      const requestWithLongMessage = {
        email: 'external@partner.com',
        displayName: 'John External',
        library: 'https://contoso.sharepoint.com/sites/client1/Shared%20Documents',
        permissions: 'Read',
        message: 'a'.repeat(1001) // exceeds 1000 char limit
      };

      const { error } = inviteUserSchema.validate(requestWithLongMessage);
      expect(error).toBeDefined();
      expect(error?.message).toContain('length');
    });
  });

  describe('Response Format', () => {
    it('should format external user response correctly', () => {
      const mockResponse: ExternalUserResponse = {
        id: 'mock-user-123',
        email: 'external@partner.com',
        displayName: 'John External',
        library: 'https://contoso.sharepoint.com/sites/client1/Shared%20Documents',
        permissions: 'Read',
        invitedBy: 'admin@contoso.com',
        invitedDate: '2024-01-15T10:00:00.000Z',
        lastAccess: null,
        status: 'Invited',
        metadata: {
          company: 'Partner Corp',
          project: 'Q1 Campaign'
        }
      };

      expect(mockResponse.id).toBeDefined();
      expect(mockResponse.email).toBe('external@partner.com');
      expect(mockResponse.displayName).toBe('John External');
      expect(mockResponse.status).toBe('Invited');
      expect(mockResponse.invitedDate).toMatch(/^\d{4}-\d{2}-\d{2}T/);
    });
  });
});
