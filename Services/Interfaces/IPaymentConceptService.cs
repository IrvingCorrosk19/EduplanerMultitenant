using SchoolManager.Dtos;
using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces;

public interface IPaymentConceptService
{
    Task<PaymentConcept?> GetByIdAsync(Guid id);
    Task<List<PaymentConceptDto>> GetAllAsync(Guid schoolId);
    Task<List<PaymentConceptDto>> GetActiveAsync(Guid schoolId);
    Task<PaymentConcept> CreateAsync(PaymentConceptCreateDto dto, Guid createdBy);
    Task<PaymentConcept> UpdateAsync(Guid id, PaymentConceptCreateDto dto, Guid updatedBy);
    Task<bool> DeleteAsync(Guid id);
}

