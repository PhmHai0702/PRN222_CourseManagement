

## 2026-06-27

Investigated issue with student enrollment — the foreign key constraint was failing when
students registered for multiple courses simultaneously.

Root cause: missing index on `Enrollment.StudentId`. Added migration to fix.

**Next:**
- Write integration test for concurrent enrollment
- Verify fix on staging


## 2026-06-28

Began implementing course search endpoint. Initial design:

```
GET /api/courses/search?q=&category=&page=&size=
```

Supports full-text search on title and description, filtered by optional category.
Pagination uses offset-based strategy with max page size of 50.

TODO: add sorting by relevance score.


## 2026-06-29

Refactored grade calculation logic out of the controller into its own service.

The old controller had ~200 lines of mixed request handling and business logic.

New structure:
- `GradeCalculationService` — pure calculation
- `GradeController` — thin HTTP layer

Reduced controller to 40 lines. Added unit tests.


## 2026-06-30

Updated dependencies across the solution:

| Package | From | To |
|---------|------|-----|
| EntityFrameworkCore | 6.0.12 | 6.0.18 |
| AutoMapper | 12.0.0 | 12.0.1 |
| Serilog | 2.12.0 | 2.13.1 |

All tests passing. No breaking changes detected.


## 2026-07-01

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


## 2026-07-02

Implemented pagination for the course listing endpoint.

Used `IQueryable<T>.Skip().Take()` pattern with:
- Default page size: 20
- Max page size: 100
- Returns `X-Total-Count` header

Tested with 10k records — response time under 200ms.

Also added Swagger examples for the query parameters.


## 2026-07-03

Wrote unit tests for authentication flow:

- Login with valid credentials ✅
- Login with wrong password ❌ returns 401
- Login with locked account ❌ returns 423
- Token refresh flow ✅
- Token expiry handling ✅

Coverage: 92% on the AuthService.

Need to add tests for the JWT middleware next.


## 2026-07-04

Code cleanup day. Ran `dotnet format` across the solution.

Changes:
- Removed 400+ trailing whitespace lines
- Fixed inconsistent indentation (tabs vs spaces in 12 files)
- Added file-scoped namespace declarations
- Removed unused `using` directives

Zero functional changes. CI linting rules updated.


## 2026-07-05

Wrote API documentation for the course management endpoints.

Documented:
- `GET /api/courses` — list with pagination
- `GET /api/courses/{id}` — detail
- `POST /api/courses` — create (requires Admin role)
- `PUT /api/courses/{id}` — update
- `DELETE /api/courses/{id}` — soft delete

Added XML comments to all DTOs and controllers.


## 2026-07-06

File upload timeout was set to 30s — not enough for large PDF attachments.

Increased to 5 minutes for uploads > 10MB. Also added:
- Chunked upload support (5MB per chunk)
- Progress tracking via SignalR
- File type validation on server side

Tested with 50MB PDF — works reliably.


## 2026-07-07

Designed the student dashboard widget layout:

```
+------------------+------------------+
| Enrollment Stats  | Upcoming Deadlines|
+------------------+------------------+
| Recent Grades    | Announcements    |
+------------------+------------------+
```

Each widget is an independent Blazor component with its own data loading.
Implemented lazy loading to avoid blocking the initial render.


## 2026-07-08

Simplified the enrollment workflow:

Before: Student selects course → adds to cart → checks out → admin approves
After: Student selects course → enroll immediately (auto-approved for eligible)

Reduced the flow from 4 steps to 2. Removed the `PendingEnrollment` table entirely.

Caveat: courses with prerequisites still require manual approval.


## 2026-07-09

Removed dead code found via `dotnet tool run dead-code`:

- `LegacyGradeConverter.cs` — unused since v2 migration
- `XmlReportExporter.cs` — replaced by CSV/PDF exporters
- `CourseMergeService.cs` — feature was dropped

Saved ~1500 lines of dead code. Solution builds 2s faster.


## 2026-07-10

Date formatting bug in the reports module:

The `ReportGenerator` was using `DateTime.Now.ToShortDateString()` which outputs
different formats depending on the server's locale.

Fix: use `DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)`
to ensure consistent ISO 8601 format across all environments.


## 2026-07-11

Added CSV export for grade reports.

Usage: `GET /api/grades/export?format=csv`

Uses `CsvHelper` library. Streams the file directly to the response to avoid
loading the entire dataset in memory.

Tested with 50k records — exported in ~3s with ~5MB file size.


## 2026-07-12

Database query optimization — the enrollment listing page was running 8 separate
queries due to lazy loading.

Fix: added `.Include(x => x.Course).Include(x => x.Student)` to the query,
reducing to a single round trip. Also added a composite index on
`Enrollment(CourseId, StudentId)`.

Page load time: 4.2s → 0.3s


## 2026-07-13

Wrote integration tests for the grade management API:

- Create grade with valid data ✅
- Create grade for nonexistent student ❌ 404
- Update grade with invalid value ❌ 400
- Delete grade (authorized) ✅
- Delete grade (unauthorized) ❌ 403

Uses `WebApplicationFactory<T>` with in-memory database.


## 2026-07-14

Found a bug: students could register for the same course twice.

The unique constraint on `Enrollment(StudentId, CourseId)` was missing.

Added the constraint via EF migration and added a check in the service layer
to return a friendly error instead of a raw SQL exception.

```sql
CREATE UNIQUE INDEX IX_Enrollment_StudentCourse ON Enrollment(StudentId, CourseId);
```


## 2026-07-15

Started implementing role-based access control (RBAC).

Roles:
- `Admin` — full access
- `Instructor` — manage courses, view grades
- `Student` — enroll, view own grades

Added policy-based authorization:
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
});
```


## 2026-07-16

Ran `dotnet format whitespace` across the entire solution.

Fixed inconsistent indentation in 20+ files. Mostly tabs vs spaces in
`.razor` files and old `.cs` files that predated the editorconfig.

Added `.editorconfig` to enforce:
- charset: utf-8
- indent_style: space
- indent_size: 4
- end_of_line: crlf


## 2026-07-17

Split the monolithic `CourseService` into focused services:

- `CourseQueryService` — read operations, search, filtering
- `CourseCommandService` — CRUD with validation
- `CourseEnrollmentService` — enrollment logic

Each service has its own interface for testability.

Old file: 600+ lines. New files: ~150-200 lines each.
