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
- Active Story:  ./docs/active-story.md
- Last Handoff:  ./docs/last-handoff.md

## Rules
1. Never mix agent roles in one session message
2. Never write code without an approved active story (does not apply to audits, bugfixes, or refactors requested directly by the user)
3. Never deviate from ARCHITECTURE.md without routing back through @architect first

## Token Optimization
- Trust information already in CLAUDE.md, memory, or prior tool results — do not re-read files you have already read in this session
- Never make speculative tool calls (reading files "just in case"). Every tool call must have a clear reason
- Parallelize independent tool calls in a single message wherever possible
- Route large outputs (audit results, search results, multi-file exploration) to subagents to keep main context clean
- Do not restate or summarize the user's request back to them — just do the work

## Handoff Chain
Every task follows a strict sequence. Nothing skips a step:
1. **Plan** — For stories: read active-story.md + last-handoff.md. For ad-hoc work: confirm scope with user.
2. **Build** — Implement exactly what was specified. Discuss design decisions before coding.
3. **Verify** — Build backend + frontend, run puppeteer tests on all pages, hit key API endpoints. Never present work as done without passing verification.
4. **Hand off** — For stories: update last-handoff.md with what was done, what changed, and what's next. For ad-hoc work: update session memory.

## Current Phase
All phases complete (P1 through P3). Runtime testing/bugfixing underway. Next: F-01 (Story Arcs).

## Recommended Folder Structure
```
/panelarr
  CLAUDE.md
  /docs
    PRD.md
    ARCHITECTURE.md
    BACKLOG.md
    active-story.md
    last-handoff.md
  /src
    ...
```
