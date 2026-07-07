

## 2026-06-07

Investigated issue with student enrollment — the foreign key constraint was failing when
students registered for multiple courses simultaneously.

Root cause: missing index on `Enrollment.StudentId`. Added migration to fix.

**Next:**
- Write integration test for concurrent enrollment
- Verify fix on staging


## 2026-06-10

Began implementing course search endpoint. Initial design:

```
GET /api/courses/search?q=&category=&page=&size=
```

Supports full-text search on title and description, filtered by optional category.
Pagination uses offset-based strategy with max page size of 50.

TODO: add sorting by relevance score.


## 2026-06-14

Refactored grade calculation logic out of the controller into its own service.

The old controller had ~200 lines of mixed request handling and business logic.

New structure:
- `GradeCalculationService` — pure calculation
- `GradeController` — thin HTTP layer

Reduced controller to 40 lines. Added unit tests.
