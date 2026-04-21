# Prueba de Escritorio: Flujo de Matrícula y Prematrícula

## 📋 Datos Iniciales de Prueba

### Escuela
- **ID:** `school-001`
- **Nombre:** "Escuela San Miguel"

### Período de Prematrícula
- **ID:** `period-001`
- **SchoolId:** `school-001`
- **StartDate:** 2025-01-01 00:00:00
- **EndDate:** 2025-01-31 23:59:59
- **IsActive:** `true`
- **MaxCapacityPerGroup:** 30
- **AutoAssignByShift:** `true`

### Estudiante
- **ID:** `student-001`
- **Nombre:** "Juan Pérez"
- **DocumentId:** "8-123-456"
- **SchoolId:** `school-001`
- **Shift:** "Mañana"

### Calificaciones del Estudiante
| Materia | Actividad | Nota |
|---------|-----------|------|
| Matemáticas | Parcial 1 | 2.5 |
| Matemáticas | Parcial 2 | 3.5 |
| Matemáticas | Final | 2.8 |
| Español | Parcial 1 | 4.0 |
| Español | Parcial 2 | 3.8 |
| Ciencias | Parcial 1 | 2.0 |
| Ciencias | Parcial 2 | 2.5 |

**Cálculo de Promedios:**
- Matemáticas: (2.5 + 3.5 + 2.8) / 3 = 2.93 → **REPROBADA** ❌
- Español: (4.0 + 3.8) / 2 = 3.9 → **APROBADA** ✅
- Ciencias: (2.0 + 2.5) / 2 = 2.25 → **REPROBADA** ❌

**Total materias reprobadas: 2** ✅ (Cumple con el límite de ≤3)

### Grupo Disponible
- **ID:** `group-001`
- **Nombre:** "10° A"
- **SchoolId:** `school-001`
- **Grade:** "10°"
- **Shift:** "Mañana"
- **MaxCapacity:** 30
- **Estudiantes actuales:** 25

### Acudiente
- **ID:** `parent-001`
- **Nombre:** "María Pérez"
- **Role:** "acudiente"

---

## 🧪 Escenario de Prueba: Prematrícula Exitosa

### PASO 1: Crear Prematrícula

**Datos de entrada:**
```csharp
PrematriculationCreateDto dto = {
    StudentId: "student-001",
    GradeId: "grade-10",
    GroupId: "group-001",
    PrematriculationPeriodId: "period-001"
}
ParentId: "parent-001"
```

**Ejecución paso a paso:**

#### 1.1 Verificar Período
```csharp
// Línea 240-241: Obtener período
var period = await _context.PrematriculationPeriods
    .FirstOrDefaultAsync(p => p.Id == dto.PrematriculationPeriodId);
// ✅ Resultado: period encontrado (period-001)
```

#### 1.2 Validar Período Activo
```csharp
// Línea 247-249: Verificar período activo
var now = DateTime.UtcNow; // 2025-01-15 10:00:00
if (!period.IsActive || period.StartDate > now || period.EndDate < now)
// Verificación:
// - period.IsActive = true ✅
// - period.StartDate (2025-01-01) <= now (2025-01-15) ✅
// - period.EndDate (2025-01-31) >= now (2025-01-15) ✅
// ✅ Resultado: Período válido
```

#### 1.3 Obtener Estudiante
```csharp
// Línea 252-254: Obtener estudiante
var student = await _context.Users
    .Include(u => u.SchoolNavigation)
    .FirstOrDefaultAsync(u => u.Id == dto.StudentId);
// ✅ Resultado: student encontrado (student-001)
// ✅ SchoolId = "school-001"
```

#### 1.4 Validar Escuela
```csharp
// Línea 262-263: Verificar que el estudiante pertenezca a la escuela del período
if (period.SchoolId != schoolId)
// Verificación:
// - period.SchoolId = "school-001"
// - schoolId = "school-001"
// ✅ Resultado: Coinciden
```

#### 1.5 Validar Condición Académica
```csharp
// Línea 266: Calcular materias reprobadas
var failedSubjects = await GetFailedSubjectsCountAsync(dto.StudentId);
// Ejecución de GetFailedSubjectsCountAsync:
// 1. Obtener calificaciones del estudiante
// 2. Agrupar por materia
// 3. Calcular promedio por materia:
//    - Matemáticas: 2.93 < 3.0 → REPROBADA
//    - Español: 3.9 >= 3.0 → APROBADA
//    - Ciencias: 2.25 < 3.0 → REPROBADA
// 4. Contar materias reprobadas: 2
// ✅ Resultado: failedSubjects = 2

// Línea 267: Validar condición
var academicConditionValid = failedSubjects <= 3;
// Verificación: 2 <= 3 → true ✅

// Línea 269-272: Si no cumple, lanzar excepción
// ✅ No se lanza excepción (cumple la condición)
```

