using API.DTOs;
using API.Models;
using AutoMapper;

namespace API.Mapping
{
    public class MappingProfile: Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDTO>();
            CreateMap<Event, EventDTO>();
            CreateMap<Subscriber, SubscriberDTO>();
            CreateMap<Photo, PhotoDTO>();


            CreateMap<UserDTO, User>();
            CreateMap<EventDTO, Event>();
            CreateMap<SubscriberDTO, Subscriber>();
            CreateMap<PhotoDTO, Photo>();
        }
    }
}