-- Agrega la columna photo_url a users si no existe.
-- Ejecutar contra tu base de datos cuando aparezca: column u.photo_url does not exist
-- Ejemplo: psql -U postgres -d schoolmanagement -f Scripts/AddPhotoUrlColumn.sql

ALTER TABLE users ADD COLUMN IF NOT EXISTS photo_url character varying(500) NULL;