#### 1.6 Generar Código de Prematrícula
```csharp
// Línea 275: Generar código único
var prematriculationCode = await GeneratePrematriculationCodeAsync();
// Ejecución de GeneratePrematriculationCodeAsync:
// - Genera: "PRE-20250115-100000-5678"
// - Verifica que sea único en base de datos
// ✅ Resultado: "PRE-20250115-100000-5678"
```

#### 1.7 Crear Prematrícula
```csharp
// Línea 278-292: Crear objeto Prematriculation
var prematriculation = new Prematriculation {
    Id: Guid.NewGuid(), // "premat-001"
    SchoolId: "school-001",
    StudentId: "student-001",
    ParentId: "parent-001",
    GradeId: "grade-10",
    GroupId: "group-001",
    PrematriculationPeriodId: "period-001",
    Status: "Pendiente",
    FailedSubjectsCount: 2,
    AcademicConditionValid: true,
    PrematriculationCode: "PRE-20250115-100000-5678",
    CreatedAt: DateTime.UtcNow // 2025-01-15 10:00:00
}
```

#### 1.8 Verificar Cupo del Grupo
```csharp
// Línea 295-300: Si se especificó grupo, verificar cupo
if (dto.GroupId.HasValue) {
    var hasCapacity = await CheckGroupCapacityAsync(dto.GroupId.Value);
    // Ejecución de CheckGroupCapacityAsync:
    // 1. Obtener grupo: group-001 (MaxCapacity: 30)
    // 2. Contar estudiantes actuales: 25
    // 3. Verificar: 25 < 30 → true ✅
    // ✅ Resultado: hasCapacity = true
}
// ✅ No se lanza excepción
```

#### 1.9 Guardar Prematrícula
```csharp
// Línea 302-303: Guardar en base de datos
_context.Prematriculations.Add(prematriculation);
await _context.SaveChangesAsync();
// ✅ Resultado: Prematrícula guardada con Status = "Pendiente"
```

#### 1.10 Asignación Automática (No aplica)
```csharp
// Línea 306: Verificar si debe asignar automáticamente
if (period.AutoAssignByShift && !dto.GroupId.HasValue && dto.GradeId.HasValue)
// Verificación:
// - period.AutoAssignByShift = true
// - !dto.GroupId.HasValue = false (ya tiene grupo asignado)
// - dto.GradeId.HasValue = true
// ✅ No entra al if (ya tiene grupo asignado)
```

#### 1.11 Actualizar Estado a Prematriculado
```csharp
// Línea 325-330: Cambiar estado
if (prematriculation.Status == "Pendiente") {
    prematriculation.Status = "Prematriculado";
    _context.Prematriculations.Update(prematriculation);
    await _context.SaveChangesAsync();
}
// ✅ Resultado: Status = "Prematriculado"
```

**Estado final del paso 1:**
- ✅ Prematrícula creada: `premat-001`
- ✅ Estado: `"Prematriculado"`
- ✅ Código: `"PRE-20250115-100000-5678"`
- ✅ Grupo asignado: `group-001`
- ✅ Materias reprobadas: 2 (válido)

---

### PASO 2: Realizar Pago con Tarjeta

**Datos de entrada:**
```csharp
PaymentCreateDto dto = {
    StudentId: "student-001",
    PrematriculationId: "premat-001",
    PaymentConceptId: "concept-matricula",
    Amount: 100.00,
    PaymentMethod: "Tarjeta"
}
```

**Ejecución paso a paso:**

#### 2.1 Crear Pago
```csharp
// PaymentService.CreateAsync() - Línea 215-216
var payment = new Payment {
    Id: Guid.NewGuid(), // "payment-001"
    PrematriculationId: "premat-001",
    Amount: 100.00,
    PaymentMethod: "Tarjeta",
    PaymentStatus: "Confirmado", // Confirmado automáticamente por tarjeta
    PaymentDate: DateTime.UtcNow
}
_context.Payments.Add(payment);
await _context.SaveChangesAsync();
// ✅ Resultado: Pago creado con Status = "Confirmado"
```

#### 2.2 Actualizar Prematrícula (Pago Confirmado)
```csharp
// PaymentService.CreateAsync() - Línea 222-233
if (payment.PaymentStatus == "Confirmado" && dto.PrematriculationId.HasValue) {
    var prematriculation = await _context.Prematriculations
        .FirstOrDefaultAsync(p => p.Id == dto.PrematriculationId.Value);
    // ✅ Resultado: prematriculation encontrado (premat-001)
    
    if (prematriculation != null && prematriculation.Status == "Prematriculado") {
        // Verificación:
        // - prematriculation != null → true ✅
        // - prematriculation.Status == "Prematriculado" → true ✅
        
        prematriculation.Status = "Pagado";
        prematriculation.PaymentDate = DateTime.UtcNow;
        prematriculation.UpdatedAt = DateTime.UtcNow;
        _context.Prematriculations.Update(prematriculation);
        await _context.SaveChangesAsync();
        // ✅ Resultado: Status = "Pagado"
    }
}
```

