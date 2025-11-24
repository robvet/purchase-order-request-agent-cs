CREATE TABLE device (
    device_id   UUID PRIMARY KEY,
    employee_id   UUID NOT NULL,
	catalog_id   UUID NOT NULL,
	ram_option_id		UUID NOT NULL,
	storage_option_id	UUID NOT NULL,
	processor_option_id	UUID NOT NULL,
    asset_tag     VARCHAR(20),
    procurement_date	TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
	created_at    TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_employee
        FOREIGN KEY (employee_id) REFERENCES employee(employee_id)
);