using AutoMapper;
using vnMentor.Models;
using System.IO.Packaging;

namespace vnMentor.Utils
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<StudentAnswer, StudentAnswerCloned>();
        }
    }

}
