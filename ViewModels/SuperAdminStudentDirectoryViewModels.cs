using Microsoft.AspNetCore.Mvc.Rendering;

namespace SchoolManager.ViewModels;

public class SuperAdminStudentDirectoryFilterVm
{
    public string? Search { get; set; }
    public Guid? SchoolId { get; set; }
    public Guid? GradeId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? ShiftId { get; set; }

    /// <summary>Todos | active | inactive</summary>
    public string? UserStatus { get; set; }

    public bool OnlyWithoutAssignment { get; set; }

    /// <summary>Página 1-based.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Tamaño de página (máx. 100 en servidor).</summary>
    public int PageSize { get; set; } = 25;
}

public class SuperAdminStudentDirectoryRowVm
{
    public Guid UserId { get; set; }
    public Guid? AssignmentId { get; set; }
    public string? PhotoUrl { get; set; }
    public string FullName { get; set; } = "";
    public string? DocumentId { get; set; }
    public string Email { get; set; } = "";
    public string? SchoolName { get; set; }
    public Guid? SchoolId { get; set; }
    public string? GradeLevelName { get; set; }
    public string? GroupName { get; set; }
    public string? ShiftName { get; set; }
    public string? UserShift { get; set; }
    public string Status { get; set; } = "";
    public bool HasActiveAssignment { get; set; }
}

public class SuperAdminStudentDirectoryPageVm
{
    public SuperAdminStudentDirectoryFilterVm Filter { get; set; } = new();
    public List<SuperAdminStudentDirectoryRowVm> Rows { get; set; } = new();
    public List<SelectListItem> SchoolOptions { get; set; } = new();
    public List<SelectListItem> GradeOptions { get; set; } = new();
    public List<SelectListItem> GroupOptions { get; set; } = new();
    public List<SelectListItem> ShiftOptions { get; set; } = new();

    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    /// <summary>Rango mostrado (1-based índices inclusivos) para el texto “Mostrando X–Y de Z”.</summary>
    public int DisplayFrom => TotalCount == 0 ? 0 : (Filter.Page - 1) * Filter.PageSize + 1;
    public int DisplayTo => TotalCount == 0 ? 0 : Math.Min(Filter.Page * Filter.PageSize, TotalCount);

    /// <summary>Parámetros GET para enlaces de paginación (misma búsqueda y filtros).</summary>
    public Dictionary<string, string> GetDirectoryQueryForPage(int page)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Page"] = Math.Max(1, page).ToString(),
            ["PageSize"] = Filter.PageSize.ToString()
        };
        if (!string.IsNullOrWhiteSpace(Filter.Search))
            d["Search"] = Filter.Search;
        if (Filter.SchoolId.HasValue)
            d["SchoolId"] = Filter.SchoolId.Value.ToString("D");
        if (Filter.GradeId.HasValue)
            d["GradeId"] = Filter.GradeId.Value.ToString("D");
        if (Filter.GroupId.HasValue)
            d["GroupId"] = Filter.GroupId.Value.ToString("D");
        if (Filter.ShiftId.HasValue)
            d["ShiftId"] = Filter.ShiftId.Value.ToString("D");
        if (!string.IsNullOrEmpty(Filter.UserStatus))
            d["UserStatus"] = Filter.UserStatus!;
        if (Filter.OnlyWithoutAssignment)
            d["OnlyWithoutAssignment"] = "true";
        return d;
    }
}
