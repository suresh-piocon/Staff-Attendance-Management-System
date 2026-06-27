using Newtonsoft.Json;
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

        [Column("staff_id")]
        public string StaffId { get; set; } = string.Empty;

        [Column("followup_date")]
        public string FollowUpDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");

        [Column("remarks")]
        public string? Remarks { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Follow-up Pending"; // New, Follow-up Pending, Interested, Not Interested, Converted, Sale Closed

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Joined display fields — NOT real DB columns
        [JsonIgnore] public string CustomerName { get; set; } = string.Empty;
        [JsonIgnore] public string StaffName { get; set; } = string.Empty;
        [JsonIgnore] public string CustomerMobile { get; set; } = string.Empty;
    }
}
