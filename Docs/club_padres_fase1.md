# Fase 1 — Módulo Club de Padres: Base de datos y roles

**Proyecto:** SchoolManager  
**Alcance:** Solo preparación de base de datos y roles. Sin servicios ni controladores.

---

## 1. Tabla creada: `student_payment_access`

Tabla nueva para el módulo Club de Padres. No se modifican `users`, `student_id_cards` ni `payments`.

| Columna                      | Tipo (BD)                  | Nullable | Descripción |
|-----------------------------|----------------------------|----------|-------------|
| `id`                        | uuid                       | NO       | PK. Default: `uuid_generate_v4()`. |
| `student_id`                | uuid                       | NO       | FK → `users.id`. Estudiante (User con rol student/estudiante). |
| `school_id`                 | uuid                       | NO       | FK → `schools.id`. Escuela. |
| `carnet_status`             | varchar(20)                | NO       | **Default: "Pendiente".** Valores: Pendiente, Pagado, Impreso, Entregado. |
| `platform_access_status`   | varchar(20)                | NO       | **Default: "Pendiente".** Valores: Pendiente, Activo. |
| `carnet_status_updated_at`  | timestamp with time zone   | SÍ       | Última actualización de estado de carnet. |
| `platform_status_updated_at` | timestamp with time zone | SÍ       | Última actualización de estado de plataforma. |
| `carnet_updated_by_user_id` | uuid                       | SÍ       | FK → `users.id`. Usuario que realizó el último cambio de carnet. |
| `platform_updated_by_user_id` | uuid                     | SÍ       | FK → `users.id`. Usuario que realizó el último cambio de plataforma. |
| `created_at`                | timestamp with time zone   | NO       | Default: `CURRENT_TIMESTAMP`. |
| `updated_at`                | timestamp with time zone   | SÍ       | Última modificación del registro. |

**Claves foráneas:**

- `student_id` → `users(id)`. `ON DELETE RESTRICT`.
- `school_id` → `schools(id)`. `ON DELETE RESTRICT`.
- `carnet_updated_by_user_id` → `users(id)`. `ON DELETE SET NULL`.
- `platform_updated_by_user_id` → `users(id)`. `ON DELETE SET NULL`.

---

## 2. Índices creados

| Índice | Tipo    | Columnas | Uso |
|--------|---------|----------|-----|
| `student_payment_access_pkey` | PK | `id` | Clave primaria. |
| `IX_student_payment_access_student_id` | Índice | `student_id` | Búsqueda por estudiante. |
| `IX_student_payment_access_school_id` | Índice | `school_id` | Listados por escuela. |
| `IX_student_payment_access_carnet_status_school_id` | Índice | `carnet_status`, `school_id` | Listar carnets por estado y escuela (ej. pagados pendientes de impresión). |
| `IX_student_payment_access_student_id_school_id` | **Único** | `(student_id, school_id)` | Un registro por estudiante por escuela. |

EF Core puede haber generado además índices para las FKs opcionales (`carnet_updated_by_user_id`, `platform_updated_by_user_id`).

---

## 3. Roles añadidos al sistema

Se actualizó el enum **`SchoolManager.Enums.UserRole`** con dos nuevos valores:

| Rol               | Descripción |
|-------------------|-------------|
| **ClubParentsAdmin** | Club de Padres: solo registro de pagos (carnet y plataforma). |
| **QlServices**      | QL Services: marcar carnet Impreso / Entregado. |

**Uso:** El valor del enum se persiste como string en `users.role` (ej. `"ClubParentsAdmin"`, `"QlServices"`). La autorización en controladores usará estos nombres (normalmente en minúsculas donde el menú use `role.ToLower()`).

**Nota:** En esta fase no se ha creado ningún usuario con estos roles ni se ha cambiado menú ni controladores. Solo están disponibles en el enum para uso en Fase 2.

---

## 4. Archivos tocados en Fase 1

| Archivo | Cambio |
|---------|--------|
| `Models/StudentPaymentAccess.cs` | **Nuevo.** Entidad con propiedades y navegación a User y School. |
| `Models/SchoolDbContext.cs` | Añadido `DbSet<StudentPaymentAccess>` y configuración de la entidad (tabla, columnas, FKs, índices, valores por defecto). |
| `Enums/UserRole.cs` | Añadidos `ClubParentsAdmin` y `QlServices`. |
| `Migrations/20260315084229_AddStudentPaymentAccessAndClubRoles.cs` | Migración que crea la tabla `student_payment_access` y sus índices. (El mismo archivo puede incluir otros cambios de esquema si había diferencias pendientes en el modelo.) |
| `Migrations/20260315084229_AddStudentPaymentAccessAndClubRoles.Designer.cs` | Snapshot del modelo actualizado. |

---

## 5. No modificado (según requisitos)

- **User** — sin cambios.
- **StudentIdCard** — sin cambios.
- **Payment** — sin cambios.
- **AuthService** — sin cambios.
- **StudentController** — sin cambios.

---

## 6. Aplicar la migración

Para aplicar los cambios en la base de datos:

```bash
cd c:\Proyectos\EduplanerIIC\SchoolManager
dotnet ef database update --context SchoolDbContext
```

Para revertir esta migración:

```bash
dotnet ef migrations remove --context SchoolDbContext
```

(Revertir elimina la última migración; si en el mismo archivo hay otros cambios de esquema, se revierten también.)

---

## 7. Siguiente fase

**Fase 2** (según `club_padres_pago_design.md`): implementar servicios (`IClubParentsPaymentService`, `IQlServicesCarnetService`, `IPlatformAccessGuardService`) y endpoints/controladores para Club de Padres y QL Services. La lógica de negocio (transiciones de estado, alertas) se implementará en esa fase.
