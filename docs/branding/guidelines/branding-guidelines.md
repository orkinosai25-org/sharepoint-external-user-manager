# ClientSpace Branding Guidelines

**Version:** 1.0.0  
**Last Updated:** February 8, 2026  
**Product:** ClientSpace – Universal External Collaboration for Microsoft 365

---

## Brand Overview

### What is ClientSpace?

ClientSpace is a universal external collaboration platform for Microsoft 365, designed for:
- Legal firms
- Project teams
- Client delivery organizations
- Enterprise collaboration
- Regulated industries

### Brand Promise

**"External collaboration, done properly"**

ClientSpace delivers secure, governed, Microsoft-native external collaboration that's enterprise-ready yet simple for non-technical users.

### Brand Values

- **Secure:** Enterprise-grade security and compliance
- **Governed:** Full control and auditability
- **Microsoft-native:** Seamlessly integrates with Microsoft 365
- **Enterprise-ready:** Built for scale and reliability
- **User-friendly:** Simple for everyone, regardless of technical expertise

---

## Visual Identity

### Design Language

ClientSpace follows **Microsoft Fluent Design System** principles:
- Clean, minimal aesthetic
- Depth through subtle shadows
- Motion and transitions
- Material-inspired surfaces
- Responsive and adaptive

### Design Philosophy

- **Professional, not corporate:** Approachable yet trustworthy
- **Calm, not boring:** Subtle elegance over flashy effects
- **Universal, not vertical-specific:** No industry-specific imagery
- **Modern, not trendy:** Timeless design that ages well

---

## Logo Usage

### Logo Variants

ClientSpace has 4 primary logo variants:

1. **Horizontal Logo (Light)** - Primary lockup for light backgrounds
2. **Horizontal Logo (Dark)** - For dark backgrounds and themes
3. **Icon-Only (Light)** - Square format for app icons, favicons
4. **Icon-Only (Dark)** - Square format for dark backgrounds
5. **AppSource Icon** - Optimized 96x96px for marketplace listings

### When to Use Each Variant

| Variant | Use Cases |
|---------|-----------|
| **Horizontal Light** | Website headers, email signatures, light UI backgrounds, presentations |
| **Horizontal Dark** | Dark mode UIs, dark backgrounds, footer areas |
| **Icon Light** | App icons, favicons, social media avatars, compact spaces |
| **Icon Dark** | Dark mode app icons, dark theme favicons |
| **AppSource Icon** | Microsoft AppSource listing, Teams app store |

### Logo Clear Space

Maintain clear space around the logo equal to **half the height** of the logo on all sides.

```
┌─────────────────────────────┐
│                             │
│   ┌─────────────────┐       │
│   │   ClientSpace   │       │
│   └─────────────────┘       │
│                             │
└─────────────────────────────┘
   ↑                         ↑
   Clear space = 0.5x height
```

### Minimum Sizes

- **Horizontal Logo:** Minimum width 120px (digital), 1 inch (print)
- **Icon-Only:** Minimum size 24x24px (digital), 0.25 inch (print)
- **AppSource Icon:** Exactly 96x96px

### Logo Don'ts

❌ **Never:**
- Change the logo colors
- Rotate or skew the logo
- Add effects (drop shadows, glows, gradients)
- Place on busy or low-contrast backgrounds
- Stretch or compress the logo
- Separate the icon from the text
- Use outdated logo versions

✅ **Always:**
- Use provided logo files
- Maintain proper clear space
- Ensure adequate contrast with background
- Use appropriate variant for context
- Scale proportionally

---

## Color Usage

### Primary Brand Color: SharePoint Blue

**Use for:**
- Primary call-to-action buttons
- Links and interactive elements
- Brand moments (headers, highlights)
- Active states

**Color:** `#0078D4`

### Secondary Color: Azure Teal

**Use for:**
- Secondary actions
- Subtle accents
- Informational callouts
- Supporting graphics

**Color:** `#008272`

### Neutral Palette

**Use for:**
- Body text: `#252423`
- Backgrounds: `#FAF9F8` (canvas), `#FFFFFF` (white)
- Borders and dividers: `#EDEBE9`

### Status Colors

