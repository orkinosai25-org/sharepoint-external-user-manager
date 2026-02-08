# ClientSpace Branding Pack

**Version:** 1.0.0  
**Product:** ClientSpace – Universal External Collaboration for Microsoft 365  
**Last Updated:** February 8, 2026

---

## 📦 What's Included

This branding pack contains everything you need to implement ClientSpace brand across all platforms:

### 1. **Logo Assets** (`/logos`)
- ✅ Primary horizontal logo (light & dark variants)
- ✅ Icon-only square logo (light & dark variants)
- ✅ AppSource-ready 96x96px icon
- ✅ All logos in SVG format (scalable, production-ready)

### 2. **Color System** (`/colors`)
- ✅ SharePoint-aligned primary blue palette
- ✅ Azure/teal secondary colors
- ✅ Complete neutral gray scale
- ✅ Semantic status colors (success, warning, error, info)
- ✅ WCAG 2.1 AA compliant contrast ratios
- ✅ Dark theme variants
- ✅ Available in CSS, JSON, and documentation formats

### 3. **Typography System** (`/typography`)
- ✅ Segoe UI font stack (Microsoft Fluent compliant)
- ✅ Complete type scale (display, headings, body)
- ✅ Responsive typography rules
- ✅ Accessibility-friendly sizing and line heights
- ✅ Ready-to-use CSS classes

### 4. **UI Style Tokens** (`/ui-tokens`)
- ✅ Spacing system (8px base unit)
- ✅ Elevation (shadow) levels
- ✅ Border radius values
- ✅ Button styles (primary, secondary, text)
- ✅ Form input components
- ✅ Table styles
- ✅ Status badges
- ✅ Card components
- ✅ Animation/transition tokens
- ✅ Z-index scale

### 5. **Brand Guidelines** (`/guidelines`)
- ✅ Complete brand usage guidelines
- ✅ Logo usage rules (do's and don'ts)
- ✅ Color application examples
- ✅ Typography best practices
- ✅ UI component guidance
- ✅ Platform-specific guidelines (Blazor, SPFx, AppSource)

### 6. **Ready-to-Use Assets** (`/assets`)
- ✅ Combined CSS file for one-import integration
- ✅ All assets optimized for production

---

## 🚀 Quick Start

### Option 1: Import All Styles (Recommended)

```html
<!-- Import the complete ClientSpace design system -->
<link rel="stylesheet" href="/docs/branding/assets/clientspace-complete.css">
```

### Option 2: Import Individual Modules

```html
<!-- Import only what you need -->
<link rel="stylesheet" href="/docs/branding/colors/clientspace-colors.css">
<link rel="stylesheet" href="/docs/branding/typography/clientspace-typography.css">
<link rel="stylesheet" href="/docs/branding/ui-tokens/clientspace-ui-tokens.css">
```

### Option 3: Use CSS Variables Directly

```css
/* In your custom CSS */
.my-button {
  background-color: var(--clientspace-primary);
  color: var(--clientspace-text-white);
  padding: var(--clientspace-spacing-md);
  border-radius: var(--clientspace-radius-md);
  font-family: var(--clientspace-font-family);
}
```

---

## 🎨 Usage Examples

### Using Logo in HTML

```html
<!-- Light theme -->
<img src="/docs/branding/logos/clientspace-logo-horizontal-light.svg" 
     alt="ClientSpace" 
     height="48">

<!-- Dark theme -->
<img src="/docs/branding/logos/clientspace-logo-horizontal-dark.svg" 
     alt="ClientSpace" 
     height="48">

<!-- Icon only (e.g., favicon) -->
<link rel="icon" 
      type="image/svg+xml" 
      href="/docs/branding/logos/clientspace-icon-light.svg">
```

### Using Pre-built Components

```html
<!-- Primary Button -->
<button class="clientspace-button-primary">
  Add External User
</button>

<!-- Form Input -->
<div>
  <label class="clientspace-label required">Email Address</label>
  <input type="email" 
         class="clientspace-input" 
         placeholder="user@example.com">
  <span class="clientspace-helper-text">Enter a valid email</span>
</div>

<!-- Status Badge -->
<span class="clientspace-badge clientspace-badge-success">Active</span>

<!-- Card -->
<div class="clientspace-card">
  <div class="clientspace-card-header">
    <h3 class="clientspace-card-title">Client Space</h3>
  </div>
  <div class="clientspace-card-body">
    <p class="clientspace-body">Manage external collaborators</p>
  </div>
</div>
```

### Using in React (SPFx)

```tsx
import { PrimaryButton, TextField } from '@fluentui/react';

// Import ClientSpace CSS
import '../../branding/assets/clientspace-complete.css';

export const MyComponent: React.FC = () => {
  return (
    <>
      <TextField 
        label="Email Address"
        className="clientspace-input"
      />
      <PrimaryButton 
        styles={{ 
          root: { backgroundColor: 'var(--clientspace-primary)' }
        }}
      >
        Add User
      </PrimaryButton>
    </>
  );
};
```

### Using in Blazor

```razor
@* Import ClientSpace styles in _Host.cshtml or App.razor *@
<link href="/docs/branding/assets/clientspace-complete.css" rel="stylesheet" />

@* Use with MudBlazor *@
<MudCard Class="clientspace-card">
    <MudCardHeader>
        <MudText Typo="Typo.h4" Class="clientspace-card-title">
            Client Dashboard
        </MudText>
    </MudCardHeader>
    <MudCardContent Class="clientspace-card-body">
        <MudText Class="clientspace-body">
            Manage your external collaborators
        </MudText>
    </MudCardContent>
    <MudCardActions Class="clientspace-card-footer">
        <MudButton Variant="Variant.Filled" 
                   Color="Color.Primary" 
                   Class="clientspace-button-primary">
            Add User
        </MudButton>
    </MudCardActions>
</MudCard>
```

