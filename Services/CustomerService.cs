using SalesmanAttendance.Models;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesmanAttendance.Services
{
    public class CustomerService
    {
        private readonly Supabase.Client _supabase;
        private readonly RoundRobinService _roundRobinService;
        private readonly StaffService _staffService;

        public CustomerService(Supabase.Client supabase, RoundRobinService roundRobinService, StaffService staffService)
        {
            _supabase = supabase;
            _roundRobinService = roundRobinService;
            _staffService = staffService;
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var result = await _supabase.From<Customer>()
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();
            var list = result.Models ?? new List<Customer>();
            await PopulateStaffNamesAsync(list);
            return list;
        }

        public async Task<List<Customer>> GetCustomersByStaffAsync(string staffId)
        {
            var result = await _supabase.From<Customer>()
                .Filter("assigned_staff_id", Postgrest.Constants.Operator.Equals, staffId)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();
            var list = result.Models ?? new List<Customer>();
            await PopulateStaffNamesAsync(list);
            return list;
        }

        public async Task<List<Customer>> GetCustomersByDateAsync(DateTime date)
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var result = await _supabase.From<Customer>()
                .Filter("visit_date", Postgrest.Constants.Operator.Equals, dateStr)
                .Order("created_at", Postgrest.Constants.Ordering.Ascending)
                .Get();
            var list = result.Models ?? new List<Customer>();
            await PopulateStaffNamesAsync(list);
            return list;
        }

        public async Task<Customer> AddCustomerAsync(Customer customer)
        {
            // Auto-assign salesman via round robin
            var staff = await _roundRobinService.AssignNextStaffAsync();
            if (staff != null)
            {
                customer.AssignedStaffId = staff.Id;
                customer.AssignedStaffName = staff.Name;
            }

            var result = await _supabase.From<Customer>().Insert(customer);
            return result.Models!.First();
        }

        public async Task UpdateCustomerStatusAsync(string customerId, string status)
        {
            var result = await _supabase.From<Customer>().Filter("id", Postgrest.Constants.Operator.Equals, customerId).Get();
            var customer = result.Models?.FirstOrDefault();
            if (customer != null)
            {
                customer.Status = status;
                await _supabase.From<Customer>().Update(customer);
            }
        }

        public async Task UpdateCustomerPurchaseValueAsync(string customerId, decimal purchaseValue, string status)
        {
            var result = await _supabase.From<Customer>().Filter("id", Postgrest.Constants.Operator.Equals, customerId).Get();
            var customer = result.Models?.FirstOrDefault();
            if (customer != null)
            {
                customer.PurchaseValue = purchaseValue;
                customer.Status = status;
                await _supabase.From<Customer>().Update(customer);
            }
        }

        public async Task<int> GetTotalCustomerCountAsync()
        {
            var result = await _supabase.From<Customer>().Get();
            return result.Models?.Count ?? 0;
        }

        public async Task<int> GetTodayCustomerCountAsync()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var result = await _supabase.From<Customer>()
                .Filter("visit_date", Postgrest.Constants.Operator.Equals, today)
                .Get();
            return result.Models?.Count ?? 0;
        }

        private async Task PopulateStaffNamesAsync(List<Customer> customers)
        {
            if (!customers.Any()) return;
            var staffList = await _staffService.GetAllStaffAsync();
            foreach (var c in customers)
            {
                if (!string.IsNullOrEmpty(c.AssignedStaffId))
                {
                    var s = staffList.FirstOrDefault(x => x.Id == c.AssignedStaffId);
                    if (s != null)
                    {
                        c.AssignedStaffName = s.Name;
                    }
                }
            }
        }
    }
}
