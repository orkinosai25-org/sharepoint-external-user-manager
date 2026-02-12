import { WebPartContext } from '@microsoft/sp-webpart-base';

export interface IAiPoweredFaqProps {
  description: string;
  context: WebPartContext;
  azureOpenAiEndpoint: string;
  azureOpenAiApiKey: string;
  enableAiSuggestions: boolean;
  enableAnalytics: boolean;
}