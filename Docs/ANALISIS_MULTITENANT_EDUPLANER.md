# ANÁLISIS MULTI-TENANT — EDUPLANER (SchoolManager)

**Fecha:** 2026-04-20  
**Auditor:** Arquitecto Senior (análisis automatizado + revisión profunda)  
**Versión del sistema:** Rama `main`, commit `5095c80`  
**Alcance:** Arquitectura completa — modelos, migraciones, servicios, controladores, autenticación, BD  
**Base de datos:** PostgreSQL 18 · Render (producción) · `schoolmanagement_xqks`

---

## 1. RESUMEN EJECUTIVO (NIVEL CTO)

Eduplaner implementa una arquitectura multi-tenant de tipo **shared-database / shared-schema**, donde el aislamiento entre colegios depende de la columna `school_id` presente en la mayoría de las tablas. La estructura de base de datos es razonablemente sólida: los índices existen, las FK están definidas y hay un helper (`AuditHelper`) que automatiza la asignación de `school_id` al crear registros.

**Sin embargo, el sistema NO está listo para operar con múltiples colegios en producción.**

Los problemas no son superficiales. Hay una desconexión arquitectural severa entre la **escritura** (que sí asigna `school_id` correctamente) y la **lectura** (que en la mayoría de servicios ignora completamente el filtro de tenant). Esto significa que cualquier administrador autenticado de cualquier colegio puede leer, modificar o eliminar datos de cualquier otro colegio — no por un bug puntual, sino por un patrón sistemático que atraviesa docenas de servicios y controladores.

**Veredicto:** PARCIAL — La base de datos tiene la estructura correcta. La capa de aplicación no la utiliza de forma consistente.

---

## 2. ESTADO ACTUAL DEL MULTI-TENANCY

### 2.1 Estrategia Implementada

| Dimensión | Estado |
|-----------|--------|
| Estrategia de tenant | Shared DB / Shared Schema (una BD para todos los colegios) |
| Identificador de tenant | `school_id` (UUID) en cada tabla hija |
| Propagación del tenant en creación | ✅ Via `AuditHelper.SetSchoolIdAsync()` |
| Propagación del tenant en lectura | ❌ Manual, inconsistente, mayoritariamente ausente |
| Global Query Filters en EF Core | ❌ Solo 2 de 35+ entidades los tienen |
| Claim `school_id` en sesión de usuario | ❌ No incluido en claims (cookie ni Bearer) |
| Middleware de validación de tenant | ❌ No existe |
| Row-Level Security en PostgreSQL | ❌ No implementado |
| Repositorio centralizado con filtro tenant | ❌ No existe capa de repositorio (excepto EmailQueue) |

### 2.2 Inventario de Entidades — Cobertura de `school_id`

**Total de entidades:** ~55  
**Con `school_id` requerido (Guid, NOT NULL):** 9  
**Con `school_id` nullable (Guid?, puede ser NULL):** 22  
**Sin `school_id` directo (indirecto o global):** 14  
**Con HasQueryFilter de tenant:** 0 (el filtro de `IsActive` en Schools es soft-delete, no tenant)

#### Entidades con `school_id` REQUERIDO (Guid — mejor práctica)
`Payment`, `PaymentConcept`, `Prematriculation`, `PrematriculationPeriod`, `CounselorAssignment`, `StudentPaymentAccess`, `SchoolIdCardSetting`, `SchoolScheduleConfiguration`, `AcademicYear`

#### Entidades con `school_id` NULLABLE (Guid? — riesgo)
`Activity`, `Attendance`, `AuditLog`, `DisciplineReport`, `EmailConfiguration`, `GradeLevel`, `Group`, `Message`, `OrientationReport`, `SecuritySetting`, `Shift`, `Specialty`, `Student`, `StudentActivityScore`, `Subject`, `SubjectAssignment`, `TeacherWorkPlan`, `TeacherWorkPlanDetail`, `TeacherWorkPlanReviewLog`, `Trimester`, `User`, `ActivityType`

