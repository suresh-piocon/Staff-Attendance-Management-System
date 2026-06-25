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
        private readonly AssignmentService _assignmentService;

        public CustomerService(Supabase.Client supabase, AssignmentService assignmentService)
        {
            _supabase = supabase;
            _assignmentService = assignmentService;
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var result = await _supabase.From<Customer>()
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();
            return result.Models ?? new List<Customer>();
        }

        public async Task<List<Customer>> GetCustomersBySalesmanAsync(string salesmanId)
        {
            var result = await _supabase.From<Customer>()
                .Filter("assigned_salesman_id", Postgrest.Constants.Operator.Equals, salesmanId)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();
            return result.Models ?? new List<Customer>();
        }

        public async Task<List<Customer>> GetTodayCustomersAsync()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var result = await _supabase.From<Customer>()
                .Filter("visit_date", Postgrest.Constants.Operator.Equals, today)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();
            return result.Models ?? new List<Customer>();
        }

        public async Task<Customer> AddCustomerAsync(Customer customer)
        {
            // Auto-assign salesman via round robin
            var salesman = await _assignmentService.AssignNextSalesmanAsync();
            if (salesman != null)
            {
                customer.AssignedSalesmanId = salesman.Id;
                customer.AssignedSalesmanName = salesman.Name;
            }

            var result = await _supabase.From<Customer>().Insert(customer);
            return result.Models!.First();
        }

        public async Task UpdateCustomerStatusAsync(string customerId, string status)
        {
            await _supabase.From<Customer>()
                .Filter("id", Postgrest.Constants.Operator.Equals, customerId)
                .Set(c => c.Status, status)
                .Update();
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
    }
}
