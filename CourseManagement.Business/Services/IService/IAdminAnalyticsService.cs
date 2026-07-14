using CourseManagement.Model.DTOs;

namespace CourseManagement.Business.Services.IService
{
    public interface IAdminAnalyticsService
    {
        Task<AdminAnalyticsResponse> GetDashboardAsync(
            AdminAnalyticsRequest request,
            CancellationToken cancellationToken = default);
    }
}
