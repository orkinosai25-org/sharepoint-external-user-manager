# ClientSpace Typography System

Professional, accessible typography aligned with Microsoft Fluent UI and SharePoint design language.

---

## Font Family

### Primary: Segoe UI
The primary font for ClientSpace is **Segoe UI**, Microsoft's system font family.

```css
--clientspace-font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, 'Roboto', 'Helvetica Neue', sans-serif;
```

**Fallback Stack:**
1. Segoe UI (Windows)
2. -apple-system (macOS/iOS)
3. BlinkMacSystemFont (Chrome on macOS)
4. Roboto (Android)
5. Helvetica Neue (older macOS)
6. sans-serif (system default)

### Monospace: Consolas
For code snippets and technical content:

```css
--clientspace-font-family-mono: 'Consolas', 'SF Mono', 'Monaco', 'Courier New', monospace;
```

---

## Type Scale

Following Microsoft Fluent UI's type ramp for consistency with SharePoint.

### Display & Headings

| Level | Size | Line Height | Weight | Letter Spacing | Usage |
|-------|------|-------------|--------|----------------|-------|
| **Display** | 68px / 4.25rem | 1.1 | 600 Semibold | -0.5px | Hero sections only |
| **H1** | 42px / 2.625rem | 1.2 | 600 Semibold | -0.5px | Page titles |
| **H2** | 32px / 2rem | 1.25 | 600 Semibold | -0.25px | Section headings |
| **H3** | 24px / 1.5rem | 1.3 | 600 Semibold | 0 | Subsection headings |
| **H4** | 20px / 1.25rem | 1.4 | 600 Semibold | 0 | Card titles |
| **H5** | 16px / 1rem | 1.4 | 600 Semibold | 0 | Minor headings |
| **H6** | 14px / 0.875rem | 1.4 | 600 Semibold | 0 | Small headings |

### Body Text

| Level | Size | Line Height | Weight | Usage |
|-------|------|-------------|--------|-------|
| **Large Body** | 18px / 1.125rem | 1.6 | 400 Regular | Emphasized content, introductions |
| **Body** | 14px / 0.875rem | 1.6 | 400 Regular | Standard body text, default |
| **Small Body** | 12px / 0.75rem | 1.5 | 400 Regular | Secondary information, captions |

### UI Elements

| Element | Size | Weight | Usage |
|---------|------|--------|-------|
| **Button** | 14px / 0.875rem | 600 Semibold | All buttons |
| **Input** | 14px / 0.875rem | 400 Regular | Form inputs |
| **Label** | 14px / 0.875rem | 600 Semibold | Form labels |
| **Caption** | 12px / 0.75rem | 400 Regular | Helper text, descriptions |
| **Badge** | 12px / 0.75rem | 600 Semibold | Status badges, tags |

---

## CSS Variables

```css
:root {
  /* Font Families */
  --clientspace-font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, 'Roboto', 'Helvetica Neue', sans-serif;
  --clientspace-font-family-mono: 'Consolas', 'SF Mono', 'Monaco', 'Courier New', monospace;

  /* Font Sizes */
  --clientspace-font-size-display: 4.25rem;    /* 68px */
  --clientspace-font-size-h1: 2.625rem;        /* 42px */
  --clientspace-font-size-h2: 2rem;            /* 32px */
  --clientspace-font-size-h3: 1.5rem;          /* 24px */
  --clientspace-font-size-h4: 1.25rem;         /* 20px */
  --clientspace-font-size-h5: 1rem;            /* 16px */
  --clientspace-font-size-h6: 0.875rem;        /* 14px */
  --clientspace-font-size-body-lg: 1.125rem;   /* 18px */
  --clientspace-font-size-body: 0.875rem;      /* 14px */
  --clientspace-font-size-body-sm: 0.75rem;    /* 12px */

  /* Font Weights */
  --clientspace-font-weight-regular: 400;
  --clientspace-font-weight-semibold: 600;
  --clientspace-font-weight-bold: 700;

  /* Line Heights */
  --clientspace-line-height-display: 1.1;
  --clientspace-line-height-heading: 1.3;
  --clientspace-line-height-body: 1.6;
  --clientspace-line-height-compact: 1.4;

  /* Letter Spacing */
  --clientspace-letter-spacing-tight: -0.5px;
  --clientspace-letter-spacing-normal: 0;
  --clientspace-letter-spacing-wide: 0.5px;
}
```

---

## CSS Classes

### Heading Classes

