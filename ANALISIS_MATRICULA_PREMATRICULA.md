# Análisis de Lógica: Matrícula y Prematrícula

## 📋 Estado Actual del Sistema

### Flujo Actual Implementado:

1. **Prematrícula** (Pendiente → Prematriculado → Pagado → Matriculado)
   - ✅ Creación de prematrícula
   - ✅ Validación de condición académica (máximo 3 materias reprobadas)
   - ✅ Validación de período activo
   - ✅ Validación de grado (no retroceder, no saltar niveles)
   - ✅ Asignación automática de grupo por jornada
   - ✅ Generación de código único
   - ✅ Integración con pagos

2. **Matrícula** (Confirmación)
   - ✅ Verificación de pago confirmado
   - ✅ Creación de StudentAssignment
   - ✅ Envío de notificaciones (email y mensajería)

---

## ❌ PROBLEMAS Y FALTANTES EN LA LÓGICA

### 🔴 CRÍTICOS

#### 1. **Gestión de Estados Incompleta**
**Problema:** Los estados son strings hardcodeados sin validación de transiciones.

**Faltante:**
- ❌ No hay validación de transiciones de estado válidas
- ❌ No hay enum o constantes para los estados
- ❌ Estados posibles: `"Pendiente"`, `"Prematriculado"`, `"Pagado"`, `"Matriculado"`, `"Rechazado"`
- ❌ No se valida que no se pueda retroceder de estado
- ❌ Falta estado `"Cancelado"` o `"Anulado"`

**Solución sugerida:**
```csharp
public enum PrematriculationStatus
{
    Pendiente = 1,
    Prematriculado = 2,
    Pagado = 3,
    Matriculado = 4,
    Rechazado = 5,
    Cancelado = 6
}
```

#### 2. **Validación de Duplicados Insuficiente**
**Problema:** No se valida si el estudiante ya tiene una prematrícula activa para el mismo período.

**Faltante:**
- ❌ No verifica si el estudiante ya está prematriculado en el mismo período
- ❌ No verifica si el estudiante ya está matriculado en otro grupo/grado
- ❌ No previene múltiples prematrículas simultáneas

**Código faltante:**
```csharp
// En CreatePrematriculationAsync, antes de crear:
var existingActive = await _context.Prematriculations
    .Where(p => p.StudentId == dto.StudentId 
        && p.PrematriculationPeriodId == dto.PrematriculationPeriodId
        && (p.Status == "Prematriculado" || p.Status == "Pagado" || p.Status == "Matriculado"))
    .FirstOrDefaultAsync();

if (existingActive != null)
    throw new Exception("El estudiante ya tiene una prematrícula activa para este período");
```

#### 3. **Gestión de StudentAssignment Incompleta**
**Problema:** Al confirmar matrícula, no se maneja correctamente las asignaciones previas.

**Faltante:**
- ❌ No se inactiva/archiva el StudentAssignment anterior del estudiante
- ❌ No se valida si el estudiante ya tiene un StudentAssignment activo en otro grupo
- ❌ No se actualiza el ShiftId en StudentAssignment cuando se asigna por jornada
- ❌ No hay campo de estado o fecha de fin en StudentAssignment

**Código faltante:**
```csharp
// En ConfirmMatriculationAsync:
// 1. Inactivar asignaciones previas
var previousAssignments = await _context.StudentAssignments
    .Where(sa => sa.StudentId == prematriculation.StudentId 
        && sa.Id != existingAssignment?.Id)
    .ToListAsync();

// Marcar como inactivas o archivar
foreach (var prev in previousAssignments)
{
    // Agregar campo IsActive o EndDate
    prev.IsActive = false;
    prev.EndDate = DateTime.UtcNow;
}

// 2. Crear nueva asignación con ShiftId
if (existingAssignment == null)
{
    var assignment = new StudentAssignment
    {
        Id = Guid.NewGuid(),
        StudentId = prematriculation.StudentId,
        GradeId = prematriculation.GradeId.Value,
        GroupId = prematriculation.GroupId.Value,
        ShiftId = prematriculation.Student?.ShiftId, // FALTA ESTO
        CreatedAt = DateTime.UtcNow,
        IsActive = true // FALTA ESTE CAMPO
    };
}
```

