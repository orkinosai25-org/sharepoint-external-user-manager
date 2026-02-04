import { CosmosClient, Container } from '@azure/cosmos';
import { Tenant } from '../models/types';

let cosmosClient: CosmosClient;
let tenantsContainer: Container;

/**
 * Initialize Cosmos DB client
 */
export function initializeCosmosClient(): void {
  if (!cosmosClient) {
    const endpoint = process.env.COSMOS_DB_ENDPOINT || '';
    const key = process.env.COSMOS_DB_KEY || '';
    
    cosmosClient = new CosmosClient({ endpoint, key });
    
    const database = cosmosClient.database('spexternal');
    tenantsContainer = database.container('Tenants');
  }
}

/**
 * Get tenant by ID
 */
export async function getTenant(tenantId: string): Promise<Tenant | null> {
  try {
    initializeCosmosClient();
    
    const querySpec = {
      query: 'SELECT * FROM c WHERE c.tenantId = @tenantId',
      parameters: [{ name: '@tenantId', value: tenantId }]
    };

    const { resources } = await tenantsContainer.items
      .query<Tenant>(querySpec)
      .fetchAll();

    return resources.length > 0 ? resources[0] : null;
  } catch (error) {
    console.error('Error getting tenant:', error);
    throw error;
  }
}

/**
 * Create new tenant
 */
export async function createTenant(tenant: Tenant): Promise<Tenant> {
  try {
    initializeCosmosClient();
    
    const { resource } = await tenantsContainer.items.create(tenant);
    return resource as Tenant;
  } catch (error) {
    console.error('Error creating tenant:', error);
    throw error;
  }
}

/**
 * Update tenant
 */
export async function updateTenant(
  tenantId: string,
  updates: Partial<Tenant>
): Promise<Tenant> {
  try {
    initializeCosmosClient();
    
    const existing = await getTenant(tenantId);
    if (!existing) {
      throw new Error(`Tenant not found: ${tenantId}`);
    }

    const updated = {
      ...existing,
      ...updates,
      lastModifiedDate: new Date().toISOString()
    };

    const { resource } = await tenantsContainer.item(existing.id, tenantId).replace(updated);
    return resource as Tenant;
  } catch (error) {
    console.error('Error updating tenant:', error);
    throw error;
  }
}

/**
 * Check if tenant exists
 */
export async function tenantExists(tenantId: string): Promise<boolean> {
  const tenant = await getTenant(tenantId);
  return tenant !== null;
}
