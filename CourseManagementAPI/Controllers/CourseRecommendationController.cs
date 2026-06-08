using CourseManagement.Business.Services.IService;
using CourseManagement.Model.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CourseManagementAPI.Controllers
{
    [ApiController]
    [Route("api/course-recommendations")]
    public sealed class CourseRecommendationController : ControllerBase
    {
        private readonly ICourseRecommendationService _recommendationService;

        public CourseRecommendationController(
            ICourseRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        /// <summary>
        /// Gợi ý các khóa học phù hợp.
        /// Khách vẫn sử dụng được; người đăng nhập có thể loại khóa học đã đăng ký.
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(CourseRecommendationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CourseRecommendationResponse>> Get(
            [FromQuery] CourseRecommendationRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var result = await _recommendationService.GetRecommendationsAsync(
                request,
                userId,
                cancellationToken);

            return Ok(result);
        }

        private string? GetCurrentUserId()
        {
            if (User.Identity?.IsAuthenticated != true)
                return null;

            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
        }
    }
}
