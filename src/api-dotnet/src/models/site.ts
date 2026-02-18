/**
 * Site information model
 * Represents SharePoint site metadata for client spaces
 */

export interface SiteInfo {
  /** SharePoint site ID */
  siteId: string;
  /** SharePoint site URL */
  siteUrl: string;
  /** Site display name */
  siteName?: string;
  /** Site description */
  siteDescription?: string;
  /** Site template type */
  templateType?: 'Team' | 'Communication';
  /** Site created date */
  createdDate?: Date;
  /** Site last modified date */
  lastModifiedDate?: Date;
  /** Site owner email */
  ownerEmail?: string;
  /** Site status */
  status?: 'Provisioning' | 'Active' | 'Archived' | 'Deleted';
}

export interface SiteMetadata {
  /** Storage quota in MB */
  storageQuotaMB?: number;
  /** Storage used in MB */
  storageUsedMB?: number;
  /** Number of subsites */
  subsiteCount?: number;
  /** Number of document libraries */
  libraryCount?: number;
  /** Number of lists */
  listCount?: number;
  /** External sharing enabled */
  externalSharingEnabled?: boolean;
}

export interface SitePermissions {
  /** Site collection administrators */
  administrators?: string[];
  /** Permission levels configured */
  permissionLevels?: string[];
  /** Unique permissions enabled */
  hasUniquePermissions?: boolean;
}

/**
 * Complete site information including metadata and permissions
 */
export interface CompleteSiteInfo extends SiteInfo {
  metadata?: SiteMetadata;
  permissions?: SitePermissions;
}

/**
 * Response format for site information
 */
export interface SiteInfoResponse {
  siteId: string;
  siteUrl: string;
  siteName: string;
  siteDescription?: string;
  templateType: string;
  createdDate: string;
  lastModifiedDate: string;
  ownerEmail: string;
  status: string;
  metadata?: SiteMetadata;
}