---

## 📋 Brand Standards Summary

### Logo
- **Minimum width:** 120px (digital)
- **Clear space:** 0.5x logo height on all sides
- **Formats:** SVG (preferred), PNG for raster needs

### Colors
- **Primary:** `#0078D4` (SharePoint Blue)
- **Secondary:** `#008272` (Azure Teal)
- **All colors:** WCAG 2.1 AA compliant

### Typography
- **Font:** Segoe UI (with system fallbacks)
- **Body size:** 14px / 0.875rem
- **Line height:** 1.6 for body text

### Spacing
- **Base unit:** 8px
- **Common values:** 4px, 8px, 16px, 24px, 32px

### Accessibility
- ✅ WCAG 2.1 AA compliant contrast ratios
- ✅ Keyboard navigation support
- ✅ Screen reader friendly markup
- ✅ Focus indicators on all interactive elements

---

## 🎯 Design Principles

### 1. Microsoft Fluent Aligned
ClientSpace follows Microsoft Fluent Design principles for seamless integration with Microsoft 365 and SharePoint.

### 2. SharePoint Native Look
Colors and components designed to feel native to SharePoint environments.

### 3. Universal Appeal
No industry-specific imagery – suitable for legal, projects, client delivery, and any enterprise use case.

### 4. Enterprise Grade
Professional, trustworthy design that meets enterprise standards.

### 5. Accessible First
All components meet WCAG 2.1 AA standards out of the box.

---

## 📁 File Structure

```
/docs/branding/
│
├── README.md (this file)
│
├── logos/
│   ├── clientspace-logo-horizontal-light.svg
│   ├── clientspace-logo-horizontal-dark.svg
│   ├── clientspace-icon-light.svg
│   ├── clientspace-icon-dark.svg
│   └── clientspace-appsource-icon.svg
│
├── colors/
│   ├── color-palette.md
│   ├── clientspace-colors.css
│   └── clientspace-colors.json
│
├── typography/
│   ├── typography-system.md
│   └── clientspace-typography.css
│
├── ui-tokens/
│   ├── ui-style-tokens.md
│   └── clientspace-ui-tokens.css
│
├── guidelines/
│   └── branding-guidelines.md
│
└── assets/
    └── clientspace-complete.css (all-in-one)
```

---

## 🔧 Integration Guides

### For Blazor Developers

1. Reference the complete CSS in `_Host.cshtml`:
   ```html
   <link href="/docs/branding/assets/clientspace-complete.css" rel="stylesheet" />
   ```

2. Use CSS variables in your custom components:
   ```css
   .my-component {
     background: var(--clientspace-primary);
     padding: var(--clientspace-spacing-md);
   }
   ```

3. Apply pre-built classes to MudBlazor components:
   ```razor
   <MudButton Class="clientspace-button-primary">Action</MudButton>
   ```

### For SPFx Developers

1. Import CSS in your web part:
   ```typescript
   import '../../branding/assets/clientspace-complete.css';
   ```

2. Use Fluent UI React with ClientSpace theming:
   ```tsx
   import { createTheme } from '@fluentui/react';
   
   const clientSpaceTheme = createTheme({
     palette: {
       themePrimary: '#0078D4',
       // ... see colors/clientspace-colors.json
     }
   });
   ```

3. Apply ClientSpace classes to your components:
   ```tsx
   <div className="clientspace-card">
     {/* Your content */}
   </div>
   ```

### For AppSource Listings

1. Use the AppSource icon: `logos/clientspace-appsource-icon.svg`
2. Ensure screenshots show ClientSpace branding
3. Use primary color in promotional graphics
4. Follow brand voice in listing description

---

## ✅ Compliance Checklist

Before shipping UI:

- [ ] Logo has proper clear space
- [ ] Logo minimum size requirements met
- [ ] Colors pass WCAG 2.1 AA contrast tests
- [ ] Typography uses Segoe UI font stack
- [ ] Minimum font size is 12px
- [ ] Interactive elements have focus indicators
- [ ] Status colors used semantically
- [ ] Spacing follows 8px grid system
- [ ] Components use elevation appropriately
- [ ] Dark theme variants provided (if applicable)

---

## 🆘 Support

### Documentation
- **Color Palette:** `colors/color-palette.md`
- **Typography:** `typography/typography-system.md`
- **UI Tokens:** `ui-tokens/ui-style-tokens.md`
- **Brand Guidelines:** `guidelines/branding-guidelines.md`

### Questions?
- Open an issue in the repository
- Tag with `branding` or `design` labels
- Include screenshots when relevant

---

## 📊 What's Next?

### Future Enhancements
- [ ] PNG logo exports (24x24, 32x32, 48x48, 96x96, 256x256)
- [ ] Figma design file
- [ ] Sketch design file
- [ ] Adobe XD design file
- [ ] Icon library expansion
- [ ] Email template library
- [ ] PowerPoint template

---

## 📜 License

This branding pack is proprietary to ClientSpace and is intended for use in ClientSpace product implementations only.

**© 2026 ClientSpace. All rights reserved.**

---

## 🎉 Ready to Build!

You now have everything needed to build consistent, accessible, enterprise-grade UIs for ClientSpace.

**Happy coding!** 🚀