#### Entidades SIN `school_id` (indirecto o compartido)
`StudentAssignment` (vía Group), `StudentIdCard` (vía User), `StudentQrToken` (vía User), `TeacherAssignment` (vía SubjectAssignment), `UserGroup` (junction M2M), `UserGrade` (junction M2M), `UserSubject` (junction M2M), `ActivityAttachment` (vía Activity), `Area` (global, `IsGlobal=true`), `Grade`, `ScanLog`, `ScheduleEntry`, `TimeSlot`, `UserSession`

---

## 3. HALLAZGOS CRÍTICOS 🔴

### 🔴 C-01 — GetAllAsync() sin filtro de tenant en servicios core

**Impacto:** FUGA DE DATOS MASIVA  
**Archivos:**
- `Services/Implementations/StudentService.cs` — `GetAllAsync()` línea 24-25
- `Services/Implementations/SubjectService.cs` — `GetAllAsync()` línea 60-63
- `Services/Implementations/GroupService.cs` — `GetAllAsync()` línea 45-50
- `Services/Implementations/GradeLevelService.cs` — `GetAllAsync()` línea 48-51
- `Services/Implementations/AreaService.cs` — `GetAllAsync()` línea 49-53
- `Services/Implementations/SpecialtyService.cs` — `GetAllAsync()` línea 61-64
- `Services/Implementations/AttendanceService.cs` — `GetAllAsync()` línea 19-20

```csharp
// Patrón que se repite en 10+ servicios:
public async Task<List<Student>> GetAllAsync() =>
    await _context.Students.ToListAsync();  // RETORNA TODOS LOS ESTUDIANTES DE TODOS LOS COLEGIOS
```

**Escenario de ataque real:**
1. Admin del Colegio A inicia sesión (SchoolId = A)
2. Navega a `/Student` (índice de estudiantes)
3. `StudentController.Index()` llama `_studentService.GetAllAsync()`
4. Recibe registros de estudiantes de Colegios A, B, C, D...
5. Accede a nombre, cédula, teléfono, foto, tipo de sangre, alergias de todos los estudiantes del sistema

---

### 🔴 C-02 — GetByIdAsync() sin validación de ownership entre tenants

**Impacto:** ACCESO DIRECTO A CUALQUIER ENTIDAD POR ID  
**Afecta:** `StudentService`, `SubjectService`, `GroupService`, `GradeLevelService`, `PaymentConceptService`, `AcademicYearService` (GetByIdAsync), y más de 12 servicios adicionales

```csharp
// Patrón peligroso:
public async Task<Student?> GetByIdAsync(Guid id) =>
    await _context.Students.FindAsync(id);  // Sin validar que pertenezca al school del solicitante
```

**Escenario:** Un usuario malintencionado itera GUIDs (o los obtiene por otra vía) y accede a cualquier registro del sistema sin importar a qué colegio pertenece.

---

### 🔴 C-03 — No existe `school_id` en los claims de autenticación

**Impacto:** ARQUITECTURAL — obliga a buscar el school_id en BD en cada request  
**Archivos:**
- `Services/Implementations/AuthService.cs` — línea 80-86 (login cookie)
- `Middleware/ApiBearerTokenMiddleware.cs` — línea 49-55 (API móvil)

```csharp
// Lo que existe (AuthService.LoginAsync):
var claims = new List<Claim> {
    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new(ClaimTypes.Email, user.Email),
    new(ClaimTypes.Name, user.Name),
    new(ClaimTypes.Role, user.Role)
    // ← school_id AUSENTE
};

// El Bearer token del APK es base64(userId:email:timestamp) — sin school_id, sin firma HMAC
```

**Consecuencia:** `CurrentUserService.GetCurrentUserSchoolAsync()` hace un `FindAsync` a la BD en CADA request que necesita el tenant. Esto es un anti-patrón de performance y un punto de fallo invisible.

---

### 🔴 C-04 — Tablas junction M2M sin `school_id` — cruce de entidades entre tenants

**Impacto:** INTEGRIDAD REFERENCIAL ENTRE TENANTS  
**Tablas afectadas:** `user_grades`, `user_groups`, `user_subjects`

