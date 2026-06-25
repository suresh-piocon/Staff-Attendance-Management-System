using SalesmanAttendance.Models;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesmanAttendance.Services
{
    public class AssignmentService
    {
        private readonly Supabase.Client _supabase;

        public AssignmentService(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        /// <summary>
        /// Round-Robin: picks next available (present today) salesman,
        /// skips absent/leave salesmen, rotates the queue.
        /// </summary>
        public async Task<Salesman?> AssignNextSalesmanAsync()
        {
            // Get today's absent/leave salesmen
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var attendanceResult = await _supabase.From<AttendanceRecord>()
                .Filter("date", Postgrest.Constants.Operator.Equals, today)
                .Get();
            var attendance = attendanceResult.Models ?? new List<AttendanceRecord>();

            var absentIds = attendance
                .Where(a => a.Status == "Absent" || a.Status == "Leave")
                .Select(a => a.SalesmanId)
                .ToHashSet();

            // Get active salesmen
            var salesmenResult = await _supabase.From<Salesman>()
                .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
                .Get();
            var activeSalesmen = salesmenResult.Models ?? new List<Salesman>();

            // Get queue ordered
            var queueResult = await _supabase.From<AssignmentQueue>()
                .Order("queue_order", Postgrest.Constants.Ordering.Ascending)
                .Get();
            var queue = queueResult.Models ?? new List<AssignmentQueue>();

            // Find next available salesman
            AssignmentQueue? assignedQueueItem = null;
            Salesman? assignedSalesman = null;

            foreach (var queueItem in queue)
            {
                var salesman = activeSalesmen.FirstOrDefault(s => s.Id == queueItem.SalesmanId);
                if (salesman == null) continue;
                if (absentIds.Contains(queueItem.SalesmanId)) continue;

                assignedQueueItem = queueItem;
                assignedSalesman = salesman;
                break;
            }

            if (assignedSalesman == null || assignedQueueItem == null)
                return null;

            // Rotate: move assigned salesman to end of queue
            int maxOrder = queue.Any() ? queue.Max(q => q.QueueOrder) : 0;
            await _supabase.From<AssignmentQueue>()
                .Filter("id", Postgrest.Constants.Operator.Equals, assignedQueueItem.Id)
                .Set(q => q.QueueOrder, maxOrder + 1)
                .Set(q => q.LastAssignedAt!, DateTime.UtcNow)
                .Update();

            return assignedSalesman;
        }
    }
}
