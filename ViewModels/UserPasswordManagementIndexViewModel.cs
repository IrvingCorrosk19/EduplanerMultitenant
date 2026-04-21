using System;
using System.Collections.Generic;
using SchoolManager.Models;

namespace SchoolManager.ViewModels;

/// <summary>Vista índice: filtros por grado/grupo y listado de usuarios.</summary>
public class UserPasswordManagementIndexViewModel
{
    public List<GradeLevel> GradeLevels { get; set; } = new();
    public List<Group> Groups { get; set; } = new();
    public List<UserPasswordViewModel> Users { get; set; } = new();

    public Guid? SelectedGradeId { get; set; }
    public Guid? SelectedGroupId { get; set; }

    /// <summary>Rol filtro (superadmin, admin, teacher, student) o vacío = todos.</summary>
    public string? SelectedRole { get; set; }

    /// <summary>Búsqueda por nombre/apellido/email.</summary>
    public string? SearchQuery { get; set; }
}
