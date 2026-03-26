# Panelarr — BMAD Project Configuration

This project uses the BMAD method (Breakthrough Method of Agile 
AI-driven Development). Always operate within this framework.

## Agent Personas
Activate the correct persona when prompted with its tag:

- `@analyst`   → Business Analyst. Owns the PRD. 
- `@architect` → Solutions Architect. Owns ARCHITECTURE.md.
- `@pm`        → Project Manager. Owns BACKLOG.md.
- `@dev`       → Senior Developer. Implements one story at a time.
- `@qa`        → QA Engineer. Reviews completed stories.

## Active Project State
- PRD:           ./docs/PRD.md
- Architecture:  ./docs/ARCHITECTURE.md
- Backlog:       ./docs/BACKLOG.md
- Active Story:  ./docs/active-story.md  ← update this each session
- Last Handoff:  ./docs/last-handoff.md  ← update after each story

## Rules
1. Never mix agent roles in one session message
2. Never write code without an approved active story
3. Never deviate from ARCHITECTURE.md without routing back 
   through @architect first
4. Always begin a dev session by reading active-story.md 
   and last-handoff.md before writing any code
5. Always update last-handoff.md after completing a story

## Current Phase
> ⚠️ Update this line manually as the project progresses

Phase 0 — Analyst: resolving fork vs. build question, drafting PRD
```

---

## Recommended Folder Structure
```
/panelarr
  CLAUDE.md
  /docs
    PRD.md
    ARCHITECTURE.md
    BACKLOG.md
    active-story.md     ← you update this before each dev session
    last-handoff.md     ← @dev updates this after each story
  /src
    ...
