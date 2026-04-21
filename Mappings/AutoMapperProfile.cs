using AutoMapper;
using SchoolManager.Models;
using SchoolManager.ViewModels;

namespace SchoolManager.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // ViewModel → Modelo
            CreateMap<CreateUserViewModel, User>();

            // Modelo → ViewModel (opcional)
            CreateMap<User, CreateUserViewModel>();
        }
    }
}
