using CourseManagement.Business.Services.IService;
using CourseManagement.DataAccess.Data;
using CourseManagement.Model.Constant;
using CourseManagement.Model.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Business.Services
{
    /// <summary>
    /// Tổng hợp các chỉ số có thể so sánh trực tiếp giữa nhiều khóa học.
    /// Service không tự kết luận khóa học nào tốt nhất cho mọi người dùng.
    /// </summary>
    public sealed class CourseComparisonService : ICourseComparisonService
    {
        private readonly CourseManagementDb _db;

        public CourseComparisonService(CourseManagementDb db)
        {
            _db = db;
        }

        public async Task<CourseComparisonResponse> CompareAsync(
            CourseComparisonRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var requestedIds = request.CourseIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            if (requestedIds.Count is < 2 or > 4)
            {
                throw new ArgumentException(
                    "Vui lòng chọn từ 2 đến 4 khóa học khác nhau.",
                    nameof(request));
            }

            var courseData = await _db.Courses
                .AsNoTracking()
                .Where(course =>
                    requestedIds.Contains(course.Id) &&
                    course.Status == CourseStatus.Publish)
                .Select(course => new ComparisonData
                {
                    CourseId = course.Id,
                    Title = course.Title,
                    Description = course.Description,
                    PreviewImage = course.PreviewImage,
                    CategoryId = course.CategoryId,
                    CategoryName = course.Category.Name,
                    Level = course.Level,
                    CourseType = course.CourseType,
                    EnrollmentCount = course.Enrollments.Count(),
                    LearningOutcomes = course.LearningOutcomes
                        .OrderBy(outcome => outcome.Id)
                        .Select(outcome => outcome.Outcome)
                        .ToList(),
                    Modules = course.Modules
                        .Where(module => module.Status == ModuleStatus.Active)
                        .Select(module => new ModuleData
                        {
                            ModuleId = module.Id,
                            Lessons = module.Lessons
                                .Where(lesson =>
                                    lesson.Status == LessonStatus.Active)
                                .Select(lesson => new LessonData
                                {
                                    LessonId = lesson.Id,
                                    Duration = lesson.VideoDuration,
                                    DocumentCount = lesson.Documents.Count()
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            var missingIds = requestedIds
                .Except(courseData.Select(course => course.CourseId))
                .ToList();

            var rawItems = courseData
                .Select(MapItem)
                .ToList();

            var summary = BuildSummary(rawItems);
            var itemsWithStrengths = rawItems
                .Select(item => AddStrengths(item, summary))
                .OrderBy(item => requestedIds.IndexOf(item.CourseId))
                .ToList();

            return new CourseComparisonResponse
            {
                RequestedCourses = requestedIds.Count,
                ComparedCourses = itemsWithStrengths.Count,
                MissingCourseIds = missingIds,
                Summary = summary,
                Courses = itemsWithStrengths
            };
        }

        private static CourseComparisonItem MapItem(ComparisonData course)
        {
            var lessons = course.Modules
                .SelectMany(module => module.Lessons)
                .ToList();

            var durationTicks = lessons.Sum(lesson =>
                lesson.Duration?.Ticks ?? 0);

            return new CourseComparisonItem
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Description = course.Description,
                PreviewImage = course.PreviewImage,
                CategoryId = course.CategoryId,
                CategoryName = course.CategoryName,
                Level = course.Level,
                CourseType = course.CourseType,
                ModuleCount = course.Modules.Count,
                LessonCount = lessons.Count,
                DocumentCount = lessons.Sum(lesson => lesson.DocumentCount),
                EnrollmentCount = course.EnrollmentCount,
                EstimatedDurationMinutes = ToMinutes(durationTicks),
                LearningOutcomes = course.LearningOutcomes
            };
        }

        private static CourseComparisonSummary BuildSummary(
            IReadOnlyList<CourseComparisonItem> courses)
        {
            if (courses.Count == 0)
                return new CourseComparisonSummary();

            var popular = courses
                .OrderByDescending(course => course.EnrollmentCount)
                .ThenBy(course => course.Title)
                .First();

            var comprehensive = courses
                .OrderByDescending(course => course.LessonCount)
                .ThenByDescending(course => course.ModuleCount)
                .ThenBy(course => course.Title)
                .First();

            var coursesWithDuration = courses
                .Where(course => course.EstimatedDurationMinutes > 0)
                .ToList();

            var shortest = coursesWithDuration
                .OrderBy(course => course.EstimatedDurationMinutes)
                .ThenBy(course => course.Title)
                .FirstOrDefault();

            var sameCategory = courses
                .Select(course => course.CategoryId)
                .Distinct()
                .Count() == 1;

            var sameLevel = courses
                .Select(course => course.Level)
                .Distinct()
                .Count() == 1;

            var highlights = new List<string>
            {
                $"\"{popular.Title}\" có nhiều lượt đăng ký nhất.",
                $"\"{comprehensive.Title}\" có nhiều bài học nhất."
            };

            if (shortest is not null)
            {
                highlights.Add(
                    $"\"{shortest.Title}\" có thời lượng ước tính ngắn nhất.");
            }

            if (sameCategory)
                highlights.Add("Các khóa học thuộc cùng một danh mục.");

            if (sameLevel)
                highlights.Add("Các khóa học có cùng cấp độ.");

            return new CourseComparisonSummary
            {
                MostPopularCourseId = popular.CourseId,
                MostComprehensiveCourseId = comprehensive.CourseId,
                ShortestCourseId = shortest?.CourseId,
                SameCategory = sameCategory,
                SameLevel = sameLevel,
                FreeCourseCount = courses.Count(course =>
                    course.CourseType == CourseType.FreeCourse),
                Highlights = highlights
            };
        }

        private static CourseComparisonItem AddStrengths(
            CourseComparisonItem item,
            CourseComparisonSummary summary)
        {
            var strengths = new List<string>();

            if (summary.MostPopularCourseId == item.CourseId)
                strengths.Add("Nhiều lượt đăng ký nhất trong nhóm");

            if (summary.MostComprehensiveCourseId == item.CourseId)
                strengths.Add("Nhiều bài học nhất trong nhóm");

            if (summary.ShortestCourseId == item.CourseId)
                strengths.Add("Thời lượng ước tính ngắn nhất");

            if (item.CourseType == CourseType.FreeCourse)
                strengths.Add("Khóa học miễn phí");

            if (item.LearningOutcomes.Count > 0)
                strengths.Add($"{item.LearningOutcomes.Count} chuẩn đầu ra");

            return new CourseComparisonItem
            {
                CourseId = item.CourseId,
                Title = item.Title,
                Description = item.Description,
                PreviewImage = item.PreviewImage,
                CategoryId = item.CategoryId,
                CategoryName = item.CategoryName,
                Level = item.Level,
                CourseType = item.CourseType,
                ModuleCount = item.ModuleCount,
                LessonCount = item.LessonCount,
                DocumentCount = item.DocumentCount,
                EnrollmentCount = item.EnrollmentCount,
                EstimatedDurationMinutes = item.EstimatedDurationMinutes,
                LearningOutcomes = item.LearningOutcomes,
                Strengths = strengths
            };
        }

        private static int ToMinutes(long ticks)
        {
            if (ticks <= 0)
                return 0;

            return (int)Math.Ceiling(TimeSpan.FromTicks(ticks).TotalMinutes);
        }

        private sealed class ComparisonData
        {
            public Guid CourseId { get; init; }
            public string Title { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public string PreviewImage { get; init; } = string.Empty;
            public int CategoryId { get; init; }
            public string CategoryName { get; init; } = string.Empty;
            public CourseLevel Level { get; init; }
            public CourseType CourseType { get; init; }
            public int EnrollmentCount { get; init; }
            public List<string> LearningOutcomes { get; init; } = new();
            public List<ModuleData> Modules { get; init; } = new();
        }

        private sealed class ModuleData
        {
            public int ModuleId { get; init; }
            public List<LessonData> Lessons { get; init; } = new();
        }

        private sealed class LessonData
        {
            public int LessonId { get; init; }
            public TimeSpan? Duration { get; init; }
            public int DocumentCount { get; init; }
        }
    }
}
