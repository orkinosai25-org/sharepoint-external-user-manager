# ClientSpace UI Style Tokens

Comprehensive design tokens for building consistent, accessible UIs aligned with Microsoft Fluent design language.

---

## Spacing System

Based on an 8px base unit for consistent rhythm and alignment.

### Base Unit
```css
--clientspace-spacing-unit: 8px;
```

### Spacing Scale

| Token | Value | Pixels | Usage |
|-------|-------|--------|-------|
| `--spacing-xs` | 0.25rem | 4px | Tight spacing, icon margins |
| `--spacing-sm` | 0.5rem | 8px | Small gaps, compact layouts |
| `--spacing-md` | 1rem | 16px | Default spacing, form fields |
| `--spacing-lg` | 1.5rem | 24px | Section spacing, card padding |
| `--spacing-xl` | 2rem | 32px | Large sections, page margins |
| `--spacing-2xl` | 3rem | 48px | Hero sections, major divisions |
| `--spacing-3xl` | 4rem | 64px | Extra large spacing |

```css
:root {
  --clientspace-spacing-xs: 0.25rem;    /* 4px */
  --clientspace-spacing-sm: 0.5rem;     /* 8px */
  --clientspace-spacing-md: 1rem;       /* 16px */
  --clientspace-spacing-lg: 1.5rem;     /* 24px */
  --clientspace-spacing-xl: 2rem;       /* 32px */
  --clientspace-spacing-2xl: 3rem;      /* 48px */
  --clientspace-spacing-3xl: 4rem;      /* 64px */
}
```

---

## Elevation (Shadows)

Fluent-style elevation using subtle shadows for depth.

### Shadow Tokens

```css
:root {
  /* No elevation */
  --clientspace-elevation-0: none;
  
  /* Subtle elevation - cards, panels */
  --clientspace-elevation-1: 0 1px 2px rgba(0, 0, 0, 0.05),
                              0 1px 3px rgba(0, 0, 0, 0.05);
  
  /* Low elevation - hover states, dropdowns */
  --clientspace-elevation-2: 0 2px 4px rgba(0, 0, 0, 0.07),
                              0 4px 8px rgba(0, 0, 0, 0.06);
  
  /* Medium elevation - modals, popovers */
  --clientspace-elevation-3: 0 4px 8px rgba(0, 0, 0, 0.08),
                              0 8px 16px rgba(0, 0, 0, 0.08);
  
  /* High elevation - dialogs, tooltips */
  --clientspace-elevation-4: 0 8px 16px rgba(0, 0, 0, 0.1),
                              0 16px 32px rgba(0, 0, 0, 0.1);
}
```

| Level | Usage |
|-------|-------|
| **0** | Flat elements, borders only |
| **1** | Cards, panels, list items |
| **2** | Hover states, dropdown menus |
| **3** | Modal dialogs, popovers, flyouts |
| **4** | Tooltips, context menus, notifications |

---

## Border Radius

Consistent corner rounding following Fluent UI standards.

```css
:root {
  --clientspace-radius-none: 0;
  --clientspace-radius-sm: 2px;      /* Small elements, badges */
  --clientspace-radius-md: 4px;      /* Buttons, inputs, default */
  --clientspace-radius-lg: 8px;      /* Cards, panels */
  --clientspace-radius-xl: 12px;     /* Large cards, modals */
  --clientspace-radius-full: 9999px; /* Pills, circular elements */
}
```

---

## Buttons

### Button Variants

#### Primary Button
```css
.clientspace-button-primary {
  background-color: var(--clientspace-primary);
  color: var(--clientspace-text-white);
  border: none;
  border-radius: var(--clientspace-radius-md);
  padding: 0.5rem 1.25rem;
  font-family: var(--clientspace-font-family);
  font-size: var(--clientspace-font-size-body);
  font-weight: var(--clientspace-font-weight-semibold);
  line-height: 1.4;
  cursor: pointer;
  transition: all 0.2s ease;
  min-height: 32px;
}

.clientspace-button-primary:hover {
  background-color: var(--clientspace-primary-hover);
}

.clientspace-button-primary:active {
  background-color: var(--clientspace-primary-pressed);
}

.clientspace-button-primary:disabled {
  background-color: var(--clientspace-bg-lighter);
  color: var(--clientspace-text-disabled);
  cursor: not-allowed;
}
```

