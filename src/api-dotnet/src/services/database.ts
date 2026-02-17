/**
 * Database service for tenant-scoped queries
 */

import * as sql from 'mssql';
import { config } from '../utils/config';
import { Tenant } from '../models/tenant';
import { Subscription } from '../models/subscription';
import { Policy } from '../models/policy';
import { AuditLog, CreateAuditLogEntry } from '../models/audit';
import { Client } from '../models/client';
import { TenantAuth, OAuthTokenResponse } from '../models/tenant-auth';

class DatabaseService {
  private pool: sql.ConnectionPool | null = null;

  async connect(): Promise<void> {
    if (this.pool) {
      return;
    }

    try {
      this.pool = await new sql.ConnectionPool(config.database).connect();
      console.log('Database connected successfully');
    } catch (error) {
      console.error('Database connection failed:', error);
      throw error;
    }
  }

  async disconnect(): Promise<void> {
    if (this.pool) {
      await this.pool.close();
      this.pool = null;
      console.log('Database disconnected');
    }
  }

  private ensureConnected(): sql.ConnectionPool {
    if (!this.pool) {
      throw new Error('Database not connected. Call connect() first.');
    }
    return this.pool;
  }

  // Tenant operations
  async getTenantByEntraId(entraIdTenantId: string): Promise<Tenant | null> {
    const pool = this.ensureConnected();
    const result = await pool.request()
      .input('entraIdTenantId', sql.NVarChar, entraIdTenantId)
      .query(`
        SELECT Id as id, EntraIdTenantId as entraIdTenantId, 
               OrganizationName as organizationName, PrimaryAdminEmail as primaryAdminEmail,
               OnboardedDate as onboardedDate, Status as status, 
               Settings as settings, CreatedDate as createdDate, ModifiedDate as modifiedDate
        FROM [dbo].[Tenant]
        WHERE EntraIdTenantId = @entraIdTenantId
      `);

    if (result.recordset.length === 0) {
      return null;
    }

    const row = result.recordset[0];
    return {
      ...row,
      settings: row.settings ? JSON.parse(row.settings) : null
    };
  }

  async getTenantById(tenantId: number): Promise<Tenant | null> {
    const pool = this.ensureConnected();
    const result = await pool.request()
      .input('tenantId', sql.Int, tenantId)
      .query(`
        SELECT Id as id, EntraIdTenantId as entraIdTenantId, 
               OrganizationName as organizationName, PrimaryAdminEmail as primaryAdminEmail,
               OnboardedDate as onboardedDate, Status as status, 
               Settings as settings, CreatedDate as createdDate, ModifiedDate as modifiedDate
        FROM [dbo].[Tenant]
        WHERE Id = @tenantId
      `);

    if (result.recordset.length === 0) {
      return null;
    }

    const row = result.recordset[0];
    return {
      ...row,
      settings: row.settings ? JSON.parse(row.settings) : null
    };
  }

  async createTenant(tenant: Omit<Tenant, 'id' | 'createdDate' | 'modifiedDate'>): Promise<Tenant> {
    const pool = this.ensureConnected();
    const result = await pool.request()
      .input('entraIdTenantId', sql.NVarChar, tenant.entraIdTenantId)
      .input('organizationName', sql.NVarChar, tenant.organizationName)
      .input('primaryAdminEmail', sql.NVarChar, tenant.primaryAdminEmail)
      .input('status', sql.NVarChar, tenant.status)
      .input('settings', sql.NVarChar, tenant.settings ? JSON.stringify(tenant.settings) : null)
      .query(`
        INSERT INTO [dbo].[Tenant] 
          (EntraIdTenantId, OrganizationName, PrimaryAdminEmail, Status, Settings, OnboardedDate)
        OUTPUT INSERTED.Id, INSERTED.CreatedDate, INSERTED.ModifiedDate
        VALUES (@entraIdTenantId, @organizationName, @primaryAdminEmail, @status, @settings, GETUTCDATE())
      `);

    const inserted = result.recordset[0];
    return {
      ...tenant,
      id: inserted.Id,
      createdDate: inserted.CreatedDate,
      modifiedDate: inserted.ModifiedDate
    };
  }

