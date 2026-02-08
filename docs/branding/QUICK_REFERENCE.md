# ClientSpace Branding Quick Reference

**Version:** 1.0.0  
**One-page cheat sheet for developers**

---

## 🎯 Quick Import

```html
<!-- Single import for everything -->
<link rel="stylesheet" href="/docs/branding/assets/clientspace-complete.css">
```

---

## 🎨 Brand Colors (HEX)

| Color | HEX | Usage |
|-------|-----|-------|
| **Primary** | `#0078D4` | Main brand, primary buttons, links |
| **Primary Hover** | `#106EBE` | Hover states |
| **Secondary** | `#008272` | Secondary actions, accents |
| **Success** | `#107C10` | Success states, completed |
| **Warning** | `#F7630C` | Warnings, important notices |
| **Error** | `#D13438` | Errors, destructive actions |
| **Text Primary** | `#252423` | Body text, headings |
| **Text Secondary** | `#605E5C` | Secondary text, labels |
| **BG Canvas** | `#FAF9F8` | Page background |

---

## 📐 Typography

| Element | Size | Weight | CSS Class |
|---------|------|--------|-----------|
| **Display** | 68px | 600 | `.clientspace-display` |
| **H1** | 42px | 600 | `.clientspace-h1` |
| **H2** | 32px | 600 | `.clientspace-h2` |
| **H3** | 24px | 600 | `.clientspace-h3` |
| **Body** | 14px | 400 | `.clientspace-body` |
| **Small** | 12px | 400 | `.clientspace-body-small` |

**Font:** Segoe UI → -apple-system → BlinkMacSystemFont → Roboto → sans-serif

---

## 🔘 Buttons

```html
<!-- Primary -->
<button class="clientspace-button-primary">Save</button>

<!-- Secondary (Outline) -->
<button class="clientspace-button-secondary">Cancel</button>

<!-- Text (Ghost) -->
<button class="clientspace-button-text">Learn More</button>
```

**CSS Variables:**
```css
background: var(--clientspace-primary);
color: var(--clientspace-text-white);
```

---

## 📝 Form Inputs

```html
<label class="clientspace-label required">Email</label>
<input type="email" class="clientspace-input" placeholder="user@example.com">
<span class="clientspace-helper-text">Enter a valid email</span>

<!-- Error state -->
<input class="clientspace-input error">
<span class="clientspace-helper-text error">Invalid email</span>
```

---

## 🏷️ Status Badges

```html
<span class="clientspace-badge clientspace-badge-success">Active</span>
<span class="clientspace-badge clientspace-badge-warning">Pending</span>
<span class="clientspace-badge clientspace-badge-error">Blocked</span>
<span class="clientspace-badge clientspace-badge-info">Invited</span>
<span class="clientspace-badge clientspace-badge-neutral">Inactive</span>
```

---

## 📊 Tables

```html
<table class="clientspace-table">
  <thead>
    <tr>
      <th>Name</th>
      <th>Email</th>
      <th>Status</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>John Doe</td>
      <td>john@example.com</td>
      <td><span class="clientspace-badge clientspace-badge-success">Active</span></td>
    </tr>
  </tbody>
</table>
```

---

## 🃏 Cards

```html
<div class="clientspace-card">
  <div class="clientspace-card-header">
    <h3 class="clientspace-card-title">Card Title</h3>
  </div>
  <div class="clientspace-card-body">
    <p class="clientspace-body">Card content goes here</p>
  </div>
  <div class="clientspace-card-footer">
    <button class="clientspace-button-primary">Action</button>
    <button class="clientspace-button-text">Cancel</button>
  </div>
</div>
```

---

## 📏 Spacing

```css
--clientspace-spacing-xs: 4px;
--clientspace-spacing-sm: 8px;
--clientspace-spacing-md: 16px;   /* Default */
--clientspace-spacing-lg: 24px;
--clientspace-spacing-xl: 32px;
--clientspace-spacing-2xl: 48px;
```

