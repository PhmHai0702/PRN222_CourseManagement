using CourseManagement.Model.DTOs;

namespace CourseManagement.Business.Services.IService
{
    public interface ICourseComparisonService
    {
        Task<CourseComparisonResponse> CompareAsync(
            CourseComparisonRequest request,
            CancellationToken cancellationToken = default);
    }
}
