# Panelarr — Next Session Prompt

Copy this into the first message of a new Claude session (fill in the
keys):

---

We're continuing work on Panelarr (~/Projects/panelarr). Read
`docs/last-handoff.md` first — it has full Session 21 state. Short
version: **v1.1.13 is released AND verified on the homelab**
(1.1.13.101, migrations 13+14 applied). Reading Lists are fully live
end-to-end: the CBL-imported 'Boom Studios - Power Rangers Saga' list
self-healed to 171/171 after the v1.1.12 resolve fix, Kavita is
connected (scan-on-import + Send Reading Lists), and a real push
landed the list in Kavita with 171/171 matched. README + GitHub wiki
(Home, Reading Lists, Pull List, Library Import, Reader Integration)
are updated. The localization surface is guard-tested (literal,
backend, dynamic-prefix keys).

Homelab Panelarr API key: <paste key here>. Kavita API key (only if
reader work planned): <paste key here>.

Today's agenda — pick from the backlog:

1. **Real staging import on the homelab**: the container only mounts
   /comics and a local /downloads — add the seedbox/staging source as
   a volume, set StagingFolder, run a real Import from Staging.
2. **Metron enrichment** for reading lists (arc records carry cv_id —
   free cross-provider mapping; docs/story-arcs.md follow-ups).
3. **Reading-list reordering UI** (positions are ours already).
4. **Issueshelf rename** (decide what the feature is first) or other
   feature-landscape items (#4 want-list/one-offs, #5 custom formats).

Dev notes: local dev instance data dir is
`~/Library/Application Support/Panelarr` (port 8787; the
`~/.config/Panelarr` copy is stale — wrong API key). The dev DB has
live indexers configured, so stop the instance when done testing.
Puppeteer is installed globally (NODE_PATH=$(npm root -g)).

Standing watch: quality "Unknown" in the Activity manual-import modal
(unreproduced since Session 18).

Standing rules as always: test before presenting, discuss designs
before implementing, scan before any push (from a file, as separate
checked steps — not && chains), no AI attribution in commits, ask
before pushing.
