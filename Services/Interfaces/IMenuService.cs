using SchoolManager.Models;

namespace SchoolManager.Services.Interfaces
{
    public interface IMenuService
    {
        Task<List<MenuItem>> GetMenuItemsForUserAsync(string role);
    }

    public class MenuItem
    {
        public string Title { get; set; }
        public string Icon { get; set; }
        public string Url { get; set; }
        public List<MenuItem> SubItems { get; set; } = new();
        public string[] RequiredRoles { get; set; }
        public bool IsActive { get; set; }
    }
} 