# ClientSpace Branding Implementation Guide

**How to integrate ClientSpace branding into your Blazor Portal and SPFx Web Parts**

---

## Table of Contents

1. [Blazor Portal Integration](#blazor-portal-integration)
2. [SPFx Web Parts Integration](#spfx-web-parts-integration)
3. [AppSource Listing Setup](#appsource-listing-setup)
4. [Testing & Validation](#testing--validation)

---

## Blazor Portal Integration

### Step 1: Add CSS Reference

**In `src/portal-blazor/wwwroot/index.html` or `_Host.cshtml`:**

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title>ClientSpace Portal</title>
    <base href="/" />
    
    <!-- ClientSpace Branding -->
    <link rel="icon" type="image/svg+xml" href="/docs/branding/logos/clientspace-icon-light.svg">
    <link rel="stylesheet" href="/docs/branding/assets/clientspace-complete.css">
    
    <!-- MudBlazor (if using) -->
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
</head>
<body>
    <div id="app">Loading...</div>
    <script src="_framework/blazor.webassembly.js"></script>
</body>
</html>
```

### Step 2: Configure MudBlazor Theme

**Create `ClientSpaceTheme.cs`:**

```csharp
using MudBlazor;

namespace ClientSpace.Portal.Themes
{
    public static class ClientSpaceTheme
    {
        public static MudTheme Theme => new MudTheme
        {
            Palette = new Palette
            {
                Primary = "#0078D4",          // ClientSpace Primary Blue
                PrimaryDarken = "#005A9E",
                PrimaryLighten = "#C7E0F4",
                Secondary = "#008272",        // Azure Teal
                Success = "#107C10",
                Warning = "#F7630C",
                Error = "#D13438",
                Info = "#0078D4",
                Dark = "#252423",
                TextPrimary = "#252423",
                TextSecondary = "#605E5C",
                Background = "#FAF9F8",
                Surface = "#FFFFFF",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#252423",
                AppbarBackground = "#0078D4",
                AppbarText = "#FFFFFF",
            },
            
            Typography = new Typography
            {
                Default = new Default
                {
                    FontFamily = new[] { "Segoe UI", "-apple-system", "BlinkMacSystemFont", "sans-serif" },
                    FontSize = "0.875rem",
                    LineHeight = 1.6
                },
                H1 = new H1
                {
                    FontSize = "2.625rem",
                    FontWeight = 600,
                    LineHeight = 1.2
                },
                H2 = new H2
                {
                    FontSize = "2rem",
                    FontWeight = 600,
                    LineHeight = 1.25
                },
                H3 = new H3
                {
                    FontSize = "1.5rem",
                    FontWeight = 600,
                    LineHeight = 1.3
                },
                H4 = new H4
                {
                    FontSize = "1.25rem",
                    FontWeight = 600,
                    LineHeight = 1.4
                },
                Body1 = new Body1
                {
                    FontSize = "0.875rem",
                    LineHeight = 1.6
                }
            }
        };
    }
}
```

**Register in `Program.cs`:**

```csharp
using ClientSpace.Portal.Themes;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add MudBlazor with ClientSpace theme
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
});

await builder.Build().RunAsync();
```

**Apply theme in `MainLayout.razor`:**

```razor
@inherits LayoutComponentBase
@using ClientSpace.Portal.Themes

<MudThemeProvider Theme="@ClientSpaceTheme.Theme" />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="1">
        <img src="/docs/branding/logos/clientspace-logo-horizontal-dark.svg" 
             alt="ClientSpace" 
             height="32" 
             style="margin-right: 16px;" />
        <MudSpacer />
        <MudIconButton Icon="@Icons.Material.Filled.Menu" Color="Color.Inherit" Edge="Edge.End" />
    </MudAppBar>
    
    <MudMainContent Class="pt-4">
        <MudContainer MaxWidth="MaxWidth.ExtraLarge">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>
```

### Step 3: Use Branded Components

**Example: User Management Card**

```razor
@page "/users"

<MudCard Class="clientspace-card mb-4">
    <MudCardHeader>
        <MudText Typo="Typo.h4" Class="clientspace-card-title">
            External Users
        </MudText>
    </MudCardHeader>
    <MudCardContent Class="clientspace-card-body">
        <MudTable Items="@users" Hover="true" Class="clientspace-table">
            <HeaderContent>
                <MudTh>Name</MudTh>
                <MudTh>Email</MudTh>
                <MudTh>Status</MudTh>
                <MudTh>Actions</MudTh>
            </HeaderContent>
            <RowTemplate>
                <MudTd>@context.Name</MudTd>
                <MudTd>@context.Email</MudTd>
                <MudTd>
                    <span class="clientspace-badge clientspace-badge-success">Active</span>
                </MudTd>
                <MudTd>
                    <MudButton Size="Size.Small" 
                               Variant="Variant.Outlined" 
                               Class="clientspace-button-secondary">
                        Edit
                    </MudButton>
                </MudTd>
            </RowTemplate>
        </MudTable>
    </MudCardContent>
    <MudCardActions Class="clientspace-card-footer">
        <MudButton Variant="Variant.Filled" 
                   Color="Color.Primary" 
                   Class="clientspace-button-primary">
            Add User
        </MudButton>
    </MudCardActions>
</MudCard>

@code {
    private List<User> users = new();
}
```

---

## SPFx Web Parts Integration

### Step 1: Copy CSS to SPFx Project

```bash
# From repository root
cp docs/branding/assets/clientspace-complete.css src/client-spfx/src/styles/
```

### Step 2: Import in Web Part

**In `YourWebPart.module.scss`:**

```scss
@import '../../../styles/clientspace-complete.css';

.yourWebPart {
  // Your custom styles
  
  .container {
    padding: var(--clientspace-spacing-md);
  }
}
```

**Or directly in your web part TypeScript:**

```typescript
import '../../../styles/clientspace-complete.css';
```

### Step 3: Create Fluent UI Theme

**Create `ClientSpaceTheme.ts`:**

```typescript
import { createTheme, ITheme } from '@fluentui/react';

export const ClientSpaceTheme: ITheme = createTheme({
  palette: {
    themePrimary: '#0078D4',
    themeLighterAlt: '#f3f9fd',
    themeLighter: '#d0e7f8',
    themeLight: '#a9d3f2',
    themeTertiary: '#5ca9e5',
    themeSecondary: '#1a86d9',
    themeDarkAlt: '#006cbe',
    themeDark: '#005ba1',
    themeDarker: '#004377',
    neutralLighterAlt: '#faf9f8',
    neutralLighter: '#f3f2f1',
    neutralLight: '#edebe9',
    neutralQuaternaryAlt: '#e1dfdd',
    neutralQuaternary: '#d0d0d0',
    neutralTertiaryAlt: '#c8c6c4',
    neutralTertiary: '#a19f9d',
    neutralSecondary: '#605e5c',
    neutralPrimaryAlt: '#3b3a39',
    neutralPrimary: '#252423',
    neutralDark: '#201f1e',
    black: '#000000',
    white: '#ffffff',
  },
  fonts: {
    small: {
      fontSize: '12px',
      fontWeight: 400
    },
    medium: {
      fontSize: '14px',
      fontWeight: 400
    },
    mediumPlus: {
      fontSize: '16px',
      fontWeight: 400
    },
    large: {
      fontSize: '18px',
      fontWeight: 400
    },
    xLarge: {
      fontSize: '24px',
      fontWeight: 600
    },
    xxLarge: {
      fontSize: '32px',
      fontWeight: 600
    }
  }
});
```

### Step 4: Apply Theme in Web Part

**In your web part's render method:**

```typescript
import * as React from 'react';
import { ThemeProvider } from '@fluentui/react';
import { ClientSpaceTheme } from './ClientSpaceTheme';

export default class YourWebPart extends React.Component<IYourWebPartProps, {}> {
  public render(): React.ReactElement<IYourWebPartProps> {
    return (
      <ThemeProvider theme={ClientSpaceTheme}>
        <div className="clientspace-card">
          <div className="clientspace-card-header">
            <h3 className="clientspace-card-title">External Users</h3>
          </div>
          <div className="clientspace-card-body">
            {/* Your content */}
          </div>
        </div>
      </ThemeProvider>
    );
  }
}
```

### Step 5: Use Branded Components

**Example: User List Component**

```tsx
import * as React from 'react';
import { PrimaryButton, DefaultButton, DetailsList, IColumn } from '@fluentui/react';

export const UserList: React.FC = () => {
  const columns: IColumn[] = [
    { key: 'name', name: 'Name', fieldName: 'name', minWidth: 100 },
    { key: 'email', name: 'Email', fieldName: 'email', minWidth: 200 },
    {
      key: 'status',
      name: 'Status',
      fieldName: 'status',
      minWidth: 100,
      onRender: (item) => (
        <span className="clientspace-badge clientspace-badge-success">
          {item.status}
        </span>
      )
    }
  ];

  return (
    <div className="clientspace-card">
      <div className="clientspace-card-header">
        <h3 className="clientspace-card-title">External Users</h3>
      </div>
      <div className="clientspace-card-body">
        <DetailsList
          items={[]}
          columns={columns}
          className="clientspace-table"
        />
      </div>
      <div className="clientspace-card-footer">
        <PrimaryButton 
          text="Add User" 
          className="clientspace-button-primary"
        />
        <DefaultButton 
          text="Import" 
          className="clientspace-button-secondary"
        />
      </div>
    </div>
  );
};
```

---

## AppSource Listing Setup

### Required Assets

1. **App Icon (96x96px)**
   - Use: `/docs/branding/logos/clientspace-appsource-icon.svg`
   - Export as PNG: 96x96px

2. **Screenshots (1366x768px recommended)**
   - Capture actual ClientSpace UI
   - Ensure logo is visible
   - Show key features

3. **Hero Image (1920x1080px)**
   - Use brand colors
   - Feature logo prominently
   - Show product in action

### Brand Voice for Listing

**Title:** ClientSpace – External Collaboration for Microsoft 365

**Short Description:**
> Secure, governed external collaboration for SharePoint. Enterprise-ready client spaces, external user management, and compliance-first architecture.

**Long Description:**
```
ClientSpace delivers external collaboration, done properly.

Built for organizations that need:
✓ Secure external user management
✓ Governed client spaces
✓ Microsoft 365 native integration
✓ Compliance-first architecture
✓ Simple for non-technical users

Perfect for:
• Legal firms
• Project teams
• Client delivery
• Regulated industries
• Enterprise collaboration

Key Features:
- Automated client space provisioning
- External user lifecycle management
- SharePoint-native UI
- Audit logging and compliance
- Role-based access control

Microsoft 365 native. Enterprise-grade. Simple to use.
```

### Assets Checklist

- [ ] App icon (96x96px PNG)
- [ ] 3-5 screenshots (1366x768px)
- [ ] Hero image (1920x1080px)
- [ ] Short description (80 chars max)
- [ ] Long description (4000 chars max)
- [ ] Privacy policy URL
- [ ] Support URL
- [ ] Video URL (optional but recommended)

---

## Testing & Validation

### Visual Regression Testing

1. **Verify Logo Display**
   ```bash
   # Ensure logos render correctly at various sizes
   - 24x24px (favicon)
   - 32px height (app bar)
   - 48px height (hero)
   ```

2. **Check Color Contrast**
   - Use browser DevTools or online tools
   - All text must meet WCAG 2.1 AA (4.5:1)
   - Test both light and dark themes

3. **Test Responsive Typography**
   ```bash
   # Test at breakpoints
   - Desktop: 1920x1080
   - Tablet: 768x1024
   - Mobile: 375x667
   ```

### Accessibility Testing

```bash
# Run accessibility audits
npm run test:a11y

# Or use browser extensions:
# - axe DevTools
# - WAVE
# - Lighthouse
```

### Cross-Browser Testing

Test in:
- ✅ Edge (primary)
- ✅ Chrome
- ✅ Firefox
- ✅ Safari

### Validation Checklist

- [ ] Logo displays correctly at all sizes
- [ ] Colors match brand guidelines
- [ ] Typography uses Segoe UI
- [ ] All interactive elements have focus states
- [ ] Status colors have non-color indicators
- [ ] Spacing follows 8px grid
- [ ] Components use correct elevation
- [ ] Dark theme works (if applicable)
- [ ] Responsive on mobile/tablet
- [ ] Passes WCAG 2.1 AA

---

## Troubleshooting

### Issue: Styles Not Loading

**Solution:**
```html
<!-- Ensure correct path in _Host.cshtml or index.html -->
<link rel="stylesheet" href="/docs/branding/assets/clientspace-complete.css">
```

### Issue: Colors Look Wrong

**Solution:**
```css
/* Verify CSS variables are loaded */
:root {
  --clientspace-primary: #0078D4; /* Should be SharePoint blue */
}
```

### Issue: Fonts Not Rendering

**Solution:**
```css
/* Check font stack */
font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, 'Roboto', 'Helvetica Neue', sans-serif;
```

---

## Support

**Questions?** Open an issue with:
- `branding` label for design questions
- `integration` label for implementation questions
- Include screenshots when relevant

---

**Last Updated:** 2026-02-08  
**Version:** 1.0.0
