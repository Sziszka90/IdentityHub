using AutoMapper;
using IdentityHub.Contracts.DTOs.Users.Requests;
using Microsoft.Graph.Models;

namespace IdentityHub.API.Mapping;

/// <summary>
/// AutoMapper profile for patching User with UpdateUserRequest (only non-null properties are mapped).
/// </summary>
public class UserPatchMappingProfile : Profile
{
    public UserPatchMappingProfile()
    {
        CreateMap<UpdateUserRequest, User>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}
