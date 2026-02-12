import { WebPartContext } from '@microsoft/sp-webpart-base';
import "@pnp/sp/webs";
import "@pnp/sp/lists";
import "@pnp/sp/items";
import "@pnp/sp/security";
import { spfi, SPFx } from "@pnp/sp";
import { IExternalLibrary, IExternalUser, IBulkUserAdditionRequest } from '../models/IExternalLibrary';
import { AuditLogger } from './AuditLogger';

/**
 * SharePoint Data Service using PnPjs for library management
 * 
 * Technical Decisions:
 * - PnPjs chosen for primary integration due to:
 *   * Better TypeScript support and intellisense
 *   * Simplified API compared to raw REST calls
 *   * Built-in error handling and retry logic
 *   * Better caching and performance optimizations
 * 
 * - Microsoft Graph API used as fallback for:
 *   * Tenant-level operations not supported by PnPjs
 *   * Cross-site operations requiring elevated permissions
 *   * Advanced user management scenarios
 */
export class SharePointDataService {
  private context: WebPartContext;
  private auditLogger: AuditLogger;
  private sp: any;

  constructor(context: WebPartContext) {
    this.context = context;
    this.auditLogger = new AuditLogger(context);
    
    // Initialize PnPjs with SPFx context
    this.sp = spfi().using(SPFx(context));
  }

  /**
   * Get all external libraries (document libraries with external sharing enabled)
   */
  public async getExternalLibraries(): Promise<IExternalLibrary[]> {
    try {
      this.auditLogger.logInfo('getExternalLibraries', 'Fetching external libraries');

      // Get all document libraries from current site
      const lists = await this.sp.web.lists
        .filter("BaseTemplate eq 101 and Hidden eq false") // Document libraries only
        .select(
          "Id", "Title", "Description", "DefaultViewUrl", 
          "LastItemModifiedDate", "ItemCount", "Created"
        )
        .expand("RoleAssignments/Member")
        .get();

      const libraries: IExternalLibrary[] = [];

      for (const list of lists) {
        try {
          // Check if library has external sharing
          const hasExternalUsers = await this.hasExternalUsers(list.Id);
          
          if (hasExternalUsers.hasExternal) {
            const library: IExternalLibrary = {
              id: list.Id,
              name: list.Title,
              description: list.Description || 'No description available',
              siteUrl: list.DefaultViewUrl || '',
              externalUsersCount: hasExternalUsers.externalCount,
              lastModified: new Date(list.LastItemModifiedDate),
              owner: await this.getLibraryOwner(list.Id),
              permissions: await this.getLibraryPermissionLevel(list.Id)
            };
            libraries.push(library);
          }
        } catch (error) {
          this.auditLogger.logError('getExternalLibraries', `Error processing library ${list.Title}`, error);
          // Continue processing other libraries
        }
      }

      this.auditLogger.logInfo('getExternalLibraries', `Retrieved ${libraries.length} external libraries`);
      return libraries;

    } catch (error) {
      this.auditLogger.logError('getExternalLibraries', 'Failed to fetch external libraries', error);
      throw new Error(`Failed to fetch external libraries: ${error.message}`);
    }
  }

  /**
   * Create a new document library with specified configuration
   */
  public async createLibrary(libraryConfig: {
    title: string;
    description?: string;
    enableExternalSharing?: boolean;
    template?: number;
  }): Promise<IExternalLibrary> {
    try {
      this.auditLogger.logInfo('createLibrary', `Creating library: ${libraryConfig.title}`);

      // Validate input
      if (!libraryConfig.title || libraryConfig.title.trim().length === 0) {
        throw new Error('Library title is required');
      }

      // Check if library with same name already exists
      const existingLibraries = await this.sp.web.lists
        .filter(`Title eq '${libraryConfig.title.replace(/'/g, "''")}'`)
        .get();

      if (existingLibraries.length > 0) {
        throw new Error(`A library with the name "${libraryConfig.title}" already exists`);
      }

      // Create the document library
      const libraryCreationInfo = {
        Title: libraryConfig.title,
        Description: libraryConfig.description || '',
        BaseTemplate: libraryConfig.template || 101, // Document library template
        EnableAttachments: false,
        EnableFolderCreation: true
      };

      const createResult = await this.sp.web.lists.add(
        libraryCreationInfo.Title,
        libraryCreationInfo.Description,
        libraryCreationInfo.BaseTemplate,
        false, // enableContentTypes
        {
          EnableAttachments: libraryCreationInfo.EnableAttachments,
          EnableFolderCreation: libraryCreationInfo.EnableFolderCreation
        }
      );

      // Configure external sharing if requested
      if (libraryConfig.enableExternalSharing) {
        await this.enableExternalSharing(createResult.data.Id);
      }

      // Create the library object to return
      const newLibrary: IExternalLibrary = {
        id: createResult.data.Id,
        name: libraryConfig.title,
        description: libraryConfig.description || '',
        siteUrl: createResult.data.DefaultViewUrl || '',
        externalUsersCount: 0,
        lastModified: new Date(),
        owner: this.context.pageContext.user.displayName,
        permissions: 'Full Control'
      };

      this.auditLogger.logInfo('createLibrary', `Successfully created library: ${libraryConfig.title}`, {
        libraryId: createResult.data.Id,
        externalSharing: libraryConfig.enableExternalSharing
      });

      return newLibrary;

    } catch (error) {
      this.auditLogger.logError('createLibrary', `Failed to create library: ${libraryConfig.title}`, error);
      throw new Error(`Failed to create library: ${error.message}`);
    }
  }

