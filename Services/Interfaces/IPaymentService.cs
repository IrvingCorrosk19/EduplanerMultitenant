using SchoolManager.Dtos;
using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces;

public interface IPaymentService
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<Payment?> GetByReceiptNumberAsync(string receiptNumber);
    Task<List<PaymentDto>> GetByPrematriculationAsync(Guid prematriculationId);
    Task<List<PaymentDto>> GetBySchoolAsync(Guid schoolId);
    Task<List<PaymentDto>> GetByStudentAsync(Guid studentId);
    Task<List<PaymentDto>> GetByParentAsync(Guid parentId);
    Task<List<PaymentDto>> GetByGroupAsync(Guid groupId);
    Task<Payment> CreateAsync(PaymentCreateDto dto, Guid registeredBy);
    Task<Payment> ConfirmPaymentAsync(Guid paymentId, Guid confirmedBy);
    Task<bool> DeleteAsync(Guid id);
}

