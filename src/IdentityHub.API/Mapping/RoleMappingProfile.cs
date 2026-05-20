using AutoMapper;
using IdentityHub.Contracts.DTOs.Permissions.Responses;
using IdentityHub.Contracts.DTOs.Roles.Responses;
using IdentityHub.Domain.Entities;

namespace IdentityHub.API.Mapping;

/// <summary>
/// AutoMapper profile for role response projections.
/// </summary>
public class RoleMappingProfile : Profile
{
    public RoleMappingProfile()
    {
        CreateMap<Permission, PermissionResponse>();

        CreateMap<Role, RoleResponse>()
            .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src =>
                src.RolePermissions
                    .Where(rp => rp.Permission != null)
                    .Select(rp => rp.Permission)
                    .ToList()));
    }
}