  // Subscription operations
  async getSubscriptionByTenantId(tenantId: number): Promise<Subscription | null> {
    const pool = this.ensureConnected();
    const result = await pool.request()
      .input('tenantId', sql.Int, tenantId)
      .query(`
        SELECT Id as id, TenantId as tenantId, Tier as tier, 
               StartDate as startDate, EndDate as endDate,
               TrialExpiry as trialExpiry, GracePeriodEnd as gracePeriodEnd,
               Status as status, MaxUsers as maxUsers, Features as features,
               CreatedDate as createdDate, ModifiedDate as modifiedDate
        FROM [dbo].[Subscription]
        WHERE TenantId = @tenantId
        ORDER BY CreatedDate DESC
      `);

    if (result.recordset.length === 0) {
      return null;
    }

    const row = result.recordset[0];
    return {
      ...row,
      features: row.features ? JSON.parse(row.features) : {}
    };
  }

  async createSubscription(subscription: Omit<Subscription, 'id' | 'createdDate' | 'modifiedDate'>): Promise<Subscription> {
    const pool = this.ensureConnected();
    const result = await pool.request()
      .input('tenantId', sql.Int, subscription.tenantId)
      .input('tier', sql.NVarChar, subscription.tier)
      .input('startDate', sql.DateTime2, subscription.startDate)
      .input('endDate', sql.DateTime2, subscription.endDate || null)
      .input('trialExpiry', sql.DateTime2, subscription.trialExpiry || null)
      .input('gracePeriodEnd', sql.DateTime2, subscription.gracePeriodEnd || null)
      .input('status', sql.NVarChar, subscription.status)
      .input('maxUsers', sql.Int, subscription.maxUsers)
      .input('features', sql.NVarChar, JSON.stringify(subscription.features))
      .query(`
        INSERT INTO [dbo].[Subscription]
          (TenantId, Tier, StartDate, EndDate, TrialExpiry, GracePeriodEnd, Status, MaxUsers, Features)
        OUTPUT INSERTED.Id, INSERTED.CreatedDate, INSERTED.ModifiedDate
        VALUES (@tenantId, @tier, @startDate, @endDate, @trialExpiry, @gracePeriodEnd, @status, @maxUsers, @features)
      `);

    const inserted = result.recordset[0];
    return {
      ...subscription,
      id: inserted.Id,
      createdDate: inserted.CreatedDate,
      modifiedDate: inserted.ModifiedDate
    };
  }

  // Policy operations
  async getPoliciesByTenantId(tenantId: number): Promise<Policy[]> {
    const pool = this.ensureConnected();
    const result = await pool.request()
      .input('tenantId', sql.Int, tenantId)
      .query(`
        SELECT Id as id, TenantId as tenantId, PolicyType as policyType,
               Enabled as enabled, Configuration as configuration,
               CreatedDate as createdDate, ModifiedDate as modifiedDate
        FROM [dbo].[Policy]
        WHERE TenantId = @tenantId
        ORDER BY PolicyType
      `);

    return result.recordset.map((row: any) => ({
      ...row,
      configuration: row.configuration ? JSON.parse(row.configuration) : {}
    }));
  }

