using CourseManagement.Business.Services.IService;
using CourseManagement.Model.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CourseManagementAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/learning-dashboard")]
    public sealed class LearningDashboardController : ControllerBase
    {
        private readonly ILearningDashboardService _dashboardService;

        public LearningDashboardController(
            ILearningDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Tổng hợp tiến độ học tập của tài khoản đang đăng nhập.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(
            typeof(LearningDashboardResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LearningDashboardResponse>> Get(
            [FromQuery] LearningDashboardRequest request,
            CancellationToken cancellationToken)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                return Unauthorized(new
                {
                    message = "Token không chứa email hợp lệ."
                });
            }

            var dashboard = await _dashboardService.GetDashboardAsync(
                email,
                request,
                cancellationToken);

            if (dashboard is null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy tài khoản người dùng."
                });
            }

            return Ok(dashboard);
        }
    }
}