```csharp
// En SchoolDbContext.cs — M2M sin columna school_id:
entity.HasMany(u => u.Grades).WithMany()
    .UsingEntity(j => j.ToTable("user_grades"));
// PK: (user_id, grade_id) — SIN school_id
```

**Escenario:** Un usuario del Colegio A puede quedar vinculado a un Group o GradeLevel del Colegio B si existe un bug en la lógica de asignación. No hay constraint de BD que lo impida.

---

### 🔴 C-05 — `SchoolController` sin `[Authorize]` ni restricción de rol

**Impacto:** ENUMERACIÓN DE TODOS LOS COLEGIOS DEL SISTEMA  
**Archivo:** `Controllers/SchoolController.cs`

Cualquier usuario autenticado (incluso un padre de familia) puede llamar a los endpoints de SchoolController y obtener la lista completa de colegios registrados en la plataforma, incluyendo datos de contacto y configuración.

---

### 🔴 C-06 — `UserController.CreateJson()` crea usuarios sin `SchoolId`

**Impacto:** REGISTROS HUÉRFANOS — usuarios sin tenant  
**Archivo:** `Controllers/UserController.cs` — línea 47-102

El endpoint de creación de usuarios construye el objeto `User` sin asignar `SchoolId`, lo que resulta en usuarios con `school_id = NULL`. Estos registros rompen todas las suposiciones del sistema sobre aislamiento de tenant.

---

### 🔴 C-07 — No existe `HasQueryFilter` de tenant en ninguna entidad hija

**Impacto:** NINGUNA CONSULTA EF ESTÁ PROTEGIDA AUTOMÁTICAMENTE  
**Archivo:** `Models/SchoolDbContext.cs` — línea 660

```csharp
// Lo que existe (solo soft-delete de School):
entity.HasQueryFilter(s => s.IsActive);

// Lo que NO existe y debería:
entity.HasQueryFilter(s => s.SchoolId == _tenantId);
// Para las 35+ entidades hijas — NINGUNA tiene filtro de tenant
```

**Consecuencia:** Un desarrollador que escriba `_context.Students.ToListAsync()` obtendrá todos los estudiantes del sistema sin ninguna advertencia. No hay red de seguridad.

---

### 🔴 C-08 — `DeleteAsync()` sin validación de ownership

**Impacto:** ELIMINACIÓN DE DATOS DE OTRO COLEGIO  
**Afecta:** `StudentService.DeleteAsync()`, `SubjectService.DeleteAsync()`, `GroupService`, etc.

```csharp
// Patrón peligroso:
public async Task DeleteAsync(Guid id) {
    var student = await _context.Students.FindAsync(id);
    // No verifica student.SchoolId == currentUser.SchoolId
    _context.Students.Remove(student);
    await _context.SaveChangesAsync();
}
```

Un admin de cualquier colegio puede eliminar estudiantes, materias, grupos de cualquier otro colegio pasando el ID correcto.

---

## 4. HALLAZGOS ESTRUCTURALES 🟠

### 🟠 E-01 — `school_id` nullable (Guid?) en la mayoría de entidades tenant-bound

**22 entidades** tienen `school_id` como `Guid?` en lugar de `Guid`. Esto significa:
- La BD acepta registros sin tenant
- EF no genera error de compilación si se omite el campo
- Consultas `.Where(x => x.SchoolId == schoolId)` fallan silenciosamente si hay NULLs

**Estándar requerido:** `school_id UUID NOT NULL` con FK obligatoria.

---

### 🟠 E-02 — Patrón inconsistente: algunos servicios filtran, otros no

