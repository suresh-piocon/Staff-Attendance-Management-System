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

        public FollowUpService(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<List<FollowUp>> GetAllFollowUpsAsync()
        {
            var result = await _supabase.From<FollowUp>()
                .Order("followup_date", Postgrest.Constants.Ordering.Descending)
                .Get();
            return result.Models ?? new List<FollowUp>();
        }

        public async Task<List<FollowUp>> GetFollowUpsBySalesmanAsync(string salesmanId)
        {
            var result = await _supabase.From<FollowUp>()
                .Filter("salesman_id", Postgrest.Constants.Operator.Equals, salesmanId)
                .Order("followup_date", Postgrest.Constants.Ordering.Descending)
                .Get();
            return result.Models ?? new List<FollowUp>();
        }

        public async Task<List<FollowUp>> GetTodayFollowUpsAsync(string salesmanId)
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var result = await _supabase.From<FollowUp>()
                .Filter("salesman_id", Postgrest.Constants.Operator.Equals, salesmanId)
                .Filter("next_followup_date", Postgrest.Constants.Operator.Equals, today)
                .Get();
            return result.Models ?? new List<FollowUp>();
        }

        public async Task<List<FollowUp>> GetPendingFollowUpsAsync()
        {
            var result = await _supabase.From<FollowUp>()
                .Filter("status", Postgrest.Constants.Operator.Equals, "Follow-up Again")
                .Order("next_followup_date", Postgrest.Constants.Ordering.Ascending)
                .Get();
            return result.Models ?? new List<FollowUp>();
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
                .Set(f => f.Status!, followUp.Status!)
                .Set(f => f.Remarks!, followUp.Remarks!)
                .Set(f => f.NextFollowUpDate!, followUp.NextFollowUpDate!)
                .Update();
        }

        public async Task<int> GetPendingCountBySalesmanAsync(string salesmanId)
        {
            var result = await _supabase.From<FollowUp>()
                .Filter("salesman_id", Postgrest.Constants.Operator.Equals, salesmanId)
                .Filter("status", Postgrest.Constants.Operator.Equals, "Follow-up Again")
                .Get();
            return result.Models?.Count ?? 0;
        }
    }
}
