using AutoMapper;
using Microsoft.Graph.Models;
using IdentityHub.Contracts.DTOs.Users.Requests;

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