#### Secondary Button (Outline)
```css
.clientspace-button-secondary {
  background-color: transparent;
  color: var(--clientspace-primary);
  border: 1px solid var(--clientspace-primary);
  border-radius: var(--clientspace-radius-md);
  padding: 0.5rem 1.25rem;
  font-family: var(--clientspace-font-family);
  font-size: var(--clientspace-font-size-body);
  font-weight: var(--clientspace-font-weight-semibold);
  line-height: 1.4;
  cursor: pointer;
  transition: all 0.2s ease;
  min-height: 32px;
}

.clientspace-button-secondary:hover {
  background-color: var(--clientspace-primary-lighter);
  border-color: var(--clientspace-primary-hover);
  color: var(--clientspace-primary-hover);
}
```

#### Text Button (Ghost)
```css
.clientspace-button-text {
  background-color: transparent;
  color: var(--clientspace-primary);
  border: none;
  padding: 0.5rem 1rem;
  font-family: var(--clientspace-font-family);
  font-size: var(--clientspace-font-size-body);
  font-weight: var(--clientspace-font-weight-semibold);
  cursor: pointer;
  transition: all 0.2s ease;
  min-height: 32px;
}

.clientspace-button-text:hover {
  background-color: var(--clientspace-bg-light);
  color: var(--clientspace-primary-hover);
}
```

### Button Sizes

| Size | Height | Padding | Font Size |
|------|--------|---------|-----------|
| **Small** | 24px | 0.25rem 0.75rem | 12px |
| **Medium** | 32px | 0.5rem 1.25rem | 14px (default) |
| **Large** | 40px | 0.75rem 1.5rem | 14px |

---

## Form Inputs

### Text Input
```css
.clientspace-input {
  background-color: var(--clientspace-bg-white);
  color: var(--clientspace-text-primary);
  border: 1px solid var(--clientspace-border-default);
  border-radius: var(--clientspace-radius-md);
  padding: 0.5rem 0.75rem;
  font-family: var(--clientspace-font-family);
  font-size: var(--clientspace-font-size-body);
  line-height: 1.4;
  height: 32px;
  transition: all 0.2s ease;
}

.clientspace-input:hover {
  border-color: var(--clientspace-border-hover);
}

.clientspace-input:focus {
  outline: none;
  border-color: var(--clientspace-primary);
  box-shadow: 0 0 0 1px var(--clientspace-primary);
}

.clientspace-input:disabled {
  background-color: var(--clientspace-bg-light);
  color: var(--clientspace-text-disabled);
  border-color: var(--clientspace-border-default);
  cursor: not-allowed;
}

.clientspace-input.error {
  border-color: var(--clientspace-error);
}

.clientspace-input.error:focus {
  border-color: var(--clientspace-error);
  box-shadow: 0 0 0 1px var(--clientspace-error);
}
```

### Label
```css
.clientspace-label {
  display: block;
  font-family: var(--clientspace-font-family);
  font-size: var(--clientspace-font-size-body);
  font-weight: var(--clientspace-font-weight-semibold);
  color: var(--clientspace-text-primary);
  margin-bottom: 0.25rem;
}

.clientspace-label.required::after {
  content: " *";
  color: var(--clientspace-error);
}
```

### Helper Text
```css
.clientspace-helper-text {
  font-size: var(--clientspace-font-size-body-sm);
  color: var(--clientspace-text-secondary);
  margin-top: 0.25rem;
}

.clientspace-helper-text.error {
  color: var(--clientspace-error);
}
```

---

## Tables

### Table Structure
```css
.clientspace-table {
  width: 100%;
  border-collapse: collapse;
  font-family: var(--clientspace-font-family);
  font-size: var(--clientspace-font-size-body);
}

.clientspace-table thead {
  background-color: var(--clientspace-bg-canvas);
  border-bottom: 2px solid var(--clientspace-border-default);
}

.clientspace-table th {
  padding: 0.75rem 1rem;
  text-align: left;
  font-weight: var(--clientspace-font-weight-semibold);
  color: var(--clientspace-text-primary);
}

.clientspace-table td {
  padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--clientspace-border-default);
  color: var(--clientspace-text-primary);
}

.clientspace-table tbody tr:hover {
  background-color: var(--clientspace-bg-canvas);
}

.clientspace-table tbody tr:last-child td {
  border-bottom: none;
}
```

---

## Status Badges

