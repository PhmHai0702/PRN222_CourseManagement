

## 2026-06-27

Working on fixing the null reference issue in course enrollment. The problem was that
when a course had no instructor assigned, the enrollment service threw a NullReferenceException.

Added a null check and fallback to display 'TBD' instead. Also added a unit test for this case.

```csharp
if (course.Instructor == null)
{
    // Display as 'To Be Determined'
}
```

Next: need to verify the fix on the staging environment.


## 2026-06-30

Added a basic search filter for the course list page. Users can now filter by:
- Course name (partial match)
- Category (dropdown)
- Status (active/inactive)

The backend uses LINQ with predicate building. Results are paginated with 20 items per page.

API: `GET /api/courses/search?name=&category=&page=&size=`

TODO: add sorting by name or date.


## 2026-07-01

Refactored the grade calculation method in `GradeService`.

The old implementation had the calculation logic mixed with database queries.

Extracted it into a separate `GradeCalculator` class with pure functions:
- `CalculateFinalGrade(homework, midterm, final)`
- `CalculateGPA(grades)`
- `GetLetterGrade(score)`

Added unit tests covering edge cases like missing assignments.


## 2026-07-04

Updated NuGet packages to latest stable versions:

| Package | Version |
|---------|---------|
| Microsoft.EntityFrameworkCore | 8.0.4 |
| AutoMapper | 13.0.1 |
| Serilog.AspNetCore | 8.0.0 |
| Swashbuckle.AspNetCore | 6.5.0 |

All existing tests pass. No breaking changes detected in the API surface.

Also cleaned up some deprecated package references in the csproj files.


## 2026-07-05

Investigated a bug where the student dashboard crashes when a profile is incomplete.

The `StudentProfile` page assumes `FullName` and `AvatarUrl` are always populated,
but new users who haven't completed onboarding are missing these fields.

Fix: added null-conditional operators and fallback values.

```csharp
var name = profile?.FullName ?? "New Student";
var avatar = profile?.AvatarUrl ?? "/images/default-avatar.png";
```


## 2026-07-08

Implemented pagination for the grade table on the student dashboard.

Before: all grades loaded at once — slow for students with 100+ courses.
After: server-side pagination with 15 rows per page.

Added:
- `X-Total-Count` response header
- Previous/Next navigation buttons
- Page number display

Load time reduced from 3.2s to 0.4s for a student with 200 grades.


## 2026-07-10

Wrote unit tests for the authentication flow:

- `Login_ValidCredentials_ReturnsToken` ✅
- `Login_InvalidPassword_Returns401` ✅
- `Login_LockedAccount_Returns423` ✅
- `RefreshToken_Valid_ReturnsNewToken` ✅
- `RefreshToken_Expired_Returns401` ✅

Coverage: 88% on `AuthService`. Used Moq for mocking `UserManager<T>`.

Need to add integration tests with `WebApplicationFactory` next sprint.
