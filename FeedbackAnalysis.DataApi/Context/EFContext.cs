using Microsoft.EntityFrameworkCore;

namespace FeedbackAnalysis.DataApi.Context
{
    public class EFContext : DbContext
    {
        public EFContext(DbContextOptions options) : base(options)
        {

        }
    }
}
