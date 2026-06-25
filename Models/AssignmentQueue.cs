using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace SalesmanAttendance.Models
{
    [Table("assignment_queue")]
    public class AssignmentQueue : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("salesman_id")]
        public string SalesmanId { get; set; } = string.Empty;

        [Column("queue_order")]
        public int QueueOrder { get; set; }

        [Column("last_assigned_at")]
        public DateTime? LastAssignedAt { get; set; }
    }
}
