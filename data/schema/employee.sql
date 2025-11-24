CREATE TABLE employee (
    employee_id UUID PRIMARY KEY,
    email VARCHAR(150) NOT NULL,
	alias      VARCHAR(25) NOT NULL,
    first_name VARCHAR(100),
    last_name  VARCHAR(100),
	role       VARCHAR(100),
    department VARCHAR(100),
    location   VARCHAR(100),
    level      VARCHAR(50),
    start_date DATE,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);