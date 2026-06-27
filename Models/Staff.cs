using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace SalesmanAttendance.Models
{
    [Table("staff")]
    public class Staff : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("staff_code")]
        public string StaffCode { get; set; } = string.Empty;

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("mobile")]
        public string? Mobile { get; set; }

        [Column("designation")]
        public string? Designation { get; set; }

        [Column("fingerprint_emp_id")]
        public string? FingerprintEmpId { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("user_id")]
        public string? UserId { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
