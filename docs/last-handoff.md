# Last Handoff — 2026-07-08 (Session 22)

Session 22 = **the bulk library import**. The homelab's full Mylar
library (~12.8k files, 1,201 series proposals) is being imported into
Panelarr via the Library Import pipeline. No code changes shipped this
session — it was ops, forensics, and one config change.

## Bulk import status (live at handoff time)

- Proposal dry run over /comics: **1,201 proposals — 1,197 exact via
  cvinfo, 1 exact via file tags, 3 probable (name search), 0
  unidentified** covering all 12,326 unmapped files. Proposal scan:
  4m26s, zero CV calls for cvinfo folders.
- **Pilot: IDW (11 series) — 78/78 files mapped, 0 failures, ~90 s.**
- Batches queued and processed sequentially (rate-limited by
  ComicVine 200/hr; each series ≈ add + refresh + rescan):
  1. small publishers (25) — DONE
  2. Image (49) — DONE
  3. Marvel (169) — DONE (23:31)
  4. DC part 1 (460, alphabetical) — was 338/460 on the morning of
     07-08; its 577 per-series RescanFolders commands wait on the
     disk-access mutex until the LibraryImport completes, THEN map
     files in a burst. Mapped-file count stays ~548 until then —
     that's expected, not a failure.
  5. DC part 2 (484) — queued as command 13482.
- **Deferred: the 3 probable Epic Collections** (Amazing Spider-Man /
  Fantastic Four / Iron Man). Their name-search matches each point at
  a single Epic Collection TPB volume while the folders hold 11–16
  files — verify granularity via the review modal (alternatives
  picker) or CV search before importing.
- Everything is idempotent: re-queuing a LibraryImport skips series
  already in the library (FindById check, per-item failure isolation).

## The scan-hang investigation (unresolved, standing watch)

The initial RescanFolders (registering the 12k unmapped files) froze
silently for ~90 min at "Importing 4 files" and needed a container
restart. Full forensics:

- All the scan's real work HAD committed: the 4 files (MMPR Annuals /
  Shattered Grid / SIKTC #48, whose rows a scan-start cleanup had
  deleted — see oddity below) imported correctly, and all 12,326
  unmapped rows were inserted. The command thread never returned
  (in-memory status flips before the repo write in Complete(), so
  status=started proves Execute() never returned).
- Cleared with evidence: mount I/O (proposal scan reads every folder
  fine), WriteTags (newFiles setting no-ops), CV rate limiter (no
  calls in that path), DB locks (parallel writes kept succeeding),
  SignalR (broadcasts are fire-and-forget), notifications/Kavita
  (scan imports early-return on !NewDownload), HTTP timeouts (100 s
  dispatcher default exists), memory ceiling (container is
  unlimited, 321 MiB used of 15.6 GiB).
- Five dev reproductions all negative (plain import, missing-issue
  path, 13k-file scale, frozen SignalR client, black-hole Kavita).
- A trace-level UNFILTERED rescan on the homelab (filter:"none" —
  re-walks everything, writes nothing) completed clean in 11m51s;
  the entire post-import tail measured 0.2 s.
- Verdict: one-time unattributed event. If it recurs: set
  config/host logLevel=trace live (no restart), the last trace line
  names the statement. Restore to info afterwards.

Related oddities parked: (1) the scan-start cleanup deleted 7 mapped
rows whose files exist on disk (MediaFileTableCleanupService — why?);
(2) exactly one of the 12,331 read files ended up with no DB row
(Ben 10 — series shows 0 files); (3) reading-phase tail crawl is
just Suske en Wiske's 377 large archives — benign.

## Also this session

- **GitHub hardening**: main branch protection (no force push, no
  deletion, direct pushes still allowed) + tag ruleset
  protect-release-tags (v* tags: no delete/update/non-FF). No bypass
  actors — disable the ruleset temporarily if a release tag ever
  needs re-pointing.
- **Quality profile 'Comic': cutoff lowered Digital→Archive** (user
  choice) so the imported Archive-quality library doesn't get
  re-grabbed as upgrades via RSS (MAM ratio). upgradeAllowed stays
  true: Scan/C2C→Archive upgrades and new-issue grabs still work.
- **RSS chain verified end-to-end** (auto grab+import of Minor
  Arcana #16 on 07-07).
- **Homelab UI "outage" solved**: user's browser was pointed at
  192.168.1.84:8787 (the Prowlarr host) instead of 192.168.1.33:8787.
  Server was healthy throughout. Frontend note: the Library Import
  page renders all 12k unmapped rows client-side (scale-readiness #5)
  — sluggish until the import drains the table.
- Staging-import backlog item retired: /downloads on the container IS
  the seedbox mount, so StagingFolder can point there any time.

## Next actions

1. Verify DC part 1 + rescan burst completed (mapped files should be
   ~7,200 after part 1, ~12,300 after part 2); spot-check mappings.
2. DC part 2 (13482) runs ~10 h after that.
3. Resolve the 3 probable Epic Collections (review modal / CV check).
4. After full import: nightly refresh load check at ~1,265 series
   (scale-readiness said fine), Library Import page perf
   (#4/#5 measurements now possible with a real 12k library).
5. Wiki push still blocked on user creating the first page in the
   GitHub UI (drafts in docs/wiki/).
6. Standing watch: scan-hang recurrence (trace toggle ready);
   quality "Unknown" in Activity manual-import modal.
