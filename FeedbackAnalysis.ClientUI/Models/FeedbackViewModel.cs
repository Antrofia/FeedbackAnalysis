namespace FeedbackAnalysis.ClientUI.Models
{
    public class FeedbackViewModel
    {
        public string Id { get; set; } = "";

        public string? Sender { get; set; }
        public string? Text { get; set; }
        public double Tonality { get; set; } = 0;

        public DateTime? CreatedDate { get; set; }


        public string? NomenclatureLink { get; set; }
    }
}
