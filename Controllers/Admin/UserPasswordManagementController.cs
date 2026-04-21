using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Dtos;
using SchoolManager.Models;
using SchoolManager.Repositories.Interfaces;
using SchoolManager.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SchoolManager.Controllers.Admin
{
    [Authorize(Roles = "SuperAdmin,superadmin,Admin,admin")]
    [Route("Admin/UserPasswordManagement")]
    public class UserPasswordManagementController : Controller
    {
        public const int MaxEnqueuePerRequest = 2000;

        private readonly IUserPasswordManagementService _userPasswordManagementService;
        private readonly IEmailQueueService _emailQueueService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailQueueRepository _emailQueueRepository;
        private readonly ILogger<UserPasswordManagementController> _logger;

        public UserPasswordManagementController(
            IUserPasswordManagementService userPasswordManagementService,
            IEmailQueueService emailQueueService,
            ICurrentUserService currentUserService,
            IEmailQueueRepository emailQueueRepository,
            ILogger<UserPasswordManagementController> logger)
        {
            _userPasswordManagementService = userPasswordManagementService;
            _emailQueueService             = emailQueueService;
            _currentUserService            = currentUserService;
            _emailQueueRepository          = emailQueueRepository;
            _logger                        = logger;
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index(
            [FromQuery] Guid? gradeId,
            [FromQuery] Guid? groupId,
            [FromQuery] string? role,
            [FromQuery] string? q)
        {
            if (gradeId == Guid.Empty) gradeId = null;
            if (groupId == Guid.Empty) groupId = null;

            var me = await _currentUserService.GetCurrentUserAsync();
            var isSuper = string.Equals(me?.Role, "superadmin", StringComparison.OrdinalIgnoreCase);
            var vm = await _userPasswordManagementService.GetIndexViewModelAsync(
                gradeId,
                groupId,
                string.IsNullOrWhiteSpace(role) ? null : role.Trim(),
                string.IsNullOrWhiteSpace(q)    ? null : q.Trim(),
                me?.SchoolId,
                isSuper);
            return View("~/Views/Admin/UserPasswordManagement/Index.cshtml", vm);
        }

        [HttpGet]
        [Route("ListJson")]
        public async Task<IActionResult> ListJson()
        {
            var users = await _userPasswordManagementService.GetAllUsersAsync();
            return Json(users);
        }

        [HttpGet]
        [Route("FilterByRole")]
        public async Task<IActionResult> FilterByRole([FromQuery] string? role)
        {
            try
            {
                var users = await _userPasswordManagementService.GetUsersByRoleAsync(role ?? string.Empty);
                return Json(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FilterByRole failed for role={Role}", role);
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Encola envío masivo de contraseñas temporales.
        /// Devuelve una respuesta honesta: jobId, correlationId y conteos reales.
        /// </summary>
        [HttpPost]
        [Route("SendPasswords")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPasswords([FromBody] SendPasswordsRequestDto? body)
        {
            var ids = (body?.UserIds ?? new List<Guid>()).Distinct().ToList();

            if (ids.Count == 0)
                return BadRequest(new
                {
                    success        = false,
                    message        = "Seleccione al menos un usuario.",
                    totalRequested = 0,
                    acceptedCount  = 0
                });

            if (ids.Count > MaxEnqueuePerRequest)
                return BadRequest(new
                {
                    success        = false,
                    message        = $"Máximo {MaxEnqueuePerRequest} usuarios por solicitud.",
                    totalRequested = ids.Count,
                    acceptedCount  = 0
                });

            if (User?.Identity?.IsAuthenticated != true)
                return Unauthorized();

            try
            {
                var result = await _emailQueueService.EnqueueUsersAsync(ids, User);

                _logger.LogInformation(
                    "SendPasswords CorrelationId={CorrelationId} JobId={JobId} Success={Success} " +
                    "Accepted={Accepted} Rejected={Rejected}",
                    result.CorrelationId, result.JobId, result.Success,
                    result.AcceptedCount, result.RejectedCount);

                if (!result.Success)
                {
                    return result.AcceptedCount == 0 && result.TotalRequested == 0
                        ? BadRequest(BuildPayload(result))
                        : Ok(BuildPayload(result)); // parcialmente aceptado con advertencias
                }

                return Ok(BuildPayload(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendPasswords error inesperado");
                return StatusCode(500, new
                {
                    success  = false,
                    message  = "Error interno al encolar los correos.",
                    warnings = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Consulta el estado de un lote de envío por su jobId.
        /// Permite polling desde el frontend sin SignalR.
        /// </summary>
        [HttpGet]
        [Route("JobStatus/{jobId:guid}")]
        public async Task<IActionResult> JobStatus(Guid jobId)
        {
            // Antes de responder, refrescar contadores desde la cola
            await _emailQueueRepository.RefreshJobCountersAsync(jobId);

            var job = await _emailQueueRepository.GetJobAsync(jobId);
            if (job == null)
                return NotFound(new { message = "Job no encontrado.", jobId });

            return Json(new JobStatusDto
            {
                JobId         = job.Id,
                CorrelationId = job.CorrelationId,
                Status        = job.Status,
                TotalItems    = job.TotalItems,
                SentCount     = job.SentCount,
                FailedCount   = job.FailedCount,
                RejectedCount = job.RejectedCount,
                RequestedAt   = job.RequestedAt,
                StartedAt     = job.StartedAt,
                CompletedAt   = job.CompletedAt
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static object BuildPayload(EnqueueResult r) => new
        {
            success        = r.Success,
            message        = r.Message,
            jobId          = r.JobId?.ToString(),
            correlationId  = r.CorrelationId.ToString(),
            totalRequested = r.TotalRequested,
            acceptedCount  = r.AcceptedCount,
            rejectedCount  = r.RejectedCount,
            warnings       = r.Warnings
        };
    }
}
