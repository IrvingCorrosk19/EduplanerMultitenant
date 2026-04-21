-- 1. ELIMINAR el constraint antiguo
ALTER TABLE public.users 
DROP CONSTRAINT IF EXISTS users_role_check;

-- 2. CREAR el nuevo constraint CON 'secretaria'
ALTER TABLE public.users 
ADD CONSTRAINT users_role_check 
CHECK (role::text = ANY (ARRAY[
    'superadmin'::text, 
    'admin'::text, 
    'director'::text, 
    'teacher'::text, 
    'parent'::text, 
    'student'::text, 
    'estudiante'::text, 
    'acudiente'::text, 
    'contable'::text, 
    'contabilidad'::text,
    'secretaria'::text
]));
