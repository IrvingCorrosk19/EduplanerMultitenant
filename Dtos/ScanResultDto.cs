namespace SchoolManager.Dtos;

public class ScanResultDto
{
    public bool Allowed { get; set; }
    public string Message { get; set; } = null!;
    public string StudentName { get; set; } = null!;
    public string Grade { get; set; } = null!;
    public string Group { get; set; } = null!;
    /// <summary>ID del estudiante cuando Allowed es true. Necesario para el módulo de disciplina.</summary>
    public Guid? StudentId { get; set; }
    /// <summary>Cantidad de reportes disciplinarios del estudiante. Solo tiene sentido cuando Allowed es true.</summary>
    public int DisciplineCount { get; set; }
    /// <summary>URL de la foto del estudiante (relativa o absoluta). Para tarjeta digital.</summary>
    public string? StudentPhotoUrl { get; set; }
    /// <summary>Nombre de la institución. Para tarjeta digital.</summary>
    public string? SchoolName { get; set; }
    /// <summary>Código o documento del estudiante. Para tarjeta digital.</summary>
    public string? StudentCode { get; set; }
    /// <summary>Contacto de emergencia (solo si el escáner tiene rol inspector/teacher/admin).</summary>
    public string? EmergencyContactName { get; set; }
    /// <summary>Teléfono de emergencia (solo si el escáner tiene rol inspector/teacher/admin).</summary>
    public string? EmergencyContactPhone { get; set; }
    /// <summary>Alergias (solo si el escáner tiene rol inspector/teacher/admin).</summary>
    public string? Allergies { get; set; }
    /// <summary>Número del carnet activo del estudiante (cuando el escaneo es válido).</summary>
    public string? CardNumber { get; set; }
    /// <summary>Estado del carnet (active/revoked).</summary>
    public string? CardStatus { get; set; }
    /// <summary>Fecha de emisión del carnet.</summary>
    public DateTime? CardIssuedDate { get; set; }
    /// <summary>Indica si el estudiante está autorizado a ingresar al colegio (estado activo, carnet activo, asignación activa). Solo informativo; no modifica Allowed.</summary>
    public bool AllowedToEnterSchool { get; set; }

    /// <summary>Jornada desde la asignación activa (tabla shifts) o, si falta, el campo users.shift.</summary>
    public string? ShiftName { get; set; }

    /// <summary>Nombre del consejero según counselor_assignments (grupo → grado → general).</summary>
    public string? CounselorName { get; set; }

    /// <summary>True si users.status es active (cuenta del estudiante en el sistema).</summary>
    public bool IsStudentAccountActive { get; set; }
}