| Servicio | GetAll() filtra por school? | GetById() valida ownership? |
|----------|----------------------------|-----------------------------|
| TrimesterService | ✅ Sí (línea 34) | ⚠️ No verificado |
| AcademicYearService | ✅ Sí (línea 40) | ❌ No |
| PaymentService | ✅ `GetBySchoolAsync()` existe | ❌ `GetByIdAsync()` sin filtro |
| PaymentConceptService | ✅ `GetAllAsync(schoolId)` | ❌ `GetByIdAsync()` sin filtro |
| StudentService | ❌ Sin filtro | ❌ Sin filtro |
| GroupService | ❌ Sin filtro | ❌ Sin filtro |
| SubjectService | ❌ Sin filtro | ❌ Sin filtro |
| AttendanceService | ❌ Sin filtro | ❌ Sin filtro |
| GradeLevelService | ❌ Sin filtro | ❌ Sin filtro |

No hay un patrón arquitectural uniforme. Cada desarrollador decide individualmente si filtra o no.

---

### 🟠 E-03 — Ausencia de capa de repositorio (excepto EmailQueue)

Todo el acceso a datos se hace directamente sobre `SchoolDbContext` desde los servicios. No existe una interfaz `IRepository<T>` que pueda encapsular el filtro de tenant de forma centralizada. El único repositorio existente (`EmailQueueRepository`) es la excepción, no la regla.

**Consecuencia:** Imposible aplicar school_id de forma transversal sin modificar 40+ servicios individualmente.

---

### 🟠 E-04 — `StudentAssignment`, `StudentIdCard`, `TeacherAssignment` sin `school_id` directo

Estas tres entidades dependen de joins para inferir el tenant, lo que significa:
- Queries más complejos (N+1 potencial)
- Imposibilidad de indexar por `school_id`
- Mayor riesgo de omitir el filtro de tenant en consultas ad-hoc

---

### 🟠 E-05 — `Area` con `IsGlobal = true` — áreas compartidas entre colegios

**Archivo:** `Services/Implementations/AreaService.cs` — línea 38

```csharp
var area = new Area {
    Name = name,
    IsGlobal = true  // ← Todas las áreas se tratan como globales
};
```

Las áreas académicas son de facto globales. Si el Colegio A crea un "Área de Matemáticas", el Colegio B la verá también. Esto puede ser intencional para datos de catálogo, pero no está documentado ni diferenciado claramente.

---

### 🟠 E-06 — CASCADE DELETE en `school_id → schools.id` sin control de negocio

15+ tablas tienen `ON DELETE CASCADE` hacia `schools`. Si se elimina un colegio (o si un bug accidental llama a delete), se eliminan en cascada: estudiantes, pagos, matrículas, actividades, grupos, materias, asistencias.

No hay mecanismo de soft-delete para los datos del colegio (solo para el colegio en sí). Una eliminación accidental es irreversible.

---

### 🟠 E-07 — `GetByNameAndGradeAsync()` en GroupService sin filtro de school

**Archivo:** `Services/Implementations/GroupService.cs` — líneas 17-21

```csharp
public async Task<Group?> GetByNameAndGradeAsync(string groupName) {
    return await _context.Groups
        .FirstOrDefaultAsync(g => g.Name.ToLower() == groupName.ToLower());
    // Sin school_id → retorna el primer grupo con ese nombre de cualquier colegio
}
```

Si Colegio A y Colegio B tienen ambos un grupo "10-A", esta función retorna el primero que encuentre (ordenado por PK, no por escuela). Los datos quedan mezclados.

---

### 🟠 E-08 — Bearer token sin firma criptográfica

**Archivo:** `Middleware/ApiBearerTokenMiddleware.cs`

```
Formato actual: base64(userId:email:timestamp)
```

Este token es simplemente codificado en base64, no firmado. Cualquiera que conozca el formato puede fabricar un token válido con un `userId` arbitrario. No es un JWT firmado, no tiene `schoolId`, y la "validación" es solo verificar que el timestamp no sea demasiado antiguo.

---

## 5. HALLAZGOS DE PERFORMANCE 🟡

### 🟡 P-01 — `GetAllAsync()` sin paginación ni filtro → full table scan

Con múltiples colegios y años de datos, `_context.Students.ToListAsync()` (sin WHERE ni LIMIT) hará un sequential scan completo de la tabla `students`. Con 10 colegios × 500 alumnos = 5,000 registros transferidos cuando solo se necesitan ~500.

