using CourseManagement.Model.Constant;
using System.ComponentModel.DataAnnotations;

namespace CourseManagement.Model.DTOs
{
    /// <summary>
    /// Tiêu chí dùng để xếp hạng danh sách khóa học.
    /// Các tiêu chí sở thích chỉ cộng điểm, không loại bỏ khóa học.
    /// </summary>
    public sealed class CourseRecommendationRequest
    {
        public int? PreferredCategoryId { get; set; }

        public CourseLevel? PreferredLevel { get; set; }

        public bool FreeOnly { get; set; }

        [StringLength(100)]
        public string? Keyword { get; set; }

        public bool ExcludeEnrolled { get; set; } = true;

        [Range(1, 20)]
        public int Take { get; set; } = 8;
    }

    /// <summary>
    /// Một khóa học sau khi được chấm điểm gợi ý.
    /// </summary>
    public sealed class CourseRecommendationItem
    {
        public Guid CourseId { get; init; }

        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string PreviewImage { get; init; } = string.Empty;

        public string PreviewVideoUrl { get; init; } = string.Empty;

        public int CategoryId { get; init; }

        public string CategoryName { get; init; } = string.Empty;

        public CourseLevel Level { get; init; }

        public CourseType CourseType { get; init; }

        public int EnrollmentCount { get; init; }

        public int Score { get; init; }

        public IReadOnlyList<string> Reasons { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Kết quả trả về cho client cùng mô tả ngắn về cách xếp hạng.
    /// </summary>
    public sealed class CourseRecommendationResponse
    {
        public int TotalCandidates { get; init; }

        public int ReturnedItems { get; init; }

        public bool PersonalizedForUser { get; init; }

        public string Summary { get; init; } = string.Empty;

        public IReadOnlyList<CourseRecommendationItem> Items { get; init; }
            = Array.Empty<CourseRecommendationItem>();
    }
}