```css
.clientspace-display {
  font-family: var(--clientspace-font-family);
  font-size: var(--clientspace-font-size-display);
  font-weight: var(--clientspace-font-weight-semibold);
  line-height: var(--clientspace-line-height-display);
  letter-spacing: var(--clientspace-letter-spacing-tight);
}

.clientspace-h1 {
  font-family: var(--clientspace-font-family);
  font-size: var(--clientspace-font-size-h1);
  font-weight: var(--clientspace-font-weight-semibold);
  line-height: 1.2;
  letter-spacing: var(--clientspace-letter-spacing-tight);
}

.clientspace-h2 {
  font-family: var(--clientspace-font-family);
  font-size: var(--clientspace-font-size-h2);
  font-weight: var(--clientspace-font-weight-semibold);
  line-height: 1.25;
  letter-spacing: -0.25px;
}

.clientspace-h3 {
  font-family: var(--clientspace-font-family);
  font-size: var(--clientspace-font-size-h3);
  font-weight: var(--clientspace-font-weight-semibold);
  line-height: var(--clientspace-line-height-heading);
}

/* ... similar for h4, h5, h6 */
```

### Body Classes

```css
.clientspace-body-large {
  font-family: var(--clientspace-font-family);
  font-size: var(--clientspace-font-size-body-lg);
  line-height: var(--clientspace-line-height-body);
}

.clientspace-body {
  font-family: var(--clientspace-font-family);
  font-size: var(--clientspace-font-size-body);
  line-height: var(--clientspace-line-height-body);
}

.clientspace-body-small {
  font-family: var(--clientspace-font-family);
  font-size: var(--clientspace-font-size-body-sm);
  line-height: 1.5;
}
```

---

## Accessibility Guidelines

### Minimum Sizes
- ✅ **Minimum readable size:** 12px (0.75rem)
- ✅ **Minimum body text:** 14px (0.875rem)
- ✅ **Minimum clickable text:** 14px (0.875rem)

### Line Length
- ✅ **Optimal:** 50-75 characters per line
- ✅ **Maximum:** 90 characters per line
- ❌ Avoid lines longer than 90 characters

### Line Height
- ✅ **Body text:** 1.5-1.6 (150%-160%)
- ✅ **Headings:** 1.2-1.3 (120%-130%)
- ❌ Avoid line heights below 1.4 for body text

### Color Contrast
All text must meet **WCAG 2.1 AA** standards:
- Normal text: 4.5:1 contrast ratio
- Large text (18px+ or 14px+ bold): 3:1 contrast ratio

---

## Responsive Typography

### Desktop (≥1024px)
Use standard sizes as defined above.

### Tablet (768px - 1023px)
```css
@media (max-width: 1023px) {
  :root {
    --clientspace-font-size-display: 3rem;    /* 48px */
    --clientspace-font-size-h1: 2rem;         /* 32px */
    --clientspace-font-size-h2: 1.5rem;       /* 24px */
  }
}
```

### Mobile (≤767px)
```css
@media (max-width: 767px) {
  :root {
    --clientspace-font-size-display: 2rem;    /* 32px */
    --clientspace-font-size-h1: 1.5rem;       /* 24px */
    --clientspace-font-size-h2: 1.25rem;      /* 20px */
    --clientspace-font-size-h3: 1rem;         /* 16px */
  }
}
```

---

## Usage Examples

### In HTML
```html
<h1 class="clientspace-h1">Welcome to ClientSpace</h1>
<p class="clientspace-body">
  Secure external collaboration for Microsoft 365.
</p>
<p class="clientspace-body-small">
  Last updated: February 8, 2026
</p>
```

### In Blazor
```razor
<MudText Typo="Typo.h1" Class="clientspace-h1">
    Client Dashboard
</MudText>
<MudText Typo="Typo.body1" Class="clientspace-body">
    Manage your external collaborators
</MudText>
```

### In React (Fluent UI)
```tsx
import { Text } from '@fluentui/react';

<Text variant="xLarge" className="clientspace-h1">
  Client Management
</Text>
<Text variant="medium" className="clientspace-body">
  Add and manage external users
</Text>
```

---

## Font Loading Strategy

### Preload Critical Fonts
```html
<link rel="preload" href="/fonts/SegoeUI-Regular.woff2" as="font" type="font/woff2" crossorigin>
<link rel="preload" href="/fonts/SegoeUI-Semibold.woff2" as="font" type="font/woff2" crossorigin>
```

### Font Display
```css
@font-face {
  font-family: 'Segoe UI';
  src: url('/fonts/SegoeUI-Regular.woff2') format('woff2');
  font-weight: 400;
  font-style: normal;
  font-display: swap; /* Prevent FOIT, show fallback immediately */
}
```

---

## Do's and Don'ts

### Do's ✅
- Use Segoe UI as the primary font
- Follow the defined type scale
- Maintain consistent line heights
- Ensure adequate color contrast
- Use semantic HTML headings (h1-h6)
- Apply responsive typography on smaller screens

### Don'ts ❌
- Don't use custom fonts without approval
- Don't use font sizes smaller than 12px
- Don't apply multiple font families in one interface
- Don't stretch or condense fonts artificially
- Don't use low-contrast text colors
- Don't override Fluent UI typography without reason

---

**Last Updated:** 2026-02-08  
**Version:** 1.0.0  
**Maintained by:** ClientSpace Brand Team
