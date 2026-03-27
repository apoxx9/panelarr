# Active Story: P1-05

## Rename Book → Issue, BookFile → ComicFile; remove Edition entity

**Status:** Ready
**Started:** —

### Tasks
- [ ] Rename `Book` entity → `Issue` (model, service, repository, events, API resource)
- [ ] Rename `BookFile` entity → `ComicFile`
- [ ] Remove `Edition` entity (flatten into `Issue`)
- [ ] Update all callers, tests, and migrations
- [ ] Verify `dotnet build src/Panelarr.sln` passes with 0 errors

### Acceptance Criteria
`Issue` and `ComicFile` compile and pass basic repository CRUD; `Edition` references are removed.

### Notes
- Ref: ARCHITECTURE.md Section 4 (Domain Model)
- Depends on P1-04 (Done)
