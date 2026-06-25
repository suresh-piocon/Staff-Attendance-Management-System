using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace SalesmanAttendance.Models
{
    [Table("followups")]
    public class FollowUp : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("customer_id")]
        public string CustomerId { get; set; } = string.Empty;

        [Column("salesman_id")]
        public string SalesmanId { get; set; } = string.Empty;

        [Column("followup_date")]
        public string FollowUpDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");

        [Column("status")]
        public string? Status { get; set; }

        [Column("remarks")]
        public string? Remarks { get; set; }

        [Column("next_followup_date")]
        public string? NextFollowUpDate { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Display fields
        public string CustomerName { get; set; } = string.Empty;
        public string SalesmanName { get; set; } = string.Empty;
        public string CustomerMobile { get; set; } = string.Empty;
    }
}
