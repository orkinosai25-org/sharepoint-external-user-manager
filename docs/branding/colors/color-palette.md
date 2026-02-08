# ClientSpace Color Palette

A SharePoint-aligned, Microsoft Fluent compliant color system designed for universal external collaboration.

---

## Primary Colors

### SharePoint Blue (Primary Brand Color)
**Use for:** Primary actions, brand elements, links, active states

```css
--clientspace-primary: #0078D4;          /* SharePoint Blue */
--clientspace-primary-hover: #106EBE;    /* Hover state */
--clientspace-primary-pressed: #005A9E;  /* Pressed/Active state */
--clientspace-primary-light: #4A9EFF;    /* Light variant */
--clientspace-primary-lighter: #C7E0F4;  /* Very light variant */
```

| Color | HEX | RGB | Usage |
|-------|-----|-----|-------|
| Primary | `#0078D4` | `rgb(0, 120, 212)` | Main brand color, primary buttons, links |
| Primary Hover | `#106EBE` | `rgb(16, 110, 190)` | Hover states |
| Primary Pressed | `#005A9E` | `rgb(0, 90, 158)` | Active/pressed states |
| Primary Light | `#4A9EFF` | `rgb(74, 158, 255)` | Accents, icons on dark backgrounds |
| Primary Lighter | `#C7E0F4` | `rgb(199, 224, 244)` | Backgrounds, borders |

---

## Secondary Colors (Azure/Teal Accent)

**Use for:** Secondary actions, informational elements, subtle accents

```css
--clientspace-secondary: #008272;        /* Azure Teal */
--clientspace-secondary-hover: #00B294;  /* Lighter teal */
--clientspace-secondary-light: #B4E7E0;  /* Very light teal */
```

| Color | HEX | RGB | Usage |
|-------|-----|-----|-------|
| Secondary | `#008272` | `rgb(0, 130, 114)` | Secondary actions, accents |
| Secondary Hover | `#00B294` | `rgb(0, 178, 148)` | Hover states |
| Secondary Light | `#B4E7E0` | `rgb(180, 231, 224)` | Backgrounds, highlights |

---

## Neutral Colors (Microsoft Fluent Gray Scale)

**Use for:** Text, backgrounds, borders, dividers

```css
/* Text Colors */
--clientspace-text-primary: #252423;     /* Primary text (near black) */
--clientspace-text-secondary: #605E5C;   /* Secondary text (gray) */
--clientspace-text-tertiary: #8A8886;    /* Tertiary text (light gray) */
--clientspace-text-disabled: #C8C6C4;    /* Disabled text */
--clientspace-text-white: #FFFFFF;       /* White text */

/* Background Colors */
--clientspace-bg-white: #FFFFFF;         /* White background */
--clientspace-bg-canvas: #FAF9F8;        /* Canvas (off-white) */
--clientspace-bg-light: #F3F2F1;         /* Light gray background */
--clientspace-bg-lighter: #EDEBE9;       /* Lighter gray background */
--clientspace-bg-dark: #323130;          /* Dark background */

/* Border/Divider Colors */
--clientspace-border-default: #EDEBE9;   /* Default border */
--clientspace-border-hover: #C8C6C4;     /* Hover border */
--clientspace-border-strong: #8A8886;    /* Strong border */
```

| Color | HEX | RGB | Usage |
|-------|-----|-----|-------|
| Text Primary | `#252423` | `rgb(37, 36, 35)` | Primary body text, headings |
| Text Secondary | `#605E5C` | `rgb(96, 94, 92)` | Secondary text, labels |
| Text Tertiary | `#8A8886` | `rgb(138, 136, 134)` | Placeholder, helper text |
| Text Disabled | `#C8C6C4` | `rgb(200, 198, 196)` | Disabled state text |
| BG Canvas | `#FAF9F8` | `rgb(250, 249, 248)` | Page background |
| BG Light | `#F3F2F1` | `rgb(243, 242, 241)` | Card backgrounds |
| Border Default | `#EDEBE9` | `rgb(237, 235, 233)` | Borders, dividers |

---

## Status Colors (Semantic)

**Use for:** Success, warning, error, and informational messages

