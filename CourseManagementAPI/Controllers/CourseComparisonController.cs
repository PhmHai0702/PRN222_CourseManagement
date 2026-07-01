using CourseManagement.Business.Services.IService;
using CourseManagement.Model.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseManagementAPI.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/course-comparison")]
    public sealed class CourseComparisonController : ControllerBase
    {
        private readonly ICourseComparisonService _comparisonService;

        public CourseComparisonController(
            ICourseComparisonService comparisonService)
        {
            _comparisonService = comparisonService;
        }

        /// <summary>
        /// So sánh từ 2 đến 4 khóa học đang được xuất bản.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(
            typeof(CourseComparisonResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CourseComparisonResponse>> Compare(
            [FromQuery] CourseComparisonRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _comparisonService.CompareAsync(
                    request,
                    cancellationToken);

                if (result.ComparedCourses < 2)
                {
                    return BadRequest(new
                    {
                        message = "Cần ít nhất hai khóa học đã xuất bản để so sánh.",
                        missingCourseIds = result.MissingCourseIds
                    });
                }

                return Ok(result);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new
                {
                    message = exception.Message
                });
            }
        }
    }
}
