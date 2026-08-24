using AutoMapper;
using FeedbackAnalysis.ClientUI.Models;
using FeedbackAnalysis.Contracts.Models;

namespace FeedbackAnalysis.ClientUI.Models
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<FeedbackDetailedModel, FeedbackViewModel>()
                .ForMember(d => d.Rating, o => o.MapFrom(s => s.Rating))
                .ForMember(d => d.Tonality, o => o.MapFrom(s => s.Tonality))
                .ForMember(d => d.Status, o => o.MapFrom(s => (int?)s.Status))
                .ForMember(d => d.AnswerSender, o => o.MapFrom(s => s.AnswerSender))
                .ForMember(d => d.AnswerText, o => o.MapFrom(s => s.AnswerText));
        }
    }
}
