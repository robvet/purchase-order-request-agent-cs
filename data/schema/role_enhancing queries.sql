SELECT * FROM public.role
ORDER BY role_id ASC 

delete from role where role_id = 'f0aaa061-4161-43df-8566-80eda94216bf'

INSERT INTO public.role (role_id, role_name, role_description, created_at)
VALUES 
    (gen_random_uuid(), 'Contractor-Junior', 'Junior level contractor', NOW()),
    (gen_random_uuid(), 'Contractor-Middle', 'Mid level contractor', NOW()),
    (gen_random_uuid(), 'Contractor-Senior', 'Senior level contractor', NOW()),
    (gen_random_uuid(), 'Intern-Junior', 'Junior level intern', NOW()),
    (gen_random_uuid(), 'Intern-Middle', 'Mid level intern', NOW()),
    (gen_random_uuid(), 'Intern-Senior', 'Senior level intern', NOW());


INSERT INTO public.role (role_id, role_name, role_description, created_at) VALUES (gen_random_uuid(), 'IC-Middle', 'Mid-level individual contributor', NOW()) 

UPDATE public.role 
SET role_name = REPLACE(role_name, 'IC', 'Indivdual Contributor')
WHERE role_name LIKE '%Middle%';

UPDATE public.role 
SET role_name = REPLACE(role_name, 'IC-Junior', 'IC Junior')
WHERE role_name = 'IC';



UPDATE public.role 
SET role_name = REPLACE(role_name, 'IC-Principal', 'IC Principal')
WHERE role_name = 'IC-Principal';

UPDATE public.role 
SET role_name = REPLACE(role_name, 'Intern-Junior', 'Intern Junior')
WHERE role_name = 'Intern-Junior';

UPDATE public.role 
SET role_name = REPLACE(role_name, 'Contractor-Senior', 'Contractor Senior')
WHERE role_name = 'Contractor-Senior';

UPDATE public.role 
SET role_name = REPLACE(role_name, 'IC-Mid-Level', 'IC Mid-Level')
WHERE role_name = 'IC-Mid-Level';

UPDATE public.role 
SET role_name = REPLACE(role_name, 'Intern-Senior', 'Intern Senior')
WHERE role_name = 'Intern-Senior';

UPDATE public.role 
SET role_name = REPLACE(role_name, 'IC-Senior', 'IC Senior')
WHERE role_name = 'IC-Senior';

UPDATE public.role 
SET role_name = REPLACE(role_name, 'Intern-Mid-Level', 'Intern Mid-Level')
WHERE role_name = 'Intern-Mid-Level';

UPDATE public.role 
SET role_name = REPLACE(role_name, 'IC-Distinguished', 'IC Distinguished')
WHERE role_name = 'IC-Distinguished';

UPDATE public.role 
SET role_name = REPLACE(role_name, 'C-level', 'Executive')
WHERE role_name = 'C-level';

UPDATE public.role 
SET role_name = REPLACE(role_name, 'Contractor-Mid-Level', 'Contractor Mid-Level')
WHERE role_name = 'Contractor-Mid-Level';

UPDATE public.role 
SET role_name = REPLACE(role_name, 'IC', 'Individual Contributor')
WHERE role_name LIKE 'IC%';

ALTER TABLE public.role 
ALTER COLUMN role_name TYPE character varying(40);

UPDATE public.role 
SET role_name = 'Manager Junior'
WHERE role_name = 'Junior Manager';

UPDATE public.role 
SET role_name = 'Manager Mid-Level'
WHERE role_name = 'Mid-Level Manager';

UPDATE public.role 
SET role_name = 'Manager Senior'
WHERE role_name = 'Senior Manager';

delete from role where role_id = '9fe2c5e2-acf6-42d8-bb37-1b429297433d'

ALTER TABLE public.role 
ADD CONSTRAINT role_name_unique UNIQUE (role_name);

-- Add the role_level column
ALTER TABLE public.role 
ADD COLUMN role_level integer;

-- Update role levels
UPDATE public.role SET role_level = 10 WHERE role_name = 'Administrative';

UPDATE public.role SET role_level = 20 WHERE role_name = 'Intern Junior';
UPDATE public.role SET role_level = 30 WHERE role_name = 'Intern Mid-Level';
UPDATE public.role SET role_level = 40 WHERE role_name = 'Intern Senior';

UPDATE public.role SET role_level = 20 WHERE role_name = 'Contractor Junior';
UPDATE public.role SET role_level = 30 WHERE role_name = 'Contractor Mid-Level';
UPDATE public.role SET role_level = 40 WHERE role_name = 'Contractor Senior';

UPDATE public.role SET role_level = 20 WHERE role_name = 'Individual Contributor Junior';
UPDATE public.role SET role_level = 30 WHERE role_name = 'Individual Contributor Mid-Level';
UPDATE public.role SET role_level = 40 WHERE role_name = 'Individual Contributor Senior';
UPDATE public.role SET role_level = 50 WHERE role_name = 'Individual Contributor Principal';
UPDATE public.role SET role_level = 60 WHERE role_name = 'Individual Contributor Distinguished';

UPDATE public.role SET role_level = 25 WHERE role_name = 'Manager Junior';
UPDATE public.role SET role_level = 35 WHERE role_name = 'Manager Mid-Level';
UPDATE public.role SET role_level = 45 WHERE role_name = 'Manager Senior';

UPDATE public.role SET role_level = 55 WHERE role_name = 'Director';
UPDATE public.role SET role_level = 65 WHERE role_name = 'Executive';