/**
 * Tests for library and list creation validation schemas
 */

import { CreateLibraryRequest, CreateListRequest } from '../../models/client';
import { createLibrarySchema, createListSchema } from '../../utils/validation';

describe('Library and List Creation Validation', () => {
  describe('Create Library Schema', () => {
    it('should accept valid library creation request', () => {
      const validRequest: CreateLibraryRequest = {
        name: 'Client Documents',
        description: 'Documents for the client project'
      };

      const { error, value } = createLibrarySchema.validate(validRequest);
      expect(error).toBeUndefined();
      expect(value.name).toBe(validRequest.name);
      expect(value.description).toBe(validRequest.description);
    });

    it('should accept library creation request without description', () => {
      const minimalRequest: CreateLibraryRequest = {
        name: 'Client Documents'
      };

      const { error, value } = createLibrarySchema.validate(minimalRequest);
      expect(error).toBeUndefined();
      expect(value.name).toBe(minimalRequest.name);
      expect(value.description).toBeUndefined();
    });

    it('should reject library creation request without name', () => {
      const invalidRequest = {
        description: 'Documents for the client project'
      };

      const { error } = createLibrarySchema.validate(invalidRequest);
      expect(error).toBeDefined();
      expect(error?.message).toContain('name');
    });

    it('should reject library creation request with empty name', () => {
      const invalidRequest = {
        name: '',
        description: 'Documents for the client project'
      };

      const { error } = createLibrarySchema.validate(invalidRequest);
      expect(error).toBeDefined();
      expect(error?.message).toContain('name');
    });

    it('should reject library creation request with name exceeding max length', () => {
      const invalidRequest = {
        name: 'A'.repeat(256),
        description: 'Documents for the client project'
      };

      const { error } = createLibrarySchema.validate(invalidRequest);
      expect(error).toBeDefined();
      expect(error?.message).toContain('name');
    });

    it('should reject library creation request with description exceeding max length', () => {
      const invalidRequest = {
        name: 'Client Documents',
        description: 'A'.repeat(1001)
      };

      const { error } = createLibrarySchema.validate(invalidRequest);
      expect(error).toBeDefined();
      expect(error?.message).toContain('description');
    });
  });

  describe('Create List Schema', () => {
    it('should accept valid list creation request', () => {
      const validRequest: CreateListRequest = {
        name: 'Project Tasks',
        description: 'Task list for the client project',
        template: 'tasks'
      };

      const { error, value } = createListSchema.validate(validRequest);
      expect(error).toBeUndefined();
      expect(value.name).toBe(validRequest.name);
      expect(value.description).toBe(validRequest.description);
      expect(value.template).toBe(validRequest.template);
    });

    it('should accept list creation request without description', () => {
      const minimalRequest: CreateListRequest = {
        name: 'Project Tasks'
      };

      const { error, value } = createListSchema.validate(minimalRequest);
      expect(error).toBeUndefined();
      expect(value.name).toBe(minimalRequest.name);
      expect(value.description).toBeUndefined();
    });

    it('should use default template if not provided', () => {
      const requestWithoutTemplate: CreateListRequest = {
        name: 'Project Tasks',
        description: 'Task list for the client project'
      };

      const { error, value } = createListSchema.validate(requestWithoutTemplate);
      expect(error).toBeUndefined();
      expect(value.template).toBe('genericList');
    });

    it('should accept valid list templates', () => {
      const validTemplates = [
        'genericList',
        'documentLibrary',
        'survey',
        'links',
        'announcements',
        'contacts',
        'events',
        'tasks',
        'issueTracking',
        'customList'
      ];

      validTemplates.forEach(template => {
        const request = {
          name: 'Test List',
          template: template
        };

        const { error } = createListSchema.validate(request);
        expect(error).toBeUndefined();
      });
    });

    it('should reject list creation request without name', () => {
      const invalidRequest = {
        description: 'Task list for the client project',
        template: 'tasks'
      };

      const { error } = createListSchema.validate(invalidRequest);
      expect(error).toBeDefined();
      expect(error?.message).toContain('name');
    });

    it('should reject list creation request with empty name', () => {
      const invalidRequest = {
        name: '',
        description: 'Task list for the client project',
        template: 'tasks'
      };

      const { error } = createListSchema.validate(invalidRequest);
      expect(error).toBeDefined();
      expect(error?.message).toContain('name');
    });

    it('should reject list creation request with invalid template', () => {
      const invalidRequest = {
        name: 'Project Tasks',
        description: 'Task list for the client project',
        template: 'invalidTemplate'
      };

      const { error } = createListSchema.validate(invalidRequest);
      expect(error).toBeDefined();
      expect(error?.message).toContain('must be one of');
    });

    it('should reject list creation request with name exceeding max length', () => {
      const invalidRequest = {
        name: 'A'.repeat(256),
        description: 'Task list for the client project'
      };

      const { error } = createListSchema.validate(invalidRequest);
      expect(error).toBeDefined();
      expect(error?.message).toContain('name');
    });

    it('should reject list creation request with description exceeding max length', () => {
      const invalidRequest = {
        name: 'Project Tasks',
        description: 'A'.repeat(1001)
      };

      const { error } = createListSchema.validate(invalidRequest);
      expect(error).toBeDefined();
      expect(error?.message).toContain('description');
    });
  });
});
