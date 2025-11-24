BEGIN;

CREATE TABLE public.cpu_option (
    cpu_option_id UUID PRIMARY KEY,
    name           VARCHAR(100),
    cores          INTEGER,
    threads        INTEGER,
    base_clock     NUMERIC(4,2),
    turbo_clock    NUMERIC(4,2),
    created_at     TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE public.employee (
    employee_id UUID PRIMARY KEY,
    email       VARCHAR(150) NOT NULL,
    alias       VARCHAR(25)  NOT NULL,
    first_name  VARCHAR(100),
    last_name   VARCHAR(100),
    role        VARCHAR(100),
    department  VARCHAR(100),
    location    VARCHAR(100),
    level       VARCHAR(50),
    start_date  DATE,
    created_at  TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE public.ram_option (
    ram_option_id UUID PRIMARY KEY,
    size_gb       INTEGER,
    type          VARCHAR(20),
    created_at    TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE public.storage_option (
    storage_option_id UUID PRIMARY KEY,
    name              VARCHAR(100),
    capacity_gb       INTEGER,
    type              VARCHAR(20),
    created_at        TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE public.catalog (
    catalog_id   UUID PRIMARY KEY,
    sku          VARCHAR(30)[],
    laptop_name  VARCHAR(30)[],
    vendor       VARCHAR(30)[],
    category     VARCHAR(20)[],
    base_price   NUMERIC(8,2)[],
    is_available BIT
);

CREATE TABLE public.device (
    device_id          UUID PRIMARY KEY,
    employee_id        UUID,
    catalog_id         UUID[],
    asset_tag          VARCHAR(25)[],
    procurement_date   TIMESTAMPTZ[],
    storage_option_id  UUID,
    ram_option_id      UUID,
    cpu_option_id      UUID,
    created_at         TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (ram_option_id),
    UNIQUE (cpu_option_id),
    UNIQUE (storage_option_id),
    CONSTRAINT fk_device_employee
        FOREIGN KEY (employee_id)
        REFERENCES public.employee (employee_id)
        ON UPDATE CASCADE
        ON DELETE SET NULL,
    CONSTRAINT fk_device_ram
        FOREIGN KEY (ram_option_id)
        REFERENCES public.ram_option (ram_option_id),
    CONSTRAINT fk_device_cpu
        FOREIGN KEY (cpu_option_id)
        REFERENCES public.cpu_option (cpu_option_id),
    CONSTRAINT fk_device_storage
        FOREIGN KEY (storage_option_id)
        REFERENCES public.storage_option (storage_option_id)
);

END;