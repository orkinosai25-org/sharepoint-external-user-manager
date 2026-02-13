/**
 * Search models and interfaces
 * Defines the foundation for ClientSpace Global Search feature
 */

import { PaginationInfo } from './common';

/**
 * Search scope - determines the breadth of the search
 */
export enum SearchScope {
  /** Search within the current client space only (Free tier) */
  CurrentClient = 'CurrentClient',
  /** Search across all client spaces (Pro tier and above) */
  AllClients = 'AllClients'
}

/**
 * Searchable entity types
 */
export enum SearchEntityType {
  ClientSpace = 'ClientSpace',
  Document = 'Document',
  User = 'User',
  Library = 'Library',
  List = 'List',
  Tag = 'Tag',
  Note = 'Note',
  Conversation = 'Conversation'
}

/**
 * Search result ranking/scoring
 */
export interface SearchScore {
  /** Overall relevance score (0-1) */
  relevance: number;
  /** Keyword match score */
  keywordMatch?: number;
  /** Recency score (newer = higher) */
  recency?: number;
  /** User activity/popularity score */
  popularity?: number;
}

/**
 * Base interface for all searchable entities
 */
export interface SearchableEntity {
  /** Unique identifier */
  id: string;
  /** Entity type */
  type: SearchEntityType;
  /** Display title/name */
  title: string;
  /** Brief description or snippet */
  description?: string;
  /** Date created */
  createdAt: Date;
  /** Date last modified */
  modifiedAt: Date;
  /** Created by user */
  createdBy: string;
  /** Modified by user */
  modifiedBy: string;
  /** Search relevance score */
  score?: SearchScore;
}

/**
 * Client space search result
 */
export interface ClientSpaceSearchResult extends SearchableEntity {
  type: SearchEntityType.ClientSpace;
  /** Client space ID */
  clientId: number;
  /** Client name */
  clientName: string;
  /** SharePoint site URL */
  siteUrl: string;
  /** SharePoint site ID */
  siteId: string;
  /** Number of documents */
  documentCount?: number;
  /** Number of external users */
  userCount?: number;
  /** Tags associated with this client */
  tags?: string[];
}

/**
 * Document search result
 */
export interface DocumentSearchResult extends SearchableEntity {
  type: SearchEntityType.Document;
  /** Document ID */
  documentId: string;
  /** Client space this document belongs to */
  clientId: number;
  /** Client name */
  clientName: string;
  /** Library/folder path */
  libraryPath: string;
  /** Document URL */
  webUrl: string;
  /** File extension */
  fileExtension: string;
  /** File size in bytes */
  fileSize: number;
  /** MIME type */
  contentType: string;
  /** Content preview/snippet */
  contentSnippet?: string;
  /** Highlighted matches in content */
  highlights?: string[];
  /** Tags associated with this document */
  tags?: string[];
  /** Version number */
  version?: string;
}

/**
 * User search result
 */
export interface UserSearchResult extends SearchableEntity {
  type: SearchEntityType.User;
  /** User email */
  email: string;
  /** Display name */
  displayName: string;
  /** User principal name */
  userPrincipalName: string;
  /** Client spaces this user has access to */
  clientIds: number[];
  /** Client names */
  clientNames: string[];
  /** User type (internal/external) */
  userType: 'Internal' | 'External';
  /** Last login date */
  lastLoginDate?: Date;
  /** Invitation status */
  invitationStatus?: 'Pending' | 'Accepted' | 'Expired';
}

/**
 * Library search result
 */
export interface LibrarySearchResult extends SearchableEntity {
  type: SearchEntityType.Library;
  /** Library ID */
  libraryId: string;
  /** Client space this library belongs to */
  clientId: number;
  /** Client name */
  clientName: string;
  /** Library web URL */
  webUrl: string;
  /** Number of items */
  itemCount: number;
  /** Template type */
  template?: string;
}

/**
 * List search result
 */
export interface ListSearchResult extends SearchableEntity {
  type: SearchEntityType.List;
  /** List ID */
  listId: string;
  /** Client space this list belongs to */
  clientId: number;
  /** Client name */
  clientName: string;
  /** List web URL */
  webUrl: string;
  /** Number of items */
  itemCount: number;
  /** List template type */
  listTemplate: string;
}

/**
 * Tag search result (for future use)
 */
export interface TagSearchResult extends SearchableEntity {
  type: SearchEntityType.Tag;
  /** Tag name */
  tagName: string;
  /** Number of items with this tag */
  usageCount: number;
  /** Client spaces using this tag */
  clientIds: number[];
}

/**
 * Union type for all search result types
 */
export type SearchResultItem = 
  | ClientSpaceSearchResult 
  | DocumentSearchResult 
  | UserSearchResult 
  | LibrarySearchResult
  | ListSearchResult
  | TagSearchResult;

/**
 * Search metadata for filtering and faceting
 */
