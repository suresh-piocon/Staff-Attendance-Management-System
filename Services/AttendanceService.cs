using SalesmanAttendance.Models;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesmanAttendance.Services
{
    public class AttendanceService
    {
        private readonly Supabase.Client _supabase;

        public AttendanceService(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<List<AttendanceRecord>> GetTodayAttendanceAsync()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var result = await _supabase.From<AttendanceRecord>()
                .Filter("date", Postgrest.Constants.Operator.Equals, today)
                .Get();
            return result.Models ?? new List<AttendanceRecord>();
        }

        public async Task<List<AttendanceRecord>> GetAttendanceByDateAsync(DateTime date)
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var result = await _supabase.From<AttendanceRecord>()
                .Filter("date", Postgrest.Constants.Operator.Equals, dateStr)
                .Get();
            return result.Models ?? new List<AttendanceRecord>();
        }

        public async Task<List<AttendanceRecord>> GetAttendanceBySalesmanAsync(string salesmanId, DateTime from, DateTime to)
        {
            var result = await _supabase.From<AttendanceRecord>()
                .Filter("salesman_id", Postgrest.Constants.Operator.Equals, salesmanId)
                .Filter("date", Postgrest.Constants.Operator.GreaterThanOrEqual, from.ToString("yyyy-MM-dd"))
                .Filter("date", Postgrest.Constants.Operator.LessThanOrEqual, to.ToString("yyyy-MM-dd"))
                .Order("date", Postgrest.Constants.Ordering.Descending)
                .Get();
            return result.Models ?? new List<AttendanceRecord>();
        }

        public async Task<AttendanceRecord?> GetTodayRecordAsync(string salesmanId)
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var result = await _supabase.From<AttendanceRecord>()
                .Filter("salesman_id", Postgrest.Constants.Operator.Equals, salesmanId)
                .Filter("date", Postgrest.Constants.Operator.Equals, today)
                .Get();
            return result.Models?.FirstOrDefault();
        }

        public async Task MarkAttendanceAsync(string salesmanId, string status)
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var existing = await GetTodayRecordAsync(salesmanId);

            if (existing != null)
            {
                await _supabase.From<AttendanceRecord>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, existing.Id)
                    .Set(a => a.Status, status)
                    .Update();
            }
            else
            {
                await _supabase.From<AttendanceRecord>().Insert(new AttendanceRecord
                {
                    SalesmanId = salesmanId,
                    Date = today,
                    Status = status
                });
            }
        }

        public async Task MarkAttendanceForDateAsync(string salesmanId, DateTime date, string status)
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var result = await _supabase.From<AttendanceRecord>()
                .Filter("salesman_id", Postgrest.Constants.Operator.Equals, salesmanId)
                .Filter("date", Postgrest.Constants.Operator.Equals, dateStr)
                .Get();
            var existing = result.Models?.FirstOrDefault();

            if (existing != null)
            {
                await _supabase.From<AttendanceRecord>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, existing.Id)
                    .Set(a => a.Status, status)
                    .Update();
            }
            else
            {
                await _supabase.From<AttendanceRecord>().Insert(new AttendanceRecord
                {
                    SalesmanId = salesmanId,
                    Date = dateStr,
                    Status = status
                });
            }
        }

        public async Task CheckInAsync(string salesmanId)
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var existing = await GetTodayRecordAsync(salesmanId);
            var now = DateTime.UtcNow;

            if (existing != null)
            {
                await _supabase.From<AttendanceRecord>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, existing.Id)
                    .Set(a => a.CheckInTime!, now)
                    .Set(a => a.Status, "Present")
                    .Update();
            }
            else
            {
                await _supabase.From<AttendanceRecord>().Insert(new AttendanceRecord
                {
                    SalesmanId = salesmanId,
                    Date = today,
                    Status = "Present",
                    CheckInTime = now
                });
            }
        }

        public async Task CheckOutAsync(string salesmanId)
        {
            var existing = await GetTodayRecordAsync(salesmanId);
            if (existing != null)
            {
                await _supabase.From<AttendanceRecord>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, existing.Id)
                    .Set(a => a.CheckOutTime!, DateTime.UtcNow)
                    .Update();
            }
        }

        public async Task<Dictionary<string, int>> GetMonthlyStatsByStatusAsync(string salesmanId, int year, int month)
        {
            var from = new DateTime(year, month, 1).ToString("yyyy-MM-dd");
            var to = new DateTime(year, month, DateTime.DaysInMonth(year, month)).ToString("yyyy-MM-dd");

            var result = await _supabase.From<AttendanceRecord>()
                .Filter("salesman_id", Postgrest.Constants.Operator.Equals, salesmanId)
                .Filter("date", Postgrest.Constants.Operator.GreaterThanOrEqual, from)
                .Filter("date", Postgrest.Constants.Operator.LessThanOrEqual, to)
                .Get();

            var records = result.Models ?? new List<AttendanceRecord>();
            return new Dictionary<string, int>
            {
                ["Present"] = records.Count(r => r.Status == "Present"),
                ["Absent"] = records.Count(r => r.Status == "Absent"),
                ["Half Day"] = records.Count(r => r.Status == "Half Day"),
                ["Leave"] = records.Count(r => r.Status == "Leave")
            };
        }
    }
}
