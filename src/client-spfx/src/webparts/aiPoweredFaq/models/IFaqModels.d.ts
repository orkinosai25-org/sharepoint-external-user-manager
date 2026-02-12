export interface IFaqItem {
    id: string;
    question: string;
    answer: string;
    category: string;
    tags: string[];
    rating: number;
    viewCount: number;
    lastUpdated: Date;
    createdBy: string;
    aiGenerated: boolean;
    confidence?: number;
    relatedQuestions?: string[];
}
export interface IKnowledgeBaseArticle {
    id: string;
    title: string;
    content: string;
    summary: string;
    category: string;
    tags: string[];
    author: string;
    publishDate: Date;
    lastModified: Date;
    rating: number;
    viewCount: number;
    attachments?: IAttachment[];
    relatedArticles?: string[];
}
export interface IAttachment {
    id: string;
    name: string;
    url: string;
    type: string;
    size: number;
}
export interface ISearchQuery {
    query: string;
    filters?: {
        category?: string;
        tags?: string[];
        dateRange?: {
            start: Date;
            end: Date;
        };
    };
}
export interface ISearchResult {
    item: IFaqItem | IKnowledgeBaseArticle;
    relevanceScore: number;
    snippet: string;
    type: 'faq' | 'article';
}
export interface IAiSuggestion {
    id: string;
    suggestion: string;
    confidence: number;
    reasoning: string;
    source: 'azure-ai' | 'cognitive-search' | 'custom-model';
}
export interface IChatMessage {
    id: string;
    message: string;
    isUser: boolean;
    timestamp: Date;
    aiResponse?: {
        answer: string;
        confidence: number;
        sources: string[];
        suggestedActions?: string[];
    };
}
export interface IAnalytics {
    totalQueries: number;
    topQuestions: string[];
    categoryStats: {
        [category: string]: number;
    };
    userSatisfactionRating: number;
    aiAccuracyRate: number;
    responseTime: number;
}
export declare type FaqCategory = 'General' | 'Technical' | 'HR' | 'IT Support' | 'Product' | 'Billing' | 'Other';
export declare type ConfidenceLevel = 'High' | 'Medium' | 'Low';
//# sourceMappingURL=IFaqModels.d.ts.map