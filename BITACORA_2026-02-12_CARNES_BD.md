# Bitácora – 12 de febrero de 2026  
## Módulo Carnet Estudiantil, tablas de BD y validación local

---

## 1. Ajustes de autorización (Acceso denegado)

- **Problema:** Al entrar a Carnet Estudiantil o a Configuración de Carnet aparecía “¡Acceso Denegado!”.
- **Causa:** Los controladores usaban roles en PascalCase (`Admin`, `SuperAdmin`, `Director`) y en la BD los roles están en minúsculas.
- **Cambios:**
  - **`Controllers/StudentIdCardController.cs`:**  
    `[Authorize(Roles = "Admin,admin,SuperAdmin,superadmin,Director,director")]`
  - **`Controllers/IdCardSettingsController.cs`:**  
    `[Authorize(Roles = "SuperAdmin,superadmin,Admin,admin,Director,director")]`

---

## 2. Tablas del módulo de carnets (no existían)

- **Problema:** Al abrir `/id-card/settings` o flujos de carnets:  
  `relation "school_id_card_settings" does not exist` (y en otro momento `column s.shift_id does not exist`).
- **Acciones realizadas:**

### 2.1 Scripts SQL creados

| Archivo | Uso |
|--------|-----|
| **`Migrations/ApplyIdCardSettingsTables.sql`** | Crea `id_card_template_fields`, `school_id_card_settings` e inserta filas en `__EFMigrationsHistory` (para BDs que ya tienen esa tabla). |
| **`Scripts/ApplyIdCardTables_LocalOnly.sql`** | Solo crea las dos tablas de carnets, sin tocar `__EFMigrationsHistory`. Para BD local sin historial de EF. |

### 2.2 Creación automática al arranque

- **`Scripts/EnsureIdCardTables.cs`:**  
  - Consulta si existe la tabla `school_id_card_settings`.  
  - Si no existe, ejecuta el SQL que crea `id_card_template_fields`, `school_id_card_settings` e inserta la migración `20260117095203_AddIdCardSettingsAndTemplates` en `__EFMigrationsHistory` (si esa tabla existe).
- **`Program.cs`:** Tras `builder.Build()` se llama a `EnsureIdCardTables.EnsureAsync(db)` una vez al iniciar la aplicación.

### 2.3 Revisión real de la BD local (sin especular)

- **`Scripts/CheckDatabaseState.cs`:** Script que, usando la connection string de la app, consulta:
  - Existencia de tablas: `schools`, `school_id_card_settings`, `id_card_template_fields`, `groups`, `__EFMigrationsHistory`
  - Columnas de `schools` y de `groups` (p. ej. si existe `shift_id`)
  - Registros de `__EFMigrationsHistory`
- **Revisión ejecutada con psql** contra la BD local (`localhost`, base `schoolmanagement`, usuario `postgres`):
  - **Tablas:** Existían `schools` y `groups`. No existían `school_id_card_settings`, `id_card_template_fields` ni `__EFMigrationsHistory`.
  - **`schools`:** columnas `id`, `name`, `address`, `phone`, `logo_url`, `created_at`, `admin_id`, `created_by`, `updated_at`, `updated_by` — **no tiene `shift_id`**.
  - **`groups`:** sí tiene columna `shift_id` (correcto).

### 2.4 Aplicación del script en BD local

- Se ejecutó **`Scripts/ApplyIdCardTables_LocalOnly.sql`** con `psql` (PostgreSQL 18 en `C:\Program Files\PostgreSQL\18\bin`) contra la BD local.
- Resultado: creadas `id_card_template_fields` y `school_id_card_settings` en `schoolmanagement`.

---

## 3. Configuración para desarrollo local

- **`appsettings.Development.json`** (nuevo):  
  Define `DefaultConnection` para BD local cuando la app corre en entorno Development:
  - `Host=localhost;Database=schoolmanagement;Username=postgres;Password=Panama2020$`
- Así la app usa la BD local al hacer `dotnet run` sin cambiar la conexión de producción en `appsettings.json`.

---

## 4. Validación

- Se arrancó la app con BD local (Development) y se confirmó:
  - Escucha en **http://localhost:5172**
  - El script de inicio comprobó/creó las tablas de carnets según corresponda.
- Para validar: iniciar sesión con usuario admin/director/superadmin y abrir:
  - **http://localhost:5172/id-card/settings**
  - **http://localhost:5172/StudentIdCard/ui**

---

## 5. Compilación

- Primera compilación falló porque `SchoolManager.exe` estaba en uso (app en ejecución, proceso 69104).
- Se detuvo el proceso y se ejecutó de nuevo `dotnet build`: **compilación correcta.**

---

## 6. Resumen de archivos tocados o creados

| Acción | Archivo |
|--------|---------|
| Modificado | `Controllers/StudentIdCardController.cs` (autorización por roles) |
| Modificado | `Controllers/IdCardSettingsController.cs` (autorización por roles) |
| Creado | `Migrations/ApplyIdCardSettingsTables.sql` |
| Creado | `Scripts/ApplyIdCardTables_LocalOnly.sql` |
| Creado | `Scripts/EnsureIdCardTables.cs` |
| Creado | `Scripts/CheckDatabaseState.cs` |
| Modificado | `Program.cs` (llamada a `EnsureIdCardTables.EnsureAsync` al inicio) |
| Creado | `appsettings.Development.json` (conexión local) |
| Creado | Esta bitácora |

---

## 7. Notas

- Todos los cambios y pruebas se hicieron **en local** (código y BD local). No se desplegó ni se modificó Render.
- El error `column s.shift_id does not exist` indica que alguna consulta usa `shift_id` sobre el alias de `schools`; en la BD la tabla `schools` no tiene esa columna (está en `groups`). Si vuelve a aparecer, conviene localizar la consulta en el código.
- Para revisar el estado de la BD con la connection string de la app (local o Render), se puede añadir en `Program.cs` el argumento `--check-db` que ejecute `CheckDatabaseState.RunAsync(context)`.
