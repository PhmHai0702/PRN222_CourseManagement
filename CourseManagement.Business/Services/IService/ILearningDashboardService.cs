using CourseManagement.Model.DTOs;

namespace CourseManagement.Business.Services.IService
{
    public interface ILearningDashboardService
    {
        Task<LearningDashboardResponse?> GetDashboardAsync(
            string email,
            LearningDashboardRequest request,
            CancellationToken cancellationToken = default);
    }
}
