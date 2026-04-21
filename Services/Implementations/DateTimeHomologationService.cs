using SchoolManager.Services.Interfaces;
using System;

namespace SchoolManager.Services.Implementations
{
    public class DateTimeHomologationService : IDateTimeHomologationService
    {
        public DateTime HomologateDateOfBirth(string fechaNacimiento, string origen)
        {
            if (string.IsNullOrWhiteSpace(fechaNacimiento))
            {
                return GetDefaultDateByOrigin(origen);
            }

            // Homologación según el origen
            switch (origen.ToLower())
            {
                case "academicassignment":
                case "studentassignment":
                    // Para módulos de carga masiva, manejar números de Excel
                    return HomologateForBulkUpload(fechaNacimiento);
                
                case "user":
                default:
                    // Para creación manual de usuarios, manejar formato estándar
                    return HomologateForManualCreation(fechaNacimiento);
            }
        }

        public DateTime ConvertExcelDateToUtc(double excelDateNumber)
        {
            try
            {
                // Convertir número de Excel a fecha
                // Excel cuenta días desde 1900-01-01, pero tiene un bug: considera 1900 como año bisiesto
                var fecha = new DateTime(1900, 1, 1).AddDays(excelDateNumber - 2);
                
                // Especificar que es UTC para PostgreSQL
                return DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
            }
            catch
            {
                // Si falla la conversión, devolver fecha por defecto
                return DateTime.SpecifyKind(DateTime.UtcNow.AddYears(-25), DateTimeKind.Utc);
            }
        }

        public DateTime ConvertStringDateToUtc(string fechaString)
        {
            if (string.IsNullOrWhiteSpace(fechaString))
            {
                return DateTime.SpecifyKind(DateTime.UtcNow.AddYears(-25), DateTimeKind.Utc);
            }

            // Intentar parsear como fecha normal
            if (DateTime.TryParse(fechaString, out DateTime fecha))
            {
                // Si la fecha no tiene Kind especificado, asumir que es UTC
                if (fecha.Kind == DateTimeKind.Unspecified)
                {
                    return DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
                }
                return fecha;
            }

            // Si no se puede parsear, devolver fecha por defecto
            return DateTime.SpecifyKind(DateTime.UtcNow.AddYears(-25), DateTimeKind.Utc);
        }

        private DateTime HomologateForBulkUpload(string fechaNacimiento)
        {
            // Para carga masiva, primero intentar como número de Excel
            if (double.TryParse(fechaNacimiento, out double excelDate))
            {
                return ConvertExcelDateToUtc(excelDate);
            }

            // Si no es número, intentar como fecha string
            return ConvertStringDateToUtc(fechaNacimiento);
        }

        private DateTime HomologateForManualCreation(string fechaNacimiento)
        {
            // Para creación manual, solo manejar fechas en formato string
            return ConvertStringDateToUtc(fechaNacimiento);
        }

        private DateTime GetDefaultDateByOrigin(string origen)
        {
            switch (origen.ToLower())
            {
                case "academicassignment":
                    // Profesores: 25 años por defecto
                    return DateTime.SpecifyKind(DateTime.UtcNow.AddYears(-25), DateTimeKind.Utc);
                
                case "studentassignment":
                    // Estudiantes: 18 años por defecto
                    return DateTime.SpecifyKind(DateTime.UtcNow.AddYears(-18), DateTimeKind.Utc);
                
                case "user":
                default:
                    // Usuarios generales: 25 años por defecto
                    return DateTime.SpecifyKind(DateTime.UtcNow.AddYears(-25), DateTimeKind.Utc);
            }
        }
    }
}
