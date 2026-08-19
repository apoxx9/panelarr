# Last Handoff — 2026-08-19 (Session 29, final)

Session 29 spanned 08-17 → 08-19 and shipped **SIXTEEN releases
(v1.1.27 … v1.1.42)**. Two headline outcomes: **GetComics automation
is restored** (the Session-28 "zero automatable mirrors" verdict was
wrong — see below), and **the collected-edition search+import chain
was rebuilt end-to-end** through adversarial testing on real
libraries (Iron Fist, ASM Nine Lives, 22× Captain America Epic, 4× ASM
Modern Era, Aliens 3/3 — all complete in the library). Every failure
observed became a shipped fix; nine distinct collected-edition bugs
were peeled, each hidden behind the previous one.

## Releases shipped (what each one is for)

- **v1.1.27** GetComics: recognize `/dls/` redirects (site renamed
  `/dlds/` → `/dls/`, silently hiding EVERY redirect mirror incl.
  Pixeldrain) + the first-party **DOWNLOAD NOW** main-server link
  (`getcomics.org/dls/` → 302 → `fs*.comicfiles.ru`, captcha-free).
  DataNodes demoted (Turnstile). Mylar research confirmed: it never
  used DataNodes; downloads main-server links with a plain GET.
- **v1.1.28** Import: collected-edition filename variants
  (`Parser.GetCollectedEditionVariants`, raw + marker-stripped
  pre-paren name) fed via `AggregateComicFilename`; volume number →
  `SeriesIndex`. Fixes "Couldn't find similar issue" for
  `<line> Vol. NN - <subtitle>` files.
- **v1.1.29** Import: sole-issue fallback in `CandidateService` for
  collected editions (61/62 Epic series are single-issue "#1" while
  releases carry LINE volume numbers); `IssueDistance` skips the
  issue-number comparison for that candidate. Also: Name + Year
  custom-filter properties on the series index.
- **v1.1.30–32** Search (`ParseIssueTitleWithSearchCriteria`): volume
  marker stands in when it agrees with the issue TITLE ("Volume 11")
  or, for a single target issue, when the marker sits inside the
  matched series span; marker stripped from the returned name;
  fuzzy-path Unknown→Archive quality default. Each moved the
  rejection one stage ("Unknown Series" → "Unknown is not wanted in
  profile" → grab).
- **v1.1.33** Import: augmentation runs even with embedded tags
  (Zone-Empire rips tag bare line name + "vNN - Subtitle" title,
  no number/id); marker-stripped local title may match the series
  subtitle. Search: **cross-grab fixed** — all volume stand-ins demand
  the post-marker subtitle matches the criteria subtitle AS A WHOLE
  (≥0.75); fuzzy containment let "The Captain" grab "…Captain America
  Lives Again".
- **v1.1.34** Import: strip parenthetical release junk from embedded
  titles before subtitle comparison.
- **v1.1.35** Import: **issue-number distance SKIPPED for collected
  editions** — release vNN mixes publication vs CV line order; a
  weight-10 "1"=="1" agreement with the WRONG series dragged a 55%
  mismatch under threshold and mis-imported a file. KEY INSIGHT:
  numbers carry no signal for collected editions; the subtitle is
  the only discriminator, now honoured at every layer.
- **v1.1.36** Downloads: **idle read timeout** (`HttpRequest.
  IdleReadTimeout`, re-arms per read; 120s idle / 300s connect) —
  the fixed 300s cap killed multi-GB Pixeldrain downloads at exactly
  5:00. Verified vs local slow-drip server; live: 13–26 min
  downloads now complete. Also: reading-list "Not found on CV"
  renders as a list.
- **v1.1.37** GetComics: **Mega mirror support** via MegaApiClient
  (anonymous login, decrypting download; `IMegaDownloader`; priority
  Pixeldrain > MainServer > Mega). Non-0-day/tpb posts have NO
  main-server button. GOTCHA: MegaApiClient ships a GLOBAL-namespace
  `BigInteger` that shadows System.Numerics — QBittorrentTorrent.Eta
  fully qualified.
- **v1.1.38** Search: comic-parsed release with series but no issue
  number fell out of the decision loop with NO decision (silently
  vanished — Aliens Vol. 3 was on GC the whole time).
- **v1.1.39** Search: fuzzy criteria parser returns the criteria
  series' canonical name (matched span dropped leading "The" / stopped
  short of subtitle → Unknown Series). Supersedes v1.1.31's strip.
- **v1.1.40** Refresh: **tiered cadence** — active daily, ended
  weekly, recent release promotes to daily; **CV status DERIVED** from
  latest cover date (>180d silent = ended; CV has no flag and ALL
  1,381 series were "continuing" by enum default — tiering alone would
  have refreshed everything daily); **RSS-triggered on-demand
  refresh** when a release matches a series but names no known issue
  (revival safety valve, 12h/series throttle). Projected ~230
  refreshes/day vs 584+ observed (3h19m runs, CV 420 storms).
