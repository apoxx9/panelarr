# Panelarr — Next Session Prompt

Copy this into the first message of a new Claude session (fill in the
keys):

---

We're continuing work on Panelarr (~/Projects/panelarr). Read
`docs/last-handoff.md` first — full Session 35 state.

Short version: Session 35 (11-02) started from a "DC Absolute Universe"
checklist where some specials could not be searched and some could not
be added, and that thread turned up **four unrelated bugs stacked in the
same flow**, all fixed, shipped and verified live, plus a fifth change
for the host's mount behaviour. Five releases: **v1.1.51** (annual
volumes named `<Series> <Year> Annual` were collapsing onto their parent
series and being rejected "Wrong series" on exact title matches),
**v1.1.52** (checklist resolve used ComicVine's name FILTER so
mismatched names got "not found" with no candidates), **v1.1.53** (a
hand-linked slot never resolved because nothing read `ForeignSeriesId`;
checklist adds now default to monitored; the test suite's warn capture
was silently dead in some fixtures), **v1.1.54** (one dead-mirror post
benched the whole indexer, silently halving batch searches), **v1.1.55**
(an empty library root is treated as a mount that is not ready — back
off 15s/30s/60s, log once per episode, never delete).

Library state: 1,429 series, reading list 339 at 135/135 resolved, all
37 annual series and their 62 issues monitored, 14 back-catalogue annual
gaps confirmed genuinely unavailable. Suite 2,747 green. Nothing is
unpushed. No open bugs.

Homelab Panelarr API key: <paste key here>.
Homelab Kavita API key (only if Kavita work planned): <paste key here>.

Today's agenda (recommended order):

1. **Confirm v1.1.55 under a full library pass.** The ~1,428 per-series
   scans come from **Refresh Series** (daily, ~15:58 UTC), NOT from
   Rescan Folders — and that had not run under v1.1.55 when the session
   closed. Check its duration is still ~2 min and that no
   "waiting for it to be mounted" lines fired. `Rescan Folders` running
   sub-second is pre-existing, not a regression.
2. **v1.1.55's actual behaviour is unexercised** and needs a reboot. When
   one happens, expect ONE
   `Folder /comics exists but is empty - waiting for it to be mounted`
   warning, then `Folder /comics has content again` at Info, with the
   scan completing on its own rather than needing the host watchdog's
   restart. If the warning appears with no recovery line, the 105s
   budget was too short for that boot — worth saying so.
3. **The 429 grab-limit backoff (v1.1.50) is still unexercised** — no
   tracker limit has been hit since. If one is, expect
   "API Grab Limit reached for X. Disabled for 01:00:00" ONCE, then
   silence from that indexer for an hour.
4. Run the **noise audit** on any health check (see gotchas) — /health
   read ZERO entries while the logs carried 295 errors on 10-21, and
   that one command found two real faults.
5. Optional, user's call: the stale Unassigned Devices share entry
   `PURGATORY:/volume1/data` -> `/mnt/remotes/PURGATORY_volume1_data`
   (unmounted, holds an auto-mount flag the live share lacks) is still
   worth deleting. Cosmetic now that the root cause is understood, and
   it is a UD web-UI setting, so the user does it.
6. Anything the user brings; otherwise quiet.

Dev notes: local dev instance data dir is
`~/Library/Application Support/Panelarr` (port 8787); binary builds to
`_output/net10.0/` via `dotnet msbuild -restore src/Panelarr.sln
-p:Configuration=Debug -p:Platform=Posix -t:Build`. Dev DB indexers and
download clients are DISABLED on purpose. Puppeteer is global
(NODE_PATH=$(npm root -g)); the HOMELAB UI is behind forms login, so UI
checks there are API-only. Suite green = Core.Test (2,747) + Host.Test
ContainerFixture (run it for any new command/dependency). A FRESH dev
instance opens on the setup wizard, which blocks every route — puppeteer
route checks there confirm a clean boot, not page content. Incremental
msbuild can skip the API project — build
`src/Panelarr.Api.V1/Panelarr.Api.V1.csproj` explicitly if an API field
doesn't appear.

Ship ritual: batch the session's fixes into ONE release → commit (no AI
attribution) → `bash ~/.claude/panelarr-prepush-scan.sh` (must print
SCAN CLEAN) → tag vX.Y.Z → push main + tag → `gh release create vX.Y.Z
--notes-file <hand-written ## New / ## Fixed>` → watch the Docker
workflow (~20 min) → user pulls. The what's-new modal reads those
GitHub release bodies.

Hard-won gotchas:
- `/api/v1/parse` resolves annual titles CORRECTLY and is MISLEADING —
  it uses the generic parser, while the download path calls
  `MapComicRelease` (the comic parser) first and that result wins.
- A batch `IssueSearch` says "0 reports downloaded" even when it grabbed
  several — the counter is per-sub-search. Confirm from `/history`.
- Check the `N active indexers` count per search line before believing a
  batch search's verdict.
- Series `statistics.issueCount`/`issueFileCount` count only MONITORED
  issues, so a series can look complete while hiding gaps.
- Monitoring a series does NOT monitor its existing issues.
- Log files are LOCAL time; the API is UTC. Convert before correlating.
- `/api/v1/series` does NOT expose `lastInfoSync`; `/api/v1/calendar`
  does NOT populate `hasFile` (use `statistics.issueFileCount`).
- Measure the scheduled refresh window by bracketing the first→last
  `Skipping refresh of series` timestamps per day; `Updating Info for`
  lines are scattered all day by RSS-triggered single-series refreshes.
- Noise audit: /health can read ZERO while the logs carry hundreds of
  warnings — group them by component across ALL rotated logs:
  `grep -hE '\|(Warn|Error|Fatal)\|' panelarr*.txt | sed -E
  's/^[0-9-]+ [0-9:.]+\|([A-Za-z]+)\|([A-Za-z0-9_.]+)\|.*/\1 \2/' |
  sort | uniq -c | sort -rn`
- NUnit `Assert.ThrowsAsync<T>` demands the EXACT type; use `CatchAsync<T>`.
- Re-home a mapped file with ManualImport(move) — never "clean up" with
  a rescan. ManualImport does not physically move a file already under a
  library root; Rename Files does (even with renameComics off).
- Add series via the FULL lookup resource.
- For collected editions the SUBTITLE is the only discriminator — volume
  numbers must never be trusted for matching.
- Trace-log diagnosis: set logLevel=trace via /config/host, reproduce,
  pull /api/v1/log/file/panelarr.trace.txt, RESTORE.

Standing rules as always: test before presenting (puppeteer + API),
discuss designs before implementing, scan before any push, no AI
attribution in commits, ask before pushing, batch fixes into one release
and ask whether we have reached the end of a session, homelab host/IP
details stay OUT of public repo docs.
---
