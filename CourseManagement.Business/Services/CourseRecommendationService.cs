using CourseManagement.Business.Services.IService;
using CourseManagement.DataAccess.Data;
using CourseManagement.Model.Constant;
using CourseManagement.Model.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Business.Services
{
    /// <summary>
    /// Bộ gợi ý dựa trên luật, không cần mô hình machine learning.
    /// Kết quả có thể giải thích được thông qua danh sách Reasons.
    /// </summary>
    public sealed class CourseRecommendationService : ICourseRecommendationService
    {
        private readonly CourseManagementDb _db;

        public CourseRecommendationService(CourseManagementDb db)
        {
            _db = db;
        }

        public async Task<CourseRecommendationResponse> GetRecommendationsAsync(
            CourseRecommendationRequest request,
            string? userId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var take = Math.Clamp(request.Take, 1, 20);
            var normalizedKeyword = Normalize(request.Keyword);

            var query = _db.Courses
                .AsNoTracking()
                .Where(course => course.Status == CourseStatus.Publish);

            if (request.FreeOnly)
            {
                query = query.Where(course =>
                    course.CourseType == CourseType.FreeCourse);
            }

            if (!string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                query = query.Where(course =>
                    course.Title.Contains(normalizedKeyword) ||
                    course.Description.Contains(normalizedKeyword));
            }

            var personalizedForUser = !string.IsNullOrWhiteSpace(userId);
            if (personalizedForUser && request.ExcludeEnrolled)
            {
                var enrolledCourseIds = _db.enrollments
                    .AsNoTracking()
                    .Where(enrollment => enrollment.UserId == userId)
                    .Select(enrollment => enrollment.CourseId);

                query = query.Where(course =>
                    !enrolledCourseIds.Contains(course.Id));
            }

            // Chỉ lấy dữ liệu cần thiết trước khi chấm điểm trong bộ nhớ.
            var candidates = await query
                .Select(course => new Candidate
                {
                    CourseId = course.Id,
                    Title = course.Title,
                    Description = course.Description,
                    PreviewImage = course.PreviewImage,
                    PreviewVideoUrl = course.PreviewVideoUrl,
                    CategoryId = course.CategoryId,
                    CategoryName = course.Category.Name,
                    Level = course.Level,
                    CourseType = course.CourseType,
                    EnrollmentCount = course.Enrollments.Count()
                })
                .ToListAsync(cancellationToken);

            var rankedItems = candidates
                .Select(candidate => Rank(candidate, request, normalizedKeyword))
                .OrderByDescending(item => item.Score)
                .ThenByDescending(item => item.EnrollmentCount)
                .ThenBy(item => item.Title)
                .Take(take)
                .ToList();

            return new CourseRecommendationResponse
            {
                TotalCandidates = candidates.Count,
                ReturnedItems = rankedItems.Count,
                PersonalizedForUser = personalizedForUser,
                Summary = BuildSummary(request, normalizedKeyword, rankedItems.Count),
                Items = rankedItems
            };
        }

        private static CourseRecommendationItem Rank(
            Candidate candidate,
            CourseRecommendationRequest request,
            string normalizedKeyword)
        {
            var score = 20;
            var reasons = new List<string>();

            if (request.PreferredCategoryId.HasValue &&
                candidate.CategoryId == request.PreferredCategoryId.Value)
            {
                score += 30;
                reasons.Add("Thuộc danh mục bạn ưu tiên");
            }

            if (request.PreferredLevel.HasValue)
            {
                var levelDistance = Math.Abs(
                    (int)candidate.Level - (int)request.PreferredLevel.Value);

                if (levelDistance == 0)
                {
                    score += 25;
                    reasons.Add("Khớp chính xác cấp độ mong muốn");
                }
                else if (levelDistance == 1)
                {
                    score += 10;
                    reasons.Add("Cấp độ gần với lựa chọn của bạn");
                }
            }

            if (!string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                if (candidate.Title.Contains(
                    normalizedKeyword,
                    StringComparison.OrdinalIgnoreCase))
                {
                    score += 25;
                    reasons.Add("Từ khóa xuất hiện trong tên khóa học");
                }
                else if (candidate.Description.Contains(
                    normalizedKeyword,
                    StringComparison.OrdinalIgnoreCase))
                {
                    score += 12;
                    reasons.Add("Nội dung phù hợp với từ khóa");
                }
            }

            if (candidate.CourseType == CourseType.FreeCourse)
            {
                score += request.FreeOnly ? 15 : 5;
                reasons.Add("Có thể học miễn phí");
            }

            var popularityScore = Math.Min(candidate.EnrollmentCount, 20);
            if (popularityScore > 0)
            {
                score += popularityScore;
                reasons.Add($"Đã có {candidate.EnrollmentCount} lượt đăng ký");
            }

            if (reasons.Count == 0)
            {
                reasons.Add("Khóa học đang được xuất bản");
            }

            return new CourseRecommendationItem
            {
                CourseId = candidate.CourseId,
                Title = candidate.Title,
                Description = candidate.Description,
                PreviewImage = candidate.PreviewImage,
                PreviewVideoUrl = candidate.PreviewVideoUrl,
                CategoryId = candidate.CategoryId,
                CategoryName = candidate.CategoryName,
                Level = candidate.Level,
                CourseType = candidate.CourseType,
                EnrollmentCount = candidate.EnrollmentCount,
                Score = score,
                Reasons = reasons
            };
        }

        private static string BuildSummary(
            CourseRecommendationRequest request,
            string normalizedKeyword,
            int resultCount)
        {
            var criteria = new List<string>();

            if (request.PreferredCategoryId.HasValue)
                criteria.Add($"danh mục #{request.PreferredCategoryId}");

            if (request.PreferredLevel.HasValue)
                criteria.Add($"cấp độ {request.PreferredLevel}");

            if (!string.IsNullOrWhiteSpace(normalizedKeyword))
                criteria.Add($"từ khóa \"{normalizedKeyword}\"");

            if (request.FreeOnly)
                criteria.Add("chỉ khóa học miễn phí");

            var criteriaText = criteria.Count == 0
                ? "độ phổ biến và trạng thái xuất bản"
                : string.Join(", ", criteria);

            return $"Tìm thấy {resultCount} gợi ý dựa trên {criteriaText}.";
        }

        private static string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }

        private sealed class Candidate
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
        }
    }
}
