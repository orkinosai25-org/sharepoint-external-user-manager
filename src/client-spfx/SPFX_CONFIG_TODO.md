# SPFx Configuration TODO

## Current State
The `config/config.json` file currently has empty bundles configuration, which allows the build to succeed but prevents the following webparts from being bundled:
- ExternalUserManagerWebPart
- ClientDashboardWebPart

## Issue
These webparts were added in PR #103 with incorrect configuration:
1. Entry points were set to `./lib/webparts/...` instead of `./webparts/...`
2. SCSS modules are not being properly compiled to `.scss.js` files
3. Webpack cannot process `.scss` imports in the transpiled JavaScript

## Required Fix
To properly include these webparts in the bundle:
1. Ensure SCSS compilation generates `.scss.js` and `.scss.d.ts` files for all components
2. OR restructure the project to follow standard SPFx layout (sources in `src/`, output in `lib/`)
3. OR configure webpack with appropriate SCSS loaders
4. Update `config.json` with correct bundle entries once SCSS issue is resolved

## Workaround
For now, the other webparts in the repository (meetingRoomBooking, timesheetManagement, aiPoweredFaq, inventoryProductCatalog) can still function. They are discovered automatically by SPFx even though they're not explicitly listed in bundles.

## Related Files
- `/src/client-spfx/config/config.json` - Bundle configuration
- `/src/client-spfx/webparts/externalUserManager/` - Affected webpart
- `/src/client-spfx/webparts/clientDashboard/` - Affected webpart
