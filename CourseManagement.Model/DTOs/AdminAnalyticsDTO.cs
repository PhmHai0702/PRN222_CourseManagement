using System.ComponentModel.DataAnnotations;

namespace CourseManagement.Model.DTOs
{
    public sealed class AdminAnalyticsRequest : IValidatableObject
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        [Range(1, 20)]
        public int Top { get; set; } = 5;

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (FromDate.HasValue &&
                ToDate.HasValue &&
                FromDate.Value.Date > ToDate.Value.Date)
            {
                yield return new ValidationResult(
                    "FromDate phải nhỏ hơn hoặc bằng ToDate.",
                    new[] { nameof(FromDate), nameof(ToDate) });
            }

            if (FromDate.HasValue &&
                ToDate.HasValue &&
                (ToDate.Value.Date - FromDate.Value.Date).TotalDays > 366)
            {
                yield return new ValidationResult(
                    "Khoảng thống kê không được vượt quá 366 ngày.",
                    new[] { nameof(FromDate), nameof(ToDate) });
            }
        }
    }

    public sealed class AdminAnalyticsResponse
    {
        public DateTime FromDate { get; init; }

        public DateTime ToDate { get; init; }

        public DateTime GeneratedAtUtc { get; init; }

        public AdminOverviewMetrics Overview { get; init; } = new();

        public AdminRevenueMetrics Revenue { get; init; } = new();

        public IReadOnlyList<AdminDailyMetric> DailyTrend { get; init; }
            = Array.Empty<AdminDailyMetric>();

        public IReadOnlyList<AdminTopCourseMetric> TopCourses { get; init; }
            = Array.Empty<AdminTopCourseMetric>();

        public IReadOnlyList<AdminCategoryMetric> Categories { get; init; }
            = Array.Empty<AdminCategoryMetric>();
    }

    public sealed class AdminOverviewMetrics
    {
        public int TotalUsers { get; init; }

        public int ActiveVipUsers { get; init; }

        public int PublishedCourses { get; init; }

        public int TotalEnrollments { get; init; }

        public int NewEnrollments { get; init; }

        public int OrdersInPeriod { get; init; }
    }

    public sealed class AdminRevenueMetrics
    {
        public decimal CompletedRevenue { get; init; }

        public decimal AverageCompletedOrderValue { get; init; }

        public int CompletedOrders { get; init; }

        public int PendingOrders { get; init; }

        public int CancelledOrders { get; init; }

        public decimal CompletionRatePercent { get; init; }
    }

    public sealed class AdminDailyMetric
    {
        public DateTime Date { get; init; }

        public int Enrollments { get; init; }

        public int CompletedOrders { get; init; }

        public decimal Revenue { get; init; }
    }

    public sealed class AdminTopCourseMetric
    {
        public Guid CourseId { get; init; }

        public string Title { get; init; } = string.Empty;

        public string CategoryName { get; init; } = string.Empty;

        public int Enrollments { get; init; }

        public decimal SharePercent { get; init; }
    }

    public sealed class AdminCategoryMetric
    {
        public int CategoryId { get; init; }

        public string CategoryName { get; init; } = string.Empty;

        public int PublishedCourses { get; init; }

        public int Enrollments { get; init; }

        public decimal EnrollmentSharePercent { get; init; }
    }
}
