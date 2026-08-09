using System.Collections.Concurrent;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations
{
    /// <summary>
    /// Menú estático por rol (sin datos de tenant). Cache en memoria seguro para MultiTenant.
    /// </summary>
    public class MenuService : IMenuService
    {
        private static readonly ConcurrentDictionary<string, List<MenuItem>> CacheByRole = new(StringComparer.OrdinalIgnoreCase);

        public Task<List<MenuItem>> GetMenuItemsForUserAsync(string role)
        {
            var key = role?.ToLowerInvariant() ?? string.Empty;
            var items = CacheByRole.GetOrAdd(key, BuildForRole);
            // Devolver copia superficial de la lista para evitar mutaciones entre requests
            return Task.FromResult(items.ToList());
        }

        private static List<MenuItem> BuildForRole(string role)
        {
            var allMenuItems = new List<MenuItem>
            {
                new MenuItem
                {
                    Title = "Dashboard",
                    Icon = "fas fa-tachometer-alt",
                    Url = "/Home/Index",
                    RequiredRoles = new[] { "admin", "teacher", "student", "director", "superadmin", "estudiante" }
                },
                new MenuItem
                {
                    Title = "Cambiar Contraseña",
                    Icon = "fas fa-key",
                    Url = "/ChangePassword/Index",
                    RequiredRoles = new[] { "admin", "teacher", "student", "director", "superadmin", "estudiante" }
                },
                new MenuItem
                {
                    Title = "Estudiantes",
                    Icon = "fas fa-user-graduate",
                    Url = "/Student/Index",
                    RequiredRoles = new[] { "student", "estudiante" }
                },
                new MenuItem
                {
                    Title = "Portal Docente",
                    Icon = "fas fa-chalkboard-teacher",
                    Url = "/TeacherGradebook/Index",
                    RequiredRoles = new[] { "teacher" }
                },
                new MenuItem
                {
                    Title = "Plan de Trabajo Trimestral",
                    Icon = "fas fa-clipboard-list",
                    Url = "/TeacherWorkPlan/Index",
                    RequiredRoles = new[] { "teacher", "admin" }
                },
                new MenuItem
                {
                    Title = "Portal Director",
                    Icon = "fas fa-user-tie",
                    Url = "/Director/Index",
                    RequiredRoles = new[] { "director" }
                },
                new MenuItem
                {
                    Title = "Reportes",
                    Icon = "fas fa-file-alt",
                    Url = "/Reportes/Index",
                    RequiredRoles = new[] { "admin", "director", "teacher" }
                },
                new MenuItem
                {
                    Title = "Catálogo de Asignaciones",
                    Icon = "fas fa-clipboard",
                    Url = "/SubjectAssignment/Index",
                    RequiredRoles = new[] { "admin" }
                },
                new MenuItem
                {
                    Title = "Administración",
                    Icon = "fas fa-cogs",
                    Url = "#",
                    RequiredRoles = new[] { "admin" },
                    SubItems = new List<MenuItem>
                    {
                        new MenuItem
                        {
                            Title = "Administrar Usuarios",
                            Icon = "fas fa-users",
                            Url = "/User/Index",
                            RequiredRoles = new[] { "admin" }
                        },
                        new MenuItem
                        {
                            Title = "Catálogo Académico",
                            Icon = "fas fa-layer-group",
                            Url = "/AcademicCatalog/Index",
                            RequiredRoles = new[] { "admin" }
                        },
                        new MenuItem
                        {
                            Title = "Asignar Docentes",
                            Icon = "fas fa-tasks",
                            Url = "/TeacherAssignment/Index",
                            RequiredRoles = new[] { "admin" }
                        },
                        new MenuItem
                        {
                            Title = "Asignar Estudiantes",
                            Icon = "fas fa-tasks",
                            Url = "/StudentAssignment/Index",
                            RequiredRoles = new[] { "admin" }
                        },
                        new MenuItem
                        {
                            Title = "Carga Asignaciones Docentes",
                            Icon = "fas fa-file-upload",
                            Url = "/AcademicAssignment/Upload",
                            RequiredRoles = new[] { "admin" }
                        },
                        new MenuItem
                        {
                            Title = "Carga Asignaciones Estudiantes",
                            Icon = "fas fa-file-upload",
                            Url = "/StudentAssignment/Upload",
                            RequiredRoles = new[] { "admin" }
                        },
                        new MenuItem
                        {
                            Title = "Carnet Estudiantil",
                            Icon = "fas fa-id-card",
                            Url = "/StudentIdCard/ui",
                            RequiredRoles = new[] { "admin" }
                        },
                        new MenuItem
                        {
                            Title = "Planes de Trabajo (Docentes)",
                            Icon = "fas fa-clipboard-list",
                            Url = "/TeacherWorkPlan/Index",
                            RequiredRoles = new[] { "admin" }
                        }
                    }
                },
                new MenuItem
                {
                    Title = "Carnet Estudiantil",
                    Icon = "fas fa-id-card",
                    Url = "/StudentIdCard/ui",
                    RequiredRoles = new[] { "superadmin" }
                },
                new MenuItem
                {
                    Title = "Club de Padres",
                    Icon = "fas fa-hand-holding-usd",
                    Url = "/ClubParents/Students",
                    RequiredRoles = new[] { "clubparentsadmin" }
                }
            };

            return allMenuItems
                .Where(m => m.RequiredRoles.Contains(role))
                .ToList();
        }
    }
}
