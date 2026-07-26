import os
import random
import subprocess
import datetime

DAYS = 30
BRANCH = "fake-history"
START_DATE = datetime.date(2026, 6, 27)
DUMMY_FILE = "devlog.md"

messages = [
    "fix: correct validation logic in enrollment",
    "feat: add course search endpoint",
    "refactor: extract grade calculation helper",
    "chore: update dependency versions",
    "fix: handle null student profile",
    "feat: implement pagination for course list",
    "test: add unit tests for authentication",
    "style: format code per project conventions",
    "docs: update API documentation",
    "fix: resolve timeout in file upload",
    "feat: add student dashboard widget",
    "refactor: simplify enrollment workflow",
    "chore: clean up unused imports",
    "fix: correct date formatting in reports",
    "feat: add export to CSV feature",
    "perf: optimize database queries",
    "test: add integration tests for grades",
    "fix: prevent duplicate course registration",
    "feat: implement role-based access control",
    "style: fix indentation across project",
    "refactor: split monolith into services",
    "docs: add setup guide to README",
    "fix: address null reference in scheduler",
    "feat: add notification preferences",
    "chore: configure CI pipeline",
    "fix: correct sort order in grade table",
    "feat: add search history feature",
    "test: add load testing scripts",
    "fix: handle edge case in fee calculation",
    "refactor: standardize error responses",
]

