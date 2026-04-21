using Microsoft.EntityFrameworkCore;
using SchoolManager.Dtos;
using SchoolManager.Models;
using SchoolManager.Services.Interfaces;

namespace SchoolManager.Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly SchoolDbContext _context;
    private readonly ILogger<PaymentService> _logger;
    private readonly IPrematriculationService _prematriculationService;

    public PaymentService(
        SchoolDbContext context,
        ILogger<PaymentService> logger,
        IPrematriculationService prematriculationService)
    {
        _context = context;
        _logger = logger;
        _prematriculationService = prematriculationService;
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        return await _context.Payments
            .Include(p => p.Prematriculation)
            .Include(p => p.RegisteredByUser)
            .Include(p => p.School)
            .Include(p => p.PaymentConcept)
            .Include(p => p.Student)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment?> GetByReceiptNumberAsync(string receiptNumber)
    {
        return await _context.Payments
            .Include(p => p.Prematriculation)
            .Include(p => p.RegisteredByUser)
            .FirstOrDefaultAsync(p => p.ReceiptNumber == receiptNumber);
    }

    public async Task<List<PaymentDto>> GetByPrematriculationAsync(Guid prematriculationId)
    {
        return await _context.Payments
            .Where(p => p.PrematriculationId == prematriculationId)
            .Include(p => p.RegisteredByUser)
            .Include(p => p.PaymentConcept)
            .Include(p => p.Student)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                SchoolId = p.SchoolId,
                PrematriculationId = p.PrematriculationId,
                RegisteredBy = p.RegisteredBy,
                RegisteredByName = p.RegisteredByUser != null 
                    ? $"{p.RegisteredByUser.Name} {p.RegisteredByUser.LastName}" 
                    : null,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                ReceiptNumber = p.ReceiptNumber,
                PaymentStatus = p.PaymentStatus,
                PaymentMethod = p.PaymentMethod,
                ReceiptImage = p.ReceiptImage,
                PaymentConceptId = p.PaymentConceptId,
                PaymentConceptName = p.PaymentConcept != null ? p.PaymentConcept.Name : null,
                StudentId = p.StudentId,
                StudentName = p.Student != null 
                    ? $"{p.Student.Name} {p.Student.LastName}" 
                    : null,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt,
                ConfirmedAt = p.ConfirmedAt
            })
            .ToListAsync();
    }

    public async Task<List<PaymentDto>> GetBySchoolAsync(Guid schoolId)
    {
        return await _context.Payments
            .Where(p => p.SchoolId == schoolId)
            .Include(p => p.RegisteredByUser)
            .Include(p => p.Prematriculation)
            .Include(p => p.PaymentConcept)
            .Include(p => p.Student)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                SchoolId = p.SchoolId,
                PrematriculationId = p.PrematriculationId,
                RegisteredBy = p.RegisteredBy,
                RegisteredByName = p.RegisteredByUser != null 
                    ? $"{p.RegisteredByUser.Name} {p.RegisteredByUser.LastName}" 
                    : null,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                ReceiptNumber = p.ReceiptNumber,
                PaymentStatus = p.PaymentStatus,
                PaymentMethod = p.PaymentMethod,
                ReceiptImage = p.ReceiptImage,
                PaymentConceptId = p.PaymentConceptId,
                PaymentConceptName = p.PaymentConcept != null ? p.PaymentConcept.Name : null,
                StudentId = p.StudentId,
                StudentName = p.Student != null 
                    ? $"{p.Student.Name} {p.Student.LastName}" 
                    : null,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt,
                ConfirmedAt = p.ConfirmedAt
            })
            .ToListAsync();
    }

    public async Task<Payment> CreateAsync(PaymentCreateDto dto, Guid registeredBy)
    {
        Guid schoolId;
        Guid? studentId = dto.StudentId;

        // Si hay prematrícula, obtener schoolId de ahí
        if (dto.PrematriculationId.HasValue)
        {
            var prematriculation = await _context.Prematriculations
                .Include(p => p.School)
                .Include(p => p.Student)
                .FirstOrDefaultAsync(p => p.Id == dto.PrematriculationId);

            if (prematriculation == null)
                throw new Exception("Prematrícula no encontrada");

            schoolId = prematriculation.SchoolId;
            
            // Si no se especificó StudentId pero hay prematrícula, usar el estudiante de la prematrícula
            if (!studentId.HasValue && prematriculation.StudentId != Guid.Empty)
            {
                studentId = prematriculation.StudentId;
            }
        }
        else if (dto.StudentId.HasValue)
        {
            // Si no hay prematrícula pero sí estudiante, obtener schoolId del estudiante
            var student = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.StudentId.Value);
            
            if (student == null || !student.SchoolId.HasValue)
                throw new Exception("Estudiante no encontrado o sin escuela asignada");

            schoolId = student.SchoolId.Value;
        }
        else
        {
            throw new Exception("Debe especificar una prematrícula o un estudiante");
        }

        // Verificar que el número de recibo no exista
        if (!string.IsNullOrEmpty(dto.ReceiptNumber))
        {
            var existingReceipt = await _context.Payments
                .AnyAsync(p => p.ReceiptNumber == dto.ReceiptNumber);

            if (existingReceipt)
                throw new Exception("El número de recibo ya existe");
        }

        // Validar monto según concepto si está especificado
        if (dto.PaymentConceptId.HasValue)
        {
            var concept = await _context.PaymentConcepts
                .FirstOrDefaultAsync(pc => pc.Id == dto.PaymentConceptId.Value);

            if (concept == null)
                throw new Exception("Concepto de pago no encontrado");

            if (!concept.IsActive)
                throw new Exception("El concepto de pago no está activo");

            // Validar que el monto no sea menor al definido por concepto
            if (dto.Amount < concept.Amount)
            {
                throw new Exception($"El monto debe ser mayor o igual al definido para el concepto ({concept.Amount:C})");
            }
        }

        // Determinar estado inicial según método de pago
        string initialStatus = "Pendiente";
        if (dto.PaymentMethod == "Tarjeta")
        {
            // ⚠️ MODO SIMULADO: Tarjeta se confirma automáticamente SIN procesamiento real
            // En producción, aquí se integraría con una pasarela de pagos real (Stripe, PayPal, Yappy, etc.)
            // y solo se confirmaría si la pasarela procesa exitosamente el pago
            initialStatus = "Confirmado";
        }

        // MEJORADO: Validar y corregir fecha de pago si no es válida
        var paymentDate = dto.PaymentDate;
        if (paymentDate == default(DateTime) || paymentDate == DateTime.MinValue || paymentDate.Year < 2000)
        {
            // Si la fecha no es válida, usar la fecha actual
            paymentDate = DateTime.UtcNow;
            _logger.LogWarning("Fecha de pago inválida detectada, usando fecha actual: {PaymentDate}", paymentDate);
        }

        // Crear el pago
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            PrematriculationId = dto.PrematriculationId ?? Guid.Empty,
            PaymentConceptId = dto.PaymentConceptId,
            StudentId = studentId,
            RegisteredBy = registeredBy,
            Amount = dto.Amount,
            PaymentDate = paymentDate,
            ReceiptNumber = dto.ReceiptNumber,
            PaymentMethod = dto.PaymentMethod,
            ReceiptImage = dto.ReceiptImage,
            PaymentStatus = initialStatus,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            ConfirmedAt = initialStatus == "Confirmado" ? DateTime.UtcNow : null
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Pago creado: {PaymentId} - Método: {Method} - Estado: {Status}", 
            payment.Id, dto.PaymentMethod ?? "N/A", payment.PaymentStatus);

        // Si se confirmó automáticamente (Tarjeta), activar matrícula si corresponde
        if (payment.PaymentStatus == "Confirmado" && dto.PrematriculationId.HasValue)
        {
            var prematriculation = await _context.Prematriculations
                .FirstOrDefaultAsync(p => p.Id == dto.PrematriculationId.Value);

            if (prematriculation != null && prematriculation.Status == "Prematriculado")
            {
                prematriculation.Status = "Pagado";
                prematriculation.PaymentDate = DateTime.UtcNow;
                prematriculation.UpdatedAt = DateTime.UtcNow;
                _context.Prematriculations.Update(prematriculation);
                await _context.SaveChangesAsync();

                // Activar matrícula automáticamente
                try
                {
                    await _prematriculationService.ConfirmMatriculationAsync(prematriculation.Id);
                    _logger.LogInformation("Matrícula activada automáticamente para prematrícula {PrematriculationId}", 
                        prematriculation.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al activar matrícula automáticamente");
                }
            }
        }

        return payment;
    }

    public async Task<Payment> ConfirmPaymentAsync(Guid paymentId, Guid confirmedBy)
    {
        var payment = await _context.Payments
            .Include(p => p.Prematriculation)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
            throw new Exception("Pago no encontrado");

        if (payment.PaymentStatus == "Confirmado")
            throw new Exception("El pago ya está confirmado");

        // Confirmar el pago
        payment.PaymentStatus = "Confirmado";
        payment.ConfirmedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;

        // Actualizar la prematrícula con la fecha de pago
        var prematriculation = payment.Prematriculation;
        if (prematriculation != null)
        {
            prematriculation.PaymentDate = DateTime.UtcNow;
            if (prematriculation.Status == "Prematriculado")
            {
                prematriculation.Status = "Pagado";
            }
            prematriculation.UpdatedAt = DateTime.UtcNow;

            _context.Prematriculations.Update(prematriculation);
        }

        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();

        // Enviar notificación al acudiente/estudiante cuando se confirma el pago
        // (La notificación de matrícula se enviará desde ConfirmMatriculationAsync)
        
        // Activar automáticamente la matrícula si el pago está confirmado
        // El email se enviará automáticamente desde ConfirmMatriculationAsync
        if (prematriculation != null && prematriculation.Status == "Pagado")
        {
            try
            {
                await _prematriculationService.ConfirmMatriculationAsync(prematriculation.Id);
                _logger.LogInformation("Matrícula activada automáticamente para prematrícula {PrematriculationId}", 
                    prematriculation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar matrícula automáticamente para prematrícula {PrematriculationId}", 
                    prematriculation.Id);
            }
        }

        _logger.LogInformation("Pago confirmado: {PaymentId}", paymentId);

        return payment;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null)
            return false;

        // No permitir eliminar pagos confirmados
        if (payment.PaymentStatus == "Confirmado")
        {
            _logger.LogWarning("No se puede eliminar el pago {PaymentId} porque ya está confirmado", id);
            return false;
        }

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Pago eliminado: {PaymentId}", id);

        return true;
    }

    public async Task<List<PaymentDto>> GetByStudentAsync(Guid studentId)
    {
        return await _context.Payments
            .Where(p => p.StudentId == studentId)
            .Include(p => p.RegisteredByUser)
            .Include(p => p.PaymentConcept)
            .Include(p => p.Student)
            .Include(p => p.Prematriculation)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                SchoolId = p.SchoolId,
                PrematriculationId = p.PrematriculationId,
                RegisteredBy = p.RegisteredBy,
                RegisteredByName = p.RegisteredByUser != null 
                    ? $"{p.RegisteredByUser.Name} {p.RegisteredByUser.LastName}" 
                    : null,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                ReceiptNumber = p.ReceiptNumber,
                PaymentStatus = p.PaymentStatus,
                PaymentMethod = p.PaymentMethod,
                ReceiptImage = p.ReceiptImage,
                PaymentConceptId = p.PaymentConceptId,
                PaymentConceptName = p.PaymentConcept != null ? p.PaymentConcept.Name : null,
                StudentId = p.StudentId,
                StudentName = p.Student != null 
                    ? $"{p.Student.Name} {p.Student.LastName}" 
                    : null,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt,
                ConfirmedAt = p.ConfirmedAt
            })
            .ToListAsync();
    }

    public async Task<List<PaymentDto>> GetByParentAsync(Guid parentId)
    {
        // Obtener estudiantes del acudiente
        var studentIds = await _context.Prematriculations
            .Where(p => p.ParentId == parentId)
            .Select(p => p.StudentId)
            .Distinct()
            .ToListAsync();

        return await _context.Payments
            .Where(p => studentIds.Contains(p.StudentId ?? Guid.Empty))
            .Include(p => p.RegisteredByUser)
            .Include(p => p.PaymentConcept)
            .Include(p => p.Student)
            .Include(p => p.Prematriculation)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                SchoolId = p.SchoolId,
                PrematriculationId = p.PrematriculationId,
                RegisteredBy = p.RegisteredBy,
                RegisteredByName = p.RegisteredByUser != null 
                    ? $"{p.RegisteredByUser.Name} {p.RegisteredByUser.LastName}" 
                    : null,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                ReceiptNumber = p.ReceiptNumber,
                PaymentStatus = p.PaymentStatus,
                PaymentMethod = p.PaymentMethod,
                ReceiptImage = p.ReceiptImage,
                PaymentConceptId = p.PaymentConceptId,
                PaymentConceptName = p.PaymentConcept != null ? p.PaymentConcept.Name : null,
                StudentId = p.StudentId,
                StudentName = p.Student != null 
                    ? $"{p.Student.Name} {p.Student.LastName}" 
                    : null,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt,
                ConfirmedAt = p.ConfirmedAt
            })
            .ToListAsync();
    }

    public async Task<List<PaymentDto>> GetByGroupAsync(Guid groupId)
    {
        // Obtener estudiantes del grupo
        var studentIds = await _context.Prematriculations
            .Where(p => p.GroupId == groupId)
            .Select(p => p.StudentId)
            .Distinct()
            .ToListAsync();

        return await _context.Payments
            .Where(p => studentIds.Contains(p.StudentId ?? Guid.Empty))
            .Include(p => p.RegisteredByUser)
            .Include(p => p.PaymentConcept)
            .Include(p => p.Student)
            .Include(p => p.Prematriculation)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                SchoolId = p.SchoolId,
                PrematriculationId = p.PrematriculationId,
                RegisteredBy = p.RegisteredBy,
                RegisteredByName = p.RegisteredByUser != null 
                    ? $"{p.RegisteredByUser.Name} {p.RegisteredByUser.LastName}" 
                    : null,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                ReceiptNumber = p.ReceiptNumber,
                PaymentStatus = p.PaymentStatus,
                PaymentMethod = p.PaymentMethod,
                ReceiptImage = p.ReceiptImage,
                PaymentConceptId = p.PaymentConceptId,
                PaymentConceptName = p.PaymentConcept != null ? p.PaymentConcept.Name : null,
                StudentId = p.StudentId,
                StudentName = p.Student != null 
                    ? $"{p.Student.Name} {p.Student.LastName}" 
                    : null,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt,
                ConfirmedAt = p.ConfirmedAt
            })
            .ToListAsync();
    }
}

