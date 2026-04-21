using SchoolManager.Dtos;
using System.Collections.Generic;

namespace SchoolManager.ViewModels
{
    public class EmailConfigurationListViewModel
    {
        public IEnumerable<EmailConfigurationDto> EmailConfigurations { get; set; } = new List<EmailConfigurationDto>();
    }
}