- **v1.1.41** Delay profiles: **DirectDownload first-class**
  (`EnableDirectDownload`, `DirectDownloadDelay`, migration 15);
  UI offers Prefer Torrent / Prefer DD / Only Torrent / Only DD,
  usenet column dropped; **default flipped to Prefer Torrent**
  (agreed: torrents finish in minutes, throttled DDL takes 30 min;
  DDL stays the fallback + only source for weeklies). Tracked
  downloads trust the grab record's series/issue over title re-parse
  (Aliens sat in queue as "unknown" with nothing to import into).
- **v1.1.42** ComicVine limiter: **two lanes** — interactive
  (lookup/search/add/manual refresh/resolve, scoped at API
  controllers via AsyncLocal `ComicVineRequestScope`) takes the next
  token ahead of bulk; bulk stands aside while interactive waits.
  Add-series search went 88s/timeout → 1–2s.

Also: **31 GitHub Releases backfilled** (v1.1.2–v1.1.32, from
filtered commit subjects) — the what's-new modal reads GH releases
and had shown the same stale content since July. **Release creation
is now part of the ship ritual** (hand-written New/Fixed notes).

## Homelab state at handoff

- v1.1.42 deployed and verified (search 1–2s; queue clean; delay
  profile = Prefer Torrent, DD enabled, DD delay 0).
- Library: 1,381 series, 1,018 GB. Epic census: 62 Epic + 22 CA + 4
  Modern Era + Aliens 3/3 complete. "Epic Collections" saved filter
  (Name contains) = 62/1,354.
- GetComics indexer: RSS + auto-search RE-ENABLED (were off since
  Session 28). Both indexers priority 25.
- MAM daily API grab limit was hit 08-18 (resets daily); Mega
  per-IP anonymous rate limit hit once (clears itself); Pixeldrain
  throttling heavy after ~10 GB/day.
- Tiered refresh (v1.1.40) has NOT yet had a scheduled run on the
  homelab — first one is the next 24h cycle. Status derivation
  converges over that pass.

## Ops knowledge (hard-won this session)

- Trace diagnosis pattern (worked 4×): `PUT /config/host logLevel=
  trace` → reproduce → `GET /api/v1/log/file/panelarr.trace.txt` →
  RESTORE logLevel. `/log/file/...` without `/api/v1/` returns empty.
- Stuck imports: `DownloadedIssuesScan {path, downloadClientId}` per
  download re-attempts a tracked import; `ManualImport {files:[{path,
  seriesId, issueId, quality}]}` bypasses identification (used twice
  to rescue files). `/manualimport` GET on a 1 GB archive hangs
  >280s (inspection) — avoid. Untracked-file scans hit an
  exact-clean-name gate BEFORE identification.
- Queue "unknown series" items are hidden from default queries
  (`includeUnknownSeriesItems=true`) and cannot be DELETEd (500).
- GetComics downloads run SYNCHRONOUSLY inside the grab command
  ("Processing release N/N" for 30 min = a download in flight);
  nothing appears in the queue until the file lands.
- Batch-validate identification offline: `dotnet fsi` against
  `_output/net10.0/Panelarr.Core.dll` (see scratchpad scripts pattern:
  GetCollectedEditionVariants + FuzzyMatch ≥0.8 across all 62 Epic
  names).
- Incremental `msbuild` can skip the API project — if an API field
  does not appear, `dotnet build src/Panelarr.Api.V1/...csproj`
  explicitly.
- `broken_report_shouldnt_blowup_the_process` is an order-dependent
  flake (passes in full runs; fails in isolation on pre-change tree).
- Library composition: 616/1,381 series are 1–2 issue fragments (357
  DC event one-shots, 121 collected editions, 42 annuals, 41 FCBD/
  specials) — the "shit series" feeling is presentation, not junk.
  Objective-junk buckets are near-empty. CV ratings are empty for
  ALL series (no rating-based filtering possible).

## Backlog (agreed / open)

1. **Observe tiered refresh** after its first scheduled homelab run:
   confirm ~230/day and that 180d/7d constants feel right.
2. **Parser NRE**: `Parser.ParseIssueTitle` throws a swallowed
   NullReferenceException on some titles under the test harness
   (seen writing TrackedDownloadServiceFixture; `ExceptionVerification
   .IgnoreErrors()` applied in that test). Root-cause ~30 min.
3. **Session-24 triage** (user calls pending): 47 annuals via
   resolve/add flow; 6 Red Hood Outlaw #41-49 → RHatO (2016)?; ~11
   duplicates; 3 CV-gap oddities (Suske #00, Radiant Black #25.5,
   USM #00.5).
4. **Library-clutter saved filters** (optional, 2 min): "Complete
   Runs" = issueCount > 3; per-line Name filters. Or prune the 41
   FCBD/specials (~5 GB) after a reviewable list.
5. **DD delay tuning**: set DirectDownloadDelay 30–60 min if DDL
   should be a true fallback rather than a parallel racer.
6. **Editorial pass** over the 31 backfilled release bodies (raw
   commit subjects, heuristic New/Fixed bucketing) — cosmetic.
7. **`ComicFileRetaggedEvent`** still dead code — discuss
   double-notify UX first.
8. FlareSolverr-for-GC-front-door is a KNOWN contingency (Mylar has
   it); Panelarr has never hit CF's front-door challenge. Not built.
9. Browser-assisted DDL resolver: SHELVED (unnecessary after v1.1.27).
