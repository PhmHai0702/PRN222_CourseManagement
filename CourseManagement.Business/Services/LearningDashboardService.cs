using CourseManagement.Business.Services.IService;
using CourseManagement.DataAccess.Data;
using CourseManagement.Model.Constant;
using CourseManagement.Model.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Business.Services
{
    /// <summary>
    /// Tổng hợp dữ liệu học tập hiện có thành dashboard cho một người dùng.
    /// Service chỉ đọc dữ liệu và không làm thay đổi tiến độ.
    /// </summary>
    public sealed class LearningDashboardService : ILearningDashboardService
    {
        private readonly CourseManagementDb _db;

        public LearningDashboardService(CourseManagementDb db)
        {
            _db = db;
        }

        public async Task<LearningDashboardResponse?> GetDashboardAsync(
            string email,
            LearningDashboardRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            ArgumentNullException.ThrowIfNull(request);

            var user = await _db.AppUsers
                .AsNoTracking()
                .Where(item => item.Email == email)
                .Select(item => new
                {
                    item.Id,
                    item.FullName,
                    item.UserName,
                    item.Email
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (user is null)
                return null;

            var enrollments = await _db.enrollments
                .AsNoTracking()
                .Where(item => item.UserId == user.Id)
                .OrderByDescending(item => item.EnrollmentDate)
                .Select(item => new EnrollmentData(
                    item.CourseId,
                    item.EnrollmentDate))
                .ToListAsync(cancellationToken);

            if (enrollments.Count == 0)
            {
                return EmptyDashboard(
                    user.Id,
                    user.FullName ?? user.UserName ?? user.Email ?? "User");
            }

            var courseIds = enrollments
                .Select(item => item.CourseId)
                .Distinct()
                .ToList();

            var courses = await _db.Courses
                .AsNoTracking()
                .Where(course => courseIds.Contains(course.Id))
                .Select(course => new CourseData
                {
                    CourseId = course.Id,
                    Title = course.Title,
                    PreviewImage = course.PreviewImage,
                    CategoryName = course.Category.Name,
                    Lessons = course.Modules
                        .Where(module => module.Status == ModuleStatus.Active)
                        .SelectMany(module => module.Lessons
                            .Where(lesson => lesson.Status == LessonStatus.Active)
                            .Select(lesson => new LessonData
                            {
                                LessonId = lesson.Id,
                                Title = lesson.Title,
                                ModuleId = module.Id,
                                ModuleTitle = module.Title,
                                ModuleOrder = module.Order ?? int.MaxValue,
                                LessonOrder = lesson.Order ?? int.MaxValue,
                                Duration = lesson.VideoDuration
                            }))
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            var completedLessonIds = await _db.LessonProgresses
                .AsNoTracking()
                .Where(progress =>
                    progress.UserId == user.Id &&
                    progress.IsCompleted &&
                    courseIds.Contains(progress.Lesson.Module.CourseId))
                .Select(progress => progress.LessonId)
                .ToListAsync(cancellationToken);

            var completedSet = completedLessonIds.ToHashSet();

            var lastViewedByCourse = await _db.CourseProgresses
                .AsNoTracking()
                .Where(progress =>
                    progress.UserId == user.Id &&
                    courseIds.Contains(progress.CourseId))
                .Select(progress => new
                {
                    progress.CourseId,
                    progress.LastViewedLessonId
                })
                .ToDictionaryAsync(
                    item => item.CourseId,
                    item => item.LastViewedLessonId,
                    cancellationToken);

            var enrollmentDates = enrollments
                .GroupBy(item => item.CourseId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Max(item => item.EnrollmentDate));

            var items = courses
                .Select(course => BuildCourseItem(
                    course,
                    enrollmentDates[course.CourseId],
                    completedSet,
                    lastViewedByCourse.GetValueOrDefault(course.CourseId)))
                .ToList();

            var completedCourses = items.Count(item => item.IsCompleted);
            var inProgressCourses = items.Count(item =>
                !item.IsCompleted && item.CompletedLessons > 0);

            var filteredItems = items
                .Where(item => request.IncludeCompleted || !item.IsCompleted)
                .OrderBy(item => item.IsCompleted)
                .ThenByDescending(item => item.ProgressPercent)
                .ThenByDescending(item => item.EnrollmentDate)
                .Take(Math.Clamp(request.Take, 1, 20))
                .ToList();

            var totalLessons = items.Sum(item => item.TotalLessons);
            var completedLessons = items.Sum(item => item.CompletedLessons);

            return new LearningDashboardResponse
            {
                UserId = user.Id,
                DisplayName = user.FullName ?? user.UserName ?? user.Email ?? "User",
                EnrolledCourses = items.Count,
                CompletedCourses = completedCourses,
                InProgressCourses = inProgressCourses,
                CompletedLessons = completedLessons,
                TotalLessons = totalLessons,
                OverallProgressPercent = CalculatePercent(
                    completedLessons,
                    totalLessons),
                LearnedMinutes = items.Sum(item => item.LearnedMinutes),
                RemainingMinutes = items.Sum(item => item.RemainingMinutes),
                Courses = filteredItems
            };
        }

        private static LearningCourseProgressItem BuildCourseItem(
            CourseData course,
            DateTime enrollmentDate,
            HashSet<int> completedLessonIds,
            int? lastViewedLessonId)
        {
            var orderedLessons = course.Lessons
                .OrderBy(lesson => lesson.ModuleOrder)
                .ThenBy(lesson => lesson.LessonOrder)
                .ThenBy(lesson => lesson.LessonId)
                .ToList();

            var completedLessons = orderedLessons
                .Where(lesson => completedLessonIds.Contains(lesson.LessonId))
                .ToList();

            var nextLesson = orderedLessons
                .FirstOrDefault(lesson =>
                    !completedLessonIds.Contains(lesson.LessonId));

            var lastViewedLesson = lastViewedLessonId.HasValue
                ? orderedLessons.FirstOrDefault(lesson =>
                    lesson.LessonId == lastViewedLessonId.Value)
                : null;

            var learnedTicks = completedLessons.Sum(item =>
                item.Duration?.Ticks ?? 0);
            var totalTicks = orderedLessons.Sum(item =>
                item.Duration?.Ticks ?? 0);
            var totalLessonCount = orderedLessons.Count;
            var completedLessonCount = completedLessons.Count;

            return new LearningCourseProgressItem
            {
                CourseId = course.CourseId,
                Title = course.Title,
                PreviewImage = course.PreviewImage,
                CategoryName = course.CategoryName,
                EnrollmentDate = enrollmentDate,
                CompletedLessons = completedLessonCount,
                TotalLessons = totalLessonCount,
                ProgressPercent = CalculatePercent(
                    completedLessonCount,
                    totalLessonCount),
                LearnedMinutes = ToRoundedMinutes(learnedTicks),
                RemainingMinutes = ToRoundedMinutes(
                    Math.Max(0, totalTicks - learnedTicks)),
                IsCompleted = totalLessonCount > 0 &&
                    completedLessonCount == totalLessonCount,
                LastViewedLesson = MapLesson(lastViewedLesson),
                NextLesson = MapLesson(nextLesson)
            };
        }

        private static LearningLessonItem? MapLesson(LessonData? lesson)
        {
            if (lesson is null)
                return null;

            return new LearningLessonItem
            {
                LessonId = lesson.LessonId,
                Title = lesson.Title,
                ModuleId = lesson.ModuleId,
                ModuleTitle = lesson.ModuleTitle,
                Order = lesson.LessonOrder == int.MaxValue
                    ? 0
                    : lesson.LessonOrder,
                DurationMinutes = ToRoundedMinutes(
                    lesson.Duration?.Ticks ?? 0)
            };
        }

        private static int CalculatePercent(int completed, int total)
        {
            if (total <= 0)
                return 0;

            return (int)Math.Round(
                completed * 100d / total,
                MidpointRounding.AwayFromZero);
        }

        private static int ToRoundedMinutes(long ticks)
        {
            if (ticks <= 0)
                return 0;

            return (int)Math.Ceiling(TimeSpan.FromTicks(ticks).TotalMinutes);
        }

        private static LearningDashboardResponse EmptyDashboard(
            string userId,
            string displayName)
        {
            return new LearningDashboardResponse
            {
                UserId = userId,
                DisplayName = displayName
            };
        }

        private sealed record EnrollmentData(
            Guid CourseId,
            DateTime EnrollmentDate);

        private sealed class CourseData
        {
            public Guid CourseId { get; init; }
            public string Title { get; init; } = string.Empty;
            public string PreviewImage { get; init; } = string.Empty;
            public string CategoryName { get; init; } = string.Empty;
            public List<LessonData> Lessons { get; init; } = new();
        }

        private sealed class LessonData
        {
            public int LessonId { get; init; }
            public string Title { get; init; } = string.Empty;
            public int ModuleId { get; init; }
            public string ModuleTitle { get; init; } = string.Empty;
            public int ModuleOrder { get; init; }
            public int LessonOrder { get; init; }
            public TimeSpan? Duration { get; init; }
        }
    }
}
