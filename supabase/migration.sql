-- ============================================================
-- 1. DROP EXISTING VIEWS & TABLES
-- ============================================================
DROP VIEW IF EXISTS today_attendance CASCADE;
DROP VIEW IF EXISTS customer_summary CASCADE;
DROP VIEW IF EXISTS pending_followups CASCADE;

DROP TABLE IF EXISTS followups CASCADE;
DROP TABLE IF EXISTS follow_ups CASCADE;
DROP TABLE IF EXISTS customers CASCADE;
DROP TABLE IF EXISTS attendance CASCADE;
DROP TABLE IF EXISTS assignment_queue CASCADE;
DROP TABLE IF EXISTS salesmen CASCADE;
DROP TABLE IF EXISTS staff CASCADE;
DROP TABLE IF EXISTS round_robin_tracker CASCADE;
DROP TABLE IF EXISTS app_settings CASCADE;

-- ============================================================
-- 2. CREATE NEW TABLES
-- ============================================================

-- STAFF TABLE
CREATE TABLE staff (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    staff_code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    mobile TEXT,
    designation TEXT,
    fingerprint_emp_id TEXT UNIQUE,
    is_active BOOLEAN DEFAULT TRUE,
    user_id UUID, -- Links to auth.users if they have a login
    created_date TIMESTAMPTZ DEFAULT NOW()
);

-- ATTENDANCE TABLE
CREATE TABLE attendance (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    staff_id UUID NOT NULL REFERENCES staff(id) ON DELETE CASCADE,
    attendance_date DATE NOT NULL DEFAULT CURRENT_DATE,
    morning_in TIMESTAMPTZ,
    morning_out TIMESTAMPTZ,
    afternoon_in TIMESTAMPTZ,
    evening_out TIMESTAMPTZ,
    status TEXT NOT NULL DEFAULT 'Absent',
    total_hours NUMERIC DEFAULT 0,
    UNIQUE(staff_id, attendance_date)
);

-- CUSTOMERS TABLE
CREATE TABLE customers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_name TEXT NOT NULL,
    mobile TEXT,
    city TEXT,
    visit_date DATE NOT NULL DEFAULT CURRENT_DATE,
    assigned_staff_id UUID REFERENCES staff(id) ON DELETE SET NULL,
    remarks TEXT,
    purchase_value NUMERIC DEFAULT 0,
    status TEXT NOT NULL DEFAULT 'New', -- New, Follow-up Pending, Interested, Not Interested, Converted, Sale Closed
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- FOLLOWUPS TABLE
CREATE TABLE followups (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id UUID NOT NULL REFERENCES customers(id) ON DELETE CASCADE,
    staff_id UUID NOT NULL REFERENCES staff(id) ON DELETE CASCADE,
    followup_date DATE NOT NULL DEFAULT CURRENT_DATE,
    remarks TEXT,
    status TEXT NOT NULL DEFAULT 'Follow-up Pending', -- New, Follow-up Pending, Interested, Not Interested, Converted, Sale Closed
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ROUND ROBIN TRACKER
CREATE TABLE round_robin_tracker (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    last_assigned_staff_id UUID REFERENCES staff(id) ON DELETE SET NULL,
    updated_date TIMESTAMPTZ DEFAULT NOW()
);

-- APP SETTINGS
CREATE TABLE app_settings (
    key TEXT PRIMARY KEY,
    value TEXT,
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- 3. ENABLE ROW LEVEL SECURITY (RLS) & POLICIES
-- ============================================================
ALTER TABLE staff ENABLE ROW LEVEL SECURITY;
ALTER TABLE attendance ENABLE ROW LEVEL SECURITY;
ALTER TABLE customers ENABLE ROW LEVEL SECURITY;
ALTER TABLE followups ENABLE ROW LEVEL SECURITY;
ALTER TABLE round_robin_tracker ENABLE ROW LEVEL SECURITY;
ALTER TABLE app_settings ENABLE ROW LEVEL SECURITY;

-- Wide open policies for developer sandbox ease of access (using API calls from WASM client directly)
CREATE POLICY "Allow anonymous all to staff" ON staff FOR ALL USING (true);
CREATE POLICY "Allow anonymous all to attendance" ON attendance FOR ALL USING (true);
CREATE POLICY "Allow anonymous all to customers" ON customers FOR ALL USING (true);
CREATE POLICY "Allow anonymous all to followups" ON followups FOR ALL USING (true);
CREATE POLICY "Allow anonymous all to round_robin_tracker" ON round_robin_tracker FOR ALL USING (true);
CREATE POLICY "Allow anonymous all to app_settings" ON app_settings FOR ALL USING (true);

-- ============================================================
-- 4. SEED SEED DATA
-- ============================================================
INSERT INTO staff (staff_code, name, mobile, designation, fingerprint_emp_id, is_active) VALUES
('S001', 'Kumar', '9876543211', 'Salesman A', 'F001', TRUE),
('S002', 'Ravi', '9876543210', 'Salesman B', 'F002', TRUE),
('S003', 'Mani', '9876543212', 'Salesman C', 'F003', TRUE),
('S004', 'Raja', '9876543213', 'Salesman D', 'F004', TRUE),
('S005', 'Senthil', '9876543214', 'Salesman E', 'F005', TRUE);

-- Initialize round robin tracker
INSERT INTO round_robin_tracker (last_assigned_staff_id, updated_date)
VALUES (NULL, NOW());

-- Settings
INSERT INTO app_settings (key, value) VALUES ('shop_name', 'Silk Saree Shop');
