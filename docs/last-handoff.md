# Last Handoff — 2026-07-09 (Session 22, final)

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

## IMPORT COMPLETE (2026-07-09 00:00)

Final state: **1,261 series (1,253 with files), 12,178 files mapped,
619 unmapped leftovers, 0 failed commands.** The leftovers are almost
entirely CORRECT holds: (a) ~516 DC Annuals + cross-volume strays —
files living in a folder whose cvinfo points at a different CV volume
(e.g. "Aquaman Annual #01" in the Rebirth folder; annuals are their
own CV volumes) — resolve via Library Import → Scan for New Series,
which will propose the annual volumes from the files' own tags;
(b) 63 Epic Collections (the 3 probables + granularity); (c) 5 Suske
en Wiske mislabeled archives (.cbr that are actually zip — "Rar
signature not found"); (d) ~35 misc across other publishers. The
restart-era damage was fully repaired by one filter:"none" root
rescan once the NFS mount was fixed.

## The scan-hang MYSTERY — SOLVED (caught live 2026-07-08 21:23)

**It was never a hang.** After a large import/insert, EVERY new
ComicFile row's ComicFileAddedEvent runs SYNCHRONOUS handlers that
OPEN AND READ THE ARCHIVE: CreditPopulationService (credit extraction)
and ArchiveInspectionService (zip walk). No progress message, all
logging at Debug/Trace. At NFS speeds that is hours of silent grinding
behind a frozen command message. Caught red-handed via the live trace
toggle during the final 6,122-file import: constant stream of
"Extracted N credits from…" / "Inspecting archive for…" while the
command message sat frozen at "Importing 6122 files" for 2h40m
(21:11→23:52, ~0.6 files/s over NFS).

Yesterday's "90-minute hang" was minute 90 of the same pass over
12,326 fresh rows on a cold NFS cache (projected ~5-8 h); the restart
interrupted it harmlessly (inserts commit before the event storm).
The trace rerun "completed in 12 min" only because nothing new was
inserted — no rows, no events, no grind. All observations reconcile.

**Follow-up fix (next code session, priority):** move archive
inspection + credit extraction OFF the synchronous import path into a
queued background command with ProgressInfo ("Inspecting archives
N/M"), or at minimum batch + progress-report them. Related:
scale-readiness #4 (per-file events).

## The scan-hang investigation (methodology log, superseded by above)

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

## Bug found (Session 22, follow-up): queued commands lost on restart

A manual server restart mid-import (16:29, graceful SIGTERM) orphaned
the RUNNING LibraryImport correctly, but the ~500 QUEUED RescanFolders
commands vanished — the queue came back empty and their file mapping
never happened. CommandQueueManager.Requeue() (ApplicationStartedEvent)
is supposed to requeue _repo.Queued(); either those rows were never
persisted as queued or the requeue path drops them. Recovery used:
re-queue the LibraryImport (idempotent skip) + one root RescanFolders
with filter:"matched" to re-map everything. Fix candidate for next
code session; also shipped this session: YearMatchSpecification
(0e771a8) — grab-side year check born from the Green Lantern Corps
wrong-grab (2026 release grabbed for the 2011 series' 2013 issue).

## Also: NFS-mount incident (2026-07-08 evening)

The user's server restart brought the container up BEFORE the NFS
mount: /comics showed 1 near-empty dir instead of 11 publishers.
Consequences: per-series rescans against the broken mount DELETED
~2,490 unmapped rows via MediaFileTableCleanupService's
folder-doesn't-exist path (DB rows only; disk untouched; all
re-registered after the fix). The root-scan empty-folder guard
("Scan folder empty" → skip) prevented worse. Fixed by remounting +
container restart.

- **Infra prevention (user)**: /comics/.zzz_check/ sentinel exists —
  wire it into a docker healthcheck (test -d /comics/.zzz_check) or a
  systemd mount dependency.
- **Code prevention (follow-up)**: vanished-mount guard — refuse
  cleanup when a folder that owns DB rows enumerates empty/missing.

## Code follow-up list (Session 22 harvest)

1. Async/queued archive-inspection + credit-extraction with progress
   (the "hang" fix — biggest UX win).
2. Queued commands lost on restart (Requeue() doesn't restore them).
3. FilterFilesType.Matched excludes unmapped rows at root scope
   (Issue==null arm treats null lazy as matched) — made the intended
   repair rescan a no-op; filter:"none" was the workaround.
4. Vanished-mount cleanup guard (above).
5. Shipped already: YearMatchSpecification (0e771a8).

## Next actions

1. **Leftovers pass**: Library Import → Scan for New Series to
   propose the DC Annual volumes + fix the 3 Epic Collections'
   matches (~50-80 new series, small CV cost); Suske mislabels need
   the RAR→CBZ normalizer or manual conversion.
2. Code follow-ups above (async inspection first).
3. Nightly refresh load check at 1,261 series; Library Import page
   perf (#4/#5 now measurable).
4. Wiki push still blocked on user creating the first page in the
   GitHub UI (drafts in docs/wiki/).
5. Standing watch (only one left): quality "Unknown" in Activity
   manual-import modal.