  async updatePolicy(tenantId: number, policyType: string, enabled: boolean, configuration: any): Promise<Policy> {
    const pool = this.ensureConnected();
    const result = await pool.request()
      .input('tenantId', sql.Int, tenantId)
      .input('policyType', sql.NVarChar, policyType)
      .input('enabled', sql.Bit, enabled)
      .input('configuration', sql.NVarChar, JSON.stringify(configuration))
      .query(`
        UPDATE [dbo].[Policy]
        SET Enabled = @enabled, Configuration = @configuration, ModifiedDate = GETUTCDATE()
        OUTPUT INSERTED.Id, INSERTED.TenantId, INSERTED.PolicyType, 
               INSERTED.Enabled, INSERTED.Configuration, INSERTED.CreatedDate, INSERTED.ModifiedDate
        WHERE TenantId = @tenantId AND PolicyType = @policyType
        
        IF @@ROWCOUNT = 0
        BEGIN
          INSERT INTO [dbo].[Policy] (TenantId, PolicyType, Enabled, Configuration)
          OUTPUT INSERTED.Id, INSERTED.TenantId, INSERTED.PolicyType, 
                 INSERTED.Enabled, INSERTED.Configuration, INSERTED.CreatedDate, INSERTED.ModifiedDate
          VALUES (@tenantId, @policyType, @enabled, @configuration)
        END
      `);

    const row = result.recordset[0];
    return {
      ...row,
      configuration: row.Configuration ? JSON.parse(row.Configuration) : {}
    };
  }

  // Audit log operations
  async createAuditLog(entry: CreateAuditLogEntry): Promise<void> {
    const pool = this.ensureConnected();
    await pool.request()
      .input('tenantId', sql.Int, entry.tenantId)
      .input('userId', sql.NVarChar, entry.userId)
      .input('userEmail', sql.NVarChar, entry.userEmail)
      .input('action', sql.NVarChar, entry.action)
      .input('resourceType', sql.NVarChar, entry.resourceType)
      .input('resourceId', sql.NVarChar, entry.resourceId)
      .input('details', sql.NVarChar, entry.details ? JSON.stringify(entry.details) : null)
      .input('ipAddress', sql.NVarChar, entry.ipAddress)
      .input('correlationId', sql.NVarChar, entry.correlationId)
      .input('status', sql.NVarChar, entry.status)
      .query(`
        INSERT INTO [dbo].[AuditLog]
          (TenantId, UserId, UserEmail, Action, ResourceType, ResourceId, Details, IpAddress, CorrelationId, Status, Timestamp)
        VALUES (@tenantId, @userId, @userEmail, @action, @resourceType, @resourceId, @details, @ipAddress, @correlationId, @status, GETUTCDATE())
      `);
  }

  async getAuditLogs(
    tenantId: number,
    filters: {
      action?: string;
      userId?: string;
      startDate?: Date;
      endDate?: Date;
      page: number;
      pageSize: number;
    }
  ): Promise<{ logs: AuditLog[]; total: number }> {
    const pool = this.ensureConnected();
    const offset = (filters.page - 1) * filters.pageSize;

    let whereClause = 'WHERE TenantId = @tenantId';
    const request = pool.request().input('tenantId', sql.Int, tenantId);

    if (filters.action) {
      whereClause += ' AND Action = @action';
      request.input('action', sql.NVarChar, filters.action);
    }

    if (filters.userId) {
      whereClause += ' AND UserId = @userId';
      request.input('userId', sql.NVarChar, filters.userId);
    }

    if (filters.startDate) {
      whereClause += ' AND Timestamp >= @startDate';
      request.input('startDate', sql.DateTime2, filters.startDate);
    }

    if (filters.endDate) {
      whereClause += ' AND Timestamp <= @endDate';
      request.input('endDate', sql.DateTime2, filters.endDate);
    }

    // Get total count
    const countResult = await request.query(`
      SELECT COUNT(*) as total FROM [dbo].[AuditLog] ${whereClause}
    `);
    const total = countResult.recordset[0].total;

    // Get paginated logs
    request
      .input('offset', sql.Int, offset)
      .input('pageSize', sql.Int, filters.pageSize);

    const result = await request.query(`
      SELECT Id as id, TenantId as tenantId, Timestamp as timestamp,
             UserId as userId, UserEmail as userEmail, Action as action,
             ResourceType as resourceType, ResourceId as resourceId,
             Details as details, IpAddress as ipAddress, CorrelationId as correlationId,
             Status as status
      FROM [dbo].[AuditLog]
      ${whereClause}
      ORDER BY Timestamp DESC
      OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY
    `);

    const logs = result.recordset.map((row: any) => ({
      ...row,
      details: row.details ? JSON.parse(row.details) : {}
    }));

    return { logs, total };
  }

