import { CosmosClient, Container } from '@azure/cosmos';
import { AuditLog } from '../models/types';
import { v4 as uuidv4 } from 'uuid';

let cosmosClient: CosmosClient;
let auditLogsContainer: Container;

/**
 * Initialize Cosmos DB client
 */
export function initializeCosmosClient(): void {
  if (!cosmosClient) {
    const endpoint = process.env.COSMOS_DB_ENDPOINT || '';
    const key = process.env.COSMOS_DB_KEY || '';
    
    cosmosClient = new CosmosClient({ endpoint, key });
    
    const database = cosmosClient.database('spexternal');
    auditLogsContainer = database.container('GlobalAuditLogs');
  }
}

/**
 * Create audit log entry
 */
export async function createAuditLog(auditLog: Omit<AuditLog, 'auditId'>): Promise<void> {
  try {
    initializeCosmosClient();
    
    const log: AuditLog = {
      ...auditLog,
      auditId: uuidv4(),
      timestamp: new Date().toISOString()
    };

    await auditLogsContainer.items.create(log);
  } catch (error) {
    console.error('Error creating audit log:', error);
    // Don't throw - audit logging shouldn't break operations
  }
}

/**
 * Query audit logs for tenant
 */
export async function queryAuditLogs(
  tenantId: string,
  filters: {
    eventType?: string;
    actor?: string;
    startDate?: string;
    endDate?: string;
    status?: 'success' | 'failure';
  },
  page: number = 1,
  pageSize: number = 50
): Promise<{ logs: AuditLog[]; total: number }> {
  try {
    initializeCosmosClient();
    
    // Build query
    let query = 'SELECT * FROM c WHERE c.tenantId = @tenantId';
    const parameters: Array<{ name: string; value: unknown }> = [
      { name: '@tenantId', value: tenantId }
    ];

    if (filters.eventType) {
      query += ' AND c.eventType = @eventType';
      parameters.push({ name: '@eventType', value: filters.eventType });
    }

    if (filters.actor) {
      query += ' AND c.actor.email = @actor';
      parameters.push({ name: '@actor', value: filters.actor });
    }

    if (filters.startDate) {
      query += ' AND c.timestamp >= @startDate';
      parameters.push({ name: '@startDate', value: filters.startDate });
    }

    if (filters.endDate) {
      query += ' AND c.timestamp <= @endDate';
      parameters.push({ name: '@endDate', value: filters.endDate });
    }

    if (filters.status) {
      query += ' AND c.status = @status';
      parameters.push({ name: '@status', value: filters.status });
    }

    query += ' ORDER BY c.timestamp DESC';

    const querySpec = { query, parameters };

    // Get all matching logs (for count)
    const { resources } = await auditLogsContainer.items
      .query<AuditLog>(querySpec)
      .fetchAll();

    // Paginate results
    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const paginatedLogs = resources.slice(startIndex, endIndex);

    return {
      logs: paginatedLogs,
      total: resources.length
    };
  } catch (error) {
    console.error('Error querying audit logs:', error);
    throw error;
  }
}
