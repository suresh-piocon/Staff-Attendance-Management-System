using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace SalesmanAttendance.Models
{
    [Table("salesmen")]
    public class Salesman : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("mobile")]
        public string? Mobile { get; set; }

        [Column("joining_date")]
        public string? JoiningDate { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("user_id")]
        public string? UserId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
