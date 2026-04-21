namespace SchoolManager.Enums
{
    public enum ActivityTypeEnum
    {
        NotasDeApreciacion = 1,
        EjerciciosDiarios = 2,
        ExamenFinal = 3
    }
    
    public static class ActivityTypeEnumExtensions
    {
        public static string GetDisplayName(this ActivityTypeEnum enumValue)
        {
            return enumValue switch
            {
                ActivityTypeEnum.NotasDeApreciacion => "Notas de apreciación",
                ActivityTypeEnum.EjerciciosDiarios => "Ejercicios diarios",
                ActivityTypeEnum.ExamenFinal => "Examen Final",
                _ => enumValue.ToString()
            };
        }
    }
} 