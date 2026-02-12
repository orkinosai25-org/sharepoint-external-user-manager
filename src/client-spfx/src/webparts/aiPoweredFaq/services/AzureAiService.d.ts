import { IAiSuggestion } from '../models/IFaqModels';
export interface IAzureAiResponse {
    answer: string;
    confidence: number;
    sources: string[];
    suggestedActions?: string[];
}
export declare class AzureAiService {
    /**
     * Get AI-powered answer suggestions using Azure OpenAI
     */
    static getAnswerSuggestions(query: string, endpoint: string, apiKey: string): Promise<IAiSuggestion[]>;
    /**
     * Get direct AI answer for a question
     */
    static getAiAnswer(question: string, endpoint: string, apiKey: string): Promise<IAzureAiResponse>;
    /**
     * Analyze FAQ content and suggest improvements
     */
    static analyzeFaqContent(faqItems: any[]): Promise<any>;
    private static getMockSuggestions;
    private static getMockAiResponse;
    private static processAzureResponse;
    private static processAiResponse;
}
//# sourceMappingURL=AzureAiService.d.ts.map