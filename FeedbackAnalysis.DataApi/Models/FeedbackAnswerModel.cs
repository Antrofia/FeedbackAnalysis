using FeedbackAnalysis.Contracts.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeedbackAnalysis.DataApi.Models
{
    [Table("feedbacks_answers")]
    public class FeedbackAnswerModel
    {
        [Key]
        [Column("feedback_id")]
        public string FeedbackId { get; set; } = "";

        [Column("sender")]
        public string Sender { get; set; } = "";

        [Column("text")]
        public string Text { get; set; } = "";

        [Column("created_date")]
        public DateTime CreatedDate { get; set; }


        [ForeignKey(nameof(FeedbackId))]
        public FeedbackModel Feedback { get; set; } = null!;
    }
}
