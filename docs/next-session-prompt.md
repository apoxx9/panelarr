# Panelarr — Next Session Prompt

Copy this into the first message of a new Claude session (fill in the
keys):

---

We're continuing work on Panelarr (~/Projects/panelarr). Read
`docs/last-handoff.md` first — full Session 23 state. Short version:
five releases shipped (v1.1.14 → v1.1.18): comic packs, async archive
inspection, the Session 22 bug batch, identification threshold fixes,
shared folders first-class, and ConvertComicFilesCommand. Unmapped went
619 → 143; Epics are modeled as 47 tagged one-shots with three
volume-ordered reading lists (ASM pushed to Kavita).

Homelab Panelarr API key: <paste key here>.

Today's agenda (in recommended order):

1. **Finish the staged conversion** (~15 min): confirm the container is
   on v1.1.18, POST command `ConvertComicFiles` for the FF Epic +
   Iron Man Epic + Suske en Wiske series ids, then push reading lists
   5 and 6 to Kavita and run a matched rescan so Suske's renamed files
   map. Details in the handoff.
2. **Auto-convert-on-import setting**: Media Management toggle running
   ConvertToRealCbz before WriteTags on import — prevents future .cbr
   grabs from recreating the Kavita drift. Design is one paragraph in
   the handoff; discuss placement, then build.
3. **Review-modal triage** of the remaining ~143 unmapped (genuine
   per-file oddities now — nothing systemic left).
4. Standing watch: quality "Unknown" modal (unreproduced), Backup task
   lastDuration cosmetic bug.

User-side homework (remind gently): docker healthcheck on the /comics
sentinel dir; create the GitHub wiki's first page so docs/wiki/ can
push.

Dev notes: local dev instance data dir is
`~/Library/Application Support/Panelarr` (port 8787); binary builds to
`_output/net10.0/` via `dotnet msbuild -restore src/Panelarr.sln
-p:Configuration=Debug -p:Platform=Posix -t:Build`. Dev DB indexers and
download clients are DISABLED on purpose — re-enable deliberately for a
test, re-disable after. Puppeteer is global (NODE_PATH=$(npm root -g)).

Standing rules as always: test before presenting, discuss designs
before implementing, scan before any push (from a file, as separate
checked steps — not && chains), no AI attribution in commits, ask
before pushing, homelab host/IP details stay OUT of public repo docs.
