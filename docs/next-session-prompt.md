# Panelarr — Next Session Prompt

Copy this into the first message of a new Claude session:

---

We are continuing work on Panelarr, a comic book management app (fork of Readarr adapted for comics). Start by reading `docs/last-handoff.md` and `docs/AUDIT.md` — these contain the full context from our last session where we completed the entire feature audit with zero open items.

**Session 12 results:** All 31 audit items PASS, all 3,023 tests pass, zero failures. Features implemented: retag/ComicInfo.xml writer, comic parser for torrent releases, metadata override UI, qBittorrent v5 fix, cutoff unmet fix, search relevance sorting, SeriesGroup cleanup, all pre-existing test failures fixed.

**The audit is complete.** There are no open FIX, IMPROVE, or REVIEW items. The project is in a clean state for new feature work.

**Potential directions for this session** (pick based on priorities):

1. **UI polish for metadata overrides** — Add per-field "Clear Override" buttons, visual indicators for overridden fields in the detail views (not just edit modal)
2. **Comic parser edge cases** — Expand torrent release title parsing for GetComics, scene group conventions, and other indexer formats beyond MAM
3. **Publisher management UI** — Currently API-only CRUD. Add a dedicated settings page for browsing/editing publishers
4. **Filename parser `#` handling** — Comic filenames with `#` in issue numbers (e.g. "Batman #45") don't parse correctly without series-override context
5. **Auto-migrate existing quality profiles** — Enable `UpgradeAllowed` on existing installations (currently only fixed for new installs)
6. **New features** — What's the next big thing you want Panelarr to do?

The codebase is at `github.com/apoxx9/panelarr`. Docker image: `ghcr.io/apoxx9/panelarr:latest`. Local dev: `dotnet run --project src/NzbDrone.Console --framework net10.0`. App runs at http://localhost:8787.