**Usage:**
```css
padding: var(--clientspace-spacing-md);
margin-bottom: var(--clientspace-spacing-lg);
```

---

## 🌟 Elevation (Shadows)

```css
--clientspace-elevation-0: none;           /* Flat */
--clientspace-elevation-1: ...;             /* Cards */
--clientspace-elevation-2: ...;             /* Hover, dropdowns */
--clientspace-elevation-3: ...;             /* Modals */
--clientspace-elevation-4: ...;             /* Tooltips */
```

---

## 🔵 Border Radius

```css
--clientspace-radius-sm: 2px;    /* Badges */
--clientspace-radius-md: 4px;    /* Buttons, inputs (default) */
--clientspace-radius-lg: 8px;    /* Cards */
--clientspace-radius-xl: 12px;   /* Large cards */
--clientspace-radius-full: 9999px; /* Pills */
```

---

## 🖼️ Logo Usage

| Variant | File | Usage |
|---------|------|-------|
| **Horizontal Light** | `logos/clientspace-logo-horizontal-light.svg` | Light backgrounds, headers |
| **Horizontal Dark** | `logos/clientspace-logo-horizontal-dark.svg` | Dark backgrounds, dark mode |
| **Icon Light** | `logos/clientspace-icon-light.svg` | App icons, favicons |
| **Icon Dark** | `logos/clientspace-icon-dark.svg` | Dark mode icons |
| **AppSource** | `logos/clientspace-appsource-icon.svg` | AppSource listing (96x96) |

**Minimum Size:** 120px width (horizontal), 24px (icon)  
**Clear Space:** 0.5x logo height on all sides

---

## ⚡ React/SPFx Integration

```tsx
import '../../docs/branding/assets/clientspace-complete.css';

// Use Fluent UI with ClientSpace colors
import { PrimaryButton } from '@fluentui/react';

<PrimaryButton 
  styles={{ 
    root: { 
      backgroundColor: 'var(--clientspace-primary)',
      borderRadius: 'var(--clientspace-radius-md)'
    }
  }}
>
  Add User
</PrimaryButton>
```

---

## 🔥 Blazor Integration

```razor
@* In _Host.cshtml *@
<link href="/docs/branding/assets/clientspace-complete.css" rel="stylesheet" />

@* Use with MudBlazor *@
<MudButton Variant="Variant.Filled" 
           Color="Color.Primary"
           Class="clientspace-button-primary">
    Add User
</MudButton>

<MudTextField Label="Email" 
              Class="clientspace-input"
              Required="true" />
```

---

## ✅ Accessibility Checklist

- [x] All text/background combos meet **WCAG 2.1 AA** (4.5:1 contrast)
- [x] Minimum font size: **12px**
- [x] Focus indicators on all interactive elements
- [x] Status colors have non-color indicators (icons/text)
- [x] Semantic HTML (proper headings h1-h6)
- [x] Keyboard navigation support

---

## 🚫 Common Mistakes to Avoid

❌ Don't use custom colors without approval  
❌ Don't use font sizes smaller than 12px  
❌ Don't skip focus indicators  
❌ Don't use color alone to convey information  
❌ Don't modify logo files  
❌ Don't use low-contrast text/background combinations

---

## 📚 Full Documentation

- **Complete Guide:** `/docs/branding/README.md`
- **Color Palette:** `/docs/branding/colors/color-palette.md`
- **Typography:** `/docs/branding/typography/typography-system.md`
- **UI Tokens:** `/docs/branding/ui-tokens/ui-style-tokens.md`
- **Brand Guidelines:** `/docs/branding/guidelines/branding-guidelines.md`
- **Live Demo:** `/docs/branding/demo.html`

---

## 🎉 That's It!

You're ready to build consistent, accessible, enterprise-grade UIs for ClientSpace.

**Questions?** Open an issue with the `branding` label.
