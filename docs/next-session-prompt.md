# Panelarr — Next Session Prompt

Copy this into the first message of a new Claude session (fill in the
keys):

---

We're continuing work on Panelarr (~/Projects/panelarr). Read
`docs/last-handoff.md` first — full Session 33 state. Short version:
the refresh and RSS work is DONE and VERIFIED. The ended tier drains
exactly 47/day with 2-minute scheduled refreshes (v1.1.47 confirmed
live on 10-14 — the 11h30m storms are structurally gone); the RSS feed
reads 3 pages and reports 117+ releases per poll (v1.1.48 confirmed);
and v1.1.49 (10-14) cleaned up the three log-noise sources those checks
exposed — archive signatures now beat extensions, the vanished-mount
and empty-folder warnings fire only when rows are at stake, and the
recent-missing search logs every run. Suite 2,710 green. Nothing is
unpushed. No open bugs.

Homelab Panelarr API key: <paste key here>.
Homelab Kavita API key (only if Kavita work planned): <paste key here>.

Today's agenda (recommended order):

1. **Nothing is outstanding.** v1.1.49 was verified live on 10-15 (all
   noise warnings 39 → 0, the three "corrupt" archives recovered and
   inspected for real, the safety net logged its first non-zero search).
   Refresh, RSS, and the archive work are all closed out.
2. **The safety net has still only handled a trickle** — one in-window
   issue so far. After a genuinely big new-comic Wednesday, confirm it
   logs "Searching for N missing issues" with N in the double digits
   and that wanted/missing holds nothing older than a day.
3. Anything the user brings; otherwise quiet — no open bugs. (Kavita
   follow-up is DONE: annuals group as their own series, no retag
   needed.) Library is current: the only recent monitored-missing issue
   is whatever released in the last day or two.

Dev notes: local dev instance data dir is
`~/Library/Application Support/Panelarr` (port 8787); binary builds to
`_output/net10.0/` via `dotnet msbuild -restore src/Panelarr.sln
-p:Configuration=Debug -p:Platform=Posix -t:Build`. Dev DB indexers and
download clients are DISABLED on purpose. Puppeteer is global
(NODE_PATH=$(npm root -g)); the HOMELAB UI is behind forms login, so
UI checks there are API-only. Suite green = Core.Test (2,710) +
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

Standing rules as always: test before presenting (puppeteer + API),
discuss designs before implementing, scan before any push, no AI
attribution in commits, ask before pushing, homelab host/IP details
stay OUT of public repo docs.
---
