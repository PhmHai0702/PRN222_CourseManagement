using CourseManagement.Model.DTOs;

namespace CourseManagement.Business.Services.IService
{
    public interface ICourseRecommendationService
    {
        /// <summary>
        /// Lấy các khóa học đã xuất bản và xếp hạng theo tiêu chí người dùng.
        /// </summary>
        /// <param name="request">Bộ tiêu chí gợi ý.</param>
        /// <param name="userId">
        /// Id người dùng hiện tại; có thể null với khách chưa đăng nhập.
        /// </param>
        /// <param name="cancellationToken">Token hủy request.</param>
        Task<CourseRecommendationResponse> GetRecommendationsAsync(
            CourseRecommendationRequest request,
            string? userId,
            CancellationToken cancellationToken = default);
    }
}
