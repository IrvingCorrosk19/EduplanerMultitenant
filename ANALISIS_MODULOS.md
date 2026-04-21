# Análisis: Funcionamiento de los Módulos en SchoolManager

## 📋 Resumen Ejecutivo

El sistema **SchoolManager** utiliza una arquitectura modular **conceptual** donde los "módulos" no son componentes físicos separados, sino agrupaciones funcionales de servicios, controladores y modelos que trabajan juntos para cumplir objetivos específicos del negocio.

## 🏗️ Arquitectura Modular

### Estructura de los Módulos

Los módulos se implementan a través de:

1. **Servicios (`Services/Implementations`)**: Lógica de negocio
2. **Interfaces (`Services/Interfaces`)**: Contratos de servicios
3. **Controladores (`Controllers`)**: Endpoints HTTP/API
4. **Modelos (`Models`)**: Entidades de base de datos
5. **DTOs (`Dtos`)**: Objetos de transferencia de datos
6. **Vistas (`Views`)**: Interfaces de usuario (Razor)
7. **Menú (`MenuService`)**: Control de acceso por roles

### Registro de Servicios

Todos los servicios se registran en `Program.cs` mediante **Inyección de Dependencias**:

```csharp
builder.Services.AddScoped<IPrematriculationService, PrematriculationService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
// ... más de 40 servicios registrados
```

## 🧩 Módulos Identificados

### 1. MÓDULO DE PREMATRÍCULA Y MATRÍCULA

**Archivo de documentación**: `MÓDULO 1 - PREMATRICULA -  MATRICULA.txt`

#### Componentes Técnicos:

**Servicios:**
- `PrematriculationService` - Gestión de prematrículas
- `PrematriculationPeriodService` - Configuración de períodos
- `StudentAssignmentService` - Asignación de estudiantes a grupos
- `GroupService` - Gestión de grupos

**Controladores:**
- `PrematriculationController` - API de prematrícula
- `PrematriculationPeriodController` - Configuración de períodos
- `StudentAssignmentController` - Asignaciones

**Modelos:**
- `Prematriculation` - Entidad principal
- `PrematriculationPeriod` - Períodos
- `StudentAssignment` - Asignaciones

#### Flujo Funcional:

1. **Configuración Inicial** (Admin):
   - Define período de pre-matrícula (fechas inicio/fin)
   - Configura cupos máximos por grupo
   - Habilita/desactiva asignación automática

2. **Prematrícula** (Acudiente):
   - Validación automática: máximo 3 materias reprobadas
   - Verificación de cupos disponibles
   - Asignación automática de grupo (mantiene turno)
   - Estado: "Prematriculado"

3. **Validación de Pago**:
   - Integración con Módulo de Pagos
   - Estado cambia a "Pagado" cuando se confirma pago

4. **Matrícula Final**:
   - Confirmación automática al validar pago
   - Creación de `StudentAssignment`
   - Estado: "Matriculado"
   - Notificación al acudiente

#### Estados del Proceso:

```
Pendiente → Prematriculado → Pagado → Matriculado
                ↓
            Rechazado
```

#### Integraciones:

- **Módulo de Pagos**: Verificación de pago confirmado
- **Control Académico**: Validación de materias reprobadas
- **Sistema de Notificaciones**: Emails automáticos

---

### 2. MÓDULO DE PAGOS

**Archivo de documentación**: `MODULO DE PAGOS.txt`

#### Componentes Técnicos:

**Servicios:**
- `PaymentService` - Gestión de pagos
- `PaymentConceptService` - Conceptos de pago

**Controladores:**
- `PaymentController` - API de pagos
- `PaymentConceptController` - Gestión de conceptos

**Modelos:**
- `Payment` - Entidad principal
- `PaymentConcept` - Conceptos (matrícula, mensualidad, etc.)

#### Flujo Funcional:

1. **Pago desde Portal** (Acudiente):
   - Selección de concepto y estudiante
   - Métodos de pago:
     - **Tarjeta**: Confirmación automática (simulado)
     - **Transferencia/Depósito/Yappy**: Requiere comprobante adjunto
   - Estado: "Pendiente de verificación"

2. **Verificación Manual** (Contabilidad):
   - Revisión de comprobante
   - Validación en banco
   - Cambio a estado: "Confirmado"