  // Client operations
  async createClient(client: Omit<Client, 'id' | 'createdAt'>): Promise<Client> {
    const pool = this.ensureConnected();
    const result = await pool.request()
      .input('tenantId', sql.Int, client.tenantId)
      .input('clientName', sql.NVarChar, client.clientName)
      .input('siteUrl', sql.NVarChar, client.siteUrl)
      .input('siteId', sql.NVarChar, client.siteId)
      .input('createdBy', sql.NVarChar, client.createdBy)
      .input('status', sql.NVarChar, client.status)
      .query(`
        INSERT INTO [dbo].[Client]
          (TenantId, ClientName, SiteUrl, SiteId, CreatedBy, Status)
        OUTPUT INSERTED.Id, INSERTED.CreatedAt
        VALUES (@tenantId, @clientName, @siteUrl, @siteId, @createdBy, @status)
      `);

    const inserted = result.recordset[0];
    return {
      ...client,
      id: inserted.Id,
      createdAt: inserted.CreatedAt
    };
  }

  async getClientById(tenantId: number, clientId: number): Promise<Client | null> {
    const pool = this.ensureConnected();
    const result = await pool.request()
      .input('tenantId', sql.Int, tenantId)
      .input('clientId', sql.Int, clientId)
      .query(`
        SELECT Id as id, TenantId as tenantId, ClientName as clientName,
               SiteUrl as siteUrl, SiteId as siteId, CreatedBy as createdBy,
               CreatedAt as createdAt, Status as status, ErrorMessage as errorMessage
        FROM [dbo].[Client]
        WHERE Id = @clientId AND TenantId = @tenantId
      `);

    if (result.recordset.length === 0) {
      return null;
    }

    return result.recordset[0];
  }

  async getClientsByTenantId(tenantId: number): Promise<Client[]> {
    const pool = this.ensureConnected();
    const result = await pool.request()
      .input('tenantId', sql.Int, tenantId)
      .query(`
        SELECT Id as id, TenantId as tenantId, ClientName as clientName,
               SiteUrl as siteUrl, SiteId as siteId, CreatedBy as createdBy,
               CreatedAt as createdAt, Status as status, ErrorMessage as errorMessage
        FROM [dbo].[Client]
        WHERE TenantId = @tenantId
        ORDER BY CreatedAt DESC
      `);

    return result.recordset;
  }

  async updateClientStatus(
    tenantId: number,
    clientId: number,
    status: 'Provisioning' | 'Active' | 'Error',
    siteUrl?: string,
    siteId?: string,
    errorMessage?: string
  ): Promise<void> {
    const pool = this.ensureConnected();
    const request = pool.request()
      .input('tenantId', sql.Int, tenantId)
      .input('clientId', sql.Int, clientId)
      .input('status', sql.NVarChar, status);

    let updateFields = 'Status = @status';
    
    if (siteUrl !== undefined) {
      request.input('siteUrl', sql.NVarChar, siteUrl);
      updateFields += ', SiteUrl = @siteUrl';
    }
    
    if (siteId !== undefined) {
      request.input('siteId', sql.NVarChar, siteId);
      updateFields += ', SiteId = @siteId';
    }
    
    if (errorMessage !== undefined) {
      request.input('errorMessage', sql.NVarChar, errorMessage);
      updateFields += ', ErrorMessage = @errorMessage';
    }

    await request.query(`
      UPDATE [dbo].[Client]
      SET ${updateFields}
      WHERE Id = @clientId AND TenantId = @tenantId
    `);
  }

