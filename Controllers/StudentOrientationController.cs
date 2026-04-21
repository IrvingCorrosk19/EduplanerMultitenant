using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SchoolManager.Models;
using SchoolManager.Dtos;
using SchoolManager.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SchoolManager.Services.Interfaces;
using SchoolManager.Interfaces;

namespace SchoolManager.Controllers
{
    [Authorize(Roles = "student,estudiante")]
    public class StudentOrientationController : Controller
    {
        private readonly IOrientationReportService _orientationReportService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<StudentOrientationController> _logger;

        public StudentOrientationController(
            IOrientationReportService orientationReportService,
            ICurrentUserService currentUserService,
            ILogger<StudentOrientationController> logger)
        {
            _orientationReportService = orientationReportService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var studentId = GetStudentId();
                var currentUser = await _currentUserService.GetCurrentUserAsync();
                
                if (currentUser == null)
                {
                    return NotFound("Usuario no encontrado.");
                }

                // Obtener información del estudiante
                var studentInfo = new
                {
                    Id = currentUser.Id,
                    Name = $"{currentUser.Name} {currentUser.LastName}",
                    Email = currentUser.Email,
                    DocumentId = currentUser.DocumentId,
                    SchoolId = currentUser.SchoolId
                };

                ViewData["StudentInfo"] = studentInfo;
                ViewData["Title"] = "Mis Reportes de Orientación";

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el índice de orientación del estudiante");
                return View("Error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMyReports()
        {
            try
            {
                var studentId = GetStudentId();
                var reports = await _orientationReportService.GetByStudentDtoAsync(studentId);
                
                var result = reports.Select(r => new {
                    id = r.Id,
                    date = r.Date.ToString("dd/MM/yyyy"),
                    time = r.Date.ToString("HH:mm"),
                    type = r.Type,
                    category = r.Category,
                    status = r.Status,
                    description = r.Description,
                    documents = r.Documents,
                    teacher = r.Teacher,
                    subjectName = r.SubjectName,
                    groupName = r.GroupName,
                    gradeName = r.GradeName
                });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener reportes de orientación del estudiante");
                return Json(new { success = false, error = "Error al obtener los reportes" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(Guid reportId, string fileName)
        {
            try
            {
                var studentId = GetStudentId();
                var report = await _orientationReportService.GetByIdAsync(reportId);
                
                if (report == null || report.StudentId != studentId)
                {
                    return NotFound("Reporte no encontrado o sin permisos para acceder.");
                }

                // Parsear documentos JSON
                if (string.IsNullOrEmpty(report.Documents))
                {
                    return NotFound("No hay documentos asociados a este reporte.");
                }

                var documents = System.Text.Json.JsonSerializer.Deserialize<List<object>>(report.Documents);
                var document = documents?.FirstOrDefault(d => 
                {
                    var docObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(d.ToString());
                    return docObj?.ContainsKey("fileName") == true && docObj["fileName"].ToString() == fileName;
                });

                if (document == null)
                {
                    return NotFound("Documento no encontrado.");
                }

                var docObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(document.ToString());
                var savedName = docObj["savedName"].ToString();
                var originalName = docObj["fileName"].ToString();

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "orientation", savedName);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("Archivo no encontrado en el servidor.");
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, "application/octet-stream", originalName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar documento de orientación");
                return StatusCode(500, "Error al descargar el documento");
            }
        }

        private Guid GetStudentId()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado.");
            }

            if (!Guid.TryParse(userId, out var studentId))
            {
                throw new UnauthorizedAccessException("ID de usuario inválido.");
            }

            return studentId;
        }
    }
}
