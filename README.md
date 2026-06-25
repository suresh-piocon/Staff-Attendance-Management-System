# Staff Attendance Management System

A **Salesman Attendance & Customer Handling System** for a silk saree shop, built with **Blazor WebAssembly + Supabase**.

---

## Features

| Module | Details |
|---|---|
| 🔐 **Auth** | Role-based login — Admin & Salesman (Supabase Email/Password) |
| 📅 **Attendance** | Daily bulk marking (Present / Absent / Half Day / Leave) |
| ✅ **Check-In/Out** | Salesman self check-in and check-out with timestamps |
| 🔄 **Round Robin Assignment** | Auto-assigns new customers to next available salesman, skips absent |
| 👥 **Customer Entry** | Add customer visit with automatic salesman assignment |
| 🔔 **Follow-Ups** | Track follow-up status (Interested / Not Interested / Confirmed / Again) |
| 📊 **Reports** | Performance report (conversion %) + monthly attendance report |
| 📋 **Admin Dashboard** | KPIs: present/absent today, new customers, pending follow-ups |
| 📱 **Salesman Dashboard** | Today's customers, follow-up alerts, check-in card |

---

## Tech Stack

- **Frontend**: Blazor WebAssembly (.NET 9)
- **Backend / DB**: [Supabase](https://supabase.com) (PostgreSQL + PostgREST + GoTrue Auth)
- **Auth**: Supabase Email/Password with custom Blazor `AuthenticationStateProvider`
- **NuGet**: `supabase-csharp` v0.16.2, `Blazor-ApexCharts`
- **Styling**: Vanilla CSS — Dark glassmorphism with silk-gold accent theme

---

## Project Structure

```
SalesmanAttendance/
├── Auth/                    # Custom AuthenticationStateProvider
├── Models/                  # Postgrest ORM models (5 tables)
├── Services/                # Business logic services
├── Pages/
│   ├── Admin/               # Dashboard, Attendance, Salesmen, Customers, FollowUps, Reports
│   └── Salesman/            # Dashboard, MyCustomers, FollowUpEntry
├── Layout/                  # MainLayout, NavMenu, EmptyLayout
├── Shared/                  # RedirectToLogin
├── supabase/
│   └── schema.sql           # Run this in Supabase SQL Editor
└── wwwroot/
    ├── appsettings.json     # Add your Supabase URL + Key here
    └── css/app.css          # Premium dark silk theme
```

---

## Setup Instructions

### 1. Supabase Setup

1. Create a project at [supabase.com](https://supabase.com)
2. Go to **SQL Editor** and run the full contents of [`supabase/schema.sql`](supabase/schema.sql)
3. This creates:
   - 5 tables: `salesmen`, `attendance`, `customers`, `followups`, `assignment_queue`
   - Row Level Security policies
   - 5 seeded salesmen (Ravi, Kumar, Mani, Senthil, Prakash)
   - Assignment queue initialized

### 2. Create Admin User

In Supabase Dashboard → **Authentication → Users → Add User**:
- Email: `admin@silksaree.com`
- Password: (your choice)

Then in SQL Editor, set the admin role:
```sql
UPDATE auth.users 
SET raw_user_meta_data = '{"role": "Admin"}'
WHERE email = 'admin@silksaree.com';
```

### 3. Configure App

Edit `wwwroot/appsettings.json`:
```json
{
  "Supabase": {
    "Url": "https://YOUR-PROJECT-ID.supabase.co",
    "Key": "your-anon-public-key"
  }
}
```

> ⚠️ Find these in Supabase Dashboard → **Settings → API**

### 4. Create Salesman Accounts (Optional)

For each salesman who needs their own login, create a user in Supabase Auth, then link:
```sql
UPDATE salesmen 
SET user_id = 'SUPABASE-USER-UUID'
WHERE name = 'Ravi';
```
Salesman accounts do **not** need role metadata (defaults to `Salesman` role).

### 5. Run the App

```bash
dotnet run
```

Then open `https://localhost:PORT` in your browser and log in with the admin credentials.

---

## Database Schema

```
salesmen          — salesman master (name, mobile, active status, supabase user_id)
attendance        — daily check-in/out records with status
customers         — customer visit entries with assigned salesman
followups         — follow-up entries linked to customers and salesmen
assignment_queue  — round-robin queue state (queue_order per salesman)
```

---

## Round Robin Assignment Logic

When a new customer is added:
1. Fetch active salesmen ordered by `queue_order`
2. Check today's attendance — skip `Absent` / `Leave` salesmen
3. Assign to first available salesman
4. Rotate that salesman to end of queue (`queue_order = MAX + 1`)

**Example** (Mani absent today):
```
Customer 1 → Ravi
Customer 2 → Kumar
Customer 3 → Senthil   ← Mani skipped (absent)
Customer 4 → Prakash
Customer 5 → Ravi       ← cycle continues
```

---

## License

MIT
