# Panelarr — Next Session Prompt

Copy this into the first message of a new Claude session (fill in the
keys):

---

We're continuing work on Panelarr (~/Projects/panelarr). Read
`docs/last-handoff.md` first — it has full Session 22 state. Short
version: **the bulk Mylar-library import is in flight on the homelab**
(1,201 proposals, all-but-3 exact via cvinfo). Small pubs + Image +
Marvel + DC part 1 (460) ran overnight 07-07→08; DC part 2 (484,
command 13482) was queued behind them. The per-series file-mapping
rescans only run after each LibraryImport batch releases the
disk-access mutex — a low mapped-file count while a batch runs is
EXPECTED. Also this session: main-branch protection + v* tag ruleset
on GitHub, quality cutoff lowered to Archive (no RSS re-grab storms),
RSS chain verified, the 90-min scan hang investigated exhaustively
(unattributed one-time event — trace-toggle playbook in the handoff).

Homelab Panelarr API key: <paste key here>. Kavita API key (only if
reader work planned): <paste key here>.

Today's agenda:

1. **Verify the import finished**: series ≈ 1,265, mapped files ≈
   12,300, unmapped ≈ a handful; spot-check a few series; check 0
   failed commands.
2. **The 3 probable Epic Collections** (ASM/FF/Iron Man): name-search
   matched single-TPB volumes for 11–16-file folders — verify the
   right CV volume (review modal alternatives / CV search) then
   import.
3. **Post-import health**: nightly refresh volume at ~1,265 series,
   Library Import / issue-index page performance with the real
   library (scale-readiness #4/#5 measurements).
4. Backlog: Metron enrichment for reading lists; reading-list
   reordering UI; Issueshelf rename / landscape #4 (want-list) / #5
   (custom formats).

Dev notes: local dev instance data dir is
`~/Library/Application Support/Panelarr` (port 8787; the
`~/.config/Panelarr` copy is stale — wrong API key). Dev binary is
built to `_output/net10.0/` (net6.0 is stale — rebuild with
`dotnet msbuild -restore src/Panelarr.sln -p:Configuration=Debug
-p:Platform=Posix -t:Build`). The dev DB has live indexers
configured, so stop the instance when done testing. Puppeteer is
installed globally (NODE_PATH=$(npm root -g)).

Standing watch: scan-hang recurrence (if a command freezes silently:
set config/host logLevel=trace LIVE, capture the last trace line,
restore info); quality "Unknown" in the Activity manual-import modal
(unreproduced since Session 18).

Standing rules as always: test before presenting, discuss designs
before implementing, scan before any push (from a file, as separate
checked steps — not && chains), no AI attribution in commits, ask
before pushing.
