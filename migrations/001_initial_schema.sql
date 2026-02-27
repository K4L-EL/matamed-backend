-- NEX Health Intelligence — Initial Schema
-- Run against Railway PostgreSQL

CREATE TABLE IF NOT EXISTS patients (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    age INT NOT NULL,
    gender TEXT NOT NULL,
    ward TEXT NOT NULL,
    bed_number TEXT NOT NULL,
    admitted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    status TEXT NOT NULL DEFAULT 'Stable',
    risk_score DOUBLE PRECISION NOT NULL DEFAULT 0,
    active_infections INT NOT NULL DEFAULT 0,
    organisms TEXT[] NOT NULL DEFAULT '{}'
);

CREATE TABLE IF NOT EXISTS infections (
    id TEXT PRIMARY KEY,
    patient_id TEXT NOT NULL REFERENCES patients(id),
    patient_name TEXT NOT NULL,
    organism TEXT NOT NULL,
    type TEXT NOT NULL,
    location TEXT,
    ward TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'Active',
    detected_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resolved_at TIMESTAMPTZ,
    severity TEXT NOT NULL DEFAULT 'Medium',
    is_hai BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS alerts (
    id TEXT PRIMARY KEY,
    title TEXT NOT NULL,
    description TEXT NOT NULL,
    severity TEXT NOT NULL DEFAULT 'Medium',
    category TEXT NOT NULL DEFAULT 'Infection',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    related_entity_id TEXT,
    related_entity_type TEXT
);

CREATE TABLE IF NOT EXISTS outbreaks (
    id TEXT PRIMARY KEY,
    organism TEXT NOT NULL,
    location TEXT NOT NULL,
    detected_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resolved_at TIMESTAMPTZ,
    status TEXT NOT NULL DEFAULT 'Active',
    affected_patients INT NOT NULL DEFAULT 1,
    severity TEXT NOT NULL DEFAULT 'Medium',
    investigation_status TEXT NOT NULL DEFAULT 'Pending'
);

CREATE TABLE IF NOT EXISTS screening_records (
    id TEXT PRIMARY KEY,
    patient_id TEXT NOT NULL REFERENCES patients(id),
    patient_name TEXT NOT NULL,
    ward TEXT NOT NULL,
    screening_type TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'Pending',
    due_date TIMESTAMPTZ NOT NULL,
    completed_date TIMESTAMPTZ,
    result TEXT
);

CREATE TABLE IF NOT EXISTS device_infections (
    id TEXT PRIMARY KEY,
    patient_id TEXT NOT NULL REFERENCES patients(id),
    patient_name TEXT NOT NULL,
    device_type TEXT NOT NULL,
    organism TEXT NOT NULL,
    ward TEXT NOT NULL,
    insertion_date TIMESTAMPTZ NOT NULL,
    infection_date TIMESTAMPTZ NOT NULL,
    days_to_infection INT NOT NULL DEFAULT 0,
    status TEXT NOT NULL DEFAULT 'Active'
);

CREATE TABLE IF NOT EXISTS resistance_patterns (
    id SERIAL PRIMARY KEY,
    organism TEXT NOT NULL,
    antibiotic TEXT NOT NULL,
    resistance_rate DOUBLE PRECISION NOT NULL DEFAULT 0,
    sample_count INT NOT NULL DEFAULT 0,
    trend TEXT NOT NULL DEFAULT 'Stable'
);

CREATE TABLE IF NOT EXISTS prescribing_records (
    id TEXT PRIMARY KEY,
    patient_id TEXT NOT NULL,
    patient_name TEXT NOT NULL,
    antibiotic TEXT NOT NULL,
    indication TEXT NOT NULL,
    start_date TIMESTAMPTZ NOT NULL,
    end_date TIMESTAMPTZ,
    duration_days INT NOT NULL DEFAULT 0,
    status TEXT NOT NULL DEFAULT 'Active',
    appropriate BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS transmission_nodes (
    id TEXT PRIMARY KEY,
    patient_name TEXT NOT NULL,
    ward TEXT NOT NULL,
    organism TEXT NOT NULL,
    detected_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    node_type TEXT NOT NULL DEFAULT 'case'
);

CREATE TABLE IF NOT EXISTS transmission_links (
    id SERIAL PRIMARY KEY,
    source_id TEXT NOT NULL REFERENCES transmission_nodes(id),
    target_id TEXT NOT NULL REFERENCES transmission_nodes(id),
    link_type TEXT NOT NULL DEFAULT 'suspected',
    confidence DOUBLE PRECISION NOT NULL DEFAULT 0,
    evidence TEXT
);

CREATE TABLE IF NOT EXISTS pipelines (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT,
    status TEXT NOT NULL DEFAULT 'Active',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_run_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS pipeline_nodes (
    id TEXT PRIMARY KEY,
    pipeline_id TEXT NOT NULL REFERENCES pipelines(id) ON DELETE CASCADE,
    type TEXT NOT NULL,
    label TEXT NOT NULL,
    position_x DOUBLE PRECISION NOT NULL DEFAULT 0,
    position_y DOUBLE PRECISION NOT NULL DEFAULT 0,
    config JSONB NOT NULL DEFAULT '{}'
);

CREATE TABLE IF NOT EXISTS pipeline_edges (
    id TEXT PRIMARY KEY,
    pipeline_id TEXT NOT NULL REFERENCES pipelines(id) ON DELETE CASCADE,
    source_id TEXT NOT NULL,
    target_id TEXT NOT NULL,
    label TEXT
);

CREATE TABLE IF NOT EXISTS forecast_risk_scores (
    id SERIAL PRIMARY KEY,
    patient_id TEXT NOT NULL,
    patient_name TEXT NOT NULL,
    ward TEXT NOT NULL,
    score DOUBLE PRECISION NOT NULL DEFAULT 0,
    risk_level TEXT NOT NULL DEFAULT 'Low',
    top_factors TEXT[] NOT NULL DEFAULT '{}'
);

CREATE TABLE IF NOT EXISTS location_risks (
    location_id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    type TEXT NOT NULL,
    risk_score DOUBLE PRECISION NOT NULL DEFAULT 0,
    active_cases INT NOT NULL DEFAULT 0,
    capacity INT NOT NULL DEFAULT 0,
    occupancy_rate DOUBLE PRECISION NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS screening_compliance (
    id SERIAL PRIMARY KEY,
    ward TEXT NOT NULL UNIQUE,
    total_required INT NOT NULL DEFAULT 0,
    completed INT NOT NULL DEFAULT 0,
    overdue INT NOT NULL DEFAULT 0,
    compliance_rate DOUBLE PRECISION NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS device_summaries (
    id SERIAL PRIMARY KEY,
    device_type TEXT NOT NULL UNIQUE,
    total_devices INT NOT NULL DEFAULT 0,
    infections INT NOT NULL DEFAULT 0,
    infection_rate DOUBLE PRECISION NOT NULL DEFAULT 0,
    avg_days_to_infection DOUBLE PRECISION NOT NULL DEFAULT 0
);

-- Indexes for common queries
CREATE INDEX IF NOT EXISTS idx_infections_patient ON infections(patient_id);
CREATE INDEX IF NOT EXISTS idx_infections_ward ON infections(ward);
CREATE INDEX IF NOT EXISTS idx_infections_status ON infections(status);
CREATE INDEX IF NOT EXISTS idx_patients_ward ON patients(ward);
CREATE INDEX IF NOT EXISTS idx_patients_status ON patients(status);
CREATE INDEX IF NOT EXISTS idx_alerts_severity ON alerts(severity);
CREATE INDEX IF NOT EXISTS idx_alerts_is_read ON alerts(is_read);
CREATE INDEX IF NOT EXISTS idx_outbreaks_status ON outbreaks(status);
CREATE INDEX IF NOT EXISTS idx_screening_records_patient ON screening_records(patient_id);
CREATE INDEX IF NOT EXISTS idx_screening_records_status ON screening_records(status);
CREATE INDEX IF NOT EXISTS idx_device_infections_patient ON device_infections(patient_id);
CREATE INDEX IF NOT EXISTS idx_transmission_links_source ON transmission_links(source_id);
CREATE INDEX IF NOT EXISTS idx_transmission_links_target ON transmission_links(target_id);
