# Configuración API de correo (Resend) — envío masivo de contraseñas

Tabla: **`email_api_configurations`**

Solo debe haber **un registro con `is_active = true`**. La migración crea una fila plantilla (`api_key` vacía): debes actualizarla.

```sql
UPDATE email_api_configurations
SET api_key = 're_xxxxxxxx',
    from_email = 'onboarding@resend.dev',  -- o dominio verificado en Resend
    from_name = 'SchoolManager',
    is_active = true
WHERE id = 'b2222222-2222-2222-2222-222222222222';
```

- **Provider** debe ser `Resend` (insensible a mayúsculas) para que `IEmailService.SendEmailAsync` funcione.
- El módulo **User Password Management** usa ese envío; máximo **30** usuarios por solicitud.
