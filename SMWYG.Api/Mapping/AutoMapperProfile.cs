using AutoMapper;
using SMWYG.Api.DTOs;
using SMWYG.Models;

namespace SMWYG.Api.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<CreateUserDto, User>();

            CreateMap<Server, SMWYG.Api.DTOs.ServerDto>();
            CreateMap<Channel, SMWYG.Api.DTOs.ChannelDto>();
            CreateMap<Message, SMWYG.Api.DTOs.MessageDto>();
        }
    }
}
