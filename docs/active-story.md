# Active Story: Black Screen Bug (Session 4 Priority 1)

## Status: BLOCKED — Frontend renders blank page

The app backend is fully functional but the frontend renders a blank/black screen when loaded at `http://localhost:8787`. This is the immediate blocker before any further feature work or testing can proceed.

## Problem

After the Session 3 search UI redesign, the React app crashes at runtime. The JS bundle builds clean (`npm run build` succeeds with no errors), all API endpoints return correct data, but the browser shows a blank page.

## Diagnosis Steps

1. Kill all instances: `pkill -9 -f "NzbDrone|panelarr"`
2. Start the app: `cd /Users/lorenzonunez-estevez/Projects/panelarr/src && dotnet run --project NzbDrone.Console -p:EnableSourceLink=false`
3. Open `http://localhost:8787` in browser
4. Open DevTools Console (Cmd+Option+J on Mac)
5. The console error will identify the crashing component

## Likely Cause

The search UI redesign touched these files — one or more likely has a React render error (bad import, undefined prop, missing component):
- `frontend/src/Search/AddNewItem.js`
- `frontend/src/Search/Series/AddNewSeriesSearchResult.js`
- `frontend/src/Search/Issue/AddNewIssueSearchResult.js`
- Related CSS and type definition files

## After Fixing

Once the black screen is resolved:
- Test series add flow via the UI
- Test remaining Goodreads code paths that need rewiring
- Verify Metron connectivity (currently down, ComicVine fallback works)
- Stage and commit all Session 3 changes (194 uncommitted files)

## Backlog Context
- Phases P1-P3: COMPLETE
- Next feature: F-01 (Story Arcs) — do not start until runtime is stable
