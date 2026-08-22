using FeedbackAnalysis.Contracts.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeedbackAnalysis.DataApi.Models
{
    [Table("feedbacks_answer_status")]
    public class FeedbackAnswerStatusModel
    {
        [Key]
        [Column("feedback_id")]
        public string FeedbackId { get; set; } = "";

        [Column("status_id")]
        public int StatusId { get; set; }


        [ForeignKey(nameof(FeedbackId))]
        public FeedbackModel Feedback { get; set; }
    }
}