log_entries = [
    lambda d: f"## {d}\n\nInvestigated issue with student enrollment — the foreign key constraint was failing when\nstudents registered for multiple courses simultaneously.\n\nRoot cause: missing index on `Enrollment.StudentId`. Added migration to fix.\n\n**Next:**\n- Write integration test for concurrent enrollment\n- Verify fix on staging",
    lambda d: f"## {d}\n\nBegan implementing course search endpoint. Initial design:\n\n```\nGET /api/courses/search?q=&category=&page=&size=\n```\n\nSupports full-text search on title and description, filtered by optional category.\nPagination uses offset-based strategy with max page size of 50.\n\nTODO: add sorting by relevance score.",
    lambda d: f"## {d}\n\nRefactored grade calculation logic out of the controller into its own service.\n\nThe old controller had ~200 lines of mixed request handling and business logic.\n\nNew structure:\n- `GradeCalculationService` — pure calculation\n- `GradeController` — thin HTTP layer\n\nReduced controller to 40 lines. Added unit tests.",
    lambda d: f"## {d}\n\nUpdated dependencies across the solution:\n\n| Package | From | To |\n|---------|------|-----|\n| EntityFrameworkCore | 6.0.12 | 6.0.18 |\n| AutoMapper | 12.0.0 | 12.0.1 |\n| Serilog | 2.12.0 | 2.13.1 |\n\nAll tests passing. No breaking changes detected.",
    lambda d: f"## {d}\n\nDebugging null reference in `StudentProfile` — the issue occurs when a new user\nregisters but the profile creation callback fires before the user claim is set in the\nHTTP context.\n\nFix: added null check and fallback to fetch user from DB directly.\n\n```csharp\nif (userId == null)\n{{\n    userId = await _userManager.GetUserIdAsync(user);\n}}\n```",
    lambda d: f"## {d}\n\nImplemented pagination for the course listing endpoint.\n\nUsed `IQueryable<T>.Skip().Take()` pattern with:\n- Default page size: 20\n- Max page size: 100\n- Returns `X-Total-Count` header\n\nTested with 10k records — response time under 200ms.\n\nAlso added Swagger examples for the query parameters.",
    lambda d: f"## {d}\n\nWrote unit tests for authentication flow:\n\n- Login with valid credentials ✅\n- Login with wrong password ❌ returns 401\n- Login with locked account ❌ returns 423\n- Token refresh flow ✅\n- Token expiry handling ✅\n\nCoverage: 92% on the AuthService.\n\nNeed to add tests for the JWT middleware next.",
    lambda d: f"## {d}\n\nCode cleanup day. Ran `dotnet format` across the solution.\n\nChanges:\n- Removed 400+ trailing whitespace lines\n- Fixed inconsistent indentation (tabs vs spaces in 12 files)\n- Added file-scoped namespace declarations\n- Removed unused `using` directives\n\nZero functional changes. CI linting rules updated.",
    lambda d: f"## {d}\n\nWrote API documentation for the course management endpoints.\n\nDocumented:\n- `GET /api/courses` — list with pagination\n- `GET /api/courses/{{id}}` — detail\n- `POST /api/courses` — create (requires Admin role)\n- `PUT /api/courses/{{id}}` — update\n- `DELETE /api/courses/{{id}}` — soft delete\n\nAdded XML comments to all DTOs and controllers.",
    lambda d: f"## {d}\n\nFile upload timeout was set to 30s — not enough for large PDF attachments.\n\nIncreased to 5 minutes for uploads > 10MB. Also added:\n- Chunked upload support (5MB per chunk)\n- Progress tracking via SignalR\n- File type validation on server side\n\nTested with 50MB PDF — works reliably.",
    lambda d: f"## {d}\n\nDesigned the student dashboard widget layout:\n\n```\n+------------------+------------------+\n| Enrollment Stats  | Upcoming Deadlines|\n+------------------+------------------+\n| Recent Grades    | Announcements    |\n+------------------+------------------+\n```\n\nEach widget is an independent Blazor component with its own data loading.\nImplemented lazy loading to avoid blocking the initial render.",
    lambda d: f"## {d}\n\nSimplified the enrollment workflow:\n\nBefore: Student selects course → adds to cart → checks out → admin approves\nAfter: Student selects course → enroll immediately (auto-approved for eligible)\n\nReduced the flow from 4 steps to 2. Removed the `PendingEnrollment` table entirely.\n\nCaveat: courses with prerequisites still require manual approval.",
    lambda d: f"## {d}\n\nRemoved dead code found via `dotnet tool run dead-code`:\n\n- `LegacyGradeConverter.cs` — unused since v2 migration\n- `XmlReportExporter.cs` — replaced by CSV/PDF exporters\n- `CourseMergeService.cs` — feature was dropped\n\nSaved ~1500 lines of dead code. Solution builds 2s faster.",
    lambda d: f"## {d}\n\nDate formatting bug in the reports module:\n\nThe `ReportGenerator` was using `DateTime.Now.ToShortDateString()` which outputs\ndifferent formats depending on the server's locale.\n\nFix: use `DateTime.Now.ToString(\"yyyy-MM-dd\", CultureInfo.InvariantCulture)`\nto ensure consistent ISO 8601 format across all environments.",
    lambda d: f"## {d}\n\nAdded CSV export for grade reports.\n\nUsage: `GET /api/grades/export?format=csv`\n\nUses `CsvHelper` library. Streams the file directly to the response to avoid\nloading the entire dataset in memory.\n\nTested with 50k records — exported in ~3s with ~5MB file size.",
    lambda d: f"## {d}\n\nDatabase query optimization — the enrollment listing page was running 8 separate\nqueries due to lazy loading.\n\nFix: added `.Include(x => x.Course).Include(x => x.Student)` to the query,\nreducing to a single round trip. Also added a composite index on\n`Enrollment(CourseId, StudentId)`.\n\nPage load time: 4.2s → 0.3s",
    lambda d: f"## {d}\n\nWrote integration tests for the grade management API:\n\n- Create grade with valid data ✅\n- Create grade for nonexistent student ❌ 404\n- Update grade with invalid value ❌ 400\n- Delete grade (authorized) ✅\n- Delete grade (unauthorized) ❌ 403\n\nUses `WebApplicationFactory<T>` with in-memory database.",
    lambda d: f"## {d}\n\nFound a bug: students could register for the same course twice.\n\nThe unique constraint on `Enrollment(StudentId, CourseId)` was missing.\n\nAdded the constraint via EF migration and added a check in the service layer\nto return a friendly error instead of a raw SQL exception.\n\n```sql\nCREATE UNIQUE INDEX IX_Enrollment_StudentCourse ON Enrollment(StudentId, CourseId);\n```",
    lambda d: f"## {d}\n\nStarted implementing role-based access control (RBAC).\n\nRoles:\n- `Admin` — full access\n- `Instructor` — manage courses, view grades\n- `Student` — enroll, view own grades\n\nAdded policy-based authorization:\n```csharp\nbuilder.Services.AddAuthorization(options =>\n{{\n    options.AddPolicy(\"RequireAdmin\", p => p.RequireRole(\"Admin\"));\n}});\n```",
    lambda d: f"## {d}\n\nRan `dotnet format whitespace` across the entire solution.\n\nFixed inconsistent indentation in 20+ files. Mostly tabs vs spaces in\n`.razor` files and old `.cs` files that predated the editorconfig.\n\nAdded `.editorconfig` to enforce:\n- charset: utf-8\n- indent_style: space\n- indent_size: 4\n- end_of_line: crlf",
    lambda d: f"## {d}\n\nSplit the monolithic `CourseService` into focused services:\n\n- `CourseQueryService` — read operations, search, filtering\n- `CourseCommandService` — CRUD with validation\n- `CourseEnrollmentService` — enrollment logic\n\nEach service has its own interface for testability.\n\nOld file: 600+ lines. New files: ~150-200 lines each.",
    lambda d: f"## {d}\n\nAdded a setup guide to README.md covering:\n\n1. Prerequisites (NET 8 SDK, SQL Server, VS 2022)\n2. Clone and restore\n3. Database setup (run migrations + seed script)\n4. Configuration (appsettings.json)\n5. Running the application\n6. Running tests\n\nAlso added troubleshooting section for common issues.",
    lambda d: f"## {d}\n\nFixed a null reference exception in the scheduler service.\n\nThe `CourseReminderJob` was accessing `course.Deadline` without checking if\nthe course had a deadline set.\n\n```csharp\nif (course.Deadline.HasValue && course.Deadline.Value.Date == today)\n{{\n    // send reminder\n}}\n```\n\nAdded null-conditional operator throughout the scheduler.",
    lambda d: f"## {d}\n\nAdded notification preferences page.\n\nUsers can now configure:\n- Email notifications (on/off)\n- Push notifications (on/off)\n- Notification types: grade changes, enrollment updates, announcements\n\nPreferences stored in `AspNetUserClaims` table. Defaults are all enabled.\n\nAPI endpoint: `PUT /api/users/notifications`",
    lambda d: f"## {d}\n\nSet up the CI/CD pipeline with GitHub Actions.\n\nWorkflow:\n1. Build solution (`dotnet build --configuration Release`)\n2. Run tests (`dotnet test`)\n3. Run linter (`dotnet format --verify-no-changes`)\n4. Deploy to staging (on push to `develop`)\n5. Deploy to production (on push to `main`)\n\nPipeline takes ~4 minutes to complete.",
    lambda d: f"## {d}\n\nFixed the sort order in the grade table — students were seeing grades sorted\nby `CreatedAt` ascending (oldest first).\n\nChanged to descending order so the most recent grade appears at the top.\n\n```csharp\nreturn await _context.Grades\n    .Where(g => g.StudentId == id)\n    .OrderByDescending(g => g.CreatedAt)\n    .ToListAsync();\n```",
    lambda d: f"## {d}\n\nAdded search history feature — the last 10 searches are saved locally\nin `localStorage` and displayed as quick filters on the search page.\n\nSearch history items include:\n- Query text\n- Applied filters (category, level)\n- Timestamp\n\nUsers can clear individual items or the entire history.\n\nData is not sent to the server for privacy.",
    lambda d: f"## {d}\n\nUsed `NBomber` to write load tests for the course listing endpoint.\n\nScenario: 100 concurrent users, each making 10 requests over 30 seconds.\n\nResults:\n- RPS: ~850\n- P95 latency: 320ms\n- Error rate: 0%\n\nGood baseline. Need to test with DB connection pooling limitations.",
    lambda d: f"## {d}\n\nEdge case in fee calculation: when a student enrolls in multiple courses that\nshare a bundle discount, the discount was applied multiple times.\n\nFix: compute bundle discount once per unique bundle, not per course.\n\n```sql\n-- Before: discount applied per course\n-- After: discount applied once per bundle\n```\n\nAdded test with 3 courses, same bundle, 1 discount expected.",
    lambda d: f"## {d}\n\nStandardized error responses across all API endpoints.\n\nUnified response format:\n```json\n{{\n    \"success\": true/false,\n    \"data\": {{ ... }},\n    \"error\": {{ \"code\": \"VALIDATION_ERROR\", \"message\": \"...\", \"details\": [...] }}\n}}\n```\n\nAdded `ApiResponse<T>` wrapper class and `ExceptionMiddleware` to catch\nunhandled exceptions and return consistent errors.",
]


