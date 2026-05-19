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
                .ForMember(dest => dest.AccountEnabled, opt => opt.MapFrom(src => src.AccountEnabled))
                .ForMember(dest => dest.MailNickname, opt => opt.MapFrom(src => src.MailNickname))
                .ForMember(dest => dest.Mail, opt => opt.MapFrom(src => src.Mail))
                .ForMember(dest => dest.PasswordProfile, opt => opt.MapFrom(src => new PasswordProfile
                {
                    Password = src.Password,
                    ForceChangePasswordNextSignIn = true
                }))
                .ForMember(dest => dest.Id, opt => opt.Ignore());
        }
    }
}
