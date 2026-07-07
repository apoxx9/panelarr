# Panelarr — Next Session Prompt

Copy this into the first message of a new Claude session (fill in the
keys):

---

We're continuing work on Panelarr (~/Projects/panelarr). Read
`docs/last-handoff.md` first — it has full Session 21 state. Short
version: **v1.1.10 is tagged** (fossil cleanup + migration 13
download-client settings rename + migration 14 + Reading Lists A+B+C
incl. push-to-Kavita). The homelab is still on 1.1.9 and needs a
manual pull. Kavita (v0.9.0.2; address: <paste here>) got its
'Comics' library created in Session 21; its initial scan was still
running at session end.

Homelab Panelarr: <paste address here>, API key: <paste key here>.
Kavita API key (if reader work planned): <paste key here>.

Today's agenda, in order:

1. **Homelab pull v1.1.10** and verify: migrations 13+14 apply,
   reading lists live, rTorrent download client still configured
   (migration 13 rewrites its settings JSON).
2. **Real-world reading list**: add Kavita as a connection on homelab
   Panelarr ('Send Reading Lists' on), import/push a real arc, check
   it in Kavita.
3. Pick from backlog: staging source mount for the homelab container
   (real Import from Staging), Metron enrichment (cv_id mapping),
   reading-list reordering UI, 130 unused translation keys (usage
   scan), Issueshelf rename.

Dev notes: local dev instance data dir is
`~/Library/Application Support/Panelarr` (port 8787; the
`~/.config/Panelarr` copy is stale — wrong API key). The dev DB has
live indexers configured, so stop the instance when done testing.
Puppeteer is installed globally (NODE_PATH=$(npm root -g)).

Standing watch: quality "Unknown" in the Activity manual-import modal
(unreproduced since Session 18).

Standing rules as always: test before presenting, discuss designs
before implementing, scan before any push, no AI attribution in
commits, ask before pushing.
