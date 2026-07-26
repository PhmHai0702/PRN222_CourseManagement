using CourseManagement.Model.Constant;
using System.ComponentModel.DataAnnotations;

namespace CourseManagement.Model.DTOs
{
    public sealed class CourseComparisonRequest
    {
        /// <summary>
        /// Danh sách từ 2 đến 4 mã khóa học cần so sánh.
        /// Ví dụ: ?CourseIds=id1&amp;CourseIds=id2
        /// </summary>
        [Required]
        public List<Guid> CourseIds { get; set; } = new();
    }

    public sealed class CourseComparisonResponse
    {
        public int RequestedCourses { get; init; }

        public int ComparedCourses { get; init; }

        public IReadOnlyList<Guid> MissingCourseIds { get; init; }
            = Array.Empty<Guid>();

        public CourseComparisonSummary Summary { get; init; } = new();

        public IReadOnlyList<CourseComparisonItem> Courses { get; init; }
            = Array.Empty<CourseComparisonItem>();
    }

    public sealed class CourseComparisonItem
    {
        public Guid CourseId { get; init; }

        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string PreviewImage { get; init; } = string.Empty;

        public int CategoryId { get; init; }

        public string CategoryName { get; init; } = string.Empty;

        public CourseLevel Level { get; init; }

        public CourseType CourseType { get; init; }

        public int ModuleCount { get; init; }

        public int LessonCount { get; init; }

        public int DocumentCount { get; init; }

        public int EnrollmentCount { get; init; }

        public int EstimatedDurationMinutes { get; init; }

        public IReadOnlyList<string> LearningOutcomes { get; init; }
            = Array.Empty<string>();

        public IReadOnlyList<string> Strengths { get; init; }
            = Array.Empty<string>();
    }

    public sealed class CourseComparisonSummary
    {
        public Guid? MostPopularCourseId { get; init; }

        public Guid? MostComprehensiveCourseId { get; init; }

        public Guid? ShortestCourseId { get; init; }

        public bool SameCategory { get; init; }

        public bool SameLevel { get; init; }

        public int FreeCourseCount { get; init; }

        public IReadOnlyList<string> Highlights { get; init; }
            = Array.Empty<string>();
    }
}
