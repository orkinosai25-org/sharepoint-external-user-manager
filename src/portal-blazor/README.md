# Blazor SaaS Portal

## Overview
This directory will contain the Blazor Web App for the SharePoint External User Manager SaaS portal.

## Planned Features
- **Pricing Page**: Display subscription tiers (Starter, Professional, Business, Enterprise)
- **Sign-up/Onboarding**: Wizard for new tenant onboarding
  - Sign in with Microsoft (Entra ID)
  - Grant tenant consent
  - Choose subscription plan
  - Stripe checkout (non-enterprise)
- **Dashboard**: 
  - Client list management
  - Create client spaces
  - Subscription status (plan, usage)
  - Link to install SPFx package

## Technology Stack
- **Framework**: ASP.NET Core Blazor Web App (.NET 8)
- **Authentication**: Microsoft Entra ID (Azure AD)
- **UI**: Blazor Server or Blazor WebAssembly
- **Styling**: Bootstrap 5 + Custom CSS

## Getting Started
Coming in ISSUE-08 implementation.

## API Integration
The portal will communicate with the backend API at `/src/api-dotnet` for all data operations.
