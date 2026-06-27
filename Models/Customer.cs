using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace SalesmanAttendance.Models
{
    [Table("customers")]
    public class Customer : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("customer_name")]
        public string CustomerName { get; set; } = string.Empty;

        [Column("mobile")]
        public string? Mobile { get; set; }

        [Column("city")]
        public string? City { get; set; }

        [Column("visit_date")]
        public string VisitDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");

        [Column("assigned_staff_id")]
        public string? AssignedStaffId { get; set; }

        [Column("remarks")]
        public string? Remarks { get; set; }

        [Column("purchase_value")]
        public decimal PurchaseValue { get; set; } = 0;

        [Column("status")]
        public string Status { get; set; } = "New"; // New, Follow-up Pending, Interested, Not Interested, Converted, Sale Closed

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Joined display fields — NOT real DB columns
        [JsonIgnore]
        public string AssignedStaffName { get; set; } = string.Empty;
    }
}
