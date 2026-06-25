-- ============================================================
-- Salesman Attendance & Customer Handling System
-- Supabase PostgreSQL Schema
-- Run this in Supabase SQL Editor (Database > SQL Editor)
-- ============================================================

-- ============================================================
-- 1. SALESMEN TABLE
-- ============================================================
CREATE TABLE IF NOT EXISTS salesmen (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  mobile TEXT,
  joining_date DATE,
  is_active BOOLEAN DEFAULT TRUE,
  user_id UUID REFERENCES auth.users(id) ON DELETE SET NULL,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- 2. ATTENDANCE TABLE
-- ============================================================
CREATE TABLE IF NOT EXISTS attendance (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  salesman_id UUID NOT NULL REFERENCES salesmen(id) ON DELETE CASCADE,
  date DATE NOT NULL DEFAULT CURRENT_DATE,
  check_in_time TIMESTAMPTZ,
  check_out_time TIMESTAMPTZ,
  status TEXT NOT NULL DEFAULT 'Absent' CHECK (status IN ('Present','Absent','Half Day','Leave')),
  created_at TIMESTAMPTZ DEFAULT NOW(),
  UNIQUE(salesman_id, date)
);

-- ============================================================
-- 3. CUSTOMERS TABLE
-- ============================================================
CREATE TABLE IF NOT EXISTS customers (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  mobile TEXT,
  city TEXT,
  interested_product TEXT,
  visit_date DATE DEFAULT CURRENT_DATE,
  assigned_salesman_id UUID REFERENCES salesmen(id) ON DELETE SET NULL,
  remarks TEXT,
  status TEXT DEFAULT 'Open' CHECK (status IN ('Open','Closed','Converted')),
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- 4. FOLLOWUPS TABLE
-- ============================================================
CREATE TABLE IF NOT EXISTS followups (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  customer_id UUID NOT NULL REFERENCES customers(id) ON DELETE CASCADE,
  salesman_id UUID NOT NULL REFERENCES salesmen(id) ON DELETE CASCADE,
  followup_date DATE NOT NULL,
  status TEXT CHECK (status IN ('Interested','Not Interested','Purchase Confirmed','Follow-up Again')),
  remarks TEXT,
  next_followup_date DATE,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- 5. ASSIGNMENT QUEUE TABLE (Round Robin state)
-- ============================================================
CREATE TABLE IF NOT EXISTS assignment_queue (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  salesman_id UUID NOT NULL REFERENCES salesmen(id) ON DELETE CASCADE UNIQUE,
  queue_order INT NOT NULL DEFAULT 0,
  last_assigned_at TIMESTAMPTZ,
  UNIQUE(salesman_id)
);

-- ============================================================
-- 6. APP SETTINGS TABLE (for admin config)
-- ============================================================
CREATE TABLE IF NOT EXISTS app_settings (
  key TEXT PRIMARY KEY,
  value TEXT,
  updated_at TIMESTAMPTZ DEFAULT NOW()
);

INSERT INTO app_settings (key, value) VALUES ('shop_name', 'Silk Saree Shop') ON CONFLICT DO NOTHING;
INSERT INTO app_settings (key, value) VALUES ('admin_email', 'admin@silksaree.com') ON CONFLICT DO NOTHING;

-- ============================================================
-- ROW LEVEL SECURITY (RLS)
-- ============================================================

ALTER TABLE salesmen ENABLE ROW LEVEL SECURITY;
ALTER TABLE attendance ENABLE ROW LEVEL SECURITY;
ALTER TABLE customers ENABLE ROW LEVEL SECURITY;
ALTER TABLE followups ENABLE ROW LEVEL SECURITY;
ALTER TABLE assignment_queue ENABLE ROW LEVEL SECURITY;
ALTER TABLE app_settings ENABLE ROW LEVEL SECURITY;

-- Helper function: check if current user is admin
CREATE OR REPLACE FUNCTION is_admin()
RETURNS BOOLEAN AS $$
  SELECT EXISTS (
    SELECT 1 FROM auth.users
    WHERE id = auth.uid()
    AND raw_user_meta_data->>'role' = 'Admin'
  );
$$ LANGUAGE sql SECURITY DEFINER;

-- Helper function: get salesman id for current user
CREATE OR REPLACE FUNCTION my_salesman_id()
RETURNS UUID AS $$
  SELECT id FROM salesmen WHERE user_id = auth.uid() LIMIT 1;
$$ LANGUAGE sql SECURITY DEFINER;

-- SALESMEN policies
CREATE POLICY "Salesmen: admin full access" ON salesmen FOR ALL USING (is_admin());
CREATE POLICY "Salesmen: salesman read own" ON salesmen FOR SELECT USING (user_id = auth.uid());

-- ATTENDANCE policies
CREATE POLICY "Attendance: admin full access" ON attendance FOR ALL USING (is_admin());
CREATE POLICY "Attendance: salesman manage own" ON attendance FOR ALL USING (salesman_id = my_salesman_id());

-- CUSTOMERS policies
CREATE POLICY "Customers: admin full access" ON customers FOR ALL USING (is_admin());
CREATE POLICY "Customers: salesman read assigned" ON customers FOR SELECT USING (assigned_salesman_id = my_salesman_id());

-- FOLLOWUPS policies
CREATE POLICY "FollowUps: admin full access" ON followups FOR ALL USING (is_admin());
CREATE POLICY "FollowUps: salesman manage own" ON followups FOR ALL USING (salesman_id = my_salesman_id());

-- ASSIGNMENT QUEUE policies
CREATE POLICY "Queue: admin full access" ON assignment_queue FOR ALL USING (is_admin());
CREATE POLICY "Queue: salesman read" ON assignment_queue FOR SELECT USING (TRUE);

-- APP SETTINGS policies
CREATE POLICY "Settings: admin full access" ON app_settings FOR ALL USING (is_admin());
CREATE POLICY "Settings: all read" ON app_settings FOR SELECT USING (TRUE);

-- ============================================================
-- SEED DATA — 5 Default Salesmen (without user_id, admin assigns later)
-- ============================================================
INSERT INTO salesmen (name, mobile, joining_date, is_active) VALUES
  ('Ravi',    '9876543210', '2024-01-01', TRUE),
  ('Kumar',   '9876543211', '2024-01-01', TRUE),
  ('Mani',    '9876543212', '2024-01-01', TRUE),
  ('Senthil', '9876543213', '2024-01-01', TRUE),
  ('Prakash', '9876543214', '2024-01-01', TRUE)
ON CONFLICT DO NOTHING;

-- Initialize assignment queue for each salesman
INSERT INTO assignment_queue (salesman_id, queue_order)
SELECT id, ROW_NUMBER() OVER (ORDER BY created_at)
FROM salesmen
ON CONFLICT (salesman_id) DO NOTHING;

-- ============================================================
-- VIEWS for easier reporting
-- ============================================================

-- Today's attendance summary
CREATE OR REPLACE VIEW today_attendance AS
SELECT
  s.id as salesman_id,
  s.name,
  s.mobile,
  COALESCE(a.status, 'Not Marked') as status,
  a.check_in_time,
  a.check_out_time,
  a.date
FROM salesmen s
LEFT JOIN attendance a ON a.salesman_id = s.id AND a.date = CURRENT_DATE
WHERE s.is_active = TRUE
ORDER BY s.name;

-- Customer assignment summary
CREATE OR REPLACE VIEW customer_summary AS
SELECT
  s.name as salesman_name,
  COUNT(c.id) as total_customers,
  COUNT(CASE WHEN c.status = 'Open' THEN 1 END) as open_customers,
  COUNT(CASE WHEN c.status = 'Converted' THEN 1 END) as converted_customers,
  COUNT(CASE WHEN c.status = 'Closed' THEN 1 END) as closed_customers
FROM salesmen s
LEFT JOIN customers c ON c.assigned_salesman_id = s.id
WHERE s.is_active = TRUE
GROUP BY s.id, s.name
ORDER BY s.name;

-- Pending follow-ups view
CREATE OR REPLACE VIEW pending_followups AS
SELECT
  f.*,
  c.name as customer_name,
  c.mobile as customer_mobile,
  c.city,
  c.interested_product,
  s.name as salesman_name
FROM followups f
JOIN customers c ON c.id = f.customer_id
JOIN salesmen s ON s.id = f.salesman_id
WHERE f.status = 'Follow-up Again'
  AND f.next_followup_date <= CURRENT_DATE + INTERVAL '7 days'
ORDER BY f.next_followup_date ASC;
