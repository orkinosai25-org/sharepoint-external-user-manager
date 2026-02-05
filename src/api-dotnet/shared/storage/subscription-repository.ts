import { CosmosClient, Container } from '@azure/cosmos';
import { Subscription } from '../models/types';

let cosmosClient: CosmosClient;
let subscriptionsContainer: Container;

/**
 * Initialize Cosmos DB client
 */
export function initializeCosmosClient(): void {
  if (!cosmosClient) {
    const endpoint = process.env.COSMOS_DB_ENDPOINT || '';
    const key = process.env.COSMOS_DB_KEY || '';
    
    cosmosClient = new CosmosClient({ endpoint, key });
    
    const database = cosmosClient.database('spexternal');
    subscriptionsContainer = database.container('Subscriptions');
  }
}

/**
 * Get tenant subscription
 */
export async function getTenantSubscription(tenantId: string): Promise<Subscription | null> {
  try {
    initializeCosmosClient();
    
    const querySpec = {
      query: 'SELECT * FROM c WHERE c.tenantId = @tenantId',
      parameters: [{ name: '@tenantId', value: tenantId }]
    };

    const { resources } = await subscriptionsContainer.items
      .query<Subscription>(querySpec)
      .fetchAll();

    return resources.length > 0 ? resources[0] : null;
  } catch (error) {
    console.error('Error getting tenant subscription:', error);
    throw error;
  }
}

/**
 * Create tenant subscription
 */
export async function createTenantSubscription(subscription: Subscription): Promise<Subscription> {
  try {
    initializeCosmosClient();
    
    const { resource } = await subscriptionsContainer.items.create(subscription);
    return resource as Subscription;
  } catch (error) {
    console.error('Error creating tenant subscription:', error);
    throw error;
  }
}

/**
 * Update tenant subscription
 */
export async function updateTenantSubscription(
  tenantId: string,
  updates: Partial<Subscription>
): Promise<Subscription> {
  try {
    initializeCosmosClient();
    
    const existing = await getTenantSubscription(tenantId);
    if (!existing) {
      throw new Error(`Subscription not found for tenant: ${tenantId}`);
    }

    const updated = {
      ...existing,
      ...updates,
      lastModifiedDate: new Date().toISOString()
    };

    const { resource } = await subscriptionsContainer.item(existing.id, tenantId).replace(updated);
    return resource as Subscription;
  } catch (error) {
    console.error('Error updating tenant subscription:', error);
    throw error;
  }
}

/**
 * Increment usage counter
 */
export async function incrementUsage(
  tenantId: string,
  metric: 'externalUsersCount' | 'librariesCount' | 'apiCallsThisMonth' | 'storageUsedMB',
  amount: number = 1
): Promise<void> {
  try {
    const subscription = await getTenantSubscription(tenantId);
    if (!subscription) {
      throw new Error(`Subscription not found for tenant: ${tenantId}`);
    }

    subscription.usage[metric] += amount;
    await updateTenantSubscription(tenantId, { usage: subscription.usage });
  } catch (error) {
    console.error('Error incrementing usage:', error);
    // Don't throw - usage tracking shouldn't break operations
  }
}
