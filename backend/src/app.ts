import express, { Express, Request, Response } from 'express';
import cors from 'cors';
import helmet from 'helmet';
import compression from 'compression';
import rateLimit from 'express-rate-limit';
import { config, validateConfig } from './config';
import { errorMiddleware, notFoundMiddleware } from './middleware/error.middleware';

// Import routes
import tenantRoutes from './routes/tenant.routes';
import userRoutes from './routes/user.routes';
import policyRoutes from './routes/policy.routes';
import subscriptionRoutes from './routes/subscription.routes';
import auditRoutes from './routes/audit.routes';

// Validate configuration
validateConfig();

const app: Express = express();

// Security middleware
app.use(helmet());
app.use(cors({
  origin: process.env.ALLOWED_ORIGINS?.split(',') || ['http://localhost:3001'],
  credentials: true
}));

// Rate limiting
const limiter = rateLimit({
  windowMs: 60 * 1000, // 1 minute
  max: 100, // Default limit
  message: {
    success: false,
    error: {
      code: 'RATE_LIMIT_EXCEEDED',
      message: 'Too many requests from this IP'
    }
  },
  standardHeaders: true,
  legacyHeaders: false
});

app.use('/api/', limiter);

// Body parsing middleware
app.use(express.json());
app.use(express.urlencoded({ extended: true }));
app.use(compression());

// Request ID middleware
app.use((req: Request, res: Response, next) => {
  req.headers['x-request-id'] = req.headers['x-request-id'] || `req-${Date.now()}-${Math.random().toString(36).substring(2, 11)}`;
  next();
});

// Health check endpoint
app.get('/health', (req: Request, res: Response) => {
  res.json({
    status: 'healthy',
    version: '1.0.0',
    timestamp: new Date().toISOString()
  });
});

// API routes
app.use('/api/v1/tenants', tenantRoutes);
app.use('/api/v1/users', userRoutes);
app.use('/api/v1/policies', policyRoutes);
app.use('/api/v1/subscription', subscriptionRoutes);
app.use('/api/v1/audit-logs', auditRoutes);

// Not found middleware
app.use(notFoundMiddleware);

// Error handling middleware (must be last)
app.use(errorMiddleware);

export default app;
