namespace FeedbackAnalysis.WBService.Models
{
    public class FeedbackModel
    {
        public string Id { get; set; } = "";

        public string Service { get; set; } = "";
        public string ServiceId { get; set; } = "";


        public double Rating { get; set; } = 0;
        public string? Text { get; set; }

        public DateTime? CreatedDate { get; set; }


        public string? NomenclatureLink { get; set; }
    }
}
