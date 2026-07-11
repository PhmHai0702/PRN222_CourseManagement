

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


## 2026-06-25

Wrote unit tests for authentication flow:

- Login with valid credentials ✅
- Login with wrong password ❌ returns 401
- Login with locked account ❌ returns 423
- Token refresh flow ✅
- Token expiry handling ✅

Coverage: 92% on the AuthService.

Need to add tests for the JWT middleware next.


## 2026-06-27

Code cleanup day. Ran `dotnet format` across the solution.

Changes:
- Removed 400+ trailing whitespace lines
- Fixed inconsistent indentation (tabs vs spaces in 12 files)
- Added file-scoped namespace declarations
- Removed unused `using` directives

Zero functional changes. CI linting rules updated.


## 2026-07-01

Wrote API documentation for the course management endpoints.

Documented:
- `GET /api/courses` — list with pagination
- `GET /api/courses/{id}` — detail
- `POST /api/courses` — create (requires Admin role)
- `PUT /api/courses/{id}` — update
- `DELETE /api/courses/{id}` — soft delete

Added XML comments to all DTOs and controllers.