def run(cmd):
    result = subprocess.run(cmd, shell=True, capture_output=True, text=True)
    if result.returncode != 0:
        print(f"Error: {result.stderr}")
        exit(1)
    return result.stdout.strip()


def main():
    name = run("git config user.name")
    email = run("git config user.email")

    run(f"git checkout -b {BRANCH} main")

    for i in range(DAYS):
        d = START_DATE + datetime.timedelta(days=i)
        h = random.randint(8, 22)
        m = random.randint(0, 59)
        s = random.randint(0, 59)
        ts = datetime.datetime(d.year, d.month, d.day, h, m, s)
        date_str = ts.strftime("%a %b %d %H:%M:%S %Y +0000")

        entry = log_entries[i % len(log_entries)](d.strftime("%Y-%m-%d"))

        with open(DUMMY_FILE, "a", encoding="utf-8") as f:
            f.write("\n\n" + entry + "\n")

        env = os.environ.copy()
        env["GIT_AUTHOR_NAME"] = name
        env["GIT_AUTHOR_EMAIL"] = email
        env["GIT_COMMITTER_NAME"] = name
        env["GIT_COMMITTER_EMAIL"] = email

        subprocess.run(["git", "add", DUMMY_FILE], env=env, check=True)
        env["GIT_AUTHOR_DATE"] = date_str
        env["GIT_COMMITTER_DATE"] = date_str
        subprocess.run(
            ["git", "commit", "-m", messages[i % len(messages)]],
            env=env, check=True
        )
        print(f"  [{i+1}/{DAYS}] {d} {h:02d}:{m:02d}:{s:02d} — {messages[i % len(messages)]}")

    print(f"\nDone! Branch '{BRANCH}' created with {DAYS} commits.")
    print(f"Run:  git checkout main")
    print(f"      git merge {BRANCH}")
    print(f"      git push origin main")


if __name__ == "__main__":
    main()
