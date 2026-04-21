using System;
using System.Collections.Generic;

namespace SchoolManager.ViewModels
{
    /// <summary>
    /// ViewModel para estadísticas del sistema
    /// </summary>
    public class SystemStatsViewModel
    {
        public int TotalEscuelas { get; set; }
        public int TotalUsuarios { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalProfesores { get; set; }
        public int TotalEstudiantes { get; set; }
        public int TotalActividades { get; set; }
        public int TotalCalificaciones { get; set; }
        public int TotalMensajes { get; set; }
        public int UsuariosActivos { get; set; }
        public int UsuariosInactivos { get; set; }
        public DateTime FechaUltimaActividad { get; set; }
        public List<EscuelaStatsDto> EscuelasStats { get; set; } = new();
    }

    public class EscuelaStatsDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = null!;
        public int TotalUsuarios { get; set; }
        public int TotalEstudiantes { get; set; }
        public int TotalProfesores { get; set; }
        public int TotalActividades { get; set; }
        public string? LogoUrl { get; set; }
    }

    /// <summary>
    /// ViewModel para logs de auditoría
    /// </summary>
    public class AuditLogViewModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = null!;
        public string UserRole { get; set; } = null!;
        public string Action { get; set; } = null!;
        public string Resource { get; set; } = null!;
        public string? Details { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? SchoolName { get; set; }
    }
}

