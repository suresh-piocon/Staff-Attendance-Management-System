using SalesmanAttendance.Models;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesmanAttendance.Services
{
    public class FollowUpService
    {
        private readonly Supabase.Client _supabase;
        private readonly StaffService _staffService;

        public FollowUpService(Supabase.Client supabase, StaffService staffService)
        {
            _supabase = supabase;
            _staffService = staffService;
        }

        public async Task<List<FollowUp>> GetAllFollowUpsAsync()
        {
            var result = await _supabase.From<FollowUp>()
                .Order("followup_date", Postgrest.Constants.Ordering.Descending)
                .Get();
            var list = result.Models ?? new List<FollowUp>();
            await PopulateDetailsAsync(list);
            return list;
        }

        public async Task<List<FollowUp>> GetFollowUpsByStaffAsync(string staffId)
        {
            var result = await _supabase.From<FollowUp>()
                .Filter("staff_id", Postgrest.Constants.Operator.Equals, staffId)
                .Order("followup_date", Postgrest.Constants.Ordering.Descending)
                .Get();
            var list = result.Models ?? new List<FollowUp>();
            await PopulateDetailsAsync(list);
            return list;
        }

        public async Task<List<FollowUp>> GetTodayFollowUpsAsync(string staffId)
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var result = await _supabase.From<FollowUp>()
                .Filter("staff_id", Postgrest.Constants.Operator.Equals, staffId)
                .Filter("followup_date", Postgrest.Constants.Operator.Equals, today)
                .Get();
            var list = result.Models ?? new List<FollowUp>();
            await PopulateDetailsAsync(list);
            return list;
        }

        public async Task<List<FollowUp>> GetPendingFollowUpsAsync()
        {
            var result = await _supabase.From<FollowUp>()
                .Filter("status", Postgrest.Constants.Operator.Equals, "Follow-up Pending")
                .Order("followup_date", Postgrest.Constants.Ordering.Ascending)
                .Get();
            var list = result.Models ?? new List<FollowUp>();
            await PopulateDetailsAsync(list);
            return list;
        }

        public async Task<FollowUp> AddFollowUpAsync(FollowUp followUp)
        {
            var result = await _supabase.From<FollowUp>().Insert(followUp);
            return result.Models!.First();
        }

        public async Task UpdateFollowUpAsync(FollowUp followUp)
        {
            await _supabase.From<FollowUp>()
                .Filter("id", Postgrest.Constants.Operator.Equals, followUp.Id)
                .Set(f => f.Status, followUp.Status)
                .Set(f => f.Remarks!, followUp.Remarks!)
                .Set(f => f.FollowUpDate, followUp.FollowUpDate)
                .Update();
        }

        public async Task<int> GetPendingCountByStaffAsync(string staffId)
        {
            var result = await _supabase.From<FollowUp>()
                .Filter("staff_id", Postgrest.Constants.Operator.Equals, staffId)
                .Filter("status", Postgrest.Constants.Operator.Equals, "Follow-up Pending")
                .Get();
            return result.Models?.Count ?? 0;
        }

        private async Task PopulateDetailsAsync(List<FollowUp> followUps)
        {
            if (!followUps.Any()) return;
            var staffList = await _staffService.GetAllStaffAsync();
            
            // Get customers
            var customersResult = await _supabase.From<Customer>().Get();
            var customers = customersResult.Models ?? new List<Customer>();

            foreach (var f in followUps)
            {
                var s = staffList.FirstOrDefault(x => x.Id == f.StaffId);
                if (s != null) f.StaffName = s.Name;

                var c = customers.FirstOrDefault(x => x.Id == f.CustomerId);
                if (c != null)
                {
                    f.CustomerName = c.CustomerName;
                    f.CustomerMobile = c.Mobile ?? string.Empty;
                }
            }
        }
    }
}