#### 4. **Validación de Cupos Incompleta**
**Problema:** La validación de cupos no considera prematrículas pendientes/pagadas que aún no están matriculadas.

**Faltante:**
- ❌ Solo cuenta StudentAssignments, no cuenta prematrículas en estado "Pagado" o "Prematriculado"
- ❌ Puede haber sobrecupo si hay prematrículas que aún no se han matriculado

**Código faltante:**
```csharp
// En CheckGroupCapacityAsync y GetAvailableGroupsAsync:
var currentStudents = await _context.StudentAssignments
    .CountAsync(sa => sa.GroupId == groupId && sa.IsActive);

// CONTAR TAMBIÉN PREMATRÍCULAS RESERVADAS
var reservedSpots = await _context.Prematriculations
    .CountAsync(p => p.GroupId == groupId 
        && (p.Status == "Prematriculado" || p.Status == "Pagado" || p.Status == "Matriculado"));

var totalOccupied = currentStudents + reservedSpots;
var availableSpots = (group.MaxCapacity ?? int.MaxValue) - totalOccupied;
```

#### 5. **Validación de Grado por Período Académico**
**Problema:** No se valida que el grado seleccionado corresponda al año académico correcto.

**Faltante:**
- ❌ No hay validación de que el grado sea del año académico actual
- ❌ No se considera el año escolar (2024, 2025, etc.)
- ❌ Un estudiante podría prematricularse en un grado que no corresponde

---

### 🟡 IMPORTANTES

#### 6. **Manejo de Rechazo/Cancelación**
**Problema:** No hay lógica para rechazar o cancelar prematrículas.

**Faltante:**
- ❌ No hay método para rechazar una prematrícula
- ❌ No hay método para cancelar una prematrícula
- ❌ No se liberan cupos cuando se cancela
- ❌ No se notifica al usuario cuando se rechaza

**Código faltante:**
```csharp
public async Task<Prematriculation> RejectPrematriculationAsync(
    Guid prematriculationId, 
    string reason, 
    Guid rejectedBy)
{
    var prematriculation = await GetByIdAsync(prematriculationId);
    if (prematriculation == null)
        throw new Exception("Prematrícula no encontrada");

    if (prematriculation.Status == "Matriculado")
        throw new Exception("No se puede rechazar una matrícula ya confirmada");

    prematriculation.Status = "Rechazado";
    prematriculation.RejectionReason = reason;
    prematriculation.UpdatedAt = DateTime.UtcNow;
    // Agregar campo RejectedBy

    await _context.SaveChangesAsync();
    // Enviar notificación
    return prematriculation;
}
```

#### 7. **Validación de Pago Completo**
**Problema:** Solo verifica que exista un pago confirmado, no valida el monto total.

**Faltante:**
- ❌ No valida que el monto total del pago sea suficiente
- ❌ No valida múltiples pagos parciales
- ❌ No hay concepto de "monto requerido" por período

**Código faltante:**
```csharp
// En ConfirmMatriculationAsync:
var totalPaid = prematriculation.Payments
    .Where(p => p.PaymentStatus == "Confirmado")
    .Sum(p => p.Amount);

var requiredAmount = prematriculation.PrematriculationPeriod.RequiredAmount; // FALTA ESTE CAMPO
if (totalPaid < requiredAmount)
    throw new Exception($"El pago es insuficiente. Se requiere ${requiredAmount} pero se ha pagado ${totalPaid}");
```

#### 8. **Asignación Automática de Grupo Mejorable**
**Problema:** La lógica de asignación automática es básica.

**Faltante:**
- ❌ No considera balance de género
- ❌ No considera necesidades especiales (inclusión)
- ❌ No permite configuración de reglas de asignación
- ❌ No tiene fallback si no hay grupos disponibles

**Mejora sugerida:**
```csharp
// Agregar reglas configurables:
- Balance de género (50/50 si es posible)
- Estudiantes con necesidades especiales (distribuir)
- Historial académico (mezclar niveles)
- Preferencias del estudiante/acudiente
```

