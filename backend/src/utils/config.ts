/**
 * Configuration management
 */

export interface Config {
  database: {
    server: string;
    database: string;
    user: string;
    password: string;
    options: {
      encrypt: boolean;
      trustServerCertificate: boolean;
    };
  };
  auth: {
    tenantId: string;
    clientId: string;
    clientSecret: string;
    audience: string;
    issuer: string;
  };
  keyVault: {
    url: string;
  };
  cors: {
    allowedOrigins: string[];
  };
  rateLimit: {
    windowMs: number;
  };
  features: {
    enableGraphIntegration: boolean;
    enableAuditLogging: boolean;
  };
}

class ConfigService {
  private config: Config;

  constructor() {
    this.config = {
      database: {
        server: process.env.SQL_SERVER || '',
        database: process.env.SQL_DATABASE || '',
        user: process.env.SQL_USER || '',
        password: process.env.SQL_PASSWORD || '',
        options: {
          encrypt: true,
          trustServerCertificate: process.env.NODE_ENV === 'development'
        }
      },
      auth: {
        tenantId: process.env.AZURE_TENANT_ID || 'common',
        clientId: process.env.AZURE_CLIENT_ID || '',
        clientSecret: process.env.AZURE_CLIENT_SECRET || '',
        audience: process.env.AZURE_AD_AUDIENCE || process.env.AZURE_CLIENT_ID || '',
        issuer: process.env.AZURE_AD_ISSUER || 'https://sts.windows.net/'
      },
      keyVault: {
        url: process.env.KEY_VAULT_URL || ''
      },
      cors: {
        allowedOrigins: (process.env.CORS_ALLOWED_ORIGINS || '').split(',').filter(Boolean)
      },
      rateLimit: {
        windowMs: parseInt(process.env.RATE_LIMIT_WINDOW_MS || '60000', 10)
      },
      features: {
        enableGraphIntegration: process.env.ENABLE_GRAPH_INTEGRATION === 'true',
        enableAuditLogging: process.env.ENABLE_AUDIT_LOGGING !== 'false'
      }
    };
  }

  get(): Config {
    return this.config;
  }

  validate(): void {
    const required = [
      'SQL_SERVER',
      'SQL_DATABASE',
      'AZURE_CLIENT_ID'
    ];

    const missing = required.filter(key => !process.env[key]);
    
    if (missing.length > 0) {
      throw new Error(`Missing required environment variables: ${missing.join(', ')}`);
    }
  }

  isDevelopment(): boolean {
    return process.env.NODE_ENV === 'development';
  }

  isProduction(): boolean {
    return process.env.NODE_ENV === 'production';
  }
}

export const configService = new ConfigService();
export const config = configService.get();
