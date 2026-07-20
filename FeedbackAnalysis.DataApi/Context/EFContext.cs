using FeedbackAnalysis.DataApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FeedbackAnalysis.DataApi.Context
{
    public class EFContext : DbContext
    {
        public DbSet<FeedbackModel> Feedbacks { get; set; }
        public DbSet<FeedbackTonalityModel> FeedbacksTonality { get; set; }
        public DbSet<FeedbackAnswerStatusModel> FeedbacksAnswerStatus { get; set; }

        public EFContext(DbContextOptions options) : base(options)
        {
            Database.EnsureCreated();
        }
    }
}
