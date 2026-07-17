using System.ComponentModel.DataAnnotations;

namespace CourseManagement.Model.DTOs
{
    public sealed class LearningDashboardRequest
    {
        /// <summary>
        /// Có hiển thị các khóa học đã hoàn thành hay không.
        /// </summary>
        public bool IncludeCompleted { get; set; } = true;

        /// <summary>
        /// Số khóa học tối đa trả về.
        /// </summary>
        [Range(1, 20)]
        public int Take { get; set; } = 10;
    }

    public sealed class LearningDashboardResponse
    {
        public string UserId { get; init; } = string.Empty;

        public string DisplayName { get; init; } = string.Empty;

        public int EnrolledCourses { get; init; }

        public int CompletedCourses { get; init; }

        public int InProgressCourses { get; init; }

        public int CompletedLessons { get; init; }

        public int TotalLessons { get; init; }

        public int OverallProgressPercent { get; init; }

        public int LearnedMinutes { get; init; }

        public int RemainingMinutes { get; init; }

        public IReadOnlyList<LearningCourseProgressItem> Courses { get; init; }
            = Array.Empty<LearningCourseProgressItem>();
    }

    public sealed class LearningCourseProgressItem
    {
        public Guid CourseId { get; init; }

        public string Title { get; init; } = string.Empty;

        public string PreviewImage { get; init; } = string.Empty;

        public string CategoryName { get; init; } = string.Empty;

        public DateTime EnrollmentDate { get; init; }

        public int CompletedLessons { get; init; }

        public int TotalLessons { get; init; }

        public int ProgressPercent { get; init; }

        public int LearnedMinutes { get; init; }

        public int RemainingMinutes { get; init; }

        public bool IsCompleted { get; init; }

        public LearningLessonItem? LastViewedLesson { get; init; }

        public LearningLessonItem? NextLesson { get; init; }
    }

    public sealed class LearningLessonItem
    {
        public int LessonId { get; init; }

        public string Title { get; init; } = string.Empty;

        public int ModuleId { get; init; }

        public string ModuleTitle { get; init; } = string.Empty;

        public int Order { get; init; }

        public int DurationMinutes { get; init; }
    }
}
