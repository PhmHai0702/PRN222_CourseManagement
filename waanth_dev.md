

## 2026-06-07

Investigated issue with student enrollment — the foreign key constraint was failing when
students registered for multiple courses simultaneously.

Root cause: missing index on `Enrollment.StudentId`. Added migration to fix.

**Next:**
- Write integration test for concurrent enrollment
- Verify fix on staging