  async getClientCount(tenantId: number): Promise<number> {
    const pool = this.ensureConnected();
    const result = await pool.request()
      .input('tenantId', sql.Int, tenantId)
      .query(`
        SELECT COUNT(*) as count
        FROM [dbo].[Client]
        WHERE TenantId = @tenantId
      `);
    
    if (!result.recordset || result.recordset.length === 0) {
      return 0;
    }
    return result.recordset[0].count;
  }

  async getExternalUserCount(tenantId: number): Promise<number> {
    const pool = this.ensureConnected();
    const result = await pool.request()
      .input('tenantId', sql.Int, tenantId)
      .query(`
        SELECT COUNT(*) as count
        FROM [dbo].[ExternalUser]
        WHERE TenantId = @tenantId
      `);
    
    if (!result.recordset || result.recordset.length === 0) {
      return 0;
    }
    return result.recordset[0].count;
  }

  // Subscription update operations
  async updateSubscription(subscriptionId: number, updates: Partial<Subscription>): Promise<Subscription> {
    const pool = this.ensureConnected();
    const request = pool.request()
      .input('subscriptionId', sql.Int, subscriptionId);

    const updateFields: string[] = ['ModifiedDate = GETUTCDATE()'];
    
    if (updates.tier !== undefined) {
      request.input('tier', sql.NVarChar, updates.tier);
      updateFields.push('Tier = @tier');
    }
    
    if (updates.status !== undefined) {
      request.input('status', sql.NVarChar, updates.status);
      updateFields.push('Status = @status');
    }
    
    if (updates.startDate !== undefined) {
      request.input('startDate', sql.DateTime2, updates.startDate);
      updateFields.push('StartDate = @startDate');
    }
    
    if (updates.endDate !== undefined) {
      request.input('endDate', sql.DateTime2, updates.endDate);
      updateFields.push('EndDate = @endDate');
    }
    
    if (updates.trialExpiry !== undefined) {
      request.input('trialExpiry', sql.DateTime2, updates.trialExpiry);
      updateFields.push('TrialExpiry = @trialExpiry');
    }
    
    if (updates.gracePeriodEnd !== undefined) {
      request.input('gracePeriodEnd', sql.DateTime2, updates.gracePeriodEnd);
      updateFields.push('GracePeriodEnd = @gracePeriodEnd');
    }
    
    if (updates.maxUsers !== undefined) {
      request.input('maxUsers', sql.Int, updates.maxUsers);
      updateFields.push('MaxUsers = @maxUsers');
    }

    // Add Stripe fields
    if (updates.stripeCustomerId !== undefined) {
      request.input('stripeCustomerId', sql.NVarChar, updates.stripeCustomerId);
      updateFields.push('StripeCustomerId = @stripeCustomerId');
    }

    if (updates.stripeSubscriptionId !== undefined) {
      request.input('stripeSubscriptionId', sql.NVarChar, updates.stripeSubscriptionId);
      updateFields.push('StripeSubscriptionId = @stripeSubscriptionId');
    }

    if (updates.stripePriceId !== undefined) {
      request.input('stripePriceId', sql.NVarChar, updates.stripePriceId);
      updateFields.push('StripePriceId = @stripePriceId');
    }

    const result = await request.query(`
      UPDATE [dbo].[Subscription]
      SET ${updateFields.join(', ')}
      OUTPUT INSERTED.Id, INSERTED.TenantId, INSERTED.Tier, 
             INSERTED.StartDate, INSERTED.EndDate, INSERTED.TrialExpiry,
             INSERTED.GracePeriodEnd, INSERTED.Status, INSERTED.MaxUsers,
             INSERTED.Features, INSERTED.CreatedDate, INSERTED.ModifiedDate,
             INSERTED.StripeCustomerId, INSERTED.StripeSubscriptionId, INSERTED.StripePriceId
      WHERE Id = @subscriptionId
    `);

    if (result.recordset.length === 0) {
      throw new Error('Subscription not found');
    }

    const row = result.recordset[0];
    return {
      id: row.Id,
      tenantId: row.TenantId,
      tier: row.Tier,
      startDate: row.StartDate,
      endDate: row.EndDate,
      trialExpiry: row.TrialExpiry,
      gracePeriodEnd: row.GracePeriodEnd,
      status: row.Status,
      maxUsers: row.MaxUsers,
      features: row.Features ? JSON.parse(row.Features) : {},
      createdDate: row.CreatedDate,
      modifiedDate: row.ModifiedDate,
      stripeCustomerId: row.StripeCustomerId,
      stripeSubscriptionId: row.StripeSubscriptionId,
      stripePriceId: row.StripePriceId
    };
  }

