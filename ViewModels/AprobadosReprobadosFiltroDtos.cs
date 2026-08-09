namespace SchoolManager.ViewModels;

/// <summary>
/// Opción del combo Materia y Grupo (mismo formato que TeacherGradebook/Index).
/// </summary>
public class AprobadosReprobadosComboFiltroDto
{
    public string Value { get; set; } = "";
    public string Text { get; set; } = "";
}

/// <summary>
/// Nivel/grado académico disponible en el filtro (desde asignaciones del docente o escuela).
/// </summary>
public class AprobadosReprobadosNivelFiltroDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public int Orden { get; set; }
}

/// <summary>
/// Grupo disponible en el filtro (misma lógica que TeacherGradebook: grade_level + nombre de grupo).
/// </summary>
public class AprobadosReprobadosGrupoFiltroDto
{
    public Guid? SubjectId { get; set; }
    public Guid GroupId { get; set; }
    public Guid GradeLevelId { get; set; }
    public string Nombre { get; set; } = "";
    public string? GradoGrupo { get; set; }
}

internal sealed class AprobadosReprobadosAsignacionRow
{
    public Guid SubjectId { get; init; }
    public string SubjectName { get; init; } = "";
    public Guid GroupId { get; init; }
    public string GroupName { get; init; } = "";
    public Guid GradeLevelId { get; init; }
    public string GradeLevelName { get; init; } = "";
    public string? GroupGrade { get; init; }
}
