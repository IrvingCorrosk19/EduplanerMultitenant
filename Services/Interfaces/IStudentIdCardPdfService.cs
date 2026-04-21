namespace SchoolManager.Services.Interfaces;

public interface IStudentIdCardPdfService
{
    Task<byte[]> GenerateCardPdfAsync(Guid studentId, Guid createdBy);
}