export interface SearchMetadata {
  /** Client space ID */
  clientId?: number;
  /** Client name */
  clientName?: string;
  /** Document type/extension */
  documentType?: string;
  /** Created/modified date range */
  dateRange?: {
    from?: Date;
    to?: Date;
  };
  /** User who created/modified */
  user?: string;
  /** Tags */
  tags?: string[];
  /** File size range (for documents) */
  sizeRange?: {
    min?: number;
    max?: number;
  };
  /** Risk level (for future AI features) */
  riskLevel?: 'Low' | 'Medium' | 'High';
}

/**
 * Search filters for refining results
 */
export interface SearchFilters {
  /** Filter by entity types */
  entityTypes?: SearchEntityType[];
  /** Filter by client space IDs */
  clientIds?: number[];
  /** Filter by date range */
  dateFrom?: Date;
  dateTo?: Date;
  /** Filter by user */
  createdBy?: string;
  modifiedBy?: string;
  /** Filter by file type (for documents) */
  fileTypes?: string[];
  /** Filter by tags */
  tags?: string[];
  /** Filter by user type */
  userType?: 'Internal' | 'External';
  /** Filter by invitation status */
  invitationStatus?: 'Pending' | 'Accepted' | 'Expired';
}

/**
 * Search sort options
 */
export enum SearchSortField {
  Relevance = 'Relevance',
  CreatedDate = 'CreatedDate',
  ModifiedDate = 'ModifiedDate',
  Title = 'Title',
  Size = 'Size'
}

export enum SearchSortOrder {
  Ascending = 'Ascending',
  Descending = 'Descending'
}

export interface SearchSort {
  field: SearchSortField;
  order: SearchSortOrder;
}

/**
 * Search query interface
 */
export interface SearchQuery {
  /** Search query string */
  query: string;
  /** Search scope */
  scope: SearchScope;
  /** Optional filters */
  filters?: SearchFilters;
  /** Sort options */
  sort?: SearchSort;
  /** Pagination */
  page?: number;
  pageSize?: number;
}

/**
 * Search request interface
 */
export interface SearchRequest {
  /** The search query */
  query: string;
  /** Search scope (CurrentClient or AllClients) */
  scope: SearchScope;
  /** Client ID (required when scope is CurrentClient) */
  clientId?: number;
  /** Entity types to search */
  entityTypes?: SearchEntityType[];
  /** Filters to apply */
  filters?: SearchFilters;
  /** Sort configuration */
  sort?: SearchSort;
  /** Page number (1-based) */
  page?: number;
  /** Page size (default: 20, max: 100) */
  pageSize?: number;
  /** Include content snippets/highlights */
  includeSnippets?: boolean;
  /** Include permission information */
  includePermissions?: boolean;
}

/**
 * Search response interface
 */
export interface SearchResponse<T = SearchResultItem> {
  /** Search results */
  results: T[];
  /** Total number of results */
  totalResults: number;
  /** Query execution time in milliseconds */
  executionTime: number;
  /** Pagination information */
  pagination: PaginationInfo;
  /** Facets for filtering */
  facets?: SearchFacets;
  /** Query that was executed */
  query: string;
  /** Scope that was used */
  scope: SearchScope;
}

/**
 * Search facets for result refinement
 */
export interface SearchFacets {
  /** Entity type counts */
  entityTypes?: Record<SearchEntityType, number>;
  /** Client space counts */
  clients?: Array<{ clientId: number; clientName: string; count: number }>;
  /** File type counts (for documents) */
  fileTypes?: Record<string, number>;
  /** Date range buckets */
  dateRanges?: Array<{ label: string; from: Date; to: Date; count: number }>;
  /** User type counts */
  userTypes?: Record<'Internal' | 'External', number>;
}

/**
 * Search permissions model
 */
export interface SearchPermissions {
  /** User can search within current client space */
  canSearchCurrentClient: boolean;
  /** User can search across all client spaces (Pro feature) */
  canSearchAllClients: boolean;
  /** Client space IDs the user has access to */
  accessibleClientIds: number[];
  /** Reason if access is denied */
  deniedReason?: string;
  /** Required tier for denied feature */
  requiredTier?: string;
}

/**
 * Search audit log entry
 */
export interface SearchAuditLog {
  /** Audit log ID */
  id: number;
  /** Tenant ID */
  tenantId: number;
  /** User who performed the search */
  userId: string;
  /** User email */
  userEmail: string;
  /** Search query */
  query: string;
  /** Search scope used */
  scope: SearchScope;
  /** Filters applied */
  filters?: SearchFilters;
  /** Number of results returned */
  resultCount: number;
  /** Execution time in milliseconds */
  executionTime: number;
  /** Timestamp */
  timestamp: Date;
  /** Client ID if scope was CurrentClient */
  clientId?: number;
  /** Zero results flag (for analytics) */
  zeroResults: boolean;
}

/**
 * Search configuration/settings per tenant
 */
export interface SearchSettings {
  /** Enable/disable search feature */
  searchEnabled: boolean;
  /** Default search scope */
  defaultScope: SearchScope;
  /** Default page size */
  defaultPageSize: number;
  /** Maximum page size */
  maxPageSize: number;
  /** Enable content indexing */
  contentIndexingEnabled: boolean;
  /** Enable snippet generation */
  snippetsEnabled: boolean;
  /** Indexing schedule (cron expression) */
  indexingSchedule?: string;
}
