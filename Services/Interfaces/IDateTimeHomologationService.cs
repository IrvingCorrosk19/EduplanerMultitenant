using System;

namespace SchoolManager.Services.Interfaces
{
    public interface IDateTimeHomologationService
    {
        /// <summary>
        /// Homologa una fecha de nacimiento según el origen de creación del usuario
        /// </summary>
        /// <param name="fechaNacimiento">Fecha de nacimiento en formato string</param>
        /// <param name="origen">Origen de creación: "User", "AcademicAssignment", "StudentAssignment"</param>
        /// <returns>DateTime homologado y normalizado</returns>
        DateTime HomologateDateOfBirth(string fechaNacimiento, string origen);
        
        /// <summary>
        /// Convierte número de Excel a fecha UTC
        /// </summary>
        /// <param name="excelDateNumber">Número de fecha de Excel</param>
        /// <returns>DateTime en UTC</returns>
        DateTime ConvertExcelDateToUtc(double excelDateNumber);
        
        /// <summary>
        /// Convierte fecha string a DateTime UTC
        /// </summary>
        /// <param name="fechaString">Fecha en formato string</param>
        /// <returns>DateTime en UTC</returns>
        DateTime ConvertStringDateToUtc(string fechaString);
    }
}
