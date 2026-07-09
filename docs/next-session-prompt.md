# Panelarr — Next Session Prompt

Copy this into the first message of a new Claude session (fill in the
keys):

---

We're continuing work on Panelarr (~/Projects/panelarr). Read
`docs/last-handoff.md` first — full Session 22 state. Short version:
**the bulk Mylar-library import is DONE**: 1,261 series, 12,178 files
mapped, 0 failures, 619 correct-hold leftovers. The two-day scan-hang
mystery is SOLVED (synchronous per-file archive inspection + credit
extraction after imports — hours of silent grinding, not a hang).
Shipped: YearMatchSpecification (grab-side year check) + 'Write Tags'
toolbar label fix. Suite 2495/0.

Homelab Panelarr API key: <paste key here>.

Today's agenda (in recommended order):

1. **Leftovers pass (~30 min)**: Library Import → Scan for New Series
   on the 619 unmapped — proposes the ~516 DC Annual/cross-volume
   series from file tags; fix the 3 Epic Collections' matches in the
   review modal; the 5 Suske en Wiske .cbr files are zip-mislabeled
   (RAR→CBZ normalizer or manual rename).
2. **Async archive-inspection fix** (design first, then build): move
   CreditPopulationService + ArchiveInspectionService off the
   synchronous import event path into a queued background command
   with ProgressInfo ("Inspecting archives N/M"). This is the
   scan-hang fix proper.
3. Small bug batch: queued commands lost on restart (Requeue),
   FilterFilesType.Matched excludes unmapped rows (Issue==null arm),
   vanished-mount cleanup guard.
4. Post-import health: nightly refresh volume at 1,261 series;
   Library Import / issue-index page perf (scale-readiness #4/#5).

User-side homework (remind gently): docker healthcheck on the
/comics sentinel dir so the NFS-mount race can't recur; create the
GitHub wiki's first page so the drafted docs/wiki/ pages can push.

Dev notes: local dev instance data dir is
`~/Library/Application Support/Panelarr` (port 8787). Dev binary
builds to `_output/net10.0/` (net6.0 is stale) via
`dotnet msbuild -restore src/Panelarr.sln -p:Configuration=Debug
-p:Platform=Posix -t:Build`. The dev DB has live indexers — stop the
instance when done testing. Puppeteer is global
(NODE_PATH=$(npm root -g)); series pages need the titleSlug
(cv-XXXXX), not the numeric id.

Standing watch (one item): quality "Unknown" in the Activity
manual-import modal (unreproduced since Session 18).

Standing rules as always: test before presenting, discuss designs
before implementing, scan before any push (from a file, as separate
checked steps — not && chains), no AI attribution in commits, ask
before pushing, homelab host/IP details stay OUT of public repo docs.
