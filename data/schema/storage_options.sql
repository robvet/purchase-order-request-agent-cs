CREATE TABLE storage_option (
    storage_optioni_id       UUID PRIMARY KEY,
    name         VARCHAR(100),
    capacityGB        INTEGER,
    type	 VARCHAR(20),
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);