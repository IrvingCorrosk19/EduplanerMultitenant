-- Incluir 'secretaria' en el CHECK de users.role (para permitir crear usuarios con ese rol).
-- Ejecutar en la BD donde falle la creación (local o producción).
-- Uso: psql -h HOST -U USER -d DB -f Scripts/AddSecretariaToUsersRoleCheck.sql

ALTER TABLE users DROP CONSTRAINT IF EXISTS users_role_check;

ALTER TABLE users ADD CONSTRAINT users_role_check CHECK (
    role IN (
        'superadmin', 'admin', 'director', 'teacher',
        'parent', 'student', 'estudiante', 'acudiente',
        'contable', 'contabilidad', 'secretaria'
    )
);
