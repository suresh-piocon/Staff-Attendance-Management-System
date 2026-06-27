using SalesmanAttendance.Models;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesmanAttendance.Services
{
    public class RoundRobinService
    {
        private readonly Supabase.Client _supabase;

        public RoundRobinService(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<Staff?> AssignNextStaffAsync()
        {
            // 1. Get all active staff, sorted by StaffCode
            var activeStaffResult = await _supabase.From<Staff>()
                .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
                .Order("staff_code", Postgrest.Constants.Ordering.Ascending)
                .Get();
            var activeStaff = activeStaffResult.Models ?? new List<Staff>();
            if (!activeStaff.Any()) return null;

            // 2. Get today's attendance records to see who is absent
            var todayStr = DateTime.Today.ToString("yyyy-MM-dd");
            var attendanceResult = await _supabase.From<AttendanceRecord>()
                .Filter("attendance_date", Postgrest.Constants.Operator.Equals, todayStr)
                .Get();
            var todayAttendance = attendanceResult.Models ?? new List<AttendanceRecord>();

            // Identify absent staff IDs
            var absentStaffIds = todayAttendance
                .Where(a => a.Status == "Absent")
                .Select(a => a.StaffId)
                .ToHashSet();

            // 3. Filter to available (active and not absent) staff
            var availableStaff = activeStaff
                .Where(s => !absentStaffIds.Contains(s.Id))
                .ToList();
            if (!availableStaff.Any()) return null;

            // 4. Get the Round Robin Tracker
            var trackerResult = await _supabase.From<RoundRobinTracker>().Get();
            var tracker = trackerResult.Models?.FirstOrDefault();
            if (tracker == null)
            {
                // Create one if it doesn't exist
                tracker = new RoundRobinTracker();
                await _supabase.From<RoundRobinTracker>().Insert(tracker);
            }

            // 5. Select the next staff
            Staff assignedStaff;
            if (string.IsNullOrEmpty(tracker.LastAssignedStaffId))
            {
                assignedStaff = availableStaff.First();
            }
            else
            {
                // Find index of last assigned staff in current available list
                int index = availableStaff.FindIndex(s => s.Id == tracker.LastAssignedStaffId);
                if (index == -1)
                {
                    // Last assigned is not available today (e.g. absent or inactive),
                    // find where they would be in the full active list to continue rotation
                    int fullIndex = activeStaff.FindIndex(s => s.Id == tracker.LastAssignedStaffId);
                    if (fullIndex == -1)
                    {
                        assignedStaff = availableStaff.First();
                    }
                    else
                    {
                        // Start from that position and find the next available staff
                        Staff? nextStaff = null;
                        for (int i = 1; i <= activeStaff.Count; i++)
                        {
                            var candidate = activeStaff[(fullIndex + i) % activeStaff.Count];
                            if (availableStaff.Any(s => s.Id == candidate.Id))
                            {
                                nextStaff = candidate;
                                break;
                            }
                        }
                        assignedStaff = nextStaff ?? availableStaff.First();
                    }
                }
                else
                {
                    // Move to the next available staff in rotation
                    assignedStaff = availableStaff[(index + 1) % availableStaff.Count];
                }
            }

            // 6. Update tracker
            await _supabase.From<RoundRobinTracker>()
                .Filter("id", Postgrest.Constants.Operator.Equals, tracker.Id)
                .Set(t => t.LastAssignedStaffId!, assignedStaff.Id)
                .Set(t => t.UpdatedDate, DateTime.UtcNow)
                .Update();

            return assignedStaff;
        }

        public async Task<RoundRobinTracker?> GetTrackerAsync()
        {
            var result = await _supabase.From<RoundRobinTracker>().Get();
            return result.Models?.FirstOrDefault();
        }
    }
}
