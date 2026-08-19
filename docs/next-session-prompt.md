# Panelarr — Next Session Prompt

Copy this into the first message of a new Claude session (fill in the
keys):

---

We're continuing work on Panelarr (~/Projects/panelarr). Read
`docs/last-handoff.md` first — full Session 29 state. Short version:
SIXTEEN releases shipped (v1.1.27 → v1.1.42). GetComics automation is
restored (the `/dls/` rename + first-party main-server link were the
real story, not captchas); the collected-edition search+import chain
was rebuilt through nine peeled bugs (every Epic test series — Iron
Fist, Nine Lives, 22× Captain America, 4× Modern Era, Aliens — is
complete in the library); Mega mirror support; idle-timeout downloads;
tiered series refresh with derived CV status; torrent-first delay
profiles with DirectDownload first-class; interactive fast lane in
the ComicVine rate limiter; Name/Year filters; GitHub releases
backfilled so the what's-new modal works.

Homelab Panelarr API key: <paste key here>.
Homelab Kavita API key (only if Kavita work planned): <paste key here>.

Today's agenda (recommended order):

1. **Check the tiered refresh's first scheduled run** (v1.1.40): count
   "Updating Info" vs "Skipping refresh" lines in the log; expect
   ~230 refreshes vs 584+ before. Tune the 180-day/7-day constants in
   `ShouldRefreshSeries` / `ComicVineProvider.EndedAfter` if off.
2. **Parser NRE** (backlog #2): `Parser.ParseIssueTitle` swallows a
   NullReferenceException on some titles — root-cause it.
3. **Session-24 triage decisions** (user calls, then execute): 47
   annuals via the resolve/add flow; Red Hood Outlaw #41-49; ~11
   duplicates; 3 CV-gap oddities.
4. Optional quick wins: library-clutter saved filters; DD delay
   tuning (30–60 min); editorial pass on backfilled release notes.

Dev notes: local dev instance data dir is
`~/Library/Application Support/Panelarr` (port 8787); binary builds to
`_output/net10.0/` via `dotnet msbuild -restore src/Panelarr.sln
-p:Configuration=Debug -p:Platform=Posix -t:Build`. Dev DB indexers and
download clients are DISABLED on purpose. Puppeteer is global
(NODE_PATH=$(npm root -g)). Suite green = Core.Test (2,646) +
Host.Test ContainerFixture (run it for any new command/dependency).
Incremental msbuild can skip the API project — `dotnet build
src/Panelarr.Api.V1/Panelarr.Api.V1.csproj` explicitly if an API field
doesn't appear.

Ship ritual: commit (no AI attribution) → `bash
~/.claude/panelarr-prepush-scan.sh` (must print SCAN CLEAN) → tag
vX.Y.Z → push main + tag → `gh release create vX.Y.Z --notes-file
<hand-written ## New / ## Fixed>` → watch the Docker workflow → user
pulls. The what's-new modal reads those GitHub release bodies.

Hard-won gotchas: trace-log diagnosis pattern (set logLevel=trace via
/config/host, reproduce, pull /api/v1/log/file/panelarr.trace.txt,
RESTORE); GetComics downloads run synchronously inside the grab
command; stuck imports → DownloadedIssuesScan {path, downloadClientId}
or ManualImport {files:[{path, seriesId, issueId, quality}]}; queue
"unknown series" rows need includeUnknownSeriesItems=true; for
collected editions the SUBTITLE is the only discriminator — volume
numbers mix publication and line order and must never be trusted for
matching.

Standing rules as always: test before presenting (puppeteer + API),
discuss designs before implementing, scan before any push, no AI
attribution in commits, ask before pushing, homelab host/IP details
stay OUT of public repo docs.
---
