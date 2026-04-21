namespace SchoolManager.Dtos;

public class SendPasswordsRequestDto
{
    public List<Guid> UserIds { get; set; } = new();
}