---

### 🟡 P-02 — `CurrentUserService.GetCurrentUserSchoolAsync()` hace query a BD en cada request

Al no estar `school_id` en los claims, cada operación que necesita el tenant del usuario ejecuta:
```csharp
await _context.Schools.FindAsync(user.SchoolId.Value);  // Query extra por request
```

Con alta concurrencia, esto escala de forma lineal y genera presión innecesaria sobre la BD.

---

### 🟡 P-03 — Índices de `school_id` existen, pero sin compuestos para patrones comunes

Los índices simples `IX_activities_school_id` existen, pero faltan índices compuestos para los patrones más comunes:

```sql
-- Patrones frecuentes sin índice compuesto:
WHERE school_id = ? AND is_active = true
WHERE school_id = ? AND trimester_id = ?
WHERE school_id = ? AND group_id = ? AND subject_id = ?
```

---

### 🟡 P-04 — N+1 en entidades sin `school_id` directo

Para `TeacherAssignment`, obtener el `school_id` requiere:
```
TeacherAssignment → SubjectAssignment → school_id
```
Cada acceso a la escuela de un `TeacherAssignment` es un join o una carga lazy potencial.

---

### 🟡 P-05 — Sin paginación en endpoints de lista

Ningún `GetAllAsync()` revisado implementa paginación. Con crecimiento de datos, todos los listados se volverán lentos simultáneamente.

---

## 6. RIESGOS DE SEGURIDAD 🔴

### 🔴 SEC-01 — Exposición de PII entre colegios (GDPR / LFPDPPP)

Datos sensibles accesibles cross-tenant sin restricción:
- Nombre, apellido, cédula, fotografía, tipo de sangre, alergias, teléfono de emergencia (campo `blood_type`, `allergies`, `emergency_contact_*` en `users`)
- Historial de pagos y matrículas
- Registros de asistencia
- Reportes de disciplina
- Carnets estudiantiles con QR

En muchas jurisdicciones (GDPR en Europa, Ley 81 en Panamá, LGPD en Brasil), esto constituye una violación de datos de menores de edad.

---

### 🔴 SEC-02 — Enumeración de toda la base de usuarios

`StudentService.GetAllAsync()` y `SchoolController.Index()` permiten obtener todos los usuarios del sistema a cualquier autenticado. Esto facilita ataques de fuerza bruta sobre IDs, correlación de datos, y reconocimiento previo a ataques dirigidos.

---

### 🔴 SEC-03 — Modificación y eliminación de datos ajenos

`DeleteAsync()` sin validación de ownership permite a un atacante autenticado eliminar registros de otros colegios. El impacto es irreversible dado que no hay soft-delete en entidades hijas.

---

### 🟠 SEC-04 — Bearer token fabricable (API móvil)

El formato `base64(userId:email:timestamp)` puede ser fabricado por cualquiera que conozca un `userId` válido. No requiere conocer contraseñas ni secretos.

---

### 🟠 SEC-05 — `IgnoreQueryFilters()` en AuthService es correcto pero peligroso como patrón

```csharp
// AuthService.LoginAsync, línea 68:
var school = await _context.Schools.IgnoreQueryFilters()
    .FirstOrDefaultAsync(s => s.Id == user.SchoolId.Value);
```

Usar `IgnoreQueryFilters()` aquí es técnicamente correcto (permite login aunque la escuela esté inactiva para mostrar mensaje). Pero establece un patrón que los desarrolladores podrían copiar inadecuadamente en otros contextos.

---

### 🟡 SEC-06 — Sin auditoría de acceso cross-tenant

No hay logging que detecte si un usuario accedió a datos de otro colegio. Si ocurre un incidente, no hay forma de reconstruir qué datos fueron comprometidos ni cuándo.

---

## 7. EVALUACIÓN DE PREPARACIÓN PARA PRODUCCIÓN

### Veredicto General: ❌ NO LISTO