  async getSubscriptionByStripeCustomerId(stripeCustomerId: string): Promise<Subscription | null> {
    const pool = this.ensureConnected();
    const result = await pool.request()
      .input('stripeCustomerId', sql.NVarChar, stripeCustomerId)
      .query(`
        SELECT Id as id, TenantId as tenantId, Tier as tier, 
               StartDate as startDate, EndDate as endDate,
               TrialExpiry as trialExpiry, GracePeriodEnd as gracePeriodEnd,
               Status as status, MaxUsers as maxUsers, Features as features,
               CreatedDate as createdDate, ModifiedDate as modifiedDate,
               StripeCustomerId as stripeCustomerId, StripeSubscriptionId as stripeSubscriptionId,
               StripePriceId as stripePriceId
        FROM [dbo].[Subscription]
        WHERE StripeCustomerId = @stripeCustomerId
        ORDER BY CreatedDate DESC
      `);

    if (result.recordset.length === 0) {
      return null;
    }

    const row = result.recordset[0];
    return {
      ...row,
      features: row.features ? JSON.parse(row.features) : {}
    };
  }

  async updateTenant(tenantId: number, updates: Partial<Tenant>): Promise<Tenant> {
    const pool = this.ensureConnected();
    const request = pool.request()
      .input('tenantId', sql.Int, tenantId);

    const updateFields: string[] = ['ModifiedDate = GETUTCDATE()'];
    
    if (updates.status !== undefined) {
      request.input('status', sql.NVarChar, updates.status);
      updateFields.push('Status = @status');
    }
    
    if (updates.organizationName !== undefined) {
      request.input('organizationName', sql.NVarChar, updates.organizationName);
      updateFields.push('OrganizationName = @organizationName');
    }
    
    if (updates.primaryAdminEmail !== undefined) {
      request.input('primaryAdminEmail', sql.NVarChar, updates.primaryAdminEmail);
      updateFields.push('PrimaryAdminEmail = @primaryAdminEmail');
    }
    
    if (updates.settings !== undefined) {
      request.input('settings', sql.NVarChar, JSON.stringify(updates.settings));
      updateFields.push('Settings = @settings');
    }

    const result = await request.query(`
      UPDATE [dbo].[Tenant]
      SET ${updateFields.join(', ')}
      OUTPUT INSERTED.Id, INSERTED.EntraIdTenantId, INSERTED.OrganizationName,
             INSERTED.PrimaryAdminEmail, INSERTED.OnboardedDate, INSERTED.Status,
             INSERTED.Settings, INSERTED.CreatedDate, INSERTED.ModifiedDate
      WHERE Id = @tenantId
    `);

    if (result.recordset.length === 0) {
      throw new Error('Tenant not found');
    }

    const row = result.recordset[0];
    return {
      id: row.Id,
      entraIdTenantId: row.EntraIdTenantId,
      organizationName: row.OrganizationName,
      primaryAdminEmail: row.PrimaryAdminEmail,
      onboardedDate: row.OnboardedDate,
      status: row.Status,
      settings: row.Settings ? JSON.parse(row.Settings) : null,
      createdDate: row.CreatedDate,
      modifiedDate: row.ModifiedDate
    };
  }