#### 9. **Historial y Auditoría**
**Problema:** No se registra quién hizo qué cambios.

**Faltante:**
- ❌ No se registra quién confirmó la matrícula
- ❌ No hay historial de cambios de estado
- ❌ No hay auditoría de modificaciones

**Código faltante:**
```csharp
// Agregar campos al modelo:
public Guid? ConfirmedBy { get; set; }
public Guid? RejectedBy { get; set; }
public DateTime? RejectedAt { get; set; }

// Crear tabla de historial:
public class PrematriculationHistory
{
    public Guid Id { get; set; }
    public Guid PrematriculationId { get; set; }
    public string PreviousStatus { get; set; }
    public string NewStatus { get; set; }
    public Guid ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
}
```

#### 10. **Validación de Documentos Requeridos**
**Problema:** No se valida que el estudiante tenga documentos completos.

**Faltante:**
- ❌ No verifica documentos del estudiante (cédula, certificados, etc.)
- ❌ No valida que el acudiente tenga documentos
- ❌ No hay checklist de documentos requeridos

---

### 🟢 MEJORAS RECOMENDADAS

#### 11. **Notificaciones Automáticas**
**Faltante:**
- ❌ No notifica cuando el período está por vencer
- ❌ No notifica recordatorios de pago
- ❌ No notifica cuando se asigna grupo automáticamente

#### 12. **Reportes y Estadísticas**
**Faltante:**
- ❌ No hay reporte de prematrículas por período
- ❌ No hay estadísticas de cupos ocupados/disponibles
- ❌ No hay reporte de estudiantes pendientes de matrícula

#### 13. **Validación de Edad/Grado**
**Faltante:**
- ❌ No valida que la edad del estudiante corresponda al grado
- ❌ No valida requisitos de edad mínima/máxima por grado

#### 14. **Manejo de Lista de Espera**
**Faltante:**
- ❌ No hay lista de espera cuando no hay cupos
- ❌ No hay notificación cuando se libera un cupo

#### 15. **Integración con Año Académico**
**Faltante:**
- ❌ No se relaciona con trimestres/períodos académicos
- ❌ No valida que el período de prematrícula corresponda al año académico correcto

---

## 📊 RESUMEN DE PRIORIDADES

### 🔴 ALTA PRIORIDAD (Implementar primero):
1. Validación de duplicados de prematrícula
2. Gestión correcta de StudentAssignment (inactivar anteriores)
3. Validación de cupos considerando prematrículas reservadas
4. Estados con enum y validación de transiciones
5. Validación de pago completo

### 🟡 MEDIA PRIORIDAD:
6. Métodos de rechazo/cancelación
7. Historial y auditoría
8. Validación de documentos requeridos
9. Asignación automática mejorada

### 🟢 BAJA PRIORIDAD:
10. Notificaciones automáticas
11. Reportes y estadísticas
12. Lista de espera
13. Validación de edad/grado

---

## 🔧 CAMBIOS NECESARIOS EN MODELOS

### Prematriculation:
```csharp
// Agregar campos:
public Guid? ConfirmedBy { get; set; }
public Guid? RejectedBy { get; set; }
public DateTime? RejectedAt { get; set; }
public PrematriculationStatus StatusEnum { get; set; } // Reemplazar string
```

### StudentAssignment:
```csharp
// Agregar campos:
public bool IsActive { get; set; } = true;
public DateTime? EndDate { get; set; }
public Guid? ShiftId { get; set; } // Ya existe pero no se usa
```

### PrematriculationPeriod:
```csharp
// Agregar campos:
public decimal RequiredAmount { get; set; }
public int? MaxPrematriculations { get; set; }
public bool AllowMultiplePrematriculations { get; set; } = false;
```

---

## ✅ CONCLUSIÓN

El sistema tiene una base sólida pero le faltan validaciones críticas y manejo de casos edge. Las prioridades son:

1. **Prevenir duplicados** y **gestionar correctamente las asignaciones**
2. **Validar cupos correctamente** considerando todas las reservas
3. **Mejorar la gestión de estados** con enums y validaciones
4. **Agregar funcionalidades de rechazo/cancelación**

Estos cambios harán el sistema más robusto y confiable.

