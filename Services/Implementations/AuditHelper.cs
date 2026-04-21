using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations
{
    public static class AuditHelper
    {
        /// <summary>
        /// Configura los campos de auditoría para una entidad nueva
        /// </summary>
        public static async Task SetAuditFieldsForCreateAsync<T>(
            T entity, 
            ICurrentUserService currentUserService) 
            where T : class
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            var currentTime = DateTime.UtcNow;

            // Usar reflexión para establecer los campos de auditoría
            var entityType = typeof(T);
            
            // Establecer CreatedAt
            var createdAtProperty = entityType.GetProperty("CreatedAt");
            if (createdAtProperty != null && createdAtProperty.CanWrite)
            {
                createdAtProperty.SetValue(entity, currentTime);
            }

            // Establecer CreatedBy
            var createdByProperty = entityType.GetProperty("CreatedBy");
            if (createdByProperty != null && createdByProperty.CanWrite)
            {
                createdByProperty.SetValue(entity, currentUserId);
            }

            // Establecer UpdatedAt
            var updatedAtProperty = entityType.GetProperty("UpdatedAt");
            if (updatedAtProperty != null && updatedAtProperty.CanWrite)
            {
                updatedAtProperty.SetValue(entity, currentTime);
            }

            // Establecer UpdatedBy
            var updatedByProperty = entityType.GetProperty("UpdatedBy");
            if (updatedByProperty != null && updatedByProperty.CanWrite)
            {
                updatedByProperty.SetValue(entity, currentUserId);
            }
        }

        /// <summary>
        /// Configura los campos de auditoría para una entidad actualizada
        /// </summary>
        public static async Task SetAuditFieldsForUpdateAsync<T>(
            T entity, 
            ICurrentUserService currentUserService) 
            where T : class
        {
            var currentUserId = await currentUserService.GetCurrentUserIdAsync();
            var currentTime = DateTime.UtcNow;

            // Usar reflexión para establecer los campos de auditoría
            var entityType = typeof(T);
            
            // Establecer UpdatedAt
            var updatedAtProperty = entityType.GetProperty("UpdatedAt");
            if (updatedAtProperty != null && updatedAtProperty.CanWrite)
            {
                updatedAtProperty.SetValue(entity, currentTime);
            }

            // Establecer UpdatedBy
            var updatedByProperty = entityType.GetProperty("UpdatedBy");
            if (updatedByProperty != null && updatedByProperty.CanWrite)
            {
                updatedByProperty.SetValue(entity, currentUserId);
            }
        }

        /// <summary>
        /// Configura el SchoolId para una entidad
        /// </summary>
        public static async Task SetSchoolIdAsync<T>(
            T entity, 
            ICurrentUserService currentUserService) 
            where T : class
        {
            var currentUser = await currentUserService.GetCurrentUserAsync();
            if (currentUser?.SchoolId != null)
            {
                var entityType = typeof(T);
                var schoolIdProperty = entityType.GetProperty("SchoolId");
                if (schoolIdProperty != null && schoolIdProperty.CanWrite)
                {
                    schoolIdProperty.SetValue(entity, currentUser.SchoolId);
                }
            }
        }
    }
}
