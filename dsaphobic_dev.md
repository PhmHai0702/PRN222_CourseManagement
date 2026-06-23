

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
