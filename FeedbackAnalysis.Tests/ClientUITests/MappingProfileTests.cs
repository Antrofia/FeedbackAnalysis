using AutoMapper;
using FeedbackAnalysis.ClientUI.Models;
using FeedbackAnalysis.Contracts.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace FeedbackAnalysis.Tests.ClientUITests;

public class MappingProfileTests
{
    private static MapperConfiguration CreateConfiguration()
    {
        return new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);
    }

    [Fact]
    public void Profile_ConfigurationIsValid_NoUnmappedDestinationMembers()
    {
        var configuration = CreateConfiguration();

        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void DetailedModelToViewModel_MapsAllFields()
    {
        var mapper = CreateConfiguration().CreateMapper();

        var model = new FeedbackDetailedModel
        {
            Id = "wb:1",
            Service = "wb",
            ServiceId = "1",
            Rating = 0.7,
            Sender = "Иван",
            Text = "текст",
            CreatedDate = new DateTime(2024, 5, 5, 10, 0, 0, DateTimeKind.Utc),
            NomenclatureLink = "www.wildberries.ru/catalog/1/detail.aspx",
            Tonality = -0.3,
            Status = FeedbackAnswerStatuses.RequireToAnswer | FeedbackAnswerStatuses.Answered,
            AnswerSender = "Оператор",
            AnswerText = "ответ"
        };

        var viewModel = mapper.Map<FeedbackViewModel>(model);

        Assert.Equal("wb:1", viewModel.Id);
        Assert.Equal("Иван", viewModel.Sender);
        Assert.Equal("текст", viewModel.Text);
        Assert.Equal(0.7, viewModel.Rating, precision: 10);
        Assert.Equal(model.CreatedDate, viewModel.CreatedDate);
        Assert.Equal("www.wildberries.ru/catalog/1/detail.aspx", viewModel.NomenclatureLink);
        Assert.Equal(-0.3, viewModel.Tonality!.Value, precision: 10);
        Assert.Equal((int)(FeedbackAnswerStatuses.RequireToAnswer | FeedbackAnswerStatuses.Answered), viewModel.Status);
        Assert.Equal("Оператор", viewModel.AnswerSender);
        Assert.Equal("ответ", viewModel.AnswerText);
    }

    [Fact]
    public void DetailedModelToViewModel_NullableFieldsRemainNull()
    {
        var mapper = CreateConfiguration().CreateMapper();

        var model = new FeedbackDetailedModel
        {
            Id = "wb:2",
            Rating = 4.0,
            Tonality = null,
            Status = null,
            AnswerSender = null,
            AnswerText = null
        };

        var viewModel = mapper.Map<FeedbackViewModel>(model);

        Assert.Null(viewModel.Tonality);
        Assert.Null(viewModel.Status);
        Assert.Null(viewModel.AnswerSender);
        Assert.Null(viewModel.AnswerText);
    }

    [Fact]
    public void ListMapping_MapsEveryElement()
    {
        var mapper = CreateConfiguration().CreateMapper();

        var models = new List<FeedbackDetailedModel>
        {
            new() { Id = "wb:a", Rating = 5, Tonality = 1.0, Status = FeedbackAnswerStatuses.RequireToAnswer },
            new() { Id = "wb:b", Rating = 3, Tonality = 0.2 }
        };

        var viewModels = mapper.Map<List<FeedbackViewModel>>(models);

        Assert.Equal(2, viewModels.Count);
        Assert.Equal(1.0, viewModels[0].Tonality!.Value, precision: 10);
        Assert.Equal((int)FeedbackAnswerStatuses.RequireToAnswer, viewModels[0].Status);
        Assert.Equal(0.2, viewModels[1].Tonality!.Value, precision: 10);
        Assert.Null(viewModels[1].Status);
    }
}
