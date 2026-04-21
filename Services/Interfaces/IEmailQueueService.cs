using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using SchoolManager.Dtos;

namespace SchoolManager.Services.Interfaces;

public interface IEmailQueueService
{
    /// <summary>
    /// Encola correos masivos para los usuarios indicados.
    /// Devuelve un resultado estructurado con jobId, correlationId y conteos.
    /// Nunca lanza excepciones de negocio — encapsula todo en EnqueueResult.
    /// </summary>
    Task<EnqueueResult> EnqueueUsersAsync(List<Guid> userIds, ClaimsPrincipal currentUser);
}
