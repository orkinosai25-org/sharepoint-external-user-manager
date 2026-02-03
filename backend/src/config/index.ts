import dotenv from 'dotenv';

dotenv.config();

interface Config {
  env: string;
  port: number;
  azureAd: {
    tenantId: string;
    clientId: string;
    clientSecret: string;
  };
  database: {
    server: string;
    database: string;
    user: string;
    password: string;
    options: {
      encrypt: boolean;
      trustServerCertificate: boolean;
      enableArithAbort: boolean;
    };
  };
  cosmosDb: {
    endpoint: string;
    key: string;
    databaseId: string;
  };
  keyVault: {
    url: string;
  };
  jwt: {
    secret: string;
  };
  api: {
    baseUrl: string;
    appUrl: string;
  };
  logging: {
    level: string;
  };
  appInsights: {
    connectionString?: string;
  };
}

export const config: Config = {
  env: process.env.NODE_ENV || 'development',
  port: parseInt(process.env.PORT || '3000', 10),
  azureAd: {
    tenantId: process.env.AZURE_AD_TENANT_ID || '',
    clientId: process.env.AZURE_AD_CLIENT_ID || '',
    clientSecret: process.env.AZURE_AD_CLIENT_SECRET || ''
  },
  database: {
    server: process.env.SQL_SERVER || '',
    database: process.env.SQL_DATABASE || 'spexternal',
    user: process.env.SQL_USER || '',
    password: process.env.SQL_PASSWORD || '',
    options: {
      encrypt: true,
      trustServerCertificate: false,
      enableArithAbort: true
    }
  },
  cosmosDb: {
    endpoint: process.env.COSMOS_DB_ENDPOINT || '',
    key: process.env.COSMOS_DB_KEY || '',
    databaseId: process.env.COSMOS_DB_DATABASE || 'spexternal'
  },
  keyVault: {
    url: process.env.KEY_VAULT_URL || ''
  },
  jwt: {
    secret: process.env.JWT_SECRET || 'dev-secret-change-in-production'
  },
  api: {
    baseUrl: process.env.API_BASE_URL || 'http://localhost:3000',
    appUrl: process.env.APP_BASE_URL || 'http://localhost:3001'
  },
  logging: {
    level: process.env.LOG_LEVEL || 'info'
  },
  appInsights: {
    connectionString: process.env.APPLICATIONINSIGHTS_CONNECTION_STRING
  }
};

export function validateConfig(): void {
  const required = [
    'AZURE_AD_TENANT_ID',
    'AZURE_AD_CLIENT_ID'
  ];

  const missing = required.filter(key => !process.env[key]);
  
  if (missing.length > 0) {
    throw new Error(
      `Missing required environment variables: ${missing.join(', ')}`
    );
  }
}
