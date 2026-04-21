using SchoolManager.Dtos;
using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces;

public interface IStudentIdCardImageService
{
    /// <summary>
    /// Renders the front face of the student ID card to a PNG byte array.
    /// Uses absolute pixel coordinates — no dynamic layouts, no overflow.
    /// </summary>
    byte[] GenerateCardImage(StudentCardRenderDto dto, SchoolIdCardSetting settings,
        IReadOnlyList<IdCardTemplateField>? customFields = null);

    /// <summary>
    /// Renders the back face of the student ID card to a PNG byte array.
    /// </summary>
    byte[] GenerateCardBackImage(StudentCardRenderDto dto, SchoolIdCardSetting settings);

    /// <summary>Returns (widthMm, heightMm) of a single card face for the given settings.</summary>
    (float WidthMm, float HeightMm) GetCardMmDimensions(SchoolIdCardSetting settings);
}