  // Tenant Auth operations
  async getTenantAuth(tenantId: number): Promise<TenantAuth | null> {
    const pool = this.ensureConnected();
    const result = await pool.request()
      .input('tenantId', sql.Int, tenantId)
      .query(`
        SELECT Id as id, TenantId as tenantId,
               AccessToken as accessToken, RefreshToken as refreshToken,
               TokenExpiresAt as tokenExpiresAt, Scope as scope,
               ConsentGrantedBy as consentGrantedBy, ConsentGrantedAt as consentGrantedAt,
               LastTokenRefresh as lastTokenRefresh,
               CreatedDate as createdDate, ModifiedDate as modifiedDate
        FROM [dbo].[TenantAuth]
        WHERE TenantId = @tenantId
      `);

    if (result.recordset.length === 0) {
      return null;
    }

    return result.recordset[0];
  }

  async saveTenantAuth(auth: Omit<TenantAuth, 'id' | 'createdDate' | 'modifiedDate'>): Promise<TenantAuth> {
    const pool = this.ensureConnected();
    
    // Check if auth record exists
    const existing = await this.getTenantAuth(auth.tenantId);
    
    if (existing) {
      // Update existing record
      const result = await pool.request()
        .input('tenantId', sql.Int, auth.tenantId)
        .input('accessToken', sql.NVarChar, auth.accessToken || null)
        .input('refreshToken', sql.NVarChar, auth.refreshToken || null)
        .input('tokenExpiresAt', sql.DateTime2, auth.tokenExpiresAt || null)
        .input('scope', sql.NVarChar, auth.scope || null)
        .input('consentGrantedBy', sql.NVarChar, auth.consentGrantedBy || null)
        .input('consentGrantedAt', sql.DateTime2, auth.consentGrantedAt || null)
        .input('lastTokenRefresh', sql.DateTime2, auth.lastTokenRefresh || null)
        .query(`
          UPDATE [dbo].[TenantAuth]
          SET AccessToken = @accessToken,
              RefreshToken = @refreshToken,
              TokenExpiresAt = @tokenExpiresAt,
              Scope = @scope,
              ConsentGrantedBy = @consentGrantedBy,
              ConsentGrantedAt = @consentGrantedAt,
              LastTokenRefresh = @lastTokenRefresh,
              ModifiedDate = GETUTCDATE()
          OUTPUT INSERTED.Id, INSERTED.TenantId, INSERTED.AccessToken, INSERTED.RefreshToken,
                 INSERTED.TokenExpiresAt, INSERTED.Scope, INSERTED.ConsentGrantedBy,
                 INSERTED.ConsentGrantedAt, INSERTED.LastTokenRefresh,
                 INSERTED.CreatedDate, INSERTED.ModifiedDate
          WHERE TenantId = @tenantId
        `);
      
      const row = result.recordset[0];
      return {
        id: row.Id,
        tenantId: row.TenantId,
        accessToken: row.AccessToken,
        refreshToken: row.RefreshToken,
        tokenExpiresAt: row.TokenExpiresAt,
        scope: row.Scope,
        consentGrantedBy: row.ConsentGrantedBy,
        consentGrantedAt: row.ConsentGrantedAt,
        lastTokenRefresh: row.LastTokenRefresh,
        createdDate: row.CreatedDate,
        modifiedDate: row.ModifiedDate
      };
    } else {
      // Insert new record
      const result = await pool.request()
        .input('tenantId', sql.Int, auth.tenantId)
        .input('accessToken', sql.NVarChar, auth.accessToken || null)
        .input('refreshToken', sql.NVarChar, auth.refreshToken || null)
        .input('tokenExpiresAt', sql.DateTime2, auth.tokenExpiresAt || null)
        .input('scope', sql.NVarChar, auth.scope || null)
        .input('consentGrantedBy', sql.NVarChar, auth.consentGrantedBy || null)
        .input('consentGrantedAt', sql.DateTime2, auth.consentGrantedAt || null)
        .input('lastTokenRefresh', sql.DateTime2, auth.lastTokenRefresh || null)
        .query(`
          INSERT INTO [dbo].[TenantAuth] (
            TenantId, AccessToken, RefreshToken, TokenExpiresAt, Scope,
            ConsentGrantedBy, ConsentGrantedAt, LastTokenRefresh
          )
          OUTPUT INSERTED.Id, INSERTED.TenantId, INSERTED.AccessToken, INSERTED.RefreshToken,
                 INSERTED.TokenExpiresAt, INSERTED.Scope, INSERTED.ConsentGrantedBy,
                 INSERTED.ConsentGrantedAt, INSERTED.LastTokenRefresh,
                 INSERTED.CreatedDate, INSERTED.ModifiedDate
          VALUES (
            @tenantId, @accessToken, @refreshToken, @tokenExpiresAt, @scope,
            @consentGrantedBy, @consentGrantedAt, @lastTokenRefresh
          )
        `);

      const row = result.recordset[0];
      return {
        id: row.Id,
        tenantId: row.TenantId,
        accessToken: row.AccessToken,
        refreshToken: row.RefreshToken,
        tokenExpiresAt: row.TokenExpiresAt,
        scope: row.Scope,
        consentGrantedBy: row.ConsentGrantedBy,
        consentGrantedAt: row.ConsentGrantedAt,
        lastTokenRefresh: row.LastTokenRefresh,
        createdDate: row.CreatedDate,
        modifiedDate: row.ModifiedDate
      };
    }
  }