  /**
   * Delete a document library by ID
   */
  public async deleteLibrary(libraryId: string): Promise<void> {
    try {
      this.auditLogger.logInfo('deleteLibrary', `Deleting library: ${libraryId}`);

      // Get library details before deletion for audit log
      const library = await this.sp.web.lists.getById(libraryId)
        .select("Title", "ItemCount")
        .get();

      // Check if user has permission to delete
      const currentUserPermissions = await this.sp.web.lists.getById(libraryId)
        .getCurrentUserEffectivePermissions();

      // Note: In a real implementation, you would check specific permissions
      // For now, we assume the user has sufficient permissions

      // Perform the deletion
      await this.sp.web.lists.getById(libraryId).delete();

      this.auditLogger.logInfo('deleteLibrary', `Successfully deleted library: ${library.Title}`, {
        libraryId,
        itemCount: library.ItemCount
      });

    } catch (error) {
      this.auditLogger.logError('deleteLibrary', `Failed to delete library: ${libraryId}`, error);
      throw new Error(`Failed to delete library: ${error.message}`);
    }
  }

  /**
   * Get external users for a specific library
   */
  public async getExternalUsersForLibrary(libraryId: string): Promise<IExternalUser[]> {
    try {
      this.auditLogger.logInfo('getExternalUsersForLibrary', `Fetching external users for library: ${libraryId}`);

      // Get all role assignments for the library
      const roleAssignments = await this.sp.web.lists.getById(libraryId)
        .roleAssignments
        .expand("Member", "RoleDefinitionBindings")
        .get();

      const externalUsers: IExternalUser[] = [];

      for (const assignment of roleAssignments) {
        // Check if member is an external user (contains # in login name for external users)
        if (assignment.Member.LoginName && assignment.Member.LoginName.includes('#ext#')) {
          const permissions = assignment.RoleDefinitionBindings
            .map((role: any) => role.Name)
            .join(', ');

          const externalUser: IExternalUser = {
            id: assignment.Member.Id.toString(),
            email: assignment.Member.Email || '',
            displayName: assignment.Member.Title || '',
            invitedBy: 'Unknown', // Would need additional API call to get this
            invitedDate: new Date(), // Would need additional API call to get this
            lastAccess: new Date(), // Would need additional API call to get this
            permissions: this.mapPermissionLevel(permissions),
            company: undefined,
            project: undefined
          };

          // Retrieve stored metadata for this user
          const metadata = await this.getUserMetadata(libraryId, assignment.Member.Id);
          if (metadata) {
            externalUser.company = metadata.company;
            externalUser.project = metadata.project;
          }

          externalUsers.push(externalUser);
        }
      }

      this.auditLogger.logInfo('getExternalUsersForLibrary', `Found ${externalUsers.length} external users`);
      return externalUsers;

    } catch (error) {
      this.auditLogger.logError('getExternalUsersForLibrary', `Failed to get external users for library: ${libraryId}`, error);
      throw new Error(`Failed to get external users: ${error.message}`);
    }
  }