#### 2.3 Activar Matrícula Automáticamente
```csharp
// PaymentService.CreateAsync() - Línea 236-238
try {
    await _prematriculationService.ConfirmMatriculationAsync(prematriculation.Id);
    // Ejecución de ConfirmMatriculationAsync:
    
    // 1. Obtener prematrícula con pagos
    var prematriculation = await _context.Prematriculations
        .Include(p => p.Payments)
        .FirstOrDefaultAsync(p => p.Id == prematriculationId);
    // ✅ Resultado: prematriculation encontrado
    
    // 2. Verificar pago confirmado (Línea 437)
    var hasConfirmedPayment = prematriculation.Payments
        .Any(p => p.PaymentStatus == "Confirmado");
    // Verificación:
    // - prematriculation.Payments contiene payment-001
    // - payment-001.PaymentStatus = "Confirmado"
    // ✅ Resultado: hasConfirmedPayment = true
    
    // 3. Si no hay pago confirmado, lanzar excepción (Línea 439-440)
    // ✅ No se lanza (hay pago confirmado)
    
    // 4. Actualizar estado a Matriculado (Línea 443-445)
    prematriculation.Status = "Matriculado";
    prematriculation.MatriculationDate = DateTime.UtcNow;
    prematriculation.UpdatedAt = DateTime.UtcNow;
    // ✅ Resultado: Status = "Matriculado"
    
    // 5. Crear StudentAssignment (Línea 448-465)
    if (prematriculation.GroupId.HasValue && prematriculation.GradeId.HasValue) {
        // Verificación:
        // - prematriculation.GroupId = "group-001" ✅
        // - prematriculation.GradeId = "grade-10" ✅
        
        var existingAssignment = await _context.StudentAssignments
            .FirstOrDefaultAsync(sa => sa.StudentId == prematriculation.StudentId 
                && sa.GroupId == prematriculation.GroupId.Value);
        // ✅ Resultado: existingAssignment = null (no existe)
        
        if (existingAssignment == null) {
            var assignment = new StudentAssignment {
                Id: Guid.NewGuid(), // "assignment-001"
                StudentId: "student-001",
                GradeId: "grade-10",
                GroupId: "group-001",
                CreatedAt: DateTime.UtcNow
            };
            _context.StudentAssignments.Add(assignment);
            // ✅ Resultado: StudentAssignment creado
        }
    }
    
    // 6. Guardar cambios (Línea 468-469)
    _context.Prematriculations.Update(prematriculation);
    await _context.SaveChangesAsync();
    // ✅ Resultado: Cambios guardados
    
    // 7. Enviar email de confirmación (Línea 474-493)
    // ✅ Email enviado (si el servicio está configurado)
    
    // 8. Enviar notificación en plataforma (Línea 496-564)
    // ✅ Notificación enviada (si el servicio está configurado)
}
```

**Estado final del paso 2:**
- ✅ Pago creado: `payment-001`
- ✅ Estado del pago: `"Confirmado"`
- ✅ Estado de prematrícula: `"Pagado"` → `"Matriculado"`
- ✅ StudentAssignment creado: `assignment-001`
- ✅ Notificaciones enviadas

---

### PASO 3: Verificación Final

**Estado final en base de datos:**

#### Tabla Prematriculations
```sql
| Id          | StudentId   | Status      | PaymentDate        | MatriculationDate | PrematriculationCode        |
|-------------|-------------|-------------|-------------------|-------------------|----------------------------|
| premat-001  | student-001 | Matriculado | 2025-01-15 10:05:00| 2025-01-15 10:05:00| PRE-20250115-100000-5678   |
```

#### Tabla Payments
```sql
| Id          | PrematriculationId | PaymentStatus | Amount | PaymentDate        |
|-------------|-------------------|---------------|--------|-------------------|
| payment-001 | premat-001        | Confirmado    | 100.00 | 2025-01-15 10:05:00|
```

#### Tabla StudentAssignments
```sql
| Id             | StudentId   | GradeId   | GroupId   | CreatedAt          |
|----------------|-------------|-----------|-----------|-------------------|
| assignment-001 | student-001 | grade-10  | group-001 | 2025-01-15 10:05:00|
```

---

## ✅ Verificación de Funcionalidad

