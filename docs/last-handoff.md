# Last Handoff — 2026-04-25 (End of Session 10)

## Session 10 Summary

Three major areas of work:

1. **Public Repo & Docker** — Cleaned repo for public release, set up CI/CD with Docker image builds
2. **Setup Wizard** — Full first-run onboarding wizard with 6 steps
3. **Startup Log Cleanup** — Silenced all spurious errors on boot

---

## Session 10 Commits (local dev repo)

1. Rewrite docs for public release (README, CONTRIBUTING)
2. Exclude internal docs from git (BACKLOG, active-story, last-handoff, CLAUDE.md)
3. Add Docker CI workflow (GitHub Actions → GHCR)
4. Fix Dockerfile: .NET 10.0, frontend build stage, Logo/, Mono.dll, PUID/PGID entrypoint
5. Replace all Readarr logos with Panelarr comic panel branding (transparent)
6. Replace loading messages with comic-themed lines
7. Add first-run setup wizard (backend + frontend)
8. Fix wizard: render outside PageConnector, metadata save id, test methods GET not POST
9. Fix wizard: root folder name + default profile IDs, indexer/download client payloads
10. Clean startup logs: silence UpdateHistory error, disable SystemTimeCheck, downgrade servarr notifications

---

## Key Technical Details

### Public Repo
- GitHub: `github.com/apoxx9/panelarr` — single squashed commit, no personal data
- Docker: `ghcr.io/apoxx9/panelarr:latest` — multi-arch (amd64 + arm64)
- CI: builds on push to `main` (latest) and on `v*` tags (versioned)
- Internal docs (BACKLOG, active-story, last-handoff, CLAUDE.md) excluded via .gitignore

### Setup Wizard
- Backend: `SetupCompleted` flag in Config table (IConfigService/ConfigService)
- Backend: `POST /api/v1/system/setup/complete` endpoint
- Backend: `setupCompleted` field in `/api/v1/system/status` response
- Frontend: `/setup` route rendered outside PageConnector in App.js (avoids redirect loop)
- Frontend: SetupWizardConnector fetches own system status (independent of PageConnector)
- Steps: Welcome → Metadata Provider → Root Folder → Indexers → Download Client → Summary
- Required: Metadata Provider (Metron or ComicVine), Root Folder
- Skippable: Indexers, Download Client
- Existing items shown in Indexer/Download Client steps to prevent duplicates

### Docker
- Entrypoint script at `docker/entrypoint.sh` — adjusts PUID/PGID at runtime via gosu
- Healthcheck uses `/ping` (unauthenticated) instead of `/api/v1/system/status`
- Dockerfile publishes both Console and Mono projects to include Panelarr.Mono.dll

### Startup Log Cleanup
- UpdateHistoryService: graceful skip with Debug log instead of crash + stack trace
- SystemTimeCheck: always passes (no panelarr.servarr.com cloud service)
- ServerSideNotificationService: downgraded from Error to Debug level

---

## How to Run

```bash
# Docker (NAS)
docker pull ghcr.io/apoxx9/panelarr:latest
# See docker-compose.minimal.yml for compose setup

# Local development
cd src/
dotnet run --project NzbDrone.Console --framework net10.0
```

App at http://localhost:8787

Config: `~/.config/Panelarr/`

---

## Current State

- 0 errors, 0 warnings (.NET build)
- Frontend webpack build clean
- Clean startup logs (no errors on boot)
- Setup wizard functional
- Docker image building and deploying successfully
- Version: v1.0.0

---

## Known Issues

- UpdateHistory table: migration defines it but some fresh installs don't create it (gracefully handled now)
- Pre-existing metadataProfile PropTypes warning on library pages (cosmetic)

---

## Next Up

- F-01: Story Arcs
- Credits display (Writer, Artist, Colorist)
