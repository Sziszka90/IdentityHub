using AutoMapper;
using IdentityHub.Contracts.DTOs.Users.Requests;
using Microsoft.Graph.Models;

namespace IdentityHub.API.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<CreateUserRequest, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
        }
    }
}
