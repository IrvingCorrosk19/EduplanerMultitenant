-- Crear tablas del módulo de carné (QR, scan logs, id cards) si no existen.
-- Ejecutar con: psql -h localhost -U postgres -d schoolmanagement -f Scripts/EnsureStudentQrTokensTable.sql

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE IF NOT EXISTS scan_logs (
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    student_id uuid NOT NULL,
    scan_type character varying(50) NOT NULL,
    result character varying(50) NOT NULL,
    scanned_by uuid NOT NULL,
    scanned_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT scan_logs_pkey PRIMARY KEY (id),
    CONSTRAINT scan_logs_student_id_fkey FOREIGN KEY (student_id) REFERENCES users (id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS IX_scan_logs_student_id ON scan_logs (student_id);
CREATE INDEX IF NOT EXISTS IX_scan_logs_scanned_at ON scan_logs (scanned_at);

CREATE TABLE IF NOT EXISTS student_id_cards (
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    student_id uuid NOT NULL,
    card_number character varying(50) NOT NULL,
    issued_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at timestamp with time zone NULL,
    status character varying(20) NOT NULL DEFAULT 'active',
    CONSTRAINT student_id_cards_pkey PRIMARY KEY (id),
    CONSTRAINT student_id_cards_student_id_fkey FOREIGN KEY (student_id) REFERENCES users (id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_student_id_cards_card_number ON student_id_cards (card_number);
CREATE INDEX IF NOT EXISTS IX_student_id_cards_student_id ON student_id_cards (student_id);

CREATE TABLE IF NOT EXISTS student_qr_tokens (
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    student_id uuid NOT NULL,
    token character varying(500) NOT NULL,
    expires_at timestamp with time zone NULL,
    is_revoked boolean NOT NULL DEFAULT false,
    CONSTRAINT student_qr_tokens_pkey PRIMARY KEY (id),
    CONSTRAINT student_qr_tokens_student_id_fkey FOREIGN KEY (student_id) REFERENCES users (id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_student_qr_tokens_token ON student_qr_tokens (token);
CREATE INDEX IF NOT EXISTS IX_student_qr_tokens_student_id ON student_qr_tokens (student_id);