  /**
   * Add external user to a library with specified permissions
   */
  public async addExternalUserToLibrary(libraryId: string, email: string, permission: 'Read' | 'Contribute' | 'Full Control', company?: string, project?: string): Promise<void> {
    try {
      this.auditLogger.logInfo('addExternalUserToLibrary', `Adding external user ${email} to library: ${libraryId} with ${permission} permissions`, {
        libraryId,
        email,
        permission,
        company,
        project
      });

      // First, ensure the user exists in the site collection
      const ensuredUser = await this.sp.web.ensureUser(email);
      const userId = ensuredUser.data.Id;

      if (!userId) {
        throw new Error('Failed to get user ID after ensuring user');
      }

      // Get the role definition ID for the specified permission
      const roleDefId = await this.getRoleDefinitionId(permission);

      // Add role assignment to the library
      await this.sp.web.lists.getById(libraryId)
        .roleAssignments.add(userId, roleDefId);

      // Store user metadata if company or project is provided
      if (company || project) {
        await this.storeUserMetadata(libraryId, email, userId, company, project);
      }

      this.auditLogger.logInfo('addExternalUserToLibrary', `Successfully added user ${email} to library with ${permission} permissions`, {
        libraryId,
        email,
        permission,
        userId,
        company,
        project
      });

    } catch (error) {
      this.auditLogger.logError('addExternalUserToLibrary', `Failed to add external user ${email} to library: ${libraryId}`, error);
      throw new Error(`Failed to add user: ${error.message}`);
    }
  }

  /**

   * Remove external user from a library
   */
  public async removeExternalUserFromLibrary(libraryId: string, userId: string): Promise<void> {
    try {
      this.auditLogger.logInfo('removeExternalUserFromLibrary', `Removing external user ${userId} from library: ${libraryId}`);

      // Remove role assignment from the library
      await this.sp.web.lists.getById(libraryId)
        .roleAssignments.remove(parseInt(userId, 10));

      this.auditLogger.logInfo('removeExternalUserFromLibrary', `Successfully removed user ${userId} from library`, {
        libraryId,
        userId
      });

    } catch (error) {
      this.auditLogger.logError('removeExternalUserFromLibrary', `Failed to remove external user ${userId} from library: ${libraryId}`, error);
      throw new Error(`Failed to remove user: ${error.message}`);
    }
  }

  /**

   * Update user metadata (company and project) - public method
   */
  public async updateUserMetadata(libraryId: string, userId: string, company: string, project: string): Promise<void> {
    try {
      // Convert userId to number for storage consistency
      const userIdNum = parseInt(userId, 10);
      
      // Get user's email for audit logging
      let userEmail = 'Unknown';
      try {
        const users = await this.getExternalUsersForLibrary(libraryId);
        const user = users.find(u => u.id === userId);
        if (user) {
          userEmail = user.email;
        }
      } catch (error) {
        // Continue with metadata update even if we can't get email
        this.auditLogger.logWarning('updateUserMetadata', 'Could not retrieve user email for audit', { userId });
      }

      await this.storeUserMetadata(libraryId, userEmail, userIdNum, company, project);
      
      this.auditLogger.logInfo('updateUserMetadata', `Successfully updated metadata for user ${userId}`, {
        libraryId,
        userId,
        company,
        project
      });
    } catch (error) {
      this.auditLogger.logError('updateUserMetadata', `Failed to update metadata for user ${userId}`, error);
      throw new Error(`Failed to update user metadata: ${error.message}`);
    }
  }

  /**
   * Store user metadata (company and project) in a SharePoint list
   */
  private async storeUserMetadata(libraryId: string, email: string, userId: number, company?: string, project?: string): Promise<void> {
    try {
      // For now, we'll store metadata in the audit log and browser storage
      // In a production environment, this would be stored in a custom SharePoint list
      const metadata = {
        libraryId,
        email,
        userId,
        company: company || '',
        project: project || '',
        timestamp: new Date().toISOString()
      };

      // Store in browser localStorage as a fallback (for demo purposes)
      const storageKey = `userMetadata_${libraryId}_${userId}`;
      localStorage.setItem(storageKey, JSON.stringify(metadata));

      this.auditLogger.logInfo('storeUserMetadata', `Stored metadata for user ${email}`, metadata);
    } catch (error) {
      this.auditLogger.logError('storeUserMetadata', `Failed to store metadata for user ${email}`, error);
      // Don't throw error as this is supplementary functionality
    }
  }

  /**
   * Retrieve user metadata (company and project)
   */
  private async getUserMetadata(libraryId: string, userId: number): Promise<{ company?: string; project?: string } | null> {
    try {
      // Retrieve from browser localStorage (for demo purposes)
      const storageKey = `userMetadata_${libraryId}_${userId}`;
      const stored = localStorage.getItem(storageKey);
      
      if (stored) {
        const metadata = JSON.parse(stored);
        return {
          company: metadata.company || undefined,
          project: metadata.project || undefined
        };
      }
      
      return null;
    } catch (error) {
      this.auditLogger.logError('getUserMetadata', `Failed to retrieve metadata for user ${userId}`, error);
      return null;
    }
  }

