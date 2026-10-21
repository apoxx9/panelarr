# Panelarr — Next Session Prompt

Copy this into the first message of a new Claude session (fill in the
keys):

---

We're continuing work on Panelarr (~/Projects/panelarr). Read
`docs/last-handoff.md` first — full Session 34 state. Short version:
v1.1.50 (10-21) fixed two faults found in a week of homelab logs — a
tracker grab limit was being retried 96×/day because a 429 without
`Retry-After` only bought a 60-second block that the next RSS success
reset (now falls back to an hour, matching the search path), and the
recent-missing search could not see issues whose release date ComicVine
revised onto an already-stale day (issues never searched are now
reconsidered out to 90 days).

Earlier context — the refresh and RSS work is DONE and VERIFIED. The
ended tier drains
exactly 47/day with 2-minute scheduled refreshes (v1.1.47 confirmed
live on 10-14 — the 11h30m storms are structurally gone); the RSS feed
reads 3 pages and reports 117+ releases per poll (v1.1.48 confirmed);
and v1.1.49 (10-14) cleaned up the three log-noise sources those checks
exposed — archive signatures now beat extensions, the vanished-mount
and empty-folder warnings fire only when rows are at stake, and the
recent-missing search logs every run. Nothing is unpushed. No open
bugs.

Homelab Panelarr API key: <paste key here>.
Homelab Kavita API key (only if Kavita work planned): <paste key here>.

Today's agenda (recommended order):

1. **Verify v1.1.50's two fixes.** For the grab limit: there is nothing
   to see unless a tracker limit is hit again — if one is, the log
   should show "API Grab Limit reached for X. Disabled for 01:00:00"
   ONCE, followed by silence from that indexer for an hour, instead of a
   line every 15 minutes. For the net: watch for it searching an issue
   whose release date is older than 14 days (that is the never-searched
   clause firing); it is a verified no-op until a late-dated issue
   appears.
2. **Count the noise.** The one-line health check that found both faults
   was grouping Warn/Error by component across all rotated logs. Do that
   every time — both faults were invisible in /health, which showed
   zero entries throughout.
3. **The safety net has still only handled a trickle** — one in-window
   issue so far. After a genuinely big new-comic Wednesday, confirm it
   logs "Searching for N missing issues" with N in double digits.
4. Anything the user brings; otherwise quiet — no open bugs. Library is
   current (nothing from the current run missing; newest gap is
   2026-03-01).

Dev notes: local dev instance data dir is
`~/Library/Application Support/Panelarr` (port 8787); binary builds to
`_output/net10.0/` via `dotnet msbuild -restore src/Panelarr.sln
-p:Configuration=Debug -p:Platform=Posix -t:Build`. Dev DB indexers and
download clients are DISABLED on purpose. Puppeteer is global
(NODE_PATH=$(npm root -g)); the HOMELAB UI is behind forms login, so
UI checks there are API-only. Suite green = Core.Test (2,715) +
Host.Test ContainerFixture (run it for any new command/dependency).
A FRESH dev instance opens on the setup wizard, which blocks every
route — puppeteer route checks there confirm a clean boot, not page
content.
Incremental msbuild can skip the API project — `dotnet build
src/Panelarr.Api.V1/Panelarr.Api.V1.csproj` explicitly if an API field
doesn't appear.

Ship ritual: commit (no AI attribution) → `bash
~/.claude/panelarr-prepush-scan.sh` (must print SCAN CLEAN) → tag
vX.Y.Z → push main + tag → `gh release create vX.Y.Z --notes-file
<hand-written ## New / ## Fixed>` → watch the Docker workflow → user
pulls. The what's-new modal reads those GitHub release bodies.

Hard-won gotchas: re-home a mapped file with ManualImport(move) — it
replaces the old row; never "clean up" with a rescan. ManualImport
does not physically move a file already under a library root; Rename
Files does (even with renameComics off). Add series via the FULL
lookup resource. Trace-log diagnosis: set logLevel=trace via
/config/host, reproduce, pull /api/v1/log/file/panelarr.trace.txt,
RESTORE. GetComics downloads run synchronously inside the grab
command. For collected editions the SUBTITLE is the only
discriminator — volume numbers must never be trusted for matching.
Auditing refresh cadence: `/api/v1/series` does NOT expose
`lastInfoSync` and `/api/v1/calendar` does NOT populate `hasFile` (use
`statistics.issueFileCount`) — measure the scheduled refresh window by
bracketing the first→last `Skipping refresh of series` timestamps per
day, because `Updating Info for` lines are scattered all day by
RSS-triggered single-series refreshes.

Auditing warn/error noise: /health can read ZERO while the logs carry
hundreds of warnings - group them by component across ALL rotated logs
(`grep -hE '\|(Warn|Error|Fatal)\|' panelarr*.txt | sed -E
's/^[0-9-]+ [0-9:.]+\|([A-Za-z]+)\|([A-Za-z0-9_.]+)\|.*/\1 \2/' |
sort | uniq -c | sort -rn`). That one line found both v1.1.50 faults.

Standing rules as always: test before presenting (puppeteer + API),
discuss designs before implementing, scan before any push, no AI
attribution in commits, ask before pushing, homelab host/IP details
stay OUT of public repo docs.
---
