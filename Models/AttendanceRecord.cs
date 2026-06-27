using Newtonsoft.Json;
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

        [Column("staff_id")]
        public string StaffId { get; set; } = string.Empty;

        [Column("attendance_date")]
        public string AttendanceDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");

        [Column("morning_in")]
        public DateTime? MorningIn { get; set; }

        [Column("morning_out")]
        public DateTime? MorningOut { get; set; }

        [Column("afternoon_in")]
        public DateTime? AfternoonIn { get; set; }

        [Column("evening_out")]
        public DateTime? EveningOut { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Absent";

        [Column("total_hours")]
        public decimal TotalHours { get; set; } = 0;

        // Joined display fields — NOT real DB columns, excluded from INSERT/UPDATE
        [JsonIgnore]
        public string StaffName { get; set; } = string.Empty;

        [JsonIgnore]
        public string StaffCode { get; set; } = string.Empty;
    }
}
