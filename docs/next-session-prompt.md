# Panelarr — Next Session Prompt

Copy this into the first message of a new Claude session (fill in the
keys):

---

We're continuing work on Panelarr (~/Projects/panelarr). Read
`docs/last-handoff.md` first — full Session 24 state. Short version:
three releases shipped (v1.1.19 → v1.1.21) fixing a three-bug pileup
on the tag-embed path (DI crash, utf-16-declared XML, Web URL that
crashed Kavita's parser); the Epic Collections arc is COMPLETE (all
three lists live in Kavita); unmapped went 143 → 67; and 83
Wikipedia-derived Epic reading lists (515 slots) now live in Panelarr
as collecting checklists.

Homelab Panelarr API key: <paste key here>.
Homelab Kavita API key (only if Kavita work planned): <paste key here>.

Today's agenda (in recommended order):

1. **Reading-list feature trio** (agreed backlog, design-discuss each
   briefly before building):
   a. Push resolved-slots-only (Kavita rejects lists wholesale when
      any series is missing — blocks pushing the new checklists).
   b. CBL matcher leniency: ignore leading "The" when resolving.
   c. Export-all reading lists (zip of CBLs; per-list export exists).
2. **Wire ComicFileRetaggedEvent** — nothing publishes it, so Kavita's
   OnIssueRetag notify is dead code. Discuss trigger UX first (import
   path already notifies; avoid double-notify).
3. **Unmapped triage decisions** (user calls, then I execute): 47
   annuals → add CV annual volumes (most have embedded CV ids); 6 Red
   Hood Outlaw #41-49 → import into Red Hood and the Outlaws (2016)?;
   ~11 redundant duplicates → delete or leave; 3 CV-gap oddities.
4. Standing watch: quality "Unknown" modal, Backup lastDuration
   cosmetic bug.

Dev notes: local dev instance data dir is
`~/Library/Application Support/Panelarr` (port 8787); binary builds to
`_output/net10.0/` via `dotnet msbuild -restore src/Panelarr.sln
-p:Configuration=Debug -p:Platform=Posix -t:Build`. Dev DB indexers and
download clients are DISABLED on purpose. Puppeteer is global
(NODE_PATH=$(npm root -g)). Suite green = Core.Test (2,573) +
Host.Test ContainerFixture (resolves every command executor — run it
for any new command/dependency).

Hard-won gotchas: Kavita's scanner skips mtime-unchanged folders and
silently drops files that throw during parse — pull the Kavita server
log zip FIRST when files don't appear; its push-rejection messages
truncate to 3 entries. ManualImport takes issueId singular per file.

Standing rules as always: test before presenting, discuss designs
before implementing, scan before any push (from a file, as separate
checked steps — not && chains), no AI attribution in commits, ask
before pushing, homelab host/IP details stay OUT of public repo docs.
---
