using SalesmanAttendance.Models;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesmanAttendance.Services
{
    public class SalesmanService
    {
        private readonly Supabase.Client _supabase;

        public SalesmanService(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<List<Salesman>> GetAllSalesmenAsync()
        {
            var result = await _supabase.From<Salesman>().Get();
            return result.Models ?? new List<Salesman>();
        }

        public async Task<List<Salesman>> GetActiveSalesmenAsync()
        {
            var result = await _supabase.From<Salesman>()
                .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
                .Order("name", Postgrest.Constants.Ordering.Ascending)
                .Get();
            return result.Models ?? new List<Salesman>();
        }

        public async Task<Salesman?> GetSalesmanByUserIdAsync(string userId)
        {
            var result = await _supabase.From<Salesman>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId)
                .Get();
            return result.Models?.FirstOrDefault();
        }

        public async Task<Salesman> AddSalesmanAsync(Salesman salesman)
        {
            var result = await _supabase.From<Salesman>().Insert(salesman);
            var newSalesman = result.Models!.First();

            // Add to assignment queue
            var queueResult = await _supabase.From<AssignmentQueue>().Get();
            int maxOrder = queueResult.Models?.Any() == true
                ? queueResult.Models.Max(q => q.QueueOrder)
                : 0;

            await _supabase.From<AssignmentQueue>().Insert(new AssignmentQueue
            {
                SalesmanId = newSalesman.Id,
                QueueOrder = maxOrder + 1
            });

            return newSalesman;
        }

        public async Task UpdateSalesmanAsync(Salesman salesman)
        {
            await _supabase.From<Salesman>()
                .Filter("id", Postgrest.Constants.Operator.Equals, salesman.Id)
                .Set(s => s.Name!, salesman.Name)
                .Set(s => s.Mobile!, salesman.Mobile!)
                .Set(s => s.IsActive, salesman.IsActive)
                .Update();
        }

        public async Task ToggleActiveAsync(string salesmanId, bool isActive)
        {
            await _supabase.From<Salesman>()
                .Filter("id", Postgrest.Constants.Operator.Equals, salesmanId)
                .Set(s => s.IsActive, isActive)
                .Update();
        }
    }
}