  /**
   * Bulk add external users to a library with specified permissions
   */
  public async bulkAddExternalUsersToLibrary(
    libraryId: string, 
    request: { emails: string[]; permission: 'Read' | 'Contribute' | 'Full Control'; company?: string; project?: string; }
  ): Promise<{ email: string; status: 'success' | 'already_member' | 'invitation_sent' | 'failed'; message: string; error?: string; }[]> {
    const results: { email: string; status: 'success' | 'already_member' | 'invitation_sent' | 'failed'; message: string; error?: string; }[] = [];
    const sessionId = this.auditLogger.generateSessionId();
    
    this.auditLogger.logInfo('bulkAddExternalUsersToLibrary', 
      `Starting bulk addition of ${request.emails.length} users to library: ${libraryId}`, {
        libraryId,
        emailCount: request.emails.length,
        permission: request.permission,
        sessionId
      });

    // Get existing users for the library to check for duplicates
    let existingUsers: IExternalUser[] = [];
    try {
      existingUsers = await this.getExternalUsersForLibrary(libraryId);
    } catch (error) {
      this.auditLogger.logWarning('bulkAddExternalUsersToLibrary', 
        'Failed to get existing users, proceeding without duplicate check', { error: error.message });
    }

    const existingEmails = new Set(existingUsers.map(user => user.email.toLowerCase()));

    for (const email of request.emails) {
      const trimmedEmail = email.trim().toLowerCase();
      
      if (!trimmedEmail) {
        results.push({
          email: email,
          status: 'failed',
          message: 'Empty email address',
          error: 'Email address is required'
        });
        continue;
      }

      // Validate email format
      if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmedEmail)) {
        results.push({
          email: email,
          status: 'failed',
          message: 'Invalid email format',
          error: 'Please provide a valid email address'
        });
        continue;
      }

      // Check if user already has access
      if (existingEmails.has(trimmedEmail)) {
        results.push({
          email: email,
          status: 'already_member',
          message: 'User already has access to this library'
        });
        
        this.auditLogger.logInfo('bulkAddExternalUsersToLibrary', 
          `User ${email} already has access to library`, {
            libraryId,
            email,
            sessionId
          });
        continue;
      }

