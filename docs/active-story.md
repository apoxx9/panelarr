# Active Story: Comic Reader UX Audit Fixes (Session 5)

## Status: READY — Prioritized cleanup backlog

Residual book/audiobook UX artifacts that leaked through from the Readarr fork. These need to be scrubbed so the UI reads as a native comic manager.

---

## Critical

1. **Naming tokens show audio metadata** — MediaInfo AudioCodec, AudioBitrate, AudioChannels, AudioBitsPerSample, AudioSampleRate tokens still appear in the naming config UI. Remove them entirely.
2. **Quality examples show AZW3/MP3** — Quality profile examples reference ebook/audiobook formats. Change to CBZ/CBR.
3. **Full Calibre integration in Root Folder modal** — Calibre library path, host, port, username, password fields show in the Add Root Folder modal. Hide or remove.
4. **Calibre Metadata section in Settings** — Dedicated Calibre settings section still present. Remove.

## High

5. **Edition concept in Interactive Import** — Edition selection/display still appears in the manual/interactive import flow. Hide or remove.
6. **ISBN/ASIN references** — ISBN/ASIN appear in the search input placeholder text and in the metadata profile configuration. Remove.
7. **Audio tagging translations** — Translation keys related to audio tagging (e.g. AudioTagging, WriteAudioTags) are still present. Clean up.

## Medium

8. **"Issueshelf" terminology** — Rename to "Pull List" or "Collection" throughout the UI.
9. **Metadata Profile poorly explained** — The metadata profile section lacks clear explanation of what it controls for comics. Improve copy/tooltips.
10. **"Skip Part Issues and Sets" toggle** — Rephrase label and description to make sense for comics (not audiobook box sets).
11. **{Edition Year} naming token** — Still available in the naming token picker. Remove.
