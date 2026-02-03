import express, { Router, Response } from 'express';
import { authMiddleware } from '../middleware/auth.middleware';
import { tenantMiddleware, TenantRequest } from '../middleware/tenant.middleware';
import { checkUserQuota } from '../middleware/licensing.middleware';
import { v4 as uuidv4 } from 'uuid';

const router: Router = express.Router();

// GET /api/v1/users - List external users
router.get('/', authMiddleware, tenantMiddleware, async (req: TenantRequest, res: Response) => {
  try {
    const page = parseInt(req.query.page as string) || 1;
    const pageSize = Math.min(parseInt(req.query.pageSize as string) || 50, 100);
    
    // Mock data - replace with actual database query
    const mockUsers = [
      {
        user_id: 'user-001',
        email: 'partner@external.com',
        display_name: 'Jane Partner',
        user_type: 'External',
        status: 'Active',
        invited_by: 'admin@contoso.com',
        invited_date: new Date('2024-01-10').toISOString(),
        last_access_date: new Date('2024-01-14').toISOString(),
        access_expiration_date: new Date('2024-04-10').toISOString(),
        company_name: 'Partner Corp',
        permissions_count: 3
      }
    ];
    
    return res.json({
      success: true,
      data: mockUsers,
      pagination: {
        page,
        page_size: pageSize,
        total: mockUsers.length,
        total_pages: 1,
        has_next: false,
        has_prev: false
      }
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      error: {
        code: 'INTERNAL_ERROR',
        message: 'Failed to retrieve users'
      }
    });
  }
});

// POST /api/v1/users/invite - Invite external user
router.post('/invite', authMiddleware, tenantMiddleware, checkUserQuota, async (req: TenantRequest, res: Response) => {
  try {
    const { email, display_name, company_name, permissions } = req.body;
    
    if (!email || !display_name) {
      return res.status(400).json({
        success: false,
        error: {
          code: 'VALIDATION_ERROR',
          message: 'Email and display name are required'
        }
      });
    }
    
    const userId = `user-${uuidv4()}`;
    const invitationId = `inv-${uuidv4()}`;
    
    // In production:
    // 1. Create user record in database
    // 2. Send invitation email
    // 3. Create permissions
    
    return res.status(201).json({
      success: true,
      data: {
        user_id: userId,
        email,
        display_name,
        status: 'Invited',
        invited_by: req.user?.email,
        invited_date: new Date().toISOString(),
        access_expiration_date: new Date(Date.now() + 90 * 24 * 60 * 60 * 1000).toISOString(),
        invitation_id: invitationId
      }
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      error: {
        code: 'INTERNAL_ERROR',
        message: 'Failed to invite user'
      }
    });
  }
});

// GET /api/v1/users/:userId - Get user details
router.get('/:userId', authMiddleware, tenantMiddleware, async (req: TenantRequest, res: Response) => {
  try {
    const { userId } = req.params;
    
    // Mock data - replace with actual database query
    const mockUser = {
      user_id: userId,
      email: 'partner@external.com',
      display_name: 'Jane Partner',
      user_type: 'External',
      status: 'Active',
      invited_by: 'admin@contoso.com',
      invited_date: new Date('2024-01-10').toISOString(),
      last_access_date: new Date('2024-01-14').toISOString(),
      access_expiration_date: new Date('2024-04-10').toISOString(),
      company_name: 'Partner Corp',
      job_title: 'Project Manager',
      permissions: []
    };
    
    return res.json({
      success: true,
      data: mockUser
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      error: {
        code: 'INTERNAL_ERROR',
        message: 'Failed to retrieve user'
      }
    });
  }
});

// DELETE /api/v1/users/:userId - Revoke user access
router.delete('/:userId', authMiddleware, tenantMiddleware, async (req: TenantRequest, res: Response) => {
  try {
    const { userId } = req.params;
    
    // In production: Update user status to 'Revoked' and remove permissions
    
    return res.json({
      success: true,
      message: 'User access revoked successfully',
      data: {
        user_id: userId,
        status: 'Revoked',
        revoked_date: new Date().toISOString(),
        permissions_revoked: 3
      }
    });
  } catch (error) {
    return res.status(500).json({
      success: false,
      error: {
        code: 'INTERNAL_ERROR',
        message: 'Failed to revoke user access'
      }
    });
  }
});

export default router;
