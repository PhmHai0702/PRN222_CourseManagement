

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


## 2026-06-15

Updated dependencies across the solution:

| Package | From | To |
|---------|------|-----|
| EntityFrameworkCore | 6.0.12 | 6.0.18 |
| AutoMapper | 12.0.0 | 12.0.1 |
| Serilog | 2.12.0 | 2.13.1 |

All tests passing. No breaking changes detected.


## 2026-06-16

Debugging null reference in `StudentProfile` — the issue occurs when a new user
registers but the profile creation callback fires before the user claim is set in the
HTTP context.

Fix: added null check and fallback to fetch user from DB directly.

```csharp
if (userId == null)
{
    userId = await _userManager.GetUserIdAsync(user);
}
```


## 2026-06-21

Implemented pagination for the course listing endpoint.

Used `IQueryable<T>.Skip().Take()` pattern with:
- Default page size: 20
- Max page size: 100
- Returns `X-Total-Count` header

Tested with 10k records — response time under 200ms.

Also added Swagger examples for the query parameters.
