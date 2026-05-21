using AutoMapper;
using IdentityHub.Contracts.DTOs.Users.Requests;
using IdentityHub.Contracts.DTOs.Users.Responses;
using IdentityHub.Domain.Models;
using Microsoft.Graph.Models;

namespace IdentityHub.API.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<UserContext, UserResponse>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
                .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
                .ForMember(dest => dest.Groups, opt => opt.MapFrom(src => src.Groups))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.Roles))
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permissions))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.IsAuthenticated, opt => opt.MapFrom(src => src.IsAuthenticated));

            CreateMap<User, UserResponse>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Mail ?? string.Empty))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName ?? string.Empty))
                .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => string.Empty))
                .ForMember(dest => dest.Groups, opt => opt.MapFrom(src => new List<string>()))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => new List<string>()))
                .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => new List<string>()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedDateTime ?? DateTimeOffset.MinValue))
                .ForMember(dest => dest.IsAuthenticated, opt => opt.MapFrom(src => false));

            CreateMap<CreateUserRequest, User>()
                .ForMember(dest => dest.AccountEnabled, opt => opt.MapFrom(src => src.AccountEnabled))
                .ForMember(dest => dest.MailNickname, opt => opt.MapFrom(src => src.MailNickname))
                .ForMember(dest => dest.Mail, opt => opt.MapFrom(src => src.Mail))
                .ForMember(dest => dest.PasswordProfile, opt => opt.MapFrom(src => new PasswordProfile
                {
                    Password = src.Password,
                    ForceChangePasswordNextSignIn = true
                }))
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            CreateMap<UpdateUserRequest, User>()
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
                .ForMember(dest => dest.AccountEnabled, opt => opt.MapFrom(src => src.AccountEnabled))
                .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobTitle))
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department))
                .ForMember(dest => dest.MobilePhone, opt => opt.MapFrom(src => src.MobilePhone))
                .ForMember(dest => dest.OfficeLocation, opt => opt.MapFrom(src => src.OfficeLocation))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()));

            CreateMap<User, UpdateUserRequest>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.Parse(src.Id!)))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
                .ForMember(dest => dest.AccountEnabled, opt => opt.MapFrom(src => src.AccountEnabled ?? true))
                .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobTitle))
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department))
                .ForMember(dest => dest.MobilePhone, opt => opt.MapFrom(src => src.MobilePhone))
                .ForMember(dest => dest.OfficeLocation, opt => opt.MapFrom(src => src.OfficeLocation));
        }
    }
}