3. **Activación Automática**:
   - Al confirmar pago, activa matrícula si corresponde
   - Integración con módulo de prematrícula

#### Estados del Pago:

```
Pendiente de verificación → Confirmado
```

#### Integraciones:

- **Módulo de Prematrícula**: Activa matrícula automáticamente
- **Sistema de Notificaciones**: Alerta a contabilidad sobre pagos pendientes

---

### 3. MÓDULO ACADÉMICO

#### Componentes Técnicos:

**Servicios:**
- `ActivityService` - Actividades académicas
- `ActivityTypeService` - Tipos de actividad
- `StudentActivityScoreService` - Calificaciones
- `SubjectService` - Materias
- `SubjectAssignmentService` - Asignación de materias
- `TeacherAssignmentService` - Asignación de docentes
- `TrimesterService` - Trimestres
- `GradeLevelService` - Niveles de grado
- `GroupService` - Grupos

**Controladores:**
- `ActivityController`
- `SubjectController`
- `SubjectAssignmentController`
- `TeacherAssignmentController`
- `TeacherGradebookController`
- `AcademicCatalogController`

#### Funcionalidades:

- Gestión de calificaciones
- Actividades académicas
- Asignación de docentes a materias
- Asignación de estudiantes a grupos
- Libro de calificaciones docente

---

### 4. MÓDULO DE ASISTENCIA

#### Componentes Técnicos:

**Servicios:**
- `AttendanceService` - Gestión de asistencia

**Controladores:**
- `AttendanceController`

**Modelos:**
- `Attendance` - Registros de asistencia

#### Funcionalidades:

- Registro de asistencia diaria
- Reportes estadísticos
- Historial de asistencia

---

### 5. MÓDULO DE REPORTES

#### Componentes Técnicos:

**Servicios:**
- `DisciplineReportService` - Reportes disciplinarios
- `OrientationReportService` - Reportes de orientación
- `StudentReportService` - Reportes de estudiantes
- `AprobadosReprobadosService` - Reportes de aprobados/reprobados

**Controladores:**
- `DisciplineReportController`
- `OrientationReportController`
- `StudentReportController`
- `AprobadosReprobadosController`

#### Funcionalidades:

- Reportes disciplinarios
- Reportes de orientación
- Reportes académicos
- Estadísticas de aprobados/reprobados

---

### 6. MÓDULO DE ADMINISTRACIÓN

#### Componentes Técnicos:

**Servicios:**
- `UserService` - Gestión de usuarios
- `SchoolService` - Gestión de escuelas
- `SecuritySettingService` - Configuración de seguridad
- `AuditLogService` - Registro de auditoría
- `EmailConfigurationService` - Configuración de email
- `EmailService` - Envío de emails
- `MessagingService` - Mensajería interna

**Controladores:**
- `UserController`
- `SchoolController`
- `SecuritySettingController`
- `AuditLogController`
- `EmailConfigurationController`
- `MessagingController`
- `SuperAdminController`

#### Funcionalidades:

- Gestión de usuarios y roles
- Configuración de seguridad
- Auditoría del sistema
- Configuración de emails
- Mensajería interna

---

## 🔐 Sistema de Autorización por Roles

### Control de Acceso

El sistema utiliza **autorización basada en roles** para controlar el acceso a los módulos:

**Políticas definidas en `Program.cs`:**
```csharp
options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin"));
options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
options.AddPolicy("Teacher", policy => policy.RequireRole("Teacher"));
options.AddPolicy("Student", policy => policy.RequireRole("Student"));
options.AddPolicy("Parent", policy => policy.RequireRole("Parent", "Acudiente"));
options.AddPolicy("Accounting", policy => policy.RequireRole("Contabilidad", "Admin", "SuperAdmin"));
```

### Menú Dinámico

El `MenuService` genera menús personalizados según el rol del usuario:

```csharp
public async Task<List<MenuItem>> GetMenuItemsForUserAsync(string role)
{
    // Filtra elementos del menú según el rol
    return allMenuItems
        .Where(m => m.RequiredRoles.Contains(role.ToLower()))
        .ToList();
}
```

**Roles disponibles:**
- `superadmin` - Acceso completo
- `admin` - Administración general
- `director` - Dirección
- `teacher` - Docente
- `student`/`estudiante` - Estudiante
- `parent`/`acudiente` - Acudiente
- `contabilidad` - Contabilidad

