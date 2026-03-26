# Active Story: P1-01

## Fork and rebrand Readarr → Panelarr

**Status:** In Progress (build verification remaining)
**Started:** 2026-03-26

### Tasks
- [x] Bring Readarr source into panelarr repo
- [x] Global rename: Readarr → Panelarr in solution, projects, namespaces
- [x] Rename project directories: Readarr.Api.V1/ → Panelarr.Api.V1/, Readarr.Http/ → Panelarr.Http/
- [x] Update branding: logos, window titles, app name strings
- [ ] Verify `dotnet build` succeeds ← requires .NET SDK, do at home
- [ ] Verify app launches with Panelarr branding ← requires .NET SDK, do at home

### Acceptance Criteria
`dotnet build` succeeds, app launches with Panelarr branding

### Notes
- NzbDrone.* directory names intentionally kept (internal codename shared across *arr ecosystem)
- Review confirmed zero remaining Readarr references outside docs/
- package.json, solution file, all .csproj files, frontend, distribution files all renamed
