declare interface IAiPoweredFaqWebPartStrings {
  PropertyPaneDescription: string;
  BasicGroupName: string;
  DescriptionFieldLabel: string;
  AzureAiGroupName: string;
  AzureOpenAiEndpointLabel: string;
  AzureOpenAiApiKeyLabel: string;
  EnableAiSuggestionsLabel: string;
  EnableAnalyticsLabel: string;
}

declare module 'AiPoweredFaqWebPartStrings' {
  const strings: IAiPoweredFaqWebPartStrings;
  export default strings;
}