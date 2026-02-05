/**
 * Tests for removeUser function
 */

import { RemoveUserRequest } from '../../models/user';
import { removeUserSchema } from '../../utils/validation';

describe('RemoveUser Function', () => {
  describe('Request Validation', () => {
    it('should accept valid remove user request', () => {
      const validRequest: RemoveUserRequest = {
        email: 'external@partner.com',
        library: 'https://contoso.sharepoint.com/sites/client1/Shared%20Documents'
      };

      const { error, value } = removeUserSchema.validate(validRequest);
      expect(error).toBeUndefined();
      expect(value.email).toBe(validRequest.email);
      expect(value.library).toBe(validRequest.library);
    });

    it('should reject request with invalid email', () => {
      const invalidRequest = {
        email: 'not-an-email',
        library: 'https://contoso.sharepoint.com/sites/client1/Shared%20Documents'
      };

      const { error } = removeUserSchema.validate(invalidRequest);
      expect(error).toBeDefined();
      expect(error?.message).toContain('email');
    });

    it('should reject request with invalid library URL', () => {
      const invalidRequest = {
        email: 'external@partner.com',
        library: 'not-a-url'
      };

      const { error } = removeUserSchema.validate(invalidRequest);
      expect(error).toBeDefined();
      expect(error?.message).toContain('uri');
    });

    it('should reject request missing required fields', () => {
      const missingEmail = {
        library: 'https://contoso.sharepoint.com/sites/client1/Shared%20Documents'
      };

      const missingLibrary = {
        email: 'external@partner.com'
      };

      const { error: emailError } = removeUserSchema.validate(missingEmail);
      const { error: libraryError } = removeUserSchema.validate(missingLibrary);

      expect(emailError).toBeDefined();
      expect(emailError?.message).toContain('email');

      expect(libraryError).toBeDefined();
      expect(libraryError?.message).toContain('library');
    });

    it('should accept various valid SharePoint library URLs', () => {
      const validUrls = [
        'https://contoso.sharepoint.com/sites/client1/Shared%20Documents',
        'https://contoso.sharepoint.com/sites/client1/Documents',
        'https://tenant.sharepoint.com/sites/project/Library',
        'https://company-my.sharepoint.com/personal/user/Documents'
      ];

      validUrls.forEach(url => {
        const request = {
          email: 'external@partner.com',
          library: url
        };

        const { error } = removeUserSchema.validate(request);
        expect(error).toBeUndefined();
      });
    });

    it('should normalize email addresses correctly', () => {
      const emails = [
        'External@Partner.com',
        'external@PARTNER.COM',
        'external@partner.com'
      ];

      emails.forEach(email => {
        const request = {
          email: email,
          library: 'https://contoso.sharepoint.com/sites/client1/Shared%20Documents'
        };

        const { error } = removeUserSchema.validate(request);
        expect(error).toBeUndefined();
      });
    });
  });

  describe('Response Format', () => {
    it('should return success message after removal', () => {
      const email = 'external@partner.com';
      const library = 'https://contoso.sharepoint.com/sites/client1/Shared%20Documents';
      
      const expectedMessage = `External user ${email} access removed from ${library}`;
      
      expect(expectedMessage).toContain(email);
      expect(expectedMessage).toContain(library);
      expect(expectedMessage).toContain('removed');
    });
  });
});
