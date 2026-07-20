using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeedbackAnalysis.DataApi.Models
{
    [Table("feedbacks_tonality")]
    public class FeedbackTonalityModel
    {
        [Key]
        [Column("feedback_id")]
        public string FeedbackId { get; set; } = "";

        [Column("tonality")]
        public double Tonality { get; set; }


        [ForeignKey(nameof(FeedbackId))]
        public FeedbackModel Feedback { get; set; }
    }
}
