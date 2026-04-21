using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SchoolManager.Models
{
    /// <summary>
    /// Interceptor global que convierte automáticamente todos los DateTime a UTC
    /// antes de guardarlos en PostgreSQL
    /// </summary>
    public class DateTimeInterceptor : ISaveChangesInterceptor
    {
        public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            ConvertDateTimesToUtc(eventData.Context);
            return result;
        }

        public async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            ConvertDateTimesToUtc(eventData.Context);
            return result;
        }

        /// <summary>
        /// Convierte todos los DateTime a UTC antes de guardar
        /// </summary>
        private void ConvertDateTimesToUtc(DbContext context)
        {
            if (context == null) return;

            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.ClrType == typeof(DateTime))
                    {
                        if (property.CurrentValue != null)
                        {
                            var dateTime = (DateTime)property.CurrentValue;
                            if (dateTime.Kind != DateTimeKind.Utc)
                            {
                                property.CurrentValue = dateTime.Kind == DateTimeKind.Local 
                                    ? dateTime.ToUniversalTime() 
                                    : DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime();
                            }
                        }
                    }
                    else if (property.Metadata.ClrType == typeof(DateTime?))
                    {
                        if (property.CurrentValue != null)
                        {
                            var dateTime = (DateTime?)property.CurrentValue;
                            if (dateTime.HasValue && dateTime.Value.Kind != DateTimeKind.Utc)
                            {
                                var utcDateTime = dateTime.Value.Kind == DateTimeKind.Local 
                                    ? dateTime.Value.ToUniversalTime() 
                                    : DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Local).ToUniversalTime();
                                property.CurrentValue = utcDateTime;
                            }
                        }
                    }
                }
            }
        }
    }
} 