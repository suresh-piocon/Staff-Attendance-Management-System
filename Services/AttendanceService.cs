using SalesmanAttendance.Models;
using Supabase;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SalesmanAttendance.Services
{
    public class AttendanceService
    {
        private readonly Supabase.Client _supabase;
        private readonly StaffService _staffService;

        public AttendanceService(Supabase.Client supabase, StaffService staffService)
        {
            _supabase = supabase;
            _staffService = staffService;
        }

        public async Task<List<AttendanceRecord>> GetAttendanceByDateAsync(DateTime date)
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var result = await _supabase.From<AttendanceRecord>()
                .Filter("attendance_date", Postgrest.Constants.Operator.Equals, dateStr)
                .Get();
            
            var records = result.Models ?? new List<AttendanceRecord>();
            var allStaff = await _staffService.GetActiveStaffAsync();

            // Map Staff details
            foreach (var r in records)
            {
                var s = allStaff.FirstOrDefault(x => x.Id == r.StaffId);
                if (s != null)
                {
                    r.StaffName = s.Name;
                    r.StaffCode = s.StaffCode;
                }
            }

            return records;
        }

        public async Task<List<AttendanceRecord>> GetAttendanceByStaffAsync(string staffId, DateTime from, DateTime to)
        {
            var result = await _supabase.From<AttendanceRecord>()
                .Filter("staff_id", Postgrest.Constants.Operator.Equals, staffId)
                .Filter("attendance_date", Postgrest.Constants.Operator.GreaterThanOrEqual, from.ToString("yyyy-MM-dd"))
                .Filter("attendance_date", Postgrest.Constants.Operator.LessThanOrEqual, to.ToString("yyyy-MM-dd"))
                .Order("attendance_date", Postgrest.Constants.Ordering.Descending)
                .Get();
            return result.Models ?? new List<AttendanceRecord>();
        }

        public async Task<AttendanceRecord?> GetRecordByStaffAndDateAsync(string staffId, DateTime date)
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var result = await _supabase.From<AttendanceRecord>()
                .Filter("staff_id", Postgrest.Constants.Operator.Equals, staffId)
                .Filter("attendance_date", Postgrest.Constants.Operator.Equals, dateStr)
                .Get();
            return result.Models?.FirstOrDefault();
        }

        public async Task SaveAttendanceRecordAsync(AttendanceRecord record)
        {
            // Auto calculate status and total hours before saving
            var (status, hours) = CalculateAttendanceStatus(
                record.MorningIn, record.MorningOut, record.AfternoonIn, record.EveningOut);
            
            record.Status = status;
            record.TotalHours = hours;

            // Check if record exists
            var existing = await GetRecordByStaffAndDateAsync(record.StaffId, DateTime.Parse(record.AttendanceDate));
            if (existing != null)
            {
                await _supabase.From<AttendanceRecord>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, existing.Id)
                    .Set(a => a.MorningIn!, record.MorningIn)
                    .Set(a => a.MorningOut!, record.MorningOut)
                    .Set(a => a.AfternoonIn!, record.AfternoonIn)
                    .Set(a => a.EveningOut!, record.EveningOut)
                    .Set(a => a.Status, record.Status)
                    .Set(a => a.TotalHours, record.TotalHours)
                    .Update();
            }
            else
            {
                await _supabase.From<AttendanceRecord>().Insert(record);
            }
        }

        public async Task MarkAttendanceForDateAsync(
            string staffId, DateTime date, 
            DateTime? morningIn, DateTime? morningOut, 
            DateTime? afternoonIn, DateTime? eveningOut)
        {
            var record = new AttendanceRecord
            {
                StaffId = staffId,
                AttendanceDate = date.ToString("yyyy-MM-dd"),
                MorningIn = morningIn,
                MorningOut = morningOut,
                AfternoonIn = afternoonIn,
                EveningOut = eveningOut
            };

            await SaveAttendanceRecordAsync(record);
        }

        // CSV Import Logic (Option 2)
        public async Task<int> ImportAttendanceCsvAsync(string csvContent)
        {
            var allStaff = await _staffService.GetAllStaffAsync();
            int importedCount = 0;

            using (var reader = new StringReader(csvContent))
            {
                string? header = await reader.ReadLineAsync(); // skip header
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    var parts = line.Split(',');
                    if (parts.Length < 2) continue;

                    string staffCodeOrFingerprintId = parts[0].Trim();
                    string dateStr = parts[1].Trim();

                    // Find staff by Code or Fingerprint Device ID
                    var staff = allStaff.FirstOrDefault(s => 
                        s.StaffCode.Equals(staffCodeOrFingerprintId, StringComparison.OrdinalIgnoreCase) || 
                        (s.FingerprintEmpId ?? "").Equals(staffCodeOrFingerprintId, StringComparison.OrdinalIgnoreCase));
                    
                    if (staff == null) continue;

                    if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                    {
                        continue; // Invalid date format
                    }

                    DateTime? morningIn = ParseDateTime(date, parts.Length > 2 ? parts[2] : null);
                    DateTime? morningOut = ParseDateTime(date, parts.Length > 3 ? parts[3] : null);
                    DateTime? afternoonIn = ParseDateTime(date, parts.Length > 4 ? parts[4] : null);
                    DateTime? eveningOut = ParseDateTime(date, parts.Length > 5 ? parts[5] : null);

                    await MarkAttendanceForDateAsync(staff.Id, date, morningIn, morningOut, afternoonIn, eveningOut);
                    importedCount++;
                }
            }

            return importedCount;
        }

        private DateTime? ParseDateTime(DateTime date, string? timeStr)
        {
            if (string.IsNullOrWhiteSpace(timeStr) || timeStr.Trim() == "--" || timeStr.Trim() == "-") 
                return null;

            string cleanTime = timeStr.Trim();
            string[] formats = { "hh:mm tt", "HH:mm", "hh:mm:ss tt", "HH:mm:ss" };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(cleanTime, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime time))
                {
                    // Combine date and time (parsed as local time, then convert to UTC)
                    var localDateTime = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, DateTimeKind.Local);
                    return localDateTime.ToUniversalTime();
                }
            }

            // Fallback to general parse
            if (DateTime.TryParse(cleanTime, out DateTime parsedTime))
            {
                var localDateTime = new DateTime(date.Year, date.Month, date.Day, parsedTime.Hour, parsedTime.Minute, parsedTime.Second, DateTimeKind.Local);
                return localDateTime.ToUniversalTime();
            }

            return null;
        }

        // Status calculation logic
        public static (string Status, decimal TotalHours) CalculateAttendanceStatus(
            DateTime? morningIn, DateTime? morningOut, DateTime? afternoonIn, DateTime? eveningOut)
        {
            if (morningIn == null && morningOut == null && afternoonIn == null && eveningOut == null)
            {
                return ("Absent", 0);
            }

            decimal totalHours = 0;
            bool morningComplete = false;
            bool afternoonComplete = false;

            if (morningIn != null && morningOut != null)
            {
                var diff = morningOut.Value - morningIn.Value;
                if (diff.TotalHours > 0)
                    totalHours += (decimal)diff.TotalHours;
                morningComplete = true;
            }

            if (afternoonIn != null && eveningOut != null)
            {
                var diff = eveningOut.Value - afternoonIn.Value;
                if (diff.TotalHours > 0)
                    totalHours += (decimal)diff.TotalHours;
                afternoonComplete = true;
            }

            // If we have some partial entries but no complete session yet
            if (!morningComplete && !afternoonComplete)
            {
                return ("Half Day", totalHours);
            }

            if (morningComplete && afternoonComplete)
            {
                // Check late arrival
                bool isLate = false;
                if (morningIn != null)
                {
                    var localIn = morningIn.Value.ToLocalTime();
                    if (localIn.TimeOfDay > new TimeSpan(9, 0, 0)) isLate = true;
                }
                if (afternoonIn != null)
                {
                    var localIn = afternoonIn.Value.ToLocalTime();
                    if (localIn.TimeOfDay > new TimeSpan(14, 0, 0)) isLate = true;
                }

                // Check early leaving
                bool isEarly = false;
                if (morningOut != null)
                {
                    var localOut = morningOut.Value.ToLocalTime();
                    if (localOut.TimeOfDay < new TimeSpan(13, 30, 0)) isEarly = true;
                }
                if (eveningOut != null)
                {
                    var localOut = eveningOut.Value.ToLocalTime();
                    if (localOut.TimeOfDay < new TimeSpan(19, 0, 0)) isEarly = true;
                }

                if (isLate) return ("Late", Math.Round(totalHours, 2));
                if (isEarly) return ("Early Out", Math.Round(totalHours, 2));
                return ("Present", Math.Round(totalHours, 2));
            }
            else
            {
                // Exactly one session completed (either morning or afternoon)
                return ("Half Day", Math.Round(totalHours, 2));
            }
        }

        public async Task CheckInAsync(string staffId)
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var record = await GetRecordByStaffAndDateAsync(staffId, DateTime.Today);
            if (record == null)
            {
                record = new AttendanceRecord
                {
                    StaffId = staffId,
                    AttendanceDate = today,
                    Status = "Absent",
                    TotalHours = 0
                };
            }

            var now = DateTime.UtcNow;
            var localTime = now.ToLocalTime();

            // Auto-assign to MorningIn or AfternoonIn based on time
            if (localTime.Hour < 14 && record.MorningIn == null)
            {
                record.MorningIn = now;
            }
            else if (record.AfternoonIn == null)
            {
                record.AfternoonIn = now;
            }

            await SaveAttendanceRecordAsync(record);
        }

        public async Task CheckOutAsync(string staffId)
        {
            var record = await GetRecordByStaffAndDateAsync(staffId, DateTime.Today);
            if (record == null) return;

            var now = DateTime.UtcNow;
            var localTime = now.ToLocalTime();

            // Auto-assign to MorningOut or EveningOut based on time and check-in status
            if (record.MorningIn != null && record.MorningOut == null && localTime.Hour < 14)
            {
                record.MorningOut = now;
            }
            else if (record.AfternoonIn != null && record.EveningOut == null)
            {
                record.EveningOut = now;
            }

            await SaveAttendanceRecordAsync(record);
        }

        public async Task<(string StaffName, string PunchType)> RecordFingerprintPunchAsync(string fingerprintEmpId, DateTime punchTime)
        {
            // 1. Find the staff member
            var staffResult = await _supabase.From<Staff>()
                .Filter("fingerprint_emp_id", Postgrest.Constants.Operator.Equals, fingerprintEmpId)
                .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
                .Get();
            var staff = staffResult.Models?.FirstOrDefault();
            if (staff == null)
            {
                throw new Exception("Fingerprint ID not recognized or inactive.");
            }

            var todayStr = DateTime.Today.ToString("yyyy-MM-dd");
            var record = await GetRecordByStaffAndDateAsync(staff.Id, DateTime.Today);
            if (record == null)
            {
                record = new AttendanceRecord
                {
                    StaffId = staff.Id,
                    AttendanceDate = todayStr,
                    Status = "Absent",
                    TotalHours = 0
                };
            }

            // Convert local punch time to UTC
            var utcTime = punchTime.ToUniversalTime();
            string punchType = string.Empty;

            // 2. Assign punch to the next empty slot in sequence
            if (record.MorningIn == null)
            {
                record.MorningIn = utcTime;
                punchType = "Morning In";
            }
            else if (record.MorningOut == null)
            {
                record.MorningOut = utcTime;
                punchType = "Morning Out";
            }
            else if (record.AfternoonIn == null)
            {
                record.AfternoonIn = utcTime;
                punchType = "Afternoon In";
            }
            else if (record.EveningOut == null)
            {
                record.EveningOut = utcTime;
                punchType = "Evening Out";
            }
            else
            {
                throw new Exception("All 4 punches for today are already recorded.");
            }

            // 3. Save and calculate status
            await SaveAttendanceRecordAsync(record);
            return (staff.Name, punchType);
        }
    }
}
