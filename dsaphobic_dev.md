

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