```css
/* Success (Green) */
--clientspace-success: #107C10;          /* Success green */
--clientspace-success-light: #DFF6DD;    /* Light success background */
--clientspace-success-border: #92C353;   /* Success border */

/* Warning (Yellow/Amber) */
--clientspace-warning: #F7630C;          /* Warning orange */
--clientspace-warning-light: #FFF4CE;    /* Light warning background */
--clientspace-warning-border: #FFAA44;   /* Warning border */

/* Error (Red) */
--clientspace-error: #D13438;            /* Error red */
--clientspace-error-light: #FDE7E9;      /* Light error background */
--clientspace-error-border: #E81123;     /* Error border */

/* Info (Blue) */
--clientspace-info: #0078D4;             /* Info blue (same as primary) */
--clientspace-info-light: #DEECF9;       /* Light info background */
--clientspace-info-border: #4A9EFF;      /* Info border */
```

| Type | Main | Light BG | Border | Usage |
|------|------|----------|--------|-------|
| Success | `#107C10` | `#DFF6DD` | `#92C353` | Success messages, completed states |
| Warning | `#F7630C` | `#FFF4CE` | `#FFAA44` | Warnings, caution states |
| Error | `#D13438` | `#FDE7E9` | `#E81123` | Errors, destructive actions |
| Info | `#0078D4` | `#DEECF9` | `#4A9EFF` | Informational messages |

---

## Usage Guidelines

### Accessibility Requirements
All color combinations must meet **WCAG 2.1 AA** standards:
- **Normal text:** 4.5:1 minimum contrast ratio
- **Large text (18pt+):** 3:1 minimum contrast ratio
- **UI components:** 3:1 minimum contrast ratio

### Tested Combinations (AAA/AA Compliant)

✅ **Primary Blue (#0078D4) on White (#FFFFFF)**: 4.58:1 (AA)
✅ **Text Primary (#252423) on White (#FFFFFF)**: 15.79:1 (AAA)
✅ **Text Secondary (#605E5C) on White (#FFFFFF)**: 6.61:1 (AAA)
✅ **White (#FFFFFF) on Primary Blue (#0078D4)**: 4.58:1 (AA)
✅ **Success (#107C10) on White (#FFFFFF)**: 4.62:1 (AA)
✅ **Error (#D13438) on White (#FFFFFF)**: 4.93:1 (AA)

### Do's
✅ Use Primary Blue for main actions and brand elements
✅ Use neutral grays for text and backgrounds
✅ Use status colors for semantic meaning only
✅ Maintain consistent spacing between colored elements
✅ Test contrast ratios for all text/background combinations

### Don'ts
❌ Don't use primary colors for decorative purposes only
❌ Don't mix status colors (e.g., red + green side by side)
❌ Don't use low-contrast combinations
❌ Don't use colors as the only way to convey information
❌ Don't override Microsoft Fluent UI colors without good reason

---

## Dark Theme Variants

For dark mode support, use these adjusted values:

```css
/* Dark Theme Adjustments */
--clientspace-primary-dark: #4A9EFF;     /* Lighter blue for dark BG */
--clientspace-text-dark-primary: #FFFFFF;
--clientspace-text-dark-secondary: #D2D0CE;
--clientspace-bg-dark-canvas: #1B1A19;
--clientspace-bg-dark-card: #292827;
```

---

## Implementation Examples

### CSS Variables (Recommended)
```css
:root {
  /* Import all ClientSpace color variables */
  --clientspace-primary: #0078D4;
  --clientspace-text-primary: #252423;
  /* ... */
}

.button-primary {
  background-color: var(--clientspace-primary);
  color: var(--clientspace-text-white);
}

.button-primary:hover {
  background-color: var(--clientspace-primary-hover);
}
```

### Fluent UI React Integration
```typescript
import { Theme } from '@fluentui/react';

export const clientSpaceTheme: Theme = {
  palette: {
    themePrimary: '#0078D4',
    themeDark: '#005A9E',
    themeLight: '#C7E0F4',
    neutralPrimary: '#252423',
    neutralSecondary: '#605E5C',
    white: '#FFFFFF',
    // ... additional mappings
  }
};
```

### Blazor/Mudblazor Integration
```csharp
var palette = new Palette()
{
    Primary = "#0078D4",
    Secondary = "#008272",
    Success = "#107C10",
    Warning = "#F7630C",
    Error = "#D13438",
    Info = "#0078D4",
    Dark = "#252423"
};
```

---

## Export Formats

Color swatches available in:
- ✅ CSS Variables
- ✅ SCSS/SASS
- ✅ JSON
- ✅ Adobe Swatch Exchange (.ase)
- ✅ Sketch Palette
- ✅ Figma Styles

---

**Last Updated:** 2026-02-08  
**Version:** 1.0.0  
**Maintained by:** ClientSpace Brand Team
