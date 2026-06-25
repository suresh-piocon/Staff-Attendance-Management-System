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

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("mobile")]
        public string? Mobile { get; set; }

        [Column("city")]
        public string? City { get; set; }

        [Column("interested_product")]
        public string? InterestedProduct { get; set; }

        [Column("visit_date")]
        public string VisitDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");

        [Column("assigned_salesman_id")]
        public string? AssignedSalesmanId { get; set; }

        [Column("remarks")]
        public string? Remarks { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Open";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Joined/display fields
        public string AssignedSalesmanName { get; set; } = string.Empty;
    }
}
