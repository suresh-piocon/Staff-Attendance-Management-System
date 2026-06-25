using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace SalesmanAttendance.Models
{
    [Table("attendance")]
    public class AttendanceRecord : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("salesman_id")]
        public string SalesmanId { get; set; } = string.Empty;

        [Column("date")]
        public string Date { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");

        [Column("check_in_time")]
        public DateTime? CheckInTime { get; set; }

        [Column("check_out_time")]
        public DateTime? CheckOutTime { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Absent";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Joined data (not mapped to column)
        public string SalesmanName { get; set; } = string.Empty;
    }
}
