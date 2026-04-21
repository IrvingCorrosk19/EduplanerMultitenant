using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SchoolManager.Services.Interfaces;
using SchoolManager.ViewModels;
using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;
using System.Text;

namespace SchoolManager.Services.Implementations
{
    public class DirectorService : IDirectorService
    {
        private readonly IUserService _userService;
        private readonly IStudentReportService _studentReportService;
        private readonly ISubjectService _subjectService;
        private readonly ITrimesterService _trimesterService;
        private readonly SchoolDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DirectorService(
            IUserService userService, 
            IStudentReportService studentReportService, 
            ISubjectService subjectService, 
            ITrimesterService trimesterService, 
            SchoolDbContext context,
            ICurrentUserService currentUserService)
        {
            _userService = userService;
            _studentReportService = studentReportService;
            _subjectService = subjectService;
            _trimesterService = trimesterService;
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<DirectorViewModel> GetDashboardViewModelAsync(string trimestre = null)
        {
            var model = new DirectorViewModel();
            var trimestres = await _trimesterService.GetAllAsync();
            model.TrimestresDisponibles = trimestres;
            model.TrimestreSeleccionado = string.IsNullOrEmpty(trimestre) ? "" : trimestre;

            // Obtener datos de estudiantes, aprobados, reprobados, etc.
            var estudiantes = await _userService.GetAllAsync();
            var soloEstudiantes = estudiantes.Where(e => e.Role.ToLower() == "estudiante" || e.Role.ToLower() == "student" || e.Role.ToLower() == "alumno").ToList();
            model.TotalEstudiantes = soloEstudiantes.Count;
            int totalAprobados = 0;
            int totalReprobados = 0;
            int totalSinEvaluar = 0;

            var reportesPorEstudiante = new Dictionary<Guid, SchoolManager.Dtos.StudentReportDto>();
            foreach (var estudiante in soloEstudiantes)
            {
                try
                {
                    var reporte = await _studentReportService.GetReportByStudentIdAsync(estudiante.Id);
                    // Si no se filtra por trimestre, incluir todos los reportes
                    if (reporte != null && (string.IsNullOrEmpty(model.TrimestreSeleccionado) || reporte.Trimester == model.TrimestreSeleccionado))
                        reportesPorEstudiante[estudiante.Id] = reporte;
                }
                catch { }
            }

            foreach (var estudiante in soloEstudiantes)
            {
                if (reportesPorEstudiante.TryGetValue(estudiante.Id, out var reporte) && reporte.Grades != null && reporte.Grades.Count > 0)
                {
                    var promedio = reporte.Grades.Average(g => (double)g.Value);
                    if (promedio >= 3.0)
                        totalAprobados++;
                    else if (promedio >= 1.0 && promedio < 3.0)
                        totalReprobados++;
                }
                else
                {
                    // CORRECCIÓN: Estudiantes sin notas van a "Sin Evaluar", NO a reprobados
                    totalSinEvaluar++;
                }
            }

            double porcentajeAprobados = model.TotalEstudiantes > 0 ? (totalAprobados * 100.0 / model.TotalEstudiantes) : 0;
            double porcentajeReprobados = model.TotalEstudiantes > 0 ? (totalReprobados * 100.0 / model.TotalEstudiantes) : 0;
            double porcentajeSinEvaluar = model.TotalEstudiantes > 0 ? (totalSinEvaluar * 100.0 / model.TotalEstudiantes) : 0;

            model.TotalAprobados = totalAprobados;
            model.TotalReprobados = totalReprobados;
            model.TotalSinEvaluar = totalSinEvaluar;
            model.PorcentajeAprobados = porcentajeAprobados;
            model.PorcentajeReprobados = porcentajeReprobados;
            model.PorcentajeSinEvaluar = porcentajeSinEvaluar;

            var materias = await _subjectService.GetAllAsync();
            var materiasDesempeno = new List<MateriaDesempenoViewModel>();
            foreach (var materia in materias)
            {
                int estudiantesMateria = 0;
                int aprobadosMateria = 0;
                int reprobadosMateria = 0;
                double sumaPromedios = 0;
                int totalPromedios = 0;

                foreach (var estudiante in soloEstudiantes)
                {
                    if (reportesPorEstudiante.TryGetValue(estudiante.Id, out var reporte) && reporte.Grades != null)
                    {
                        var notasMateria = reporte.Grades.Where(g => g.Subject == materia.Name).ToList();
                        if (notasMateria.Count > 0)
                        {
                            estudiantesMateria++;
                            var promedioMateria = notasMateria.Average(g => (double)g.Value);
                            sumaPromedios += promedioMateria;
                            totalPromedios++;
                            if (promedioMateria >= 3.0)
                                aprobadosMateria++;
                            else if (promedioMateria >= 1.0 && promedioMateria < 3.0)
                                reprobadosMateria++;
                        }
                    }
                }
                double promedioFinal = totalPromedios > 0 ? sumaPromedios / totalPromedios : 0;
                materiasDesempeno.Add(new MateriaDesempenoViewModel
                {
                    Nombre = materia.Name,
                    Estudiantes = estudiantesMateria,
                    Promedio = Math.Round(promedioFinal, 1),
                    Aprobados = aprobadosMateria,
                    Reprobados = reprobadosMateria,
                    ColorBarra = promedioFinal >= 4.0 ? "#27ae60" : "#f1c40f"
                });
            }

            var profesores = await _userService.GetAllWithAssignmentsByRoleAsync("teacher");
            var profesoresDesempeno = new List<ProfesorDesempenoViewModel>();
            foreach (var prof in profesores)
            {
                var asignaciones = prof.TeacherAssignments;
                if (asignaciones == null || asignaciones.Count == 0)
                    continue;

                var materiasProfesor = asignaciones.Select(a => a.SubjectAssignment?.Subject?.Name).Distinct().Where(n => !string.IsNullOrEmpty(n)).ToList();
                int profTotalEstudiantes = 0;
                double profSumaPromedios = 0;
                int profTotalPromedios = 0;
                int profTotalAprobados = 0;
                int profTotalReprobados = 0;
                DateTime? ultimaActividad = null;

                foreach (var materiaNombre in materiasProfesor)
                {
                    foreach (var reporte in reportesPorEstudiante.Values)
                    {
                        var notas = reporte.Grades.Where(g => g.Subject == materiaNombre && g.Teacher == prof.Name).ToList();
                        if (notas.Count > 0)
                        {
                            profTotalEstudiantes++;
                            var promedio = notas.Average(g => (double)g.Value);
                            profSumaPromedios += promedio;
                            profTotalPromedios++;
                            if (promedio >= 3.0)
                                profTotalAprobados++;
                            else if (promedio >= 1.0 && promedio < 3.0)
                                profTotalReprobados++;
                            var fechaUltima = notas.Max(g => g.CreatedAt);
                            if (!ultimaActividad.HasValue || fechaUltima > ultimaActividad)
                                ultimaActividad = fechaUltima;
                        }
                    }
                }
                double promedioGeneral = profTotalPromedios > 0 ? profSumaPromedios / profTotalPromedios : 0;
                double porcentajeAprobadosProf = profTotalEstudiantes > 0 ? (profTotalAprobados * 100.0 / profTotalEstudiantes) : 0;
                double porcentajeReprobadosProf = profTotalEstudiantes > 0 ? (profTotalReprobados * 100.0 / profTotalEstudiantes) : 0;
                string estado = "Crítico";
                if (porcentajeAprobadosProf >= 80) estado = "Excelente";
                else if (porcentajeAprobadosProf >= 60) estado = "Regular";

                profesoresDesempeno.Add(new ProfesorDesempenoViewModel
                {
                    Nombre = prof.Name,
                    Materia = string.Join(", ", materiasProfesor),
                    Desempeno = Math.Round(promedioGeneral, 1),
                    Estudiantes = profTotalEstudiantes,
                    Promedio = Math.Round(promedioGeneral, 1),
                    Aprobados = profTotalAprobados,
                    PorcentajeAprobados = porcentajeAprobadosProf,
                    Reprobados = profTotalReprobados,
                    PorcentajeReprobados = porcentajeReprobadosProf,
                    UltimaActividad = ultimaActividad ?? DateTime.MinValue,
                    Estado = estado
                });
            }

            double promedioGeneralActual = 0;
            int totalPromediosGlobal = 0;
            foreach (var reporte in reportesPorEstudiante.Values)
            {
                if (reporte.Grades != null && reporte.Grades.Count > 0)
                {
                    promedioGeneralActual += reporte.Grades.Average(g => (double)g.Value);
                    totalPromediosGlobal++;
                }
            }
            promedioGeneralActual = totalPromediosGlobal > 0 ? promedioGeneralActual / totalPromediosGlobal : 0;

            var materiasAprobacion = new List<MateriaAprobacionViewModel>();
            foreach (var mat in materiasDesempeno)
            {
                var profesor = profesoresDesempeno.FirstOrDefault(p => p.Materia.Contains(mat.Nombre));
                double porcentajeAprobacion = mat.Estudiantes > 0 ? (mat.Aprobados * 100.0 / mat.Estudiantes) : 0;
                materiasAprobacion.Add(new MateriaAprobacionViewModel
                {
                    Nombre = mat.Nombre,
                    Profesor = profesor?.Nombre ?? "-",
                    TotalEstudiantes = mat.Estudiantes,
                    Aprobados = mat.Aprobados,
                    Reprobados = mat.Reprobados,
                    PorcentajeAprobacion = porcentajeAprobacion
                });
            }

            var recomendaciones = new List<string>();
            var materiaBajo = materiasDesempeno.OrderBy(m => m.Promedio).FirstOrDefault();
            var materiaAlto = materiasDesempeno.OrderByDescending(m => m.Promedio).FirstOrDefault();
            if (materiaBajo != null)
                recomendaciones.Add($"Implementar plan de refuerzo para la materia de {materiaBajo.Nombre}");
            if (materiaAlto != null)
                recomendaciones.Add($"Extender las estrategias exitosas de {materiaAlto.Nombre} a otras materias");
            var profDestacado = profesoresDesempeno.OrderByDescending(p => p.Desempeno).FirstOrDefault();
            if (profDestacado != null)
                recomendaciones.Add($"Reconocer el desempeño destacado del profesor {profDestacado.Nombre}");

            var alertas = new List<AlertaNotificacionViewModel>();
            foreach (var mat in materiasDesempeno)
            {
                if (mat.Promedio < 3.0)
                {
                    alertas.Add(new AlertaNotificacionViewModel
                    {
                        Tipo = "Bajo",
                        Titulo = $"Bajo rendimiento en {mat.Nombre}",
                        Mensaje = $"El promedio de calificaciones en {mat.Nombre} está por debajo del objetivo."
                    });
                }
                if (mat.Estudiantes == 0)
                {
                    alertas.Add(new AlertaNotificacionViewModel
                    {
                        Tipo = "Critico",
                        Titulo = $"Materia sin estudiantes: {mat.Nombre}",
                        Mensaje = $"No hay estudiantes inscritos en la materia {mat.Nombre}."
                    });
                }
                if (mat.Estudiantes > 0 && mat.Reprobados * 100.0 / mat.Estudiantes > 40)
                {
                    alertas.Add(new AlertaNotificacionViewModel
                    {
                        Tipo = "Bajo",
                        Titulo = $"Alto porcentaje de reprobados en {mat.Nombre}",
                        Mensaje = $"Más del 40% de los estudiantes reprobaron {mat.Nombre}."
                    });
                }
            }
            foreach (var prof in profesoresDesempeno)
            {
                if (prof.Estado == "Excelente")
                {
                    alertas.Add(new AlertaNotificacionViewModel
                    {
                        Tipo = "Excelente",
                        Titulo = $"Excelente desempeño de {prof.Nombre}",
                        Mensaje = $"El profesor {prof.Nombre} mantiene un desempeño destacado en sus materias."
                    });
                }
                if (prof.Estudiantes == 0)
                {
                    alertas.Add(new AlertaNotificacionViewModel
                    {
                        Tipo = "Critico",
                        Titulo = $"Profesor sin asignaciones: {prof.Nombre}",
                        Mensaje = $"El profesor {prof.Nombre} no tiene estudiantes asignados actualmente."
                    });
                }
            }
            alertas.Add(new AlertaNotificacionViewModel
            {
                Tipo = "Reporte",
                Titulo = "Reporte mensual disponible",
                Mensaje = "El reporte de desempeño docente de este mes está listo para su revisión."
            });

            model.MateriasDesempeno = materiasDesempeno;
            model.Profesores = profesoresDesempeno;
            model.TasaAprobacionGeneral = porcentajeAprobados;
            model.MateriasAprobacion = materiasAprobacion;
            model.Alertas = alertas;
            model.Recomendaciones = recomendaciones;

            return model;
        }

        public async Task<PagedResult<MateriaDesempenoViewModel>> GetMateriasDesempenoAsync(int page, int pageSize, string trimestre = null)
        {
            var materias = await _context.Subjects
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var totalCount = await _context.Subjects.CountAsync();

            var scoreQuery = from score in _context.StudentActivityScores
                             join activity in _context.Activities on score.ActivityId equals activity.Id
                             where (trimestre == "todos" || activity.Trimester == trimestre)
                             select new { activity.SubjectId, score.Score };

            var scoreList = await scoreQuery.ToListAsync();

            var result = materias.Select(subject => {
                var scores = scoreList.Where(x => x.SubjectId == subject.Id).ToList();
                return new MateriaDesempenoViewModel
                {
                    Nombre = subject.Name,
                    Estudiantes = scores.Count,
                    Promedio = scores.Any() ? Math.Round((double)scores.Average(x => (decimal)x.Score), 1) : 0,
                    Aprobados = scores.Count(x => x.Score >= 3.0m),
                    Reprobados = scores.Count(x => x.Score < 3.0m && x.Score >= 1.0m),
                    ColorBarra = scores.Any() ? (scores.Average(x => (decimal)x.Score) >= 4.0m ? "#27ae60" : "#f1c40f") : "#f1c40f"
                };
            }).ToList();

            return new PagedResult<MateriaDesempenoViewModel>
            {
                Items = result,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<ProfesorDesempenoViewModel>> GetProfesoresDesempenoAsync(int page, int pageSize, string trimestre = null)
        {
            var profesores = await _context.Users
                .Where(u => u.Role.ToLower() == "teacher")
                .OrderBy(u => u.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var totalCount = await _context.Users.CountAsync(u => u.Role.ToLower() == "teacher");

            var scoreQuery = from score in _context.StudentActivityScores
                             join activity in _context.Activities on score.ActivityId equals activity.Id
                             where (trimestre == "todos" || activity.Trimester == trimestre)
                             select new { activity.TeacherId, activity.SubjectId, activity.CreatedAt, score.Score };
            var scoreList = await scoreQuery.ToListAsync();

            var subjectDict = await _context.Subjects.ToDictionaryAsync(s => s.Id, s => s.Name);

            var result = profesores.Select(prof => {
                var scores = scoreList.Where(x => x.TeacherId == prof.Id).ToList();
                var materias = scores.Where(x => x.SubjectId.HasValue)
                                   .Select(x => subjectDict.ContainsKey(x.SubjectId.Value) ? subjectDict[x.SubjectId.Value] : "")
                                   .Distinct();
                double promedio = scores.Any() ? (double)scores.Average(x => (decimal)x.Score) : 0;
                int aprobados = scores.Count(x => x.Score >= 3.0m);
                int reprobados = scores.Count(x => x.Score < 3.0m && x.Score >= 1.0m);
                int totalEstudiantes = scores.Count;
                double porcentajeAprobados = totalEstudiantes > 0 ? aprobados * 100.0 / totalEstudiantes : 0;
                double porcentajeReprobados = totalEstudiantes > 0 ? reprobados * 100.0 / totalEstudiantes : 0;
                string estado = "Crítico";
                if (porcentajeAprobados >= 80) estado = "Excelente";
                else if (porcentajeAprobados >= 60) estado = "Regular";
                return new ProfesorDesempenoViewModel
                {
                    Nombre = prof.Name,
                    Materia = string.Join(", ", materias),
                    Desempeno = Math.Round(promedio, 1),
                    Estudiantes = totalEstudiantes,
                    Promedio = Math.Round(promedio, 1),
                    Aprobados = aprobados,
                    PorcentajeAprobados = porcentajeAprobados,
                    Reprobados = reprobados,
                    PorcentajeReprobados = porcentajeReprobados,
                    UltimaActividad = scores.Any() ? scores.Max(x => x.CreatedAt ?? DateTime.MinValue) : DateTime.MinValue,
                    Estado = estado
                };
            }).ToList();

            return new PagedResult<ProfesorDesempenoViewModel>
            {
                Items = result,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<MateriaAprobacionViewModel>> GetMateriasAprobacionAsync(int page, int pageSize, string trimestre = null)
        {
            var materias = await _context.Subjects
                .OrderBy(s => s.Name)
                .ToListAsync();

            var scoreQuery = from score in _context.StudentActivityScores
                             join activity in _context.Activities on score.ActivityId equals activity.Id
                             join teacher in _context.Users on activity.TeacherId equals teacher.Id into teacherJoin
                             from teacher in teacherJoin.DefaultIfEmpty()
                             where (trimestre == "todos" || activity.Trimester == trimestre)
                             select new { activity.SubjectId, Teacher = teacher != null ? teacher.Name : "-", score.Score };
            
            var scoreList = await scoreQuery.ToListAsync();

            // Paginación de resultados
            var materiasPage = materias
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = materiasPage.Select(subject =>
            {
                var scores = scoreList.Where(x => x.SubjectId == subject.Id).ToList();
                string profesor = scores.Select(x => x.Teacher).FirstOrDefault() ?? "-";
                int totalEstudiantes = scores.Count;
                int aprobados = scores.Count(x => x.Score >= 3.0m);
                int reprobados = scores.Count(x => x.Score < 3.0m && x.Score >= 1.0m);
                double porcentajeAprobacion = totalEstudiantes > 0 ? aprobados * 100.0 / totalEstudiantes : 0;

                return new MateriaAprobacionViewModel
                {
                    Nombre = subject.Name,
                    Profesor = profesor,
                    TotalEstudiantes = totalEstudiantes,
                    Aprobados = aprobados,
                    Reprobados = reprobados,
                    PorcentajeAprobacion = porcentajeAprobacion
                };
            }).ToList();

            return new PagedResult<MateriaAprobacionViewModel>
            {
                Items = result,
                TotalCount = materias.Count
            };
        }

        public async Task<PagedResult<AlertaNotificacionViewModel>> GetAlertasAsync(int page, int pageSize, string trimestre = null)
        {
            var materias = await _context.Subjects
                .OrderBy(s => s.Name)
                .ToListAsync();

            var scoreQuery = from score in _context.StudentActivityScores
                             join activity in _context.Activities on score.ActivityId equals activity.Id
                             where (trimestre == "todos" || activity.Trimester == trimestre)
                             select new { activity.SubjectId, score.Score };
            var scoreList = await scoreQuery.ToListAsync();

            var alertas = new List<AlertaNotificacionViewModel>();
            foreach (var subject in materias)
            {
                var scores = scoreList.Where(x => x.SubjectId == subject.Id).ToList();
                double promedio = scores.Any() ? (double)scores.Average(x => (decimal)x.Score) : 0;
                int totalEstudiantes = scores.Count;
                int reprobados = scores.Count(x => x.Score < 3.0m && x.Score >= 1.0m);
                if (scores.Any() && promedio < 3.0)
                {
                    alertas.Add(new AlertaNotificacionViewModel
                    {
                        Tipo = "Bajo",
                        Titulo = $"Bajo rendimiento en {subject.Name}",
                        Mensaje = $"El promedio de calificaciones en {subject.Name} está por debajo del objetivo."
                    });
                }
                if (totalEstudiantes == 0)
                {
                    alertas.Add(new AlertaNotificacionViewModel
                    {
                        Tipo = "Critico",
                        Titulo = $"Materia sin estudiantes: {subject.Name}",
                        Mensaje = $"No hay estudiantes inscritos en la materia {subject.Name}."
                    });
                }
                if (totalEstudiantes > 0 && reprobados * 100.0 / totalEstudiantes > 40)
                {
                    alertas.Add(new AlertaNotificacionViewModel
                    {
                        Tipo = "Bajo",
                        Titulo = $"Alto porcentaje de reprobados en {subject.Name}",
                        Mensaje = $"Más del 40% de los estudiantes reprobaron {subject.Name}."
                    });
                }
            }
            // Alerta de reporte mensual (solo una vez por página)
            if (page == 1)
            {
                alertas.Add(new AlertaNotificacionViewModel
                {
                    Tipo = "Reporte",
                    Titulo = "Reporte mensual disponible",
                    Mensaje = "El reporte de desempeño docente de este mes está listo para su revisión."
                });
            }
            var pagedAlertas = alertas.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return new PagedResult<AlertaNotificacionViewModel>
            {
                Items = pagedAlertas,
                TotalCount = alertas.Count
            };
        }

        public async Task<DirectorViewModel> GetInitialTotalsAsync()
        {
            var model = new DirectorViewModel();
            
            // Obtener trimestres (necesario para el dropdown)
            model.TrimestresDisponibles = await _trimesterService.GetAllAsync();
            model.TrimestreSeleccionado = "";

            // Consulta optimizada para obtener totales en una sola consulta SQL
            var totals = await _context.Users
                .Where(u => u.Role.ToLower() == "estudiante" || 
                           u.Role.ToLower() == "student" || 
                           u.Role.ToLower() == "alumno")
                .GroupBy(u => 1)
                .Select(g => new
                {
                    TotalEstudiantes = g.Count(),
                    TotalAprobados = g.Count(u => u.StudentActivityScores.Any() && 
                        u.StudentActivityScores.Average(s => (double)s.Score) >= 3.0),
                    TotalReprobados = g.Count(u => u.StudentActivityScores.Any() && 
                        u.StudentActivityScores.Average(s => (double)s.Score) < 3.0),
                    TotalSinEvaluar = g.Count(u => !u.StudentActivityScores.Any())
                })
                .FirstOrDefaultAsync();

            if (totals != null)
            {
                model.TotalEstudiantes = totals.TotalEstudiantes;
                model.TotalAprobados = totals.TotalAprobados;
                model.TotalReprobados = totals.TotalReprobados;
                model.TotalSinEvaluar = totals.TotalSinEvaluar;
            }
            else
            {
                model.TotalEstudiantes = 0;
                model.TotalAprobados = 0;
                model.TotalReprobados = 0;
                model.TotalSinEvaluar = 0;
            }

            // Calcular porcentajes
            model.PorcentajeAprobados = model.TotalEstudiantes > 0 
                ? (model.TotalAprobados * 100.0 / model.TotalEstudiantes) 
                : 0;
            model.PorcentajeReprobados = model.TotalEstudiantes > 0 
                ? (model.TotalReprobados * 100.0 / model.TotalEstudiantes) 
                : 0;
            model.PorcentajeSinEvaluar = model.TotalEstudiantes > 0 
                ? (model.TotalSinEvaluar * 100.0 / model.TotalEstudiantes) 
                : 0;

            // Calcular tasa de aprobación general
            model.TasaAprobacionGeneral = model.PorcentajeAprobados;

            // Generar análisis de tendencia
            var materiasQuery = from score in _context.StudentActivityScores
                              join activity in _context.Activities on score.ActivityId equals activity.Id
                              join subject in _context.Subjects on activity.SubjectId equals subject.Id
                              group score by subject.Name into g
                              select new
                              {
                                  Materia = g.Key,
                                  Promedio = g.Average(s => (double)s.Score),
                                  TotalEstudiantes = g.Count(),
                                  Aprobados = g.Count(s => s.Score >= 3.0m),
                                  Reprobados = g.Count(s => s.Score < 3.0m)
                              };

            var materias = await materiasQuery.ToListAsync();
            var materiasOrdenadas = materias.OrderByDescending(m => m.Promedio).ToList();

            // Generar análisis de tendencia
            var analisis = new System.Text.StringBuilder();
            analisis.Append("Se observa ");

            var materiasDestacadas = materiasOrdenadas.Where(m => m.Promedio >= 4.0).ToList();
            var materiasCriticas = materiasOrdenadas.Where(m => m.Promedio < 3.0).ToList();

            if (materiasDestacadas.Any())
            {
                analisis.Append($"un desempeño sobresaliente en {string.Join(", ", materiasDestacadas.Select(m => m.Materia))}, ");
            }

            if (materiasCriticas.Any())
            {
                analisis.Append($"mientras que {string.Join(", ", materiasCriticas.Select(m => m.Materia))} requieren atención inmediata. ");
            }

            model.AnalisisTendencia = analisis.ToString().Trim();

            // Generar recomendaciones
            var recomendaciones = new List<string>();

            // Recomendaciones basadas en materias críticas
            foreach (var materia in materiasCriticas)
            {
                recomendaciones.Add($"Implementar plan de refuerzo para la materia de {materia.Materia}");
            }

            // Recomendaciones basadas en materias destacadas
            foreach (var materia in materiasDestacadas)
            {
                recomendaciones.Add($"Extender las estrategias exitosas de {materia.Materia} a otras materias");
            }

            // Recomendaciones generales si hay materias con alto índice de reprobación
            var materiasAltoIndiceReprobacion = materias.Where(m => m.TotalEstudiantes > 0 && 
                (double)m.Reprobados / m.TotalEstudiantes > 0.4).ToList();

            foreach (var materia in materiasAltoIndiceReprobacion)
            {
                recomendaciones.Add($"Realizar seguimiento especial en {materia.Materia} por alto índice de reprobación");
            }

            model.Recomendaciones = recomendaciones;

            // Inicializar listas vacías para carga posterior vía AJAX
            model.MateriasDesempeno = new List<MateriaDesempenoViewModel>();
            model.Profesores = new List<ProfesorDesempenoViewModel>();
            model.MateriasAprobacion = new List<MateriaAprobacionViewModel>();
            model.Alertas = new List<AlertaNotificacionViewModel>();

            return model;
        }
    }
} 