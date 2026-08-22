using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeedbackAnalysis.Contracts.Models
{
    [Table("feedbacks")]
    public class FeedbackModel
    {
        [Key]
        [Column("id")]
        public string Id { get; set; } = "";

        [Column("service")]
        public string Service { get; set; } = "";
        [Column("service_id")]
        public string ServiceId { get; set; } = "";


        [Column("rating")]
        public double Rating { get; set; } = 0;
        [Column("sender")]
        public string? Sender { get; set; }
        [Column("text")]
        public string? Text { get; set; }

        [Column("created_date")]
        public DateTime? CreatedDate { get; set; }


        [Column("nomenclature_link")]
        public string? NomenclatureLink { get; set; }
    }
}