**Success:** `#107C10` (Green) - Completed actions, success messages  
**Warning:** `#F7630C` (Orange) - Warnings, important notices  
**Error:** `#D13438` (Red) - Errors, destructive actions  
**Info:** `#0078D4` (Blue) - Informational messages

### Accessibility Requirements

All text/background combinations **must meet WCAG 2.1 AA standards:**
- Normal text: **4.5:1** minimum contrast ratio
- Large text: **3:1** minimum contrast ratio
- UI components: **3:1** minimum contrast ratio

### Color Application Examples

#### ✅ Do
- Use primary blue sparingly for emphasis
- Maintain consistent color meaning (e.g., red always = error)
- Test contrast ratios
- Use status colors semantically
- Provide non-color indicators (icons, text)

#### ❌ Don't
- Use color as the only way to convey information
- Mix multiple bright colors in one view
- Use off-brand colors without approval
- Use colors that fail accessibility tests
- Override Fluent UI colors unnecessarily

---

## Typography

### Font Family

**Primary:** Segoe UI (Microsoft's system font)

```
Font Stack: 'Segoe UI', -apple-system, BlinkMacSystemFont, 
            'Roboto', 'Helvetica Neue', sans-serif
```

**Monospace:** Consolas (for code and technical content)

### Type Hierarchy

| Element | Size | Weight | Usage |
|---------|------|--------|-------|
| Display | 68px | Semibold | Hero sections only |
| H1 | 42px | Semibold | Page titles |
| H2 | 32px | Semibold | Section headings |
| H3 | 24px | Semibold | Subsections |
| H4 | 20px | Semibold | Card titles |
| Body | 14px | Regular | Standard text |
| Small | 12px | Regular | Captions, helper text |

### Typography Don'ts

❌ **Never:**
- Use custom fonts without brand approval
- Use sizes smaller than 12px
- Use low-contrast text colors
- Use all caps for long text passages
- Stretch or condense fonts

✅ **Always:**
- Use semantic HTML headings (h1-h6)
- Maintain consistent line heights (1.6 for body)
- Apply responsive typography on mobile
- Test readability at actual size

---

## Imagery & Graphics

### Photography Style

**ClientSpace does NOT use photography.**

We rely on:
- Clean UI screenshots
- Abstract geometric patterns
- Microsoft Fluent iconography
- Data visualizations

### Iconography

Use **Fluent UI System Icons** exclusively:
- Consistent with Microsoft 365
- Available in multiple sizes (16px, 20px, 24px, 32px)
- Outlined style (not filled)
- Neutral colors with option for primary blue

**Icon Resources:**
- [Fluent UI Icons](https://react.fluentui.dev/?path=/docs/icons-catalog--page)
- Microsoft 365 icon set

### Illustration Style

If illustrations are needed:
- Use abstract, geometric shapes
- Stick to brand color palette
- Avoid human figures or industry-specific imagery
- Maintain professional, minimal aesthetic

---

## UI Components

### Buttons

**Primary Button:** High-contrast, primary color, for main actions  
**Secondary Button:** Outlined style for secondary actions  
**Text Button:** No background, for tertiary actions

### Form Inputs

- 32px height (standard)
- 4px border radius
- Clear focus indicators
- Error states with red border and helper text

### Cards

- 8px border radius
- Subtle elevation (shadow level 1)
- 24px padding
- Hover state with increased elevation

### Tables

- Light gray header background
- Subtle row borders
- Hover state highlighting
- Responsive on mobile (scroll or stack)

---

## Application in Different Contexts

### Blazor SaaS Portal

**Guidelines:**
- Use horizontal logo in header
- Apply full color palette and typography system
- Implement all UI tokens for consistency
- Follow card-based layouts for content areas
- Use MudBlazor components styled with ClientSpace tokens

**Example:**
```html
<MudAppBar Color="Color.Primary">
    <img src="logo-horizontal-dark.svg" height="32" />
</MudAppBar>
```

### SPFx Web Parts

**Guidelines:**
- Use icon-only logo in web part property panes
- Inherit SharePoint's Fluent UI theme
- Apply ClientSpace primary color for brand moments
- Keep UI lightweight and SharePoint-native
- Use Fluent UI React components

**Example:**
```tsx
import { PrimaryButton } from '@fluentui/react';

<PrimaryButton 
  styles={{ root: { backgroundColor: '#0078D4' }}}
>
  Add External User
</PrimaryButton>
```

### AppSource Listing

**Guidelines:**
- Use 96x96px AppSource icon
- Feature screenshots with ClientSpace branding visible
- Use brand colors in promotional graphics
- Include logo in marketing materials
- Highlight "Microsoft 365 native" positioning

**Required Assets:**
- AppSource icon (96x96px)
- Screenshots (1366x768px recommended)
- Video thumbnail (1280x720px)
- Partner logo (216x216px)

### Email & Documents

**Guidelines:**
- Use horizontal logo in email signatures
- Apply color sparingly in body content
- Use Segoe UI for consistency
- Include "ClientSpace" trademark correctly
- Link logo to product website

---

## Brand Voice & Messaging

### Tone of Voice

- **Professional:** We're experts, but not stuffy
- **Clear:** No jargon or buzzwords
- **Helpful:** User-focused, not product-focused
- **Confident:** We know our stuff

### Key Messages

- "External collaboration, done properly"
- "Secure by design, simple by nature"
- "Microsoft 365 native"
- "Built for compliance-first organizations"
- "No vertical limitations – universal collaboration"

### Writing Style

✅ **Do:**
- Use active voice
- Write concise sentences
- Focus on benefits, not features
- Address the user directly ("you", "your")
- Use sentence case for headings

❌ **Don't:**
- Use technical jargon without explanation
- Make exaggerated claims
- Compare directly to competitors
- Use ALL CAPS for emphasis
- Mention specific industries unless relevant

---

## Trademark Usage

### Product Name

**Correct:** ClientSpace  
**Incorrect:** Client Space, Clientspace, client space, CLIENTSPACE

### Trademark Symbol

Use ™ on first mention in marketing materials:

> ClientSpace™ is a universal external collaboration platform...

### Legal Attribution

Include in footers and legal pages:

> ClientSpace is a trademark of [Your Company Name]. Microsoft, Microsoft 365, SharePoint, and Teams are trademarks of Microsoft Corporation.

---

## File Organization

### Branding Pack Structure

```
/docs/branding/
├── logos/
│   ├── clientspace-logo-horizontal-light.svg
│   ├── clientspace-logo-horizontal-dark.svg
│   ├── clientspace-icon-light.svg
│   ├── clientspace-icon-dark.svg
│   └── clientspace-appsource-icon.svg
├── colors/
│   ├── color-palette.md
│   ├── clientspace-colors.css
│   └── clientspace-colors.json
├── typography/
│   ├── typography-system.md
│   └── clientspace-typography.css
├── ui-tokens/
│   ├── ui-style-tokens.md
│   └── clientspace-ui-tokens.css
├── guidelines/
│   └── branding-guidelines.md (this file)
└── assets/
    └── (ready-to-use compiled CSS)
```

---

## Quick Start for Developers

### 1. Import Core Styles

```html
<!-- In your HTML head -->
<link rel="stylesheet" href="/branding/colors/clientspace-colors.css">
<link rel="stylesheet" href="/branding/typography/clientspace-typography.css">
<link rel="stylesheet" href="/branding/ui-tokens/clientspace-ui-tokens.css">
```

### 2. Use CSS Variables

```css
.my-component {
  background-color: var(--clientspace-primary);
  color: var(--clientspace-text-white);
  padding: var(--clientspace-spacing-md);
  border-radius: var(--clientspace-radius-md);
}
```

### 3. Apply Component Classes

```html
<button class="clientspace-button-primary">
  Add User
</button>

<input type="text" class="clientspace-input" placeholder="Email address" />

<span class="clientspace-badge clientspace-badge-success">Active</span>
```

---

## Support & Questions

For questions about brand usage, contact:
- **Email:** branding@clientspace.io (example)
- **GitHub:** Open an issue in the repository
- **Slack:** #brand-design channel (if applicable)

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2026-02-08 | Initial branding pack release |

---

## Approval & Governance

**Brand Owner:** ClientSpace Product Team  
**Review Cycle:** Quarterly  
**Next Review:** 2026-05-08

All brand changes must be approved by the product team before implementation.

---

**© 2026 ClientSpace. All rights reserved.**
