using SalesmanAttendance.Models;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesmanAttendance.Services
{
    public class StaffPerformance
    {
        public string StaffId { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public int TotalCustomers { get; set; }
        public int ConvertedCustomers { get; set; }
        public int PendingFollowUps { get; set; }
        public decimal TotalSales { get; set; }
        public double ConversionRate => TotalCustomers > 0
            ? Math.Round((double)ConvertedCustomers / TotalCustomers * 100, 1)
            : 0;
    }

    public class AdminDashboardStats
    {
        public int TotalStaff { get; set; }
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }
        public int TodayCustomers { get; set; }
        public int TotalPendingFollowUps { get; set; }
        public List<StaffPerformance> StaffStats { get; set; } = new();
    }

    public class AttendanceSummaryItem
    {
        public string StaffName { get; set; } = string.Empty;
        public string StaffCode { get; set; } = string.Empty;
        public int Present { get; set; }
        public int HalfDay { get; set; }
        public int Absent { get; set; }
        public int Late { get; set; }
        public int EarlyOut { get; set; }
        public decimal TotalHours { get; set; }
        public int WorkingDays => Present + HalfDay + Late + EarlyOut;
    }

    public class DailyAllocationItem
    {
        public string StaffName { get; set; } = string.Empty;
        public string StaffCode { get; set; } = string.Empty;
        public int CustomersAssigned { get; set; }
    }

    public class ReportService
    {
        private readonly Supabase.Client _supabase;
        private readonly StaffService _staffService;

        public ReportService(Supabase.Client supabase, StaffService staffService)
        {
            _supabase = supabase;
            _staffService = staffService;
        }

        public async Task<AdminDashboardStats> GetAdminDashboardStatsAsync()
        {
            var stats = new AdminDashboardStats();
            var today = DateTime.Today.ToString("yyyy-MM-dd");

            // Staff
            var staffList = await _staffService.GetActiveStaffAsync();
            stats.TotalStaff = staffList.Count;

            // Attendance today
            var attendanceResult = await _supabase.From<AttendanceRecord>()
                .Filter("attendance_date", Postgrest.Constants.Operator.Equals, today)
                .Get();
            var attendance = attendanceResult.Models ?? new List<AttendanceRecord>();
            
            stats.PresentToday = attendance.Count(a => a.Status == "Present" || a.Status == "Late" || a.Status == "Early Out" || a.Status == "Half Day");
            stats.AbsentToday = staffList.Count - stats.PresentToday;

            // Today customers
            var customersResult = await _supabase.From<Customer>()
                .Filter("visit_date", Postgrest.Constants.Operator.Equals, today)
                .Get();
            stats.TodayCustomers = customersResult.Models?.Count ?? 0;

            // Pending follow-ups
            var followUpsResult = await _supabase.From<FollowUp>()
                .Filter("status", Postgrest.Constants.Operator.Equals, "Follow-up Pending")
                .Get();
            stats.TotalPendingFollowUps = followUpsResult.Models?.Count ?? 0;

            // Per-staff stats
            var allCustomers = (await _supabase.From<Customer>().Get()).Models ?? new List<Customer>();
            var allFollowUps = followUpsResult.Models ?? new List<FollowUp>();

            foreach (var s in staffList)
            {
                var myCustomers = allCustomers.Where(c => c.AssignedStaffId == s.Id).ToList();
                stats.StaffStats.Add(new StaffPerformance
                {
                    StaffId = s.Id,
                    StaffName = s.Name,
                    TotalCustomers = myCustomers.Count,
                    ConvertedCustomers = myCustomers.Count(c => c.Status == "Converted" || c.Status == "Sale Closed"),
                    PendingFollowUps = allFollowUps.Count(f => f.StaffId == s.Id),
                    TotalSales = myCustomers.Sum(c => c.PurchaseValue)
                });
            }

            return stats;
        }

        // Attendance Report: Summary for a date range
        public async Task<List<AttendanceSummaryItem>> GetAttendanceSummaryAsync(DateTime from, DateTime to)
        {
            var staffList = await _staffService.GetAllStaffAsync();
            var result = new List<AttendanceSummaryItem>();

            var fromStr = from.ToString("yyyy-MM-dd");
            var toStr = to.ToString("yyyy-MM-dd");

            var attendanceResult = await _supabase.From<AttendanceRecord>()
                .Filter("attendance_date", Postgrest.Constants.Operator.GreaterThanOrEqual, fromStr)
                .Filter("attendance_date", Postgrest.Constants.Operator.LessThanOrEqual, toStr)
                .Get();
            var attendance = attendanceResult.Models ?? new List<AttendanceRecord>();

            // Calculate total working days in date range (excluding Sundays for simplicity, or just calculate total days)
            int totalDays = (to - from).Days + 1;

            foreach (var s in staffList)
            {
                var myAttendance = attendance.Where(a => a.StaffId == s.Id).ToList();
                int presentCount = myAttendance.Count(a => a.Status == "Present");
                int halfDayCount = myAttendance.Count(a => a.Status == "Half Day");
                int lateCount = myAttendance.Count(a => a.Status == "Late");
                int earlyOutCount = myAttendance.Count(a => a.Status == "Early Out");
                
                // Absent count = total days - marked working days
                int markedDays = presentCount + halfDayCount + lateCount + earlyOutCount;
                int explicitAbsents = myAttendance.Count(a => a.Status == "Absent");
                int absentCount = explicitAbsents + Math.Max(0, totalDays - myAttendance.Count);

                result.Add(new AttendanceSummaryItem
                {
                    StaffName = s.Name,
                    StaffCode = s.StaffCode,
                    Present = presentCount + lateCount + earlyOutCount, // combined full days
                    HalfDay = halfDayCount,
                    Absent = absentCount,
                    Late = lateCount,
                    EarlyOut = earlyOutCount,
                    TotalHours = myAttendance.Sum(a => a.TotalHours)
                });
            }

            return result;
        }

        // Round Robin: Daily Allocation Report
        public async Task<List<DailyAllocationItem>> GetDailyAllocationReportAsync(DateTime date)
        {
            var staffList = await _staffService.GetActiveStaffAsync();
            var dateStr = date.ToString("yyyy-MM-dd");

            var customersResult = await _supabase.From<Customer>()
                .Filter("visit_date", Postgrest.Constants.Operator.Equals, dateStr)
                .Get();
            var customers = customersResult.Models ?? new List<Customer>();

            return staffList.Select(s => new DailyAllocationItem
            {
                StaffName = s.Name,
                StaffCode = s.StaffCode,
                CustomersAssigned = customers.Count(c => c.AssignedStaffId == s.Id)
            }).ToList();
        }

        // Round Robin: Monthly Performance
        public async Task<List<StaffPerformance>> GetMonthlyPerformanceReportAsync(int year, int month)
        {
            var staffList = await _staffService.GetAllStaffAsync();
            var fromDate = new DateTime(year, month, 1).ToString("yyyy-MM-dd");
            var toDate = new DateTime(year, month, DateTime.DaysInMonth(year, month)).ToString("yyyy-MM-dd");

            var customersResult = await _supabase.From<Customer>()
                .Filter("visit_date", Postgrest.Constants.Operator.GreaterThanOrEqual, fromDate)
                .Filter("visit_date", Postgrest.Constants.Operator.LessThanOrEqual, toDate)
                .Get();
            var customers = customersResult.Models ?? new List<Customer>();

            var followUpsResult = await _supabase.From<FollowUp>()
                .Filter("status", Postgrest.Constants.Operator.Equals, "Follow-up Pending")
                .Get();
            var followUps = followUpsResult.Models ?? new List<FollowUp>();

            return staffList.Select(s =>
            {
                var myCustomers = customers.Where(c => c.AssignedStaffId == s.Id).ToList();
                return new StaffPerformance
                {
                    StaffId = s.Id,
                    StaffName = s.Name,
                    TotalCustomers = myCustomers.Count,
                    ConvertedCustomers = myCustomers.Count(c => c.Status == "Converted" || c.Status == "Sale Closed"),
                    PendingFollowUps = followUps.Count(f => f.StaffId == s.Id),
                    TotalSales = myCustomers.Sum(c => c.PurchaseValue)
                };
            }).ToList();
        }
    }
}
