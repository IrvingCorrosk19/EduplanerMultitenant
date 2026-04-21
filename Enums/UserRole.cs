namespace SchoolManager.Enums
{
    public enum UserRole
    {
        Superadmin,
        Admin,
        Director,
        Teacher,
        Contable,
        Secretaria,
        // Parent,
        Student,
        Estudiante,
        /// <summary>Club de Padres: solo registro de pagos (carnet y plataforma).</summary>
        ClubParentsAdmin,
        /// <summary>QL Services: marcar carnet Impreso/Entregado.</summary>
        QlServices,
        /// <summary>Inspector: revisión/control (ej. entrada, carnets).</summary>
        Inspector
    }
}
