using System;
using System.Collections.Generic;

namespace SchoolManager.ViewModels
{
    public class DirectorViewModel
    {
        // Totales generales
        public int TotalEstudiantes { get; set; }
        public int TotalAprobados { get; set; }
        public int TotalReprobados { get; set; }
        public int TotalSinEvaluar { get; set; }
        public double PorcentajeAprobados { get; set; }
        public double PorcentajeReprobados { get; set; }
        public double PorcentajeSinEvaluar { get; set; }

        // Desempeño por materia
        public List<MateriaDesempenoViewModel> MateriasDesempeno { get; set; }

        // Profesores
        public List<ProfesorDesempenoViewModel> Profesores { get; set; }

        // Estadísticas de aprobación
        public double TasaAprobacionGeneral { get; set; }
        public string AnalisisTendencia { get; set; }

        // Aprobación por materia
        public List<MateriaAprobacionViewModel> MateriasAprobacion { get; set; }

        // Alertas y notificaciones
        public List<AlertaNotificacionViewModel> Alertas { get; set; }

        // Recomendaciones
        public List<string> Recomendaciones { get; set; }

        // Filtros globales
        public List<SchoolManager.Dtos.TrimesterDto> TrimestresDisponibles { get; set; }
        public string TrimestreSeleccionado { get; set; }
    }

    public class MateriaDesempenoViewModel
    {
        public string Nombre { get; set; }
        public int Estudiantes { get; set; }
        public double Promedio { get; set; }
        public int Aprobados { get; set; }
        public int Reprobados { get; set; }
        public string ColorBarra { get; set; } // Para el color de la barra de desempeño
    }

    public class ProfesorDesempenoViewModel
    {
        public string Nombre { get; set; }
        public string Materia { get; set; }
        public double Desempeno { get; set; }
        public int Estudiantes { get; set; }
        public double Promedio { get; set; }
        public int Aprobados { get; set; }
        public double PorcentajeAprobados { get; set; }
        public int Reprobados { get; set; }
        public double PorcentajeReprobados { get; set; }
        public DateTime UltimaActividad { get; set; }
        public string Estado { get; set; } // Excelente, Regular, etc.
    }

    public class MateriaAprobacionViewModel
    {
        public string Nombre { get; set; }
        public string Profesor { get; set; }
        public int TotalEstudiantes { get; set; }
        public int Aprobados { get; set; }
        public int Reprobados { get; set; }
        public double PorcentajeAprobacion { get; set; }
    }

    public class AlertaNotificacionViewModel
    {
        public string Tipo { get; set; } // Ej: "Bajo", "Excelente", "Reporte"
        public string Titulo { get; set; }
        public string Mensaje { get; set; }
    }
} 