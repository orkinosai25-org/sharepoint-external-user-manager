/**
 * Input validation schemas
 */

import Joi from 'joi';
import { ValidationError } from '../models/common';

export const tenantOnboardSchema = Joi.object({
  organizationName: Joi.string().min(1).max(255).required(),
  primaryAdminEmail: Joi.string().email().required(),
  settings: Joi.object({
    timezone: Joi.string().optional(),
    locale: Joi.string().optional(),
    region: Joi.string().optional(),
    customDomain: Joi.string().optional()
  }).optional()
});

export const inviteUserSchema = Joi.object({
  email: Joi.string().email().required(),
  displayName: Joi.string().min(1).max(255).required(),
  library: Joi.string().uri().required(),
  permissions: Joi.string().valid('Read', 'Contribute', 'Edit', 'FullControl').required(),
  message: Joi.string().max(1000).optional(),
  metadata: Joi.object({
    company: Joi.string().max(255).optional(),
    project: Joi.string().max(255).optional(),
    department: Joi.string().max(255).optional(),
    notes: Joi.string().max(1000).optional()
  }).optional()
});

export const removeUserSchema = Joi.object({
  email: Joi.string().email().required(),
  library: Joi.string().uri().required()
});

export const updatePolicySchema = Joi.object({
  policyType: Joi.string().valid(
    'ExternalSharingDefault',
    'GuestExpiration',
    'RequireApproval',
    'AllowedDomains',
    'ReviewCampaigns'
  ).required(),
  enabled: Joi.boolean().required(),
  configuration: Joi.object().required()
});

export const listUsersQuerySchema = Joi.object({
  library: Joi.string().uri().optional(),
  status: Joi.string().valid('Active', 'Invited', 'Expired', 'Removed').optional(),
  email: Joi.string().email().optional(),
  company: Joi.string().max(255).optional(),
  project: Joi.string().max(255).optional(),
  page: Joi.number().integer().min(1).default(1),
  pageSize: Joi.number().integer().min(1).max(100).default(50)
});

export const auditLogQuerySchema = Joi.object({
  action: Joi.string().optional(),
  userId: Joi.string().optional(),
  startDate: Joi.date().iso().optional(),
  endDate: Joi.date().iso().optional(),
  page: Joi.number().integer().min(1).default(1),
  pageSize: Joi.number().integer().min(1).max(100).default(50)
});

export const createClientSchema = Joi.object({
  clientName: Joi.string().min(1).max(255).required(),
  siteTemplate: Joi.string().valid('Team', 'Communication').optional().default('Team')
});

export const createLibrarySchema = Joi.object({
  name: Joi.string().min(1).max(255).required(),
  description: Joi.string().max(1000).optional()
});

export const createListSchema = Joi.object({
  name: Joi.string().min(1).max(255).required(),
  description: Joi.string().max(1000).optional(),
  template: Joi.string().valid('genericList', 'documentLibrary', 'survey', 'links', 'announcements', 'contacts', 'events', 'tasks', 'issueTracking', 'customList').optional().default('genericList')
});

export function validate<T>(data: any, schema: Joi.Schema): T {
  const { error, value } = schema.validate(data, {
    abortEarly: false,
    stripUnknown: true
  });

  if (error) {
    const details = error.details.map((d: any) => d.message).join('; ');
    throw new ValidationError('Validation failed', details);
  }

  return value as T;
}

export function validateQuery<T>(query: any, schema: Joi.Schema): T {
  return validate<T>(query, schema);
}

export function validateBody<T>(body: any, schema: Joi.Schema): T {
  return validate<T>(body, schema);
}