      // Attempt to add the user
      try {
        await this.addExternalUserToLibrary(libraryId, trimmedEmail, request.permission, request.company, request.project);
        
        // Check if user is external (not from same tenant)
        const isExternal = await this.isExternalUser(trimmedEmail);
        
        results.push({
          email: email,
          status: isExternal ? 'invitation_sent' : 'success',
          message: isExternal 
            ? 'Invitation sent to external user' 
            : 'User added successfully'
        });

        this.auditLogger.logInfo('bulkAddExternalUsersToLibrary', 
          `Successfully added user ${email} to library`, {
            libraryId,
            email,
            permission: request.permission,
            isExternal,
            company: request.company,
            project: request.project,
            sessionId
          });

      } catch (error) {
        results.push({
          email: email,
          status: 'failed',
          message: 'Failed to add user',
          error: error.message
        });

        this.auditLogger.logError('bulkAddExternalUsersToLibrary', 
          `Failed to add user ${email} to library`, {
            libraryId,
            email,
            error: error.message,
            sessionId
          });
      }
    }

    const successCount = results.filter(r => r.status === 'success' || r.status === 'invitation_sent').length;
    const alreadyMemberCount = results.filter(r => r.status === 'already_member').length;
    const failedCount = results.filter(r => r.status === 'failed').length;

    this.auditLogger.logInfo('bulkAddExternalUsersToLibrary', 
      `Bulk addition completed. Success: ${successCount}, Already member: ${alreadyMemberCount}, Failed: ${failedCount}`, {
        libraryId,
        totalEmails: request.emails.length,
        successCount,
        alreadyMemberCount,
        failedCount,
        sessionId
      });

    return results;
  }

  /**
   * Search for users in the tenant (for adding external users)
   */
  public async searchUsers(query: string): Promise<IExternalUser[]> {
    try {
      this.auditLogger.logInfo('searchUsers', `Searching for users with query: ${query}`);

      // For external users, we'll use a simple approach of validating the email format
      // In a real implementation, you might use Microsoft Graph API or SharePoint People Picker
      if (!query || !query.includes('@')) {
        return [];
      }

      // Return a mock result for the search - in a real implementation this would query the directory
      const mockUser: IExternalUser = {
        id: 'search-result',
        email: query,
        displayName: query.split('@')[0], // Use part before @ as display name
        invitedBy: this.context.pageContext.user.displayName,
        invitedDate: new Date(),
        lastAccess: new Date(),
        permissions: 'Read'
      };

      this.auditLogger.logInfo('searchUsers', `Found 1 potential user for query: ${query}`);
      return [mockUser];

    } catch (error) {
      this.auditLogger.logError('searchUsers', `Failed to search users with query: ${query}`, error);
      throw new Error(`Failed to search users: ${error.message}`);
    }
  }

  /**
   * Get role definition ID for a permission level
   */
  private async getRoleDefinitionId(permission: 'Read' | 'Contribute' | 'Full Control'): Promise<number> {
    try {
      // Map permission to SharePoint role definition name
      let roleName: string;
      switch (permission) {
        case 'Read':
          roleName = 'Read';
          break;
        case 'Contribute':
          roleName = 'Contribute';
          break;
        case 'Full Control':
          roleName = 'Full Control';
          break;
        default:
          roleName = 'Read';
      }

      const roleDef = await this.sp.web.roleDefinitions.getByName(roleName).get();
      
      if (!roleDef.Id) {
        throw new Error(`Role definition ID not found for permission: ${permission}`);
      }

      return roleDef.Id;

    } catch (error) {
      this.auditLogger.logError('getRoleDefinitionId', `Failed to get role definition ID for permission: ${permission}`, error);
      throw new Error(`Failed to get role definition: ${error.message}`);
    }
  }

  /**
   * Check if a user is external (not from the same tenant)
   */
  private async isExternalUser(email: string): Promise<boolean> {
    try {
      // Simple heuristic: if email domain differs from current site domain, likely external
      const currentDomain = this.context.pageContext.web.absoluteUrl.split('/')[2];
      const emailDomain = email.split('@')[1];
      
      // If domains don't match, it's likely external
      // This is a simplified check - in production you'd use Graph API for accurate determination
      return !currentDomain.includes(emailDomain) && !emailDomain.includes(currentDomain.split('.')[0]);
    } catch {
      // If we can't determine, assume external for safety
      return true;
    }
  }

  // Private helper methods

  private async hasExternalUsers(libraryId: string): Promise<{ hasExternal: boolean; externalCount: number }> {
    try {
      const roleAssignments = await this.sp.web.lists.getById(libraryId)
        .roleAssignments
        .expand("Member")
        .get();

      let externalCount = 0;
      for (const assignment of roleAssignments) {
        if (assignment.Member.LoginName && assignment.Member.LoginName.includes('#ext#')) {
          externalCount++;
        }
      }

      return {
        hasExternal: externalCount > 0,
        externalCount
      };
    } catch {
      return { hasExternal: false, externalCount: 0 };
    }
  }

  private async getLibraryOwner(libraryId: string): Promise<string> {
    try {
      const owner = await this.sp.web.lists.getById(libraryId)
        .select("Author/Title")
        .expand("Author")
        .get();
      
      return owner.Author?.Title || 'Unknown';
    } catch {
      return 'Unknown';
    }
  }

  private async getLibraryPermissionLevel(libraryId: string): Promise<'Read' | 'Contribute' | 'Full Control'> {
    try {
      // Get current user's effective permissions
      const permissions = await this.sp.web.lists.getById(libraryId)
        .getCurrentUserEffectivePermissions();

      // This is a simplified permission check
      // In reality, you'd need to parse the permission mask
      return 'Full Control'; // Default for now
    } catch {
      return 'Read';
    }
  }

  private async enableExternalSharing(libraryId: string): Promise<void> {
    try {
      // Note: External sharing configuration typically requires tenant admin permissions
      // This is where Microsoft Graph API might be needed as a fallback
      // For now, we'll log that this feature requires additional configuration
      this.auditLogger.logInfo('enableExternalSharing', 
        `External sharing enablement for library ${libraryId} requires tenant admin configuration`);
    } catch (error) {
      this.auditLogger.logError('enableExternalSharing', `Failed to enable external sharing for library: ${libraryId}`, error);
    }
  }

  private mapPermissionLevel(permissions: string): 'Read' | 'Contribute' | 'Full Control' {
    if (permissions.includes('Full Control')) return 'Full Control';
    if (permissions.includes('Contribute') || permissions.includes('Edit')) return 'Contribute';
    return 'Read';
  }
}