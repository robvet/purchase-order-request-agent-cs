CREATE TABLE cpu_option (
    cpu_optioni_id       UUID PRIMARY KEY,
    name         VARCHAR(100),
    cores        INTEGER,
    threads      INTEGER,
    base_clock   NUMERIC(4,2),
    turbo_clock  NUMERIC(4,2),
	created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);