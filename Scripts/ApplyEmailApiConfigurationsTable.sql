-- Si la pantalla SuperAdmin → Correo API (Resend) falla por tabla inexistente:

CREATE TABLE IF NOT EXISTS email_api_configurations (
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    provider character varying(50) NOT NULL,
    api_key character varying(500) NOT NULL,
    from_email character varying(255) NOT NULL,
    from_name character varying(200) NOT NULL,
    is_active boolean NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT email_api_configurations_pkey PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS IX_email_api_configurations_is_active ON email_api_configurations (is_active);

INSERT INTO email_api_configurations (id, provider, api_key, from_email, from_name, is_active, created_at)
SELECT 'b2222222-2222-2222-2222-222222222222'::uuid, 'Resend', '', 'noreply@tusistema.com', 'SchoolManager', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM email_api_configurations LIMIT 1);
