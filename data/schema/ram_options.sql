CREATE TABLE ram_option (
    rame_optioni_id       UUID PRIMARY KEY,
    size_gb        INTEGER,
    type	 VARCHAR(20),
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);