---

## 🔄 Integración Entre Módulos

### Ejemplo: Prematrícula ↔ Pagos

**Flujo de integración:**

1. **Prematrícula crea registro**:
   ```csharp
   // PrematriculationService.CreateAsync()
   prematriculation.Status = "Prematriculado";
   ```

2. **Pago se registra**:
   ```csharp
   // PaymentService.CreateAsync()
   if (payment.PaymentStatus == "Confirmado" && dto.PrematriculationId.HasValue)
   {
       prematriculation.Status = "Pagado";
       await _prematriculationService.ConfirmMatriculationAsync(prematriculation.Id);
   }
   ```

3. **Confirmación de matrícula**:
   ```csharp
   // PrematriculationService.ConfirmMatriculationAsync()
   // Verifica pago confirmado
   var hasConfirmedPayment = prematriculation.Payments.Any(p => p.PaymentStatus == "Confirmado");
   // Crea StudentAssignment
   // Cambia estado a "Matriculado"
   ```

### Patrón de Integración

Los módulos se integran mediante:

1. **Referencias entre servicios**: Un servicio inyecta otro servicio
2. **Relaciones en base de datos**: Foreign keys entre entidades
3. **Eventos/Notificaciones**: Emails automáticos al cambiar estados
4. **Validaciones cruzadas**: Verificación de condiciones entre módulos

---

## 📊 Patrón de Diseño

### Arquitectura en Capas

```
┌─────────────────────────────────┐
│      Controllers (API)          │
├─────────────────────────────────┤
│      Services (Lógica)          │
├─────────────────────────────────┤
│      Models (Entidades)         │
├─────────────────────────────────┤
│      Database Context           │
└─────────────────────────────────┘
```

### Inyección de Dependencias

Todos los servicios se registran como **Scoped** (una instancia por request):

```csharp
builder.Services.AddScoped<IService, Service>();
```

Esto permite:
- Reutilización de instancias durante un request
- Fácil testing mediante mocks
- Bajo acoplamiento entre componentes

---

## 🎯 Características Clave

### 1. **Modularidad Conceptual**
   - Los módulos no están físicamente separados
   - Agrupación lógica por funcionalidad
   - Fácil mantenimiento y extensión

### 2. **Autorización Flexible**
   - Control granular por rol
   - Menús dinámicos según usuario
   - Políticas de seguridad configurables

### 3. **Integración Automática**
   - Estados se actualizan automáticamente
   - Notificaciones automáticas
   - Validaciones cruzadas entre módulos

### 4. **Arquitectura Escalable**
   - Más de 40 servicios independientes
   - Fácil agregar nuevos módulos
   - Separación clara de responsabilidades

---

## 📝 Notas Técnicas

### Base de Datos

- **Motor**: PostgreSQL
- **ORM**: Entity Framework Core
- **Migraciones**: Code First

### Framework

- **Backend**: ASP.NET Core MVC
- **Frontend**: Razor Views + jQuery
- **Autenticación**: Cookie Authentication
- **Autorización**: Role-based Authorization

### Servicios Adicionales

- **Cloudinary**: Almacenamiento de archivos en la nube
- **Email Service**: Notificaciones por correo
- **Messaging Service**: Mensajería interna
- **Audit Log**: Registro de auditoría

---

## 🔍 Conclusión

El sistema **SchoolManager** utiliza una arquitectura modular **conceptual** donde:

1. **Los módulos son agrupaciones funcionales** de servicios, controladores y modelos
2. **Se integran mediante inyección de dependencias** y relaciones en base de datos
3. **El acceso se controla por roles** mediante políticas de autorización
4. **La arquitectura es escalable** y permite agregar nuevos módulos fácilmente

**Ventajas:**
- ✅ Mantenimiento sencillo
- ✅ Separación de responsabilidades
- ✅ Fácil testing
- ✅ Escalabilidad

**Consideraciones:**
- Los módulos no están físicamente separados (no hay proyectos separados)
- La documentación está en archivos .txt (considerar mover a documentación estructurada)
- El menú está hardcodeado en `MenuService` (considerar configuración dinámica)

---

**Última actualización**: 2025-01-XX
**Versión del sistema**: SchoolManager (ASP.NET Core MVC)

