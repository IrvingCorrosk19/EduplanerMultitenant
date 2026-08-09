-- Índice funcional para resolución de login por correo (multi-tenant).
-- Complementa uq_users_school_email_ci (school_id, lower(email)).
CREATE INDEX IF NOT EXISTS ix_users_lower_email
    ON users (lower((email)::text));