### Badge Styles
```css
.clientspace-badge {
  display: inline-flex;
  align-items: center;
  padding: 0.25rem 0.5rem;
  border-radius: var(--clientspace-radius-sm);
  font-size: var(--clientspace-font-size-body-sm);
  font-weight: var(--clientspace-font-weight-semibold);
  line-height: 1;
}

.clientspace-badge-success {
  background-color: var(--clientspace-success-light);
  color: var(--clientspace-success);
  border: 1px solid var(--clientspace-success-border);
}

.clientspace-badge-warning {
  background-color: var(--clientspace-warning-light);
  color: var(--clientspace-warning);
  border: 1px solid var(--clientspace-warning-border);
}

.clientspace-badge-error {
  background-color: var(--clientspace-error-light);
  color: var(--clientspace-error);
  border: 1px solid var(--clientspace-error-border);
}

.clientspace-badge-info {
  background-color: var(--clientspace-info-light);
  color: var(--clientspace-info);
  border: 1px solid var(--clientspace-info-border);
}

.clientspace-badge-neutral {
  background-color: var(--clientspace-bg-light);
  color: var(--clientspace-text-secondary);
  border: 1px solid var(--clientspace-border-default);
}
```

---

## Cards

### Card Container
```css
.clientspace-card {
  background-color: var(--clientspace-bg-white);
  border: 1px solid var(--clientspace-border-default);
  border-radius: var(--clientspace-radius-lg);
  box-shadow: var(--clientspace-elevation-1);
  padding: var(--clientspace-spacing-lg);
  transition: all 0.2s ease;
}

.clientspace-card:hover {
  box-shadow: var(--clientspace-elevation-2);
  border-color: var(--clientspace-border-hover);
}

.clientspace-card-header {
  margin-bottom: var(--clientspace-spacing-md);
  padding-bottom: var(--clientspace-spacing-md);
  border-bottom: 1px solid var(--clientspace-border-default);
}

.clientspace-card-title {
  font-size: var(--clientspace-font-size-h4);
  font-weight: var(--clientspace-font-weight-semibold);
  color: var(--clientspace-text-primary);
  margin: 0;
}

.clientspace-card-body {
  color: var(--clientspace-text-primary);
}

.clientspace-card-footer {
  margin-top: var(--clientspace-spacing-md);
  padding-top: var(--clientspace-spacing-md);
  border-top: 1px solid var(--clientspace-border-default);
}
```

---

## Animation & Transitions

### Transition Durations
```css
:root {
  --clientspace-transition-fast: 150ms;
  --clientspace-transition-base: 200ms;
  --clientspace-transition-slow: 300ms;
}
```

### Easing Functions
```css
:root {
  --clientspace-ease-in: cubic-bezier(0.4, 0, 1, 1);
  --clientspace-ease-out: cubic-bezier(0, 0, 0.2, 1);
  --clientspace-ease-in-out: cubic-bezier(0.4, 0, 0.2, 1);
}
```

### Common Transitions
```css
.transition-all {
  transition: all var(--clientspace-transition-base) var(--clientspace-ease-in-out);
}

.transition-colors {
  transition: color var(--clientspace-transition-base) var(--clientspace-ease-in-out),
              background-color var(--clientspace-transition-base) var(--clientspace-ease-in-out),
              border-color var(--clientspace-transition-base) var(--clientspace-ease-in-out);
}
```

---

## Z-Index Scale

```css
:root {
  --clientspace-z-base: 0;
  --clientspace-z-dropdown: 1000;
  --clientspace-z-sticky: 1020;
  --clientspace-z-fixed: 1030;
  --clientspace-z-modal-backdrop: 1040;
  --clientspace-z-modal: 1050;
  --clientspace-z-popover: 1060;
  --clientspace-z-tooltip: 1070;
}
```

---

## Focus Indicators

Accessible focus states for keyboard navigation.

```css
:root {
  --clientspace-focus-outline: 2px solid var(--clientspace-primary);
  --clientspace-focus-offset: 2px;
}

.clientspace-focusable:focus-visible {
  outline: var(--clientspace-focus-outline);
  outline-offset: var(--clientspace-focus-offset);
  border-radius: var(--clientspace-radius-md);
}
```

---

## Usage Example: Complete Form

```html
<div class="clientspace-card">
  <div class="clientspace-card-header">
    <h3 class="clientspace-card-title">Add External User</h3>
  </div>
  
  <div class="clientspace-card-body">
    <div style="margin-bottom: var(--clientspace-spacing-md)">
      <label class="clientspace-label required">Email Address</label>
      <input type="email" class="clientspace-input" placeholder="user@example.com" />
      <span class="clientspace-helper-text">Enter a valid email address</span>
    </div>
    
    <div style="margin-bottom: var(--clientspace-spacing-md)">
      <label class="clientspace-label required">Display Name</label>
      <input type="text" class="clientspace-input" placeholder="John Doe" />
    </div>
  </div>
  
  <div class="clientspace-card-footer">
    <button class="clientspace-button-primary">Add User</button>
    <button class="clientspace-button-text">Cancel</button>
  </div>
</div>
```

---

**Last Updated:** 2026-02-08  
**Version:** 1.0.0  
**Maintained by:** ClientSpace Brand Team
