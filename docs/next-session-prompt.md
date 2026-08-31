# Panelarr — Next Session Prompt

Copy this into the first message of a new Claude session (fill in the
keys):

---

We're continuing work on Panelarr (~/Projects/panelarr). Read
`docs/last-handoff.md` first — full Session 30 state. Short version:
v1.1.43–45 shipped (retag events, TMNT false-grab fix, and a two-step
fix for a rescan that re-homed another series' files); the Session-24
library triage is CLOSED (40 annual volumes added, files re-homed and
moved into their own folders, unmapped 67 → 1); tiered refresh settled
at ~47 min/run; v1.1.46 (identification hardening) shipped 08-31.
Nothing is unpushed.

Homelab Panelarr API key: <paste key here>.
Homelab Kavita API key (only if Kavita work planned): <paste key here>.

Today's agenda (recommended order):

1. **Kavita follow-up**: after the user's library scan, check whether
   the moved annuals group as their own series; if not, retag the
   annual series (preview diffs first — retag replaces ComicInfo
   wholesale).
2. Anything the user brings; otherwise quiet — no open bugs.

Dev notes: local dev instance data dir is
`~/Library/Application Support/Panelarr` (port 8787); binary builds to
`_output/net10.0/` via `dotnet msbuild -restore src/Panelarr.sln
-p:Configuration=Debug -p:Platform=Posix -t:Build`. Dev DB indexers and
download clients are DISABLED on purpose. Puppeteer is global
(NODE_PATH=$(npm root -g)); the HOMELAB UI is behind forms login, so
UI checks there are API-only. Suite green = Core.Test (2,677) +
Host.Test ContainerFixture (run it for any new command/dependency).
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

Standing rules as always: test before presenting (puppeteer + API),
discuss designs before implementing, scan before any push, no AI
attribution in commits, ask before pushing, homelab host/IP details
stay OUT of public repo docs.
---
