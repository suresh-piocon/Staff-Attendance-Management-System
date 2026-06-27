using SalesmanAttendance.Models;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesmanAttendance.Services
{
    public class StaffService
    {
        private readonly Supabase.Client _supabase;

        public StaffService(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<List<Staff>> GetAllStaffAsync()
        {
            var result = await _supabase.From<Staff>().Get();
            return result.Models ?? new List<Staff>();
        }

        public async Task<List<Staff>> GetActiveStaffAsync()
        {
            var result = await _supabase.From<Staff>()
                .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
                .Order("name", Postgrest.Constants.Ordering.Ascending)
                .Get();
            return result.Models ?? new List<Staff>();
        }

        public async Task<Staff?> GetStaffByUserIdAsync(string userId)
        {
            var result = await _supabase.From<Staff>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId)
                .Get();
            return result.Models?.FirstOrDefault();
        }

        public async Task<Staff> AddStaffAsync(Staff staff)
        {
            var result = await _supabase.From<Staff>().Insert(staff);
            return result.Models!.First();
        }

        public async Task UpdateStaffAsync(Staff staff)
        {
            await _supabase.From<Staff>().Update(staff);
        }

        public async Task ToggleActiveAsync(string staffId, bool isActive)
        {
            await _supabase.From<Staff>()
                .Filter("id", Postgrest.Constants.Operator.Equals, staffId)
                .Set(s => s.IsActive, isActive)
                .Update();
        }
    }
}