  async refreshTenantToken(tenantId: number, tokenResponse: OAuthTokenResponse): Promise<TenantAuth> {
    const pool = this.ensureConnected();
    const expiresAt = new Date(Date.now() + tokenResponse.expires_in * 1000);

    const result = await pool.request()
      .input('tenantId', sql.Int, tenantId)
      .input('accessToken', sql.NVarChar, tokenResponse.access_token)
      .input('refreshToken', sql.NVarChar, tokenResponse.refresh_token || null)
      .input('tokenExpiresAt', sql.DateTime2, expiresAt)
      .input('scope', sql.NVarChar, tokenResponse.scope || null)
      .query(`
        UPDATE [dbo].[TenantAuth]
        SET AccessToken = @accessToken,
            RefreshToken = COALESCE(@refreshToken, RefreshToken),
            TokenExpiresAt = @tokenExpiresAt,
            Scope = COALESCE(@scope, Scope),
            LastTokenRefresh = GETUTCDATE(),
            ModifiedDate = GETUTCDATE()
        OUTPUT INSERTED.Id, INSERTED.TenantId, INSERTED.AccessToken, INSERTED.RefreshToken,
               INSERTED.TokenExpiresAt, INSERTED.Scope, INSERTED.ConsentGrantedBy,
               INSERTED.ConsentGrantedAt, INSERTED.LastTokenRefresh,
               INSERTED.CreatedDate, INSERTED.ModifiedDate
        WHERE TenantId = @tenantId
      `);

    if (result.recordset.length === 0) {
      throw new Error('Tenant auth record not found');
    }

    const row = result.recordset[0];
    return {
      id: row.Id,
      tenantId: row.TenantId,
      accessToken: row.AccessToken,
      refreshToken: row.RefreshToken,
      tokenExpiresAt: row.TokenExpiresAt,
      scope: row.Scope,
      consentGrantedBy: row.ConsentGrantedBy,
      consentGrantedAt: row.ConsentGrantedAt,
      lastTokenRefresh: row.LastTokenRefresh,
      createdDate: row.CreatedDate,
      modifiedDate: row.ModifiedDate
    };
  }
}

export const databaseService = new DatabaseService();