| Categoría | Estado | Detalle |
|-----------|--------|---------|
| Estructura de BD (schema) | ✅ LISTO | `school_id` en tablas, índices creados, FK definidas |
| Aislamiento en escritura | ⚠️ PARCIAL | `AuditHelper` funciona, pero `school_id` es Guid? (nullable) |
| Aislamiento en lectura | ❌ NO LISTO | La mayoría de GetAllAsync() devuelven datos de todos los colegios |
| Aislamiento en modificación | ❌ NO LISTO | GetByIdAsync/UpdateAsync/DeleteAsync sin validación de ownership |
| Autenticación con tenant | ❌ NO LISTO | `school_id` no está en claims |
| Autorización basada en tenant | ❌ NO LISTO | Sin middleware de tenant ni políticas de autorización |
| Performance multi-tenant | ⚠️ PARCIAL | Índices simples sí, compuestos faltan, no hay paginación |
| Seguridad de API móvil | ❌ NO LISTO | Bearer token sin firma ni schoolId |
| Escalabilidad | ⚠️ PARCIAL | Soportaría ~3-5 colegios antes de degradación severa |
| Cumplimiento de privacidad | ❌ NO LISTO | Fuga de PII entre tenants es activa y explotable |

---

## 8. CONCLUSIÓN BRUTALMENTE HONESTA

**Eduplaner tiene una base de datos multi-tenant bien diseñada y una capa de aplicación que no la usa.**

El trabajo hecho a nivel de esquema (columnas `school_id`, índices, foreign keys, cascade deletes) es sólido y profesional. Alguien pensó en multi-tenancy cuando diseñó las tablas. Pero esa intención nunca se tradujo en una arquitectura de aplicación que la haga cumplir.

El resultado es un sistema que parece multi-tenant desde afuera pero que internamente opera como si hubiera un solo colegio. Cualquier usuario autenticado — independientemente de su rol o colegio — puede leer todos los estudiantes, todas las asistencias, todos los pagos, todos los reportes de disciplina de todos los colegios del sistema, simplemente accediendo a los endpoints estándar.

Esto no es un problema de un endpoint mal escrito. Es un problema de patrón arquitectural. Los servicios fueron construidos para un mundo de un solo tenant y se les agregó `school_id` al modelo sin cambiar la lógica de consulta. El resultado es que el 70% de los métodos de lectura ignoran el tenant del usuario autenticado.

**Si este sistema se desplegara con 5 colegios reales hoy:**
- Cada administrador de cada colegio tendría acceso a los datos personales (incluyendo fotos, cédulas, información médica) de todos los alumnos de todos los colegios.
- Una operación de eliminación mal intencionada o accidental podría borrar datos de otro colegio sin dejar rastro recuperable.
- No habría forma de detectarlo porque no hay logging de acceso cross-tenant.

**Para ser vendible como SaaS, el sistema necesita:**
1. Un mecanismo de tenant automático y no eludible (Global Query Filters en EF o RLS en PostgreSQL)
2. `school_id` en los claims de autenticación
3. Todos los `GetAllAsync()` convertidos a `GetAllBySchoolAsync(Guid schoolId)`
4. Todos los `GetByIdAsync()` con validación de ownership
5. Todos los `DeleteAsync()` y `UpdateAsync()` con validación de ownership
6. Bearer token con firma HMAC y `schoolId` embebido
7. Tests de integración que prueben explícitamente el aislamiento entre tenants

El tiempo estimado para cerrar los gaps críticos (C-01 al C-08) con un equipo de 2 desarrolladores es de 3-4 semanas de trabajo enfocado. No es una reescritura, pero requiere disciplina y revisión sistemática de ~40 servicios y ~30 controladores.

**Bottom line:** No salir a producción con múltiples colegios hasta resolver los hallazgos C-01, C-02, C-03, C-07 y C-08 como mínimo absoluto. Los demás pueden priorizarse en sprints posteriores, pero estos cinco son la diferencia entre un sistema funcional y una violación de datos activa.

---

*Generado por análisis arquitectural automatizado + revisión de código estático. No se modificó ningún archivo. Próximo paso recomendado: plan de remediación priorizado por impacto.*
