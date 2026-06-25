using SalesmanAttendance.Models;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesmanAttendance.Services
{
    public class SalesmanPerformance
    {
        public string SalesmanId { get; set; } = string.Empty;
        public string SalesmanName { get; set; } = string.Empty;
        public int TotalCustomers { get; set; }
        public int ConvertedCustomers { get; set; }
        public int PendingFollowUps { get; set; }
        public double ConversionRate => TotalCustomers > 0
            ? Math.Round((double)ConvertedCustomers / TotalCustomers * 100, 1)
            : 0;
    }

    public class AdminDashboardStats
    {
        public int TotalSalesmen { get; set; }
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }
        public int TodayCustomers { get; set; }
        public int TotalPendingFollowUps { get; set; }
        public List<SalesmanPerformance> SalesmanStats { get; set; } = new();
    }

    public class ReportService
    {
        private readonly Supabase.Client _supabase;

        public ReportService(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<AdminDashboardStats> GetAdminDashboardStatsAsync()
        {
            var stats = new AdminDashboardStats();
            var today = DateTime.Today.ToString("yyyy-MM-dd");

            // Salesmen
            var salesmenResult = await _supabase.From<Salesman>()
                .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
                .Get();
            var salesmen = salesmenResult.Models ?? new List<Salesman>();
            stats.TotalSalesmen = salesmen.Count;

            // Attendance today
            var attendanceResult = await _supabase.From<AttendanceRecord>()
                .Filter("date", Postgrest.Constants.Operator.Equals, today)
                .Get();
            var attendance = attendanceResult.Models ?? new List<AttendanceRecord>();
            stats.PresentToday = attendance.Count(a => a.Status == "Present" || a.Status == "Half Day");
            stats.AbsentToday = attendance.Count(a => a.Status == "Absent" || a.Status == "Leave");

            // Today customers
            var customersResult = await _supabase.From<Customer>()
                .Filter("visit_date", Postgrest.Constants.Operator.Equals, today)
                .Get();
            stats.TodayCustomers = customersResult.Models?.Count ?? 0;

            // Pending follow-ups
            var followUpsResult = await _supabase.From<FollowUp>()
                .Filter("status", Postgrest.Constants.Operator.Equals, "Follow-up Again")
                .Get();
            stats.TotalPendingFollowUps = followUpsResult.Models?.Count ?? 0;

            // Per-salesman stats
            var allCustomers = (await _supabase.From<Customer>().Get()).Models ?? new List<Customer>();
            var allFollowUps = followUpsResult.Models ?? new List<FollowUp>();

            foreach (var s in salesmen)
            {
                var myCustomers = allCustomers.Where(c => c.AssignedSalesmanId == s.Id).ToList();
                stats.SalesmanStats.Add(new SalesmanPerformance
                {
                    SalesmanId = s.Id,
                    SalesmanName = s.Name,
                    TotalCustomers = myCustomers.Count,
                    ConvertedCustomers = myCustomers.Count(c => c.Status == "Converted"),
                    PendingFollowUps = allFollowUps.Count(f => f.SalesmanId == s.Id)
                });
            }

            return stats;
        }
    }
}
