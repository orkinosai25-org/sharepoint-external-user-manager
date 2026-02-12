import { IFaqItem, IAiSuggestion, IChatMessage } from '../models/IFaqModels';

export class MockFaqDataService {
  public static getFaqItems(): IFaqItem[] {
    return [
      {
        id: '1',
        question: 'How do I reset my password?',
        answer: 'To reset your password, go to the login page and click "Forgot Password". Enter your email address and follow the instructions sent to your email.',
        category: 'IT Support',
        tags: ['password', 'login', 'security'],
        rating: 4.5,
        viewCount: 245,
        lastUpdated: new Date('2024-01-15'),
        createdBy: 'admin@company.com',
        aiGenerated: false,
        relatedQuestions: ['How to change password?', 'Account locked out']
      },
      {
        id: '2',
        question: 'What are the company holidays for 2024?',
        answer: 'Company holidays for 2024 include New Year\'s Day, Martin Luther King Jr. Day, Presidents\' Day, Memorial Day, Independence Day, Labor Day, Thanksgiving, and Christmas Day. Please refer to the HR portal for the complete list.',
        category: 'HR',
        tags: ['holidays', 'time-off', 'calendar'],
        rating: 4.8,
        viewCount: 189,
        lastUpdated: new Date('2024-01-01'),
        createdBy: 'hr@company.com',
        aiGenerated: false,
        relatedQuestions: ['How to request time off?', 'Vacation policy']
      },
      {
        id: '3',
        question: 'How do I submit an expense report?',
        answer: 'To submit an expense report, log into the expense management system, click "New Report", add your expenses with receipts, and submit for approval. Reports must be submitted within 30 days of the expense date.',
        category: 'Billing',
        tags: ['expenses', 'reimbursement', 'finance'],
        rating: 4.2,
        viewCount: 156,
        lastUpdated: new Date('2024-01-10'),
        createdBy: 'finance@company.com',
        aiGenerated: false,
        relatedQuestions: ['Expense policy', 'Receipt requirements']
      },
      {
        id: '4',
        question: 'What software can I install on my work computer?',
        answer: 'Only approved software from the company catalog can be installed. Contact IT for software requests or to add new software to the approved list. Personal software installation is not permitted for security reasons.',
        category: 'IT Support',
        tags: ['software', 'installation', 'security', 'policy'],
        rating: 4.0,
        viewCount: 201,
        lastUpdated: new Date('2024-01-08'),
        createdBy: 'it@company.com',
        aiGenerated: false,
        relatedQuestions: ['Software approval process', 'Security policies']
      },
      {
        id: '5',
        question: 'How do I access SharePoint from home?',
        answer: 'To access SharePoint from home, use your company credentials to log in through the web portal at portal.company.com. Ensure you have VPN access if required. For mobile access, download the SharePoint mobile app.',
        category: 'Technical',
        tags: ['sharepoint', 'remote-access', 'vpn', 'mobile'],
        rating: 4.6,
        viewCount: 312,
        lastUpdated: new Date('2024-01-12'),
        createdBy: 'it@company.com',
        aiGenerated: true,
        confidence: 0.89,
        relatedQuestions: ['VPN setup', 'Mobile app installation']
      }
    ];
  }

  public static async searchFaqItems(query: string): Promise<IFaqItem[]> {
    const allItems = this.getFaqItems();
    return allItems.filter(item => 
      item.question.toLowerCase().includes(query.toLowerCase()) ||
      item.answer.toLowerCase().includes(query.toLowerCase()) ||
      item.tags.some(tag => tag.toLowerCase().includes(query.toLowerCase()))
    );
  }

  public static async createFaqItem(faq: Partial<IFaqItem>): Promise<IFaqItem> {
    // Mock implementation - would integrate with SharePoint Lists or external API
    return {
      id: Math.random().toString(36).substr(2, 9),
      question: faq.question || '',
      answer: faq.answer || '',
      category: faq.category || 'General',
      tags: faq.tags || [],
      rating: 0,
      viewCount: 0,
      lastUpdated: new Date(),
      createdBy: 'user@company.com',
      aiGenerated: false
    };
  }

  public static async updateFaqItem(id: string, updates: Partial<IFaqItem>): Promise<void> {
    // Mock implementation - would integrate with SharePoint Lists or external API
    console.log(`Updating FAQ item ${id}`, updates);
  }

  public static async deleteFaqItem(id: string): Promise<void> {
    // Mock implementation - would integrate with SharePoint Lists or external API
    console.log(`Deleting FAQ item ${id}`);
  }

  public static async incrementViewCount(id: string): Promise<void> {
    // Mock implementation - would track analytics
    console.log(`Incrementing view count for FAQ ${id}`);
  }

  public static async rateFaqItem(id: string, rating: number): Promise<void> {
    // Mock implementation - would store user ratings
    console.log(`Rating FAQ ${id} with ${rating} stars`);
  }
}