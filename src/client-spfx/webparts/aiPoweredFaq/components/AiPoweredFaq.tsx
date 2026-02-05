import * as React from 'react';
import { useState, useEffect } from 'react';
import { 
  Stack, 
  SearchBox, 
  CommandBar, 
  DetailsList, 
  IColumn, 
  SelectionMode, 
  Spinner, 
  SpinnerSize,
  MessageBar,
  MessageBarType,
  Panel,
  PanelType,
  Text,
  Rating,
  Separator,
  Pivot,
  PivotItem
} from '@fluentui/react';
import { IAiPoweredFaqProps } from './IAiPoweredFaqProps';
import { IFaqItem, IKnowledgeBaseArticle, IChatMessage } from '../models/IFaqModels';
import { MockFaqDataService } from '../services/MockFaqDataService';
import { AzureAiService } from '../services/AzureAiService';
import styles from './AiPoweredFaq.module.scss';

const AiPoweredFaq: React.FC<IAiPoweredFaqProps> = (props) => {
  const [faqItems, setFaqItems] = useState<IFaqItem[]>([]);
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(true);
  const [selectedItem, setSelectedItem] = useState<IFaqItem | null>(null);
  const [showPanel, setShowPanel] = useState<boolean>(false);
  const [chatMessages, setChatMessages] = useState<IChatMessage[]>([]);
  const [aiProcessing, setAiProcessing] = useState<boolean>(false);

  useEffect(() => {
    loadFaqData();
  }, []);

  const loadFaqData = async (): Promise<void> => {
    setLoading(true);
    try {
      setTimeout(() => {
        const mockFaqs = MockFaqDataService.getFaqItems();
        setFaqItems(mockFaqs);
        setLoading(false);
      }, 1000);
    } catch (error) {
      console.error('Error loading FAQ data:', error);
      setLoading(false);
    }
  };

  const handleSearch = async (query: string): Promise<void> => {
    setSearchQuery(query);
    if (query.trim() === '') {
      loadFaqData();
      return;
    }

    setLoading(true);
    try {
      // Filter FAQ items based on search query
      const filteredItems = faqItems.filter(item => 
        item.question.toLowerCase().includes(query.toLowerCase()) ||
        item.answer.toLowerCase().includes(query.toLowerCase()) ||
        item.tags.some(tag => tag.toLowerCase().includes(query.toLowerCase()))
      );
      
      setFaqItems(filteredItems);
      
      // If AI is enabled, also get AI suggestions
      if (props.enableAiSuggestions && props.azureOpenAiApiKey) {
        setAiProcessing(true);
        try {
          const aiSuggestions = await AzureAiService.getAnswerSuggestions(query, props.azureOpenAiEndpoint, props.azureOpenAiApiKey);
          // Handle AI suggestions here
          console.log('AI Suggestions:', aiSuggestions);
        } catch (aiError) {
          console.error('AI service error:', aiError);
        }
        setAiProcessing(false);
      }
      
      setLoading(false);
    } catch (error) {
      console.error('Error searching FAQ:', error);
      setLoading(false);
    }
  };

  const handleAskAi = async (question: string): Promise<void> => {
    const newMessage: IChatMessage = {
      id: Date.now().toString(),
      message: question,
      isUser: true,
      timestamp: new Date()
    };
    
    setChatMessages([...chatMessages, newMessage]);
    setAiProcessing(true);

    try {
      if (props.enableAiSuggestions && props.azureOpenAiApiKey) {
        const aiResponse = await AzureAiService.getAiAnswer(question, props.azureOpenAiEndpoint, props.azureOpenAiApiKey);
        
        const aiMessage: IChatMessage = {
          id: (Date.now() + 1).toString(),
          message: '',
          isUser: false,
          timestamp: new Date(),
          aiResponse: {
            answer: aiResponse.answer,
            confidence: aiResponse.confidence,
            sources: aiResponse.sources,
            suggestedActions: aiResponse.suggestedActions
          }
        };
        
        setChatMessages(prev => [...prev, aiMessage]);
      } else {
        // Fallback to mock response
        const mockResponse: IChatMessage = {
          id: (Date.now() + 1).toString(),
          message: '',
          isUser: false,
          timestamp: new Date(),
          aiResponse: {
            answer: 'I understand your question. However, AI services are not currently configured. Please check with your administrator or browse our FAQ section for answers.',
            confidence: 0.5,
            sources: ['Mock Response'],
            suggestedActions: ['Browse FAQ', 'Contact Support']
          }
        };
        
        setChatMessages(prev => [...prev, mockResponse]);
      }
    } catch (error) {
      console.error('Error getting AI response:', error);
      const errorMessage: IChatMessage = {
        id: (Date.now() + 1).toString(),
        message: '',
        isUser: false,
        timestamp: new Date(),
        aiResponse: {
          answer: 'I apologize, but I encountered an error while processing your question. Please try again or contact support.',
          confidence: 0,
          sources: ['Error Handler'],
          suggestedActions: ['Try Again', 'Contact Support']
        }
      };
      
      setChatMessages(prev => [...prev, errorMessage]);
    }
    
    setAiProcessing(false);
  };

  const columns: IColumn[] = [
    {
      key: 'question',
      name: 'Question',
      fieldName: 'question',
      minWidth: 200,
      maxWidth: 400,
      isResizable: true,
      onRender: (item: IFaqItem) => (
        <span 
          style={{ cursor: 'pointer', color: '#0078d4' }}
          onClick={() => {
            setSelectedItem(item);
            setShowPanel(true);
          }}
        >
          {item.question}
        </span>
      )
    },
    {
      key: 'category',
      name: 'Category',
      fieldName: 'category',
      minWidth: 100,
      maxWidth: 150,
      isResizable: true
    },
    {
      key: 'rating',
      name: 'Rating',
      fieldName: 'rating',
      minWidth: 100,
      maxWidth: 120,
      isResizable: true,
      onRender: (item: IFaqItem) => (
        <Rating rating={item.rating} max={5} readOnly />
      )
    },
    {
      key: 'viewCount',
      name: 'Views',
      fieldName: 'viewCount',
      minWidth: 60,
      maxWidth: 80,
      isResizable: true
    }
  ];

  const commandBarItems = [
    {
      key: 'addFaq',
      text: 'Add FAQ',
      iconProps: { iconName: 'Add' },
      onClick: () => alert('Add FAQ functionality will be implemented')
    },
    {
      key: 'askAi',
      text: 'Ask AI',
      iconProps: { iconName: 'Robot' },
      onClick: () => {
        const question = prompt('What would you like to ask?');
        if (question) {
          handleAskAi(question);
        }
      }
    },
    {
      key: 'analytics',
      text: 'Analytics',
      iconProps: { iconName: 'BarChart4' },
      onClick: () => alert('Analytics dashboard will be implemented')
    },
    {
      key: 'refresh',
      text: 'Refresh',
      iconProps: { iconName: 'Refresh' },
      onClick: loadFaqData
    }
  ];

  const renderChatMessages = () => (
    <Stack tokens={{ childrenGap: 10 }}>
      {chatMessages.map((message) => (
        <div key={message.id} className={message.isUser ? styles.userMessage : styles.aiMessage}>
          {message.isUser ? (
            <Text variant="medium">{message.message}</Text>
          ) : (
            <Stack tokens={{ childrenGap: 5 }}>
              <Text variant="medium">{message.aiResponse?.answer}</Text>
              <Text variant="small">Confidence: {Math.round((message.aiResponse?.confidence || 0) * 100)}%</Text>
              {message.aiResponse?.sources && (
                <Text variant="small">Sources: {message.aiResponse.sources.join(', ')}</Text>
              )}
            </Stack>
          )}
        </div>
      ))}
      {aiProcessing && <Spinner size={SpinnerSize.small} label="AI is thinking..." />}
    </Stack>
  );

  return (
    <div className={styles.aiPoweredFaq}>
      <Stack tokens={{ childrenGap: 20 }}>
        <h2>AI-Powered FAQ & Knowledge Base</h2>
        
        {!props.azureOpenAiApiKey && (
          <MessageBar messageBarType={MessageBarType.warning}>
            Azure OpenAI is not configured. AI features will use mock responses. Please configure Azure OpenAI settings in web part properties.
          </MessageBar>
        )}
        
        <SearchBox
          placeholder="Search FAQ or ask a question..."
          value={searchQuery}
          onSearch={handleSearch}
          onChange={(_, newValue) => setSearchQuery(newValue || '')}
        />
        
        <Pivot>
          <PivotItem headerText="FAQ" itemIcon="Help">
            <Stack tokens={{ childrenGap: 15 }}>
              <CommandBar items={commandBarItems} />
              
              {loading ? (
                <Spinner size={SpinnerSize.large} label="Loading FAQ items..." />
              ) : (
                <DetailsList
                  items={faqItems}
                  columns={columns}
                  selectionMode={SelectionMode.none}
                  setKey="set"
                  layoutMode={0}
                  isHeaderVisible={true}
                />
              )}
            </Stack>
          </PivotItem>
          
          <PivotItem headerText="AI Chat" itemIcon="Robot">
            <Stack tokens={{ childrenGap: 15 }}>
              <Text variant="mediumPlus">Ask AI Assistant</Text>
              {renderChatMessages()}
            </Stack>
          </PivotItem>
        </Pivot>
        
        <Panel
          isOpen={showPanel}
          type={PanelType.medium}
          onDismiss={() => setShowPanel(false)}
          headerText="FAQ Details"
        >
          {selectedItem && (
            <Stack tokens={{ childrenGap: 15 }}>
              <Text variant="xLarge">{selectedItem.question}</Text>
              <Separator />
              <Text variant="medium">{selectedItem.answer}</Text>
              <Separator />
              <Text variant="small">Category: {selectedItem.category}</Text>
              <Text variant="small">Tags: {selectedItem.tags.join(', ')}</Text>
              <Rating rating={selectedItem.rating} max={5} readOnly />
              <Text variant="small">Views: {selectedItem.viewCount}</Text>
              <Text variant="small">Last Updated: {selectedItem.lastUpdated.toLocaleDateString()}</Text>
            </Stack>
          )}
        </Panel>
      </Stack>
    </div>
  );
};

export default AiPoweredFaq;