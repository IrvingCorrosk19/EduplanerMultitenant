# 📋 Análisis de Tablas para Módulo SchoolManager.ID

**Fecha:** 2025-01-XX  
**Propósito:** Diseñar módulo de identificación estudiantil (carnet digital)  
**Estado Base:** SchoolManager v1.0.0

---

## 📊 Resumen Ejecutivo

Este documento analiza las tablas existentes en la base de datos actual que son necesarias para diseñar el módulo **SchoolManager.ID** (sistema de identificación estudiantil con carnet digital).

### Tablas Analizadas

- ✅ **Existentes**: 12 tablas
- ⚠️ **Parciales**: 2 tablas (con campos relacionados)
- ❌ **No Existentes**: 9 tablas (requieren diseño)

---

## 1️⃣ IDENTIDAD DEL ESTUDIANTE

### 1.1 Tabla: `students`

**Estado:** ✅ **EXISTE**

#### Estructura Completa

```sql
CREATE TABLE students (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    school_id UUID REFERENCES schools(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    birth_date DATE,
    grade VARCHAR(20),                    -- Legacy: grado como string
    group_name VARCHAR(20),                -- Legacy: grupo como string
    parent_id UUID REFERENCES users(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
```

#### Campos Clave para ID Module

| Campo | Tipo | Descripción | Uso en ID |
|-------|------|-------------|-----------|
| `id` | UUID | Identificador único | ✅ Primary key para carnet |
| `school_id` | UUID | Escuela del estudiante | ✅ Validación de pertenencia |
| `name` | VARCHAR(100) | Nombre completo | ✅ Mostrar en carnet |
| `birth_date` | DATE | Fecha de nacimiento | ✅ Validación de edad |
| `parent_id` | UUID | Acudiente | ✅ Contacto de emergencia |

#### Análisis

**✅ Fortalezas:**
- UUID como primary key (ideal para sistemas distribuidos)
- Relación con `schools` (multi-escuela)
- Relación con `users` (acudiente)

**⚠️ Limitaciones:**
- No tiene foto del estudiante
- No tiene número de identificación (cédula/pasaporte)
- Campos `grade` y `group_name` son legacy (usar `student_assignments`)
- No tiene dirección física
- No tiene teléfono de contacto directo

**🔧 Recomendaciones para ID Module:**
- Usar `student_assignments` para obtener grado/grupo actual
- Agregar tabla `student_photos` o campo en `users` para foto
- Considerar agregar `document_id` (cédula) si no está en `users`

---

### 1.2 Tabla: `users`

**Estado:** ✅ **EXISTE** (Usada tanto para estudiantes como otros roles)

#### Estructura Completa

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    school_id UUID REFERENCES schools(id) ON DELETE SET NULL,
    name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL DEFAULT '',
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(100) NOT NULL,
    document_id VARCHAR(50) UNIQUE,       -- ✅ Cédula/Pasaporte
    date_of_birth TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    role VARCHAR(20) NOT NULL CHECK (role IN ('superadmin', 'admin', 'director', 'teacher', 'parent', 'student', 'estudiante', 'contable', 'contabilidad', 'acudiente')),
    status VARCHAR(10) DEFAULT 'active' CHECK (status IN ('active', 'inactive')),
    cellphone_primary VARCHAR(20),
    cellphone_secondary VARCHAR(20),
    two_factor_enabled BOOLEAN DEFAULT false,
    last_login TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE,
    created_by UUID REFERENCES users(id),
    updated_by UUID REFERENCES users(id),
    
    -- Campos específicos para estudiantes
    shift VARCHAR(20),                    -- Jornada: Mañana, Tarde, Noche
    disciplina BOOLEAN DEFAULT false,
    inclusion TEXT,                        -- ✅ Información de inclusión
    orientacion BOOLEAN DEFAULT false,
    inclusivo BOOLEAN DEFAULT false         -- ✅ Estudiante con necesidades especiales
);
```

#### Campos Clave para ID Module

| Campo | Tipo | Descripción | Uso en ID |
|-------|------|-------------|-----------|
| `id` | UUID | Identificador único | ✅ Primary key (mismo que students.id) |
| `document_id` | VARCHAR(50) | Cédula/Pasaporte | ✅ Mostrar en carnet |
| `name` + `last_name` | VARCHAR(100) | Nombre completo | ✅ Mostrar en carnet |
| `email` | VARCHAR(100) | Email | ✅ Contacto |
| `cellphone_primary` | VARCHAR(20) | Teléfono principal | ✅ Contacto de emergencia |
| `date_of_birth` | TIMESTAMP | Fecha de nacimiento | ✅ Validación de edad |
| `inclusion` | TEXT | Info de inclusión | ✅ Badge especial en carnet |
| `inclusivo` | BOOLEAN | Necesidades especiales | ✅ Badge especial en carnet |
| `shift` | VARCHAR(20) | Jornada | ✅ Mostrar en carnet |

#### Análisis

**✅ Fortalezas:**
- Tiene `document_id` (cédula) - **CRÍTICO para carnet**
- Tiene información de contacto (email, teléfono)
- Tiene campos de inclusión (`inclusion`, `inclusivo`)
- Tiene jornada (`shift`)
- Relación con escuela

**⚠️ Limitaciones:**
- No tiene foto del usuario
- No tiene dirección física
- `shift` es string (debería usar `shifts` table)
- No tiene tipo de sangre
- No tiene alergias o condiciones médicas

**🔧 Recomendaciones para ID Module:**
- **CRÍTICO**: Agregar campo `photo_url` o tabla `user_photos`
- Considerar agregar `address` si se necesita para carnet
- Usar `shifts` table en lugar de string `shift`
- Considerar tabla `student_medical_info` para información médica

---

### 1.3 Tabla: `user_roles` o Equivalente

**Estado:** ❌ **NO EXISTE** (Rol está en campo `users.role`)

#### Estructura Actual

El rol está almacenado como un campo `VARCHAR(20)` en la tabla `users`:

```sql
role VARCHAR(20) NOT NULL CHECK (
    role IN (
        'superadmin', 'admin', 'director', 'teacher', 
        'parent', 'student', 'estudiante', 
        'contable', 'contabilidad', 'acudiente'
    )
)
```

#### Análisis

**⚠️ Limitaciones:**
- No hay tabla separada de roles
- No hay sistema de permisos granular
- Roles hardcodeados en CHECK constraint
- No se puede agregar roles dinámicamente

**🔧 Recomendaciones para ID Module:**
- Para el módulo ID, el campo `role` es suficiente
- Verificar que el usuario tenga `role = 'student'` o `role = 'estudiante'`
- No se requiere tabla de roles para el módulo ID

---

### 1.4 Tabla: `student_profiles`

**Estado:** ❌ **NO EXISTE** (Solo existe como ViewModel en código)

#### Estructura Actual (ViewModel)

```csharp
public class StudentProfileViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    // ... otros campos
}
```

#### Análisis

**⚠️ Situación:**
- No existe tabla física `student_profiles`
- La información del perfil se obtiene de `users` + `students` + `student_assignments`
- Es una vista lógica, no una entidad persistente

**🔧 Recomendaciones para ID Module:**
- **NO crear tabla `student_profiles`**
- Usar join de `users` + `students` + `student_assignments` para obtener perfil completo
- El perfil se construye dinámicamente desde las tablas existentes

---

### 1.5 Tabla: `photos` / `media`

**Estado:** ❌ **NO EXISTE**

#### Análisis

**⚠️ Situación Crítica:**
- No hay tabla para almacenar fotos de estudiantes
- No hay sistema de gestión de medios
- Las fotos son **CRÍTICAS** para un carnet de identificación

**🔧 Recomendaciones para ID Module:**

**Opción 1: Campo en `users` (Simple)**
```sql
ALTER TABLE users ADD COLUMN photo_url VARCHAR(500);
-- Almacenar URL de Cloudinary o path local
```

**Opción 2: Tabla Separada (Recomendado)**
```sql
CREATE TABLE user_photos (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    photo_url VARCHAR(500) NOT NULL,
    photo_type VARCHAR(20) DEFAULT 'profile', -- profile, id_card, etc.
    is_active BOOLEAN DEFAULT true,
    uploaded_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    uploaded_by UUID REFERENCES users(id)
);

CREATE INDEX idx_user_photos_user_active ON user_photos(user_id, is_active) WHERE is_active = true;
```

**Ventajas de Opción 2:**
- Historial de fotos
- Múltiples tipos de fotos (perfil, carnet, etc.)
- Mejor organización
- Facilita auditoría

---

## 2️⃣ ESTRUCTURA ACADÉMICA

### 2.1 Tabla: `grade_levels`

**Estado:** ✅ **EXISTE**

#### Estructura Completa

```sql
CREATE TABLE grade_levels (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    school_id UUID REFERENCES schools(id) ON DELETE SET NULL,
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE,
    created_by UUID REFERENCES users(id),
    updated_by UUID REFERENCES users(id)
);
```

#### Campos Clave para ID Module

| Campo | Tipo | Descripción | Uso en ID |
|-------|------|-------------|-----------|
| `id` | UUID | Identificador único | ✅ FK en student_assignments |
| `name` | VARCHAR(100) | Nombre del grado | ✅ Mostrar en carnet |
| `school_id` | UUID | Escuela | ✅ Validación |

#### Análisis

**✅ Fortalezas:**
- Estructura simple y clara
- Relación con escuela
- Auditoría completa

**🔧 Uso en ID Module:**
- Obtener grado actual del estudiante desde `student_assignments`
- Mostrar en carnet: "10° Grado" o similar

---

### 2.2 Tabla: `groups`

**Estado:** ✅ **EXISTE**

#### Estructura Completa

```sql
CREATE TABLE groups (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    school_id UUID REFERENCES schools(id) ON DELETE CASCADE,
    name VARCHAR(20) NOT NULL,
    grade VARCHAR(20),                     -- Legacy: grado como string
    description TEXT,
    max_capacity INTEGER,
    shift VARCHAR(20),                    -- Legacy: jornada como string
    shift_id UUID REFERENCES shifts(id) ON DELETE SET NULL,  -- ✅ Relación con catálogo
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE,
    created_by UUID REFERENCES users(id),
    updated_by UUID REFERENCES users(id)
);
```

#### Campos Clave para ID Module

| Campo | Tipo | Descripción | Uso en ID |
|-------|------|-------------|-----------|
| `id` | UUID | Identificador único | ✅ FK en student_assignments |
| `name` | VARCHAR(20) | Nombre del grupo (A, B, C) | ✅ Mostrar en carnet |
| `shift_id` | UUID | Jornada | ✅ Mostrar jornada en carnet |
| `school_id` | UUID | Escuela | ✅ Validación |

#### Análisis

**✅ Fortalezas:**
- Relación con `shifts` (catálogo de jornadas)
- Relación con escuela
- Campo `max_capacity` para control de cupos

**⚠️ Limitaciones:**
- Campos legacy (`grade`, `shift` como strings) - mantener por compatibilidad

**🔧 Uso en ID Module:**
- Obtener grupo actual desde `student_assignments`
- Mostrar en carnet: "Grupo A" o "10° A"
- Obtener jornada desde `shift_id` → `shifts.name`

---

### 2.3 Tabla: `student_assignments`

**Estado:** ✅ **EXISTE** (Tabla CRÍTICA para ID Module)

#### Estructura Completa

```sql
CREATE TABLE student_assignments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    student_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    grade_id UUID NOT NULL REFERENCES grade_levels(id),
    group_id UUID NOT NULL REFERENCES groups(id),
    shift_id UUID REFERENCES shifts(id) ON DELETE SET NULL,  -- ✅ Jornada
    academic_year_id UUID REFERENCES academic_years(id) ON DELETE SET NULL,
    is_active BOOLEAN DEFAULT true,        -- ✅ Solo asignaciones activas
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    end_date TIMESTAMP WITH TIME ZONE      -- ✅ Fecha de finalización
);
```

#### Campos Clave para ID Module

| Campo | Tipo | Descripción | Uso en ID |
|-------|------|-------------|-----------|
| `student_id` | UUID | Estudiante | ✅ Primary key para búsqueda |
| `grade_id` | UUID | Grado actual | ✅ Mostrar en carnet |
| `group_id` | UUID | Grupo actual | ✅ Mostrar en carnet |
| `shift_id` | UUID | Jornada | ✅ Mostrar en carnet |
| `is_active` | BOOLEAN | Asignación activa | ✅ Filtrar solo activas |
| `academic_year_id` | UUID | Año académico | ✅ Validar vigencia |

#### Análisis

**✅ Fortalezas:**
- **TABLA CRÍTICA** para obtener información académica actual
- Campo `is_active` para filtrar asignaciones vigentes
- Relación con año académico (validar vigencia)
- Relación con jornada (`shift_id`)

**🔧 Uso en ID Module:**

**Query para obtener información académica actual:**
```sql
SELECT 
    sa.student_id,
    gl.name AS grade_name,
    g.name AS group_name,
    s.name AS shift_name,
    ay.name AS academic_year
FROM student_assignments sa
INNER JOIN grade_levels gl ON sa.grade_id = gl.id
INNER JOIN groups g ON sa.group_id = g.id
LEFT JOIN shifts s ON sa.shift_id = s.id
LEFT JOIN academic_years ay ON sa.academic_year_id = ay.id
WHERE sa.student_id = :student_id
  AND sa.is_active = true
ORDER BY sa.created_at DESC
LIMIT 1;
```

---

### 2.4 Tabla: `subject_assignments`

**Estado:** ✅ **EXISTE**

#### Estructura Completa

```sql
CREATE TABLE subject_assignments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    school_id UUID REFERENCES schools(id),
    specialty_id UUID NOT NULL REFERENCES specialties(id),
    area_id UUID NOT NULL REFERENCES areas(id),
    subject_id UUID NOT NULL REFERENCES subjects(id),
    grade_level_id UUID NOT NULL REFERENCES grade_levels(id),
    group_id UUID NOT NULL REFERENCES groups(id),
    status VARCHAR(10),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
```

#### Análisis

**🔧 Uso en ID Module:**
- **NO es crítica** para el módulo ID
- Se puede usar para mostrar materias del estudiante (opcional)
- No es necesaria para información básica del carnet

---

### 2.5 Tabla: `user_grades`

**Estado:** ✅ **EXISTE** (Tabla intermedia Many-to-Many)

#### Estructura Completa

```sql
CREATE TABLE user_grades (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    grade_id UUID NOT NULL REFERENCES grade_levels(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, grade_id)
);

CREATE INDEX IX_user_grades_grade_id ON user_grades(grade_id);
```

#### Análisis

**⚠️ Uso:**
- Relación many-to-many entre `users` y `grade_levels`
- Usado principalmente para docentes (asignar docentes a grados)
- **NO es crítica** para estudiantes en el módulo ID

**🔧 Recomendación:**
- Para estudiantes, usar `student_assignments` en lugar de `user_grades`
- `user_grades` es más para docentes/administradores

---

### 2.6 Tabla: `user_groups`

**Estado:** ✅ **EXISTE** (Tabla intermedia Many-to-Many)

#### Estructura Completa

```sql
CREATE TABLE user_groups (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    group_id UUID NOT NULL REFERENCES groups(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, group_id)
);

CREATE INDEX IX_user_groups_group_id ON user_groups(group_id);
```

#### Análisis

**⚠️ Uso:**
- Relación many-to-many entre `users` y `groups`
- Usado principalmente para docentes (asignar docentes a grupos)
- **NO es crítica** para estudiantes en el módulo ID

**🔧 Recomendación:**
- Para estudiantes, usar `student_assignments` en lugar de `user_groups`
- `user_groups` es más para docentes/administradores

---

## 3️⃣ ASISTENCIA Y CONTROL

### 3.1 Tabla: `attendance`

**Estado:** ✅ **EXISTE**

#### Estructura Completa

```sql
CREATE TABLE attendance (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    school_id UUID REFERENCES schools(id),
    student_id UUID REFERENCES users(id),
    teacher_id UUID REFERENCES users(id),
    group_id UUID REFERENCES groups(id),
    grade_id UUID REFERENCES grade_levels(id),
    date DATE NOT NULL,
    status VARCHAR(10) NOT NULL,          -- Presente, Ausente, Tardanza, etc.
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE,
    created_by UUID REFERENCES users(id),
    updated_by UUID REFERENCES users(id)
);
```

#### Campos Clave para ID Module

| Campo | Tipo | Descripción | Uso en ID |
|-------|------|-------------|-----------|
| `student_id` | UUID | Estudiante | ✅ Validar asistencia |
| `date` | DATE | Fecha | ✅ Validar vigencia |
| `status` | VARCHAR(10) | Estado | ✅ Mostrar estadísticas (opcional) |

#### Análisis

**🔧 Uso en ID Module:**
- **Opcional**: Mostrar estadísticas de asistencia en carnet digital
- Validar que el estudiante esté activo (tiene registros recientes)
- No es crítica para información básica del carnet

---

### 3.2 Tabla: `attendance_logs`

**Estado:** ❌ **NO EXISTE**

#### Análisis

**⚠️ Situación:**
- No hay tabla separada de logs de asistencia
- La tabla `attendance` actúa como log histórico
- Cada registro es un log de asistencia de un día

**🔧 Recomendación:**
- **NO es necesaria** para el módulo ID
- La tabla `attendance` ya funciona como log histórico
- Si se necesita más detalle, se puede agregar tabla `attendance_logs` en el futuro

---

### 3.3 Tabla: `security_settings`

**Estado:** ✅ **EXISTE**

#### Estructura Completa

```sql
CREATE TABLE security_settings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    school_id UUID REFERENCES schools(id) ON DELETE CASCADE,
    password_min_length INTEGER DEFAULT 8,
    require_uppercase BOOLEAN DEFAULT true,
    require_lowercase BOOLEAN DEFAULT true,
    require_numbers BOOLEAN DEFAULT true,
    require_special BOOLEAN DEFAULT true,
    expiry_days INTEGER DEFAULT 90,
    prevent_reuse INTEGER DEFAULT 5,
    max_login_attempts INTEGER DEFAULT 5,
    session_timeout_minutes INTEGER DEFAULT 30,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
```

#### Análisis

**🔧 Uso en ID Module:**
- **NO es crítica** para el módulo ID
- Se puede usar para validar políticas de seguridad al generar tokens
- Útil para configurar expiración de carnets digitales

**🔧 Recomendación:**
- Considerar agregar campos específicos para módulo ID:
  - `id_card_expiry_days` - Días de validez del carnet
  - `id_card_require_photo` - Requerir foto para carnet
  - `id_card_qr_expiry_hours` - Expiración del QR code

---

### 3.4 Tabla: `audit_logs`

**Estado:** ✅ **EXISTE**

#### Estructura Completa

```sql
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    school_id UUID REFERENCES schools(id),
    user_id UUID REFERENCES users(id),
    user_name VARCHAR(100),
    user_role VARCHAR(20),
    action VARCHAR(30),                   -- CREATE, UPDATE, DELETE, etc.
    resource VARCHAR(50),                 -- Tabla o recurso afectado
    details TEXT,                         -- Detalles del cambio
    ip_address VARCHAR(50),
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
```

#### Análisis

**🔧 Uso en ID Module:**
- **CRÍTICA** para auditoría de carnets
- Registrar cada vez que se genera/renueva un carnet
- Registrar accesos al carnet digital
- Registrar validaciones de QR codes

**🔧 Recomendación:**
- Agregar acciones específicas para módulo ID:
  - `ID_CARD_GENERATED` - Carnet generado
  - `ID_CARD_RENEWED` - Carnet renovado
  - `ID_CARD_ACCESSED` - Acceso al carnet digital
  - `ID_CARD_QR_VALIDATED` - QR code validado

---

## 4️⃣ BENEFICIOS / CONDICIONES ESPECIALES

### 4.1 Tabla: `student_benefits`

**Estado:** ❌ **NO EXISTE**

#### Análisis

**⚠️ Situación:**
- No hay tabla para beneficios estudiantiles
- No hay sistema de becas o descuentos
- Información de inclusión está en `users.inclusion` y `users.inclusivo`

**🔧 Recomendación para ID Module:**

**Opción 1: Usar campos existentes**
- `users.inclusion` - Texto libre con información
- `users.inclusivo` - Boolean para badge especial

**Opción 2: Crear tabla (si se necesita más detalle)**
```sql
CREATE TABLE student_benefits (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    student_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    benefit_type VARCHAR(50) NOT NULL,    -- scholarship, discount, transport, meal, etc.
    benefit_name VARCHAR(100),
    description TEXT,
    amount DECIMAL(10,2),                 -- Si es descuento monetario
    percentage DECIMAL(5,2),               -- Si es porcentaje
    start_date DATE,
    end_date DATE,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_student_benefits_student_active 
    ON student_benefits(student_id, is_active) 
    WHERE is_active = true;
```

**Uso en ID Module:**
- Mostrar badges especiales en carnet
- Mostrar información de beneficios activos
- Validar acceso a servicios (transporte, comedor)

---

### 4.2 Tabla: `scholarships`

**Estado:** ❌ **NO EXISTE**

#### Análisis

**🔧 Recomendación:**
- Usar tabla `student_benefits` con `benefit_type = 'scholarship'`
- O crear tabla específica si se necesita más campos:
  - Porcentaje de beca
  - Requisitos académicos
  - Documentos requeridos
  - Renovación anual

---

### 4.3 Tabla: `special_needs`

**Estado:** ⚠️ **PARCIAL** (Campos en `users`)

#### Estructura Actual

```sql
-- En tabla users:
inclusion TEXT,        -- Información de inclusión
inclusivo BOOLEAN,     -- Estudiante con necesidades especiales
```

#### Análisis

**⚠️ Limitaciones:**
- Solo campos básicos en `users`
- No hay detalle de necesidades específicas
- No hay información de adaptaciones requeridas

**🔧 Recomendación para ID Module:**

**Opción 1: Usar campos existentes (Simple)**
- `users.inclusivo = true` → Mostrar badge especial en carnet
- `users.inclusion` → Texto descriptivo (opcional)

**Opción 2: Crear tabla (Si se necesita más detalle)**
```sql
CREATE TABLE special_needs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    student_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    need_type VARCHAR(50) NOT NULL,        -- physical, learning, visual, hearing, etc.
    description TEXT,
    accommodations TEXT,                  -- Adaptaciones requeridas
    medical_info TEXT,                    -- Información médica relevante
    emergency_contact_name VARCHAR(100),
    emergency_contact_phone VARCHAR(20),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
```

**Uso en ID Module:**
- Mostrar badge de inclusión en carnet
- Información de emergencia médica
- Acceso rápido a contactos de emergencia

---

### 4.4 Tabla: `transport_assignments`

**Estado:** ❌ **NO EXISTE**

#### Análisis

**🔧 Recomendación para ID Module:**

```sql
CREATE TABLE transport_assignments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    student_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    route_id UUID,                        -- Si hay tabla de rutas
    route_name VARCHAR(100),
    pickup_location VARCHAR(200),
    dropoff_location VARCHAR(200),
    pickup_time TIME,
    dropoff_time TIME,
    driver_name VARCHAR(100),
    driver_phone VARCHAR(20),
    vehicle_plate VARCHAR(20),
    is_active BOOLEAN DEFAULT true,
    start_date DATE,
    end_date DATE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_transport_student_active 
    ON transport_assignments(student_id, is_active) 
    WHERE is_active = true;
```

**Uso en ID Module:**
- Mostrar información de transporte en carnet
- QR code para validar acceso al transporte
- Información de contacto del conductor

---

### 4.5 Tabla: `meal_plans`

**Estado:** ❌ **NO EXISTE**

#### Análisis

**🔧 Recomendación para ID Module:**

```sql
CREATE TABLE meal_plans (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    student_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    plan_type VARCHAR(50) NOT NULL,        -- breakfast, lunch, full_day, etc.
    plan_name VARCHAR(100),
    days_per_week INTEGER,                -- 5 días, solo lunes-viernes
    cost_per_month DECIMAL(10,2),
    start_date DATE,
    end_date DATE,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_meal_plans_student_active 
    ON meal_plans(student_id, is_active) 
    WHERE is_active = true;
```

**Uso en ID Module:**
- Mostrar información de plan de comidas en carnet
- QR code para validar acceso al comedor
- Mostrar días y horarios de comida

---

## 5️⃣ INFRAESTRUCTURA DE PERMISOS

### 5.1 Tabla: `roles`

**Estado:** ❌ **NO EXISTE** (Rol está en `users.role`)

#### Análisis

**⚠️ Situación:**
- Roles están hardcodeados en CHECK constraint
- No hay tabla de roles
- No se puede agregar roles dinámicamente

**🔧 Recomendación:**
- Para el módulo ID, **NO es necesario** crear tabla de roles
- El campo `users.role` es suficiente
- Solo se necesita verificar que `role = 'student'` o `role = 'estudiante'`

---

### 5.2 Tabla: `permissions`

**Estado:** ❌ **NO EXISTE**

#### Análisis

**🔧 Recomendación:**
- Para el módulo ID, **NO es necesario** crear tabla de permisos
- Los permisos se pueden manejar a nivel de aplicación
- Si se necesita en el futuro, se puede crear:
  ```sql
  CREATE TABLE permissions (
      id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
      name VARCHAR(100) NOT NULL UNIQUE,
      description TEXT,
      resource VARCHAR(50),               -- id_card, attendance, etc.
      action VARCHAR(50)                  -- view, generate, validate, etc.
  );
  ```

---

### 5.3 Tabla: `role_permissions`

**Estado:** ❌ **NO EXISTE**

#### Análisis

**🔧 Recomendación:**
- Para el módulo ID, **NO es necesario** crear tabla de role_permissions
- Los permisos se pueden manejar a nivel de aplicación
- Si se necesita en el futuro:
  ```sql
  CREATE TABLE role_permissions (
      role_name VARCHAR(20) NOT NULL,
      permission_id UUID NOT NULL REFERENCES permissions(id),
      PRIMARY KEY (role_name, permission_id)
  );
  ```

---

## 📊 RESUMEN DE TABLAS PARA ID MODULE

### ✅ Tablas Existentes y Listas para Usar

| Tabla | Estado | Uso en ID Module | Prioridad |
|-------|--------|------------------|-----------|
| `students` | ✅ Existe | Información básica del estudiante | 🔴 CRÍTICA |
| `users` | ✅ Existe | Identidad completa, foto (falta), cédula | 🔴 CRÍTICA |
| `grade_levels` | ✅ Existe | Grado académico | 🟡 IMPORTANTE |
| `groups` | ✅ Existe | Grupo académico | 🟡 IMPORTANTE |
| `student_assignments` | ✅ Existe | Información académica actual | 🔴 CRÍTICA |
| `shifts` | ✅ Existe | Jornada (Mañana/Tarde/Noche) | 🟡 IMPORTANTE |
| `schools` | ✅ Existe | Validación de escuela | 🟡 IMPORTANTE |
| `security_settings` | ✅ Existe | Configuración de seguridad | 🟢 OPCIONAL |
| `audit_logs` | ✅ Existe | Auditoría de carnets | 🟡 IMPORTANTE |
| `attendance` | ✅ Existe | Validación de actividad | 🟢 OPCIONAL |
| `user_grades` | ✅ Existe | No necesario para estudiantes | ⚪ NO USAR |
| `user_groups` | ✅ Existe | No necesario para estudiantes | ⚪ NO USAR |
| `subject_assignments` | ✅ Existe | No necesario para carnet básico | ⚪ NO USAR |

### ⚠️ Tablas Parciales (Campos en otras tablas)

| Concepto | Ubicación Actual | Recomendación |
|----------|------------------|---------------|
| Necesidades especiales | `users.inclusion`, `users.inclusivo` | ✅ Usar campos existentes |
| Roles | `users.role` (campo) | ✅ Usar campo existente |

### ❌ Tablas No Existentes (Requeridas para ID Module)

| Tabla | Prioridad | Recomendación |
|-------|-----------|---------------|
| `user_photos` o `student_photos` | 🔴 **CRÍTICA** | **CREAR** - Fotos son esenciales para carnet |
| `student_benefits` | 🟡 Opcional | Crear si se necesita mostrar beneficios |
| `transport_assignments` | 🟢 Opcional | Crear si se necesita transporte |
| `meal_plans` | 🟢 Opcional | Crear si se necesita comedor |
| `special_needs` (detallado) | 🟢 Opcional | Crear si se necesita más detalle |

---

## 🎯 QUERY PRINCIPAL PARA OBTENER DATOS DEL CARNET

### Query Completo para Información del Carnet

```sql
-- Query para obtener toda la información necesaria para generar un carnet
SELECT 
    -- Información del estudiante
    u.id AS student_id,
    u.document_id,
    u.name || ' ' || u.last_name AS full_name,
    u.email,
    u.cellphone_primary,
    u.date_of_birth,
    u.inclusivo AS has_special_needs,
    u.inclusion AS special_needs_info,
    
    -- Información académica actual
    gl.name AS grade_name,
    g.name AS group_name,
    s.name AS shift_name,
    ay.name AS academic_year,
    
    -- Información de la escuela
    sc.name AS school_name,
    sc.logo_url AS school_logo,
    
    -- Información de asignación
    sa.is_active AS assignment_active,
    sa.created_at AS assignment_date,
    sa.academic_year_id
    
FROM users u
INNER JOIN students st ON u.id = st.id  -- Si students tiene registro separado
INNER JOIN schools sc ON u.school_id = sc.id
LEFT JOIN student_assignments sa ON u.id = sa.student_id AND sa.is_active = true
LEFT JOIN grade_levels gl ON sa.grade_id = gl.id
LEFT JOIN groups g ON sa.group_id = g.id
LEFT JOIN shifts s ON sa.shift_id = s.id
LEFT JOIN academic_years ay ON sa.academic_year_id = ay.id
WHERE u.id = :student_id
  AND u.role IN ('student', 'estudiante')
  AND u.status = 'active'
ORDER BY sa.created_at DESC
LIMIT 1;
```

---

## 🔧 RECOMENDACIONES FINALES

### 1. Tablas Críticas a Crear

**🔴 PRIORIDAD ALTA:**
1. **`user_photos`** - Fotos de estudiantes (CRÍTICO para carnet)

### 2. Campos a Agregar

**En `users`:**
- `photo_url` VARCHAR(500) - Si no se crea tabla separada

**En `security_settings`:**
- `id_card_expiry_days` INTEGER - Días de validez del carnet
- `id_card_require_photo` BOOLEAN - Requerir foto para carnet

### 3. Tablas Opcionales (Según Requerimientos)

- `student_benefits` - Si se necesita mostrar beneficios
- `transport_assignments` - Si se necesita transporte
- `meal_plans` - Si se necesita comedor
- `special_needs` (detallado) - Si se necesita más detalle de inclusión

### 4. Estructura de Datos para Carnet Digital

```json
{
  "student_id": "uuid",
  "document_id": "string",
  "full_name": "string",
  "photo_url": "string",
  "grade": "string",
  "group": "string",
  "shift": "string",
  "academic_year": "string",
  "school_name": "string",
  "school_logo": "string",
  "has_special_needs": boolean,
  "benefits": [...],
  "transport": {...},
  "meal_plan": {...},
  "qr_code": "string",
  "expiry_date": "date",
  "issued_date": "date"
}
```

---

## ✅ CHECKLIST PARA IMPLEMENTACIÓN

### Fase 1: Estructura Base (Crítica)

- [ ] Crear tabla `user_photos` o agregar `photo_url` a `users`
- [ ] Verificar que `users.document_id` esté completo
- [ ] Verificar que `student_assignments` tenga datos activos
- [ ] Probar query principal de obtención de datos

### Fase 2: Funcionalidades Básicas

- [ ] Implementar generación de carnet digital
- [ ] Implementar generación de QR code
- [ ] Implementar validación de QR code
- [ ] Implementar renovación de carnet

### Fase 3: Funcionalidades Avanzadas (Opcional)

- [ ] Crear tabla `student_benefits` (si se necesita)
- [ ] Crear tabla `transport_assignments` (si se necesita)
- [ ] Crear tabla `meal_plans` (si se necesita)
- [ ] Implementar badges especiales en carnet

---

**Última actualización:** 2025-01-XX  
**Versión del documento:** 1.0  
**Estado:** ✅ Listo para diseño del módulo ID
