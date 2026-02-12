import { IFaqItem } from '../models/IFaqModels';
export declare class MockFaqDataService {
    static getFaqItems(): IFaqItem[];
    static searchFaqItems(query: string): Promise<IFaqItem[]>;
    static createFaqItem(faq: Partial<IFaqItem>): Promise<IFaqItem>;
    static updateFaqItem(id: string, updates: Partial<IFaqItem>): Promise<void>;
    static deleteFaqItem(id: string): Promise<void>;
    static incrementViewCount(id: string): Promise<void>;
    static rateFaqItem(id: string, rating: number): Promise<void>;
}
//# sourceMappingURL=MockFaqDataService.d.ts.map