using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AdaptiveCognitiveRehabilitationPlatform.Services;
using NeuroPath.DTOs;

namespace Adaptive_Cognitive_Rehabilitation_Platform.Controllers
{
    /* ==================================================================================
     * SQL/EF Core - Commented out for JSON-only mode
     * This controller uses JSON storage for game statistics.
     * ================================================================================== */

    /// <summary>
    /// Instructor/Trainer API controller - Uses JSON-based stats in JSON-only mode
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InstructorController : ControllerBase
    {
        private readonly IJsonGameStatsService _jsonStatsService;
        private readonly ILogger<InstructorController> _logger;

        public InstructorController(IJsonGameStatsService jsonStatsService, ILogger<InstructorController> logger)
        {
            _jsonStatsService = jsonStatsService;
            _logger = logger;
        }

        /// <summary>
        /// Get all students assigned to an instructor - Returns empty in JSON-only mode
        /// </summary>
        [HttpGet("students/{trainerId}")]
        public IActionResult GetAssignedStudents(int trainerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            _logger.LogInformation($"[INSTRUCTOR-API] JSON-only mode: Returning empty student list for trainer {trainerId}");
            return Ok(new
            {
                Students = new List<StudentSummaryDto>(),
                TotalCount = 0,
                PageNumber = page,
                PageSize = pageSize,
                Message = "JSON-only mode - InstructorAssignments not available"
            });
        }
    }
}