### Validaciones Verificadas

1. **✅ Validación de Período Activo**
   - Verifica que la fecha actual esté dentro del rango
   - Bloquea si el período no está activo

2. **✅ Validación de Condición Académica**
   - Calcula correctamente materias reprobadas (2 materias)
   - Valida que sea ≤ 3 materias
   - Permite continuar si cumple

3. **✅ Validación de Cupos**
   - Verifica capacidad del grupo (25 < 30)
   - Permite asignar si hay cupos disponibles

4. **✅ Creación de Prematrícula**
   - Genera código único
   - Estado inicial: "Pendiente" → "Prematriculado"
   - Guarda correctamente todos los datos

5. **✅ Confirmación de Pago**
   - Pago con tarjeta se confirma automáticamente
   - Actualiza estado de prematrícula: "Prematriculado" → "Pagado"

6. **✅ Activación Automática de Matrícula**
   - Verifica que haya pago confirmado
   - Cambia estado: "Pagado" → "Matriculado"
   - Crea StudentAssignment
   - Envía notificaciones

### Flujo Completo Verificado

```
1. Crear Prematrícula
   ✅ Validación de período
   ✅ Validación académica (2 materias reprobadas ≤ 3)
   ✅ Validación de cupos (25 < 30)
   ✅ Estado: "Pendiente" → "Prematriculado"

2. Realizar Pago
   ✅ Pago con tarjeta se confirma automáticamente
   ✅ Estado: "Prematriculado" → "Pagado"

3. Matrícula Automática
   ✅ Verifica pago confirmado
   ✅ Estado: "Pagado" → "Matriculado"
   ✅ Crea StudentAssignment
   ✅ Envía notificaciones
```

---

## 🧪 Escenario de Prueba: Casos de Error

### Caso 1: Estudiante con Más de 3 Materias Reprobadas

**Datos:**
- Materias reprobadas: 4

**Resultado esperado:**
```csharp
// Línea 269-272
if (!academicConditionValid) {
    throw new Exception("El estudiante no puede participar en la prematrícula por exceder el límite de materias reprobadas");
}
// ✅ Excepción lanzada correctamente
```

### Caso 2: Período Inactivo

**Datos:**
- Fecha actual: 2025-02-15
- EndDate: 2025-01-31

**Resultado esperado:**
```csharp
// Línea 248-249
if (!period.IsActive || period.StartDate > now || period.EndDate < now)
    throw new Exception("El período de prematrícula no está disponible");
// ✅ Excepción lanzada (period.EndDate < now)
```

### Caso 3: Grupo Sin Cupos

**Datos:**
- Grupo: 30 estudiantes actuales
- MaxCapacity: 30

**Resultado esperado:**
```csharp
// Línea 298-299
if (!hasCapacity)
    throw new Exception("El grupo seleccionado no tiene cupos disponibles");
// ✅ Excepción lanzada (30 >= 30)
```

### Caso 4: Matrícula Sin Pago Confirmado

**Datos:**
- Prematrícula con Status: "Prematriculado"
- Sin pagos confirmados

**Resultado esperado:**
```csharp
// Línea 439-440
if (!hasConfirmedPayment)
    throw new Exception("No se puede confirmar la matrícula sin un pago confirmado");
// ✅ Excepción lanzada correctamente
```

---

## 📊 Resumen de Prueba de Escritorio

### ✅ Funcionalidades Verificadas

1. **Creación de Prematrícula**
   - ✅ Validación de período activo
   - ✅ Validación de condición académica
   - ✅ Validación de cupos
   - ✅ Generación de código único
   - ✅ Cambio de estado correcto

2. **Proceso de Pago**
   - ✅ Confirmación automática (tarjeta)
   - ✅ Actualización de estado de prematrícula
   - ✅ Integración con módulo de pagos

3. **Matrícula Automática**
   - ✅ Verificación de pago confirmado
   - ✅ Creación de StudentAssignment
   - ✅ Envío de notificaciones
   - ✅ Cambio de estado final

4. **Manejo de Errores**
   - ✅ Validaciones académicas
   - ✅ Validaciones de período
   - ✅ Validaciones de cupos
   - ✅ Validaciones de pago

### 🎯 Conclusión

**El flujo funciona correctamente según el código analizado.**

Todos los pasos se ejecutan en el orden correcto:
1. Validaciones → Crear Prematrícula → Estado "Prematriculado"
2. Pago → Confirmar → Estado "Pagado"
3. Matrícula Automática → Estado "Matriculado" → StudentAssignment creado

Las validaciones están correctamente implementadas y los errores se manejan apropiadamente.

---

**Prueba realizada:** 2025-01-XX
**Estado:** ✅ APROBADA

