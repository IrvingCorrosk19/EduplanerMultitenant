# ✅ Checklist: Completar Módulo de Matrícula y Prematrícula al 100%

## 🟢 IMPLEMENTADO

- ✅ Creación de prematrícula
- ✅ Validación de condición académica (max 3 materias reprobadas)
- ✅ Validación de período activo
- ✅ Validación de grado (no retroceder, no saltar niveles)
- ✅ Asignación automática de grupo por jornada
- ✅ Generación de código único
- ✅ Integración con pagos
- ✅ Confirmación de matrícula
- ✅ Validación condición académica ANTES de confirmar
- ✅ Asignación automática de grado/grupo (para estudiantes existentes)
- ✅ Validaciones para nuevo ingreso (documentos, acudiente)
- ✅ Detección de estudiante nuevo (IsNewStudentAsync)
- ✅ Validación de documentos requeridos
- ✅ Validación de acudiente para menores
- ✅ Requiere asignación manual de grado para nuevo ingreso

---

## 🔴 CRÍTICO - FALTA IMPLEMENTAR

### 1. **Validación de Duplicados de Prematrícula** ❌
**Problema:** Un estudiante puede crear múltiples prematrículas activas para el mismo período.

**Código faltante:**
```csharp
// En CreatePrematriculationAsync, ANTES de crear:
var existingActive = await _context.Prematriculations
    .Where(p => p.StudentId == dto.StudentId 
        && p.PrematriculationPeriodId == dto.PrematriculationPeriodId
        && (p.Status == "Prematriculado" || p.Status == "Pagado" || p.Status == "Matriculado" || p.Status == "Pendiente"))
    .FirstOrDefaultAsync();

if (existingActive != null)
{
    throw new Exception($"El estudiante ya tiene una prematrícula activa para este período (Estado: {existingActive.Status}, Código: {existingActive.PrematriculationCode})");
}
```

### 2. **Gestión Correcta de StudentAssignment** ❌
**Problema:** Al confirmar matrícula, no se inactivan asignaciones previas del estudiante.

**Faltante:**
- Modelo StudentAssignment necesita campo `IsActive` o `EndDate`
- Al crear nueva asignación, inactivar las anteriores
- Validar que no tenga múltiples asignaciones activas

**Código faltante:**
```csharp
// En ConfirmMatriculationAsync, ANTES de crear StudentAssignment:
// 1. Inactivar asignaciones previas
var previousAssignments = await _context.StudentAssignments
    .Where(sa => sa.StudentId == prematriculation.StudentId)
    .ToListAsync();

foreach (var prev in previousAssignments)
{
    // Si StudentAssignment tiene IsActive:
    prev.IsActive = false;
    prev.EndDate = DateTime.UtcNow;
}

// 2. Validar que no exista ya una asignación activa para este grupo/grado
var existingActive = previousAssignments
    .FirstOrDefault(sa => sa.GroupId == prematriculation.GroupId.Value 
        && sa.GradeId == prematriculation.GradeId.Value);

if (existingActive != null && existingActive.IsActive)
{
    throw new Exception("El estudiante ya tiene una asignación activa para este grupo y grado");
}
```

### 3. **Validación de Cupos Considerando Prematrículas** ❌
**Problema:** La validación de cupos solo cuenta StudentAssignments, no cuenta prematrículas que reservan cupos.

**Código faltante:**
```csharp
// En CheckGroupCapacityAsync:
public async Task<bool> CheckGroupCapacityAsync(Guid groupId)
{
    var group = await _context.Groups.FindAsync(groupId);
    if (group == null)
        return false;

    // Contar estudiantes matriculados (StudentAssignments activos)
    var currentStudents = await _context.StudentAssignments
        .CountAsync(sa => sa.GroupId == groupId && sa.IsActive); // Si hay campo IsActive

    // Contar prematrículas que reservan cupos
    var reservedSpots = await _context.Prematriculations
        .CountAsync(p => p.GroupId == groupId 
            && (p.Status == "Prematriculado" || p.Status == "Pagado" || p.Status == "Matriculado"));

    var totalOccupied = currentStudents + reservedSpots;
    var maxCapacity = group.MaxCapacity ?? int.MaxValue;
    
    return totalOccupied < maxCapacity;
}

// En GetAvailableGroupsAsync:
// Ya está parcialmente implementado pero hay que mejorarlo
// Actualmente solo cuenta StudentAssignments, no cuenta prematrículas reservadas
```

### 4. **Validación de Pago Completo (Monto Total)** ❌
**Problema:** Solo verifica que exista un pago confirmado, no valida el monto total requerido.

**Faltante:**
- Campo `RequiredAmount` en PrematriculationPeriod
- Validar suma total de pagos confirmados

**Código faltante:**
```csharp
// Agregar campo a PrematriculationPeriod:
public decimal? RequiredAmount { get; set; }

// En ConfirmMatriculationAsync:
var totalPaid = prematriculation.Payments
    .Where(p => p.PaymentStatus == "Confirmado")
    .Sum(p => p.Amount);

var requiredAmount = prematriculation.PrematriculationPeriod.RequiredAmount ?? 0;

if (requiredAmount > 0 && totalPaid < requiredAmount)
{
    throw new Exception($"El pago es insuficiente. Se requiere ${requiredAmount:F2} pero se ha pagado ${totalPaid:F2}");
}
```

### 5. **Gestión de Estados con Enum** ❌
**Problema:** Los estados son strings hardcodeados sin validación de transiciones.

**Código faltante:**
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

// Método para validar transiciones válidas:
public bool CanTransitionTo(PrematriculationStatus current, PrematriculationStatus newStatus)
{
    return current switch
    {
        PrematriculationStatus.Pendiente => newStatus == PrematriculationStatus.Prematriculado || 
                                            newStatus == PrematriculationStatus.Rechazado || 
                                            newStatus == PrematriculationStatus.Cancelado,
        PrematriculationStatus.Prematriculado => newStatus == PrematriculationStatus.Pagado || 
                                                 newStatus == PrematriculationStatus.Rechazado || 
                                                 newStatus == PrematriculationStatus.Cancelado,
        PrematriculationStatus.Pagado => newStatus == PrematriculationStatus.Matriculado || 
                                         newStatus == PrematriculationStatus.Rechazado || 
                                         newStatus == PrematriculationStatus.Cancelado,
        PrematriculationStatus.Matriculado => false, // No se puede cambiar
        PrematriculationStatus.Rechazado => false, // No se puede cambiar
        PrematriculationStatus.Cancelado => false, // No se puede cambiar
        _ => false
    };
}
```

---

## 🟡 IMPORTANTE - FALTA IMPLEMENTAR

### 6. **Métodos de Rechazo y Cancelación** ❌
**Faltante:** No hay métodos para rechazar o cancelar prematrículas.

**Código faltante:**
```csharp
// Agregar campos al modelo Prematriculation:
public Guid? ConfirmedBy { get; set; }
public Guid? RejectedBy { get; set; }
public DateTime? RejectedAt { get; set; }
public Guid? CancelledBy { get; set; }
public DateTime? CancelledAt { get; set; }

// Métodos en IPrematriculationService y PrematriculationService:
Task<Prematriculation> RejectPrematriculationAsync(Guid prematriculationId, string reason, Guid rejectedBy);
Task<Prematriculation> CancelPrematriculationAsync(Guid prematriculationId, string reason, Guid cancelledBy);

// En RejectPrematriculationAsync:
public async Task<Prematriculation> RejectPrematriculationAsync(Guid prematriculationId, string reason, Guid rejectedBy)
{
    var prematriculation = await GetByIdAsync(prematriculationId);
    if (prematriculation == null)
        throw new Exception("Prematrícula no encontrada");

    if (prematriculation.Status == "Matriculado")
        throw new Exception("No se puede rechazar una matrícula ya confirmada");

    prematriculation.Status = "Rechazado";
    prematriculation.RejectionReason = reason;
    prematriculation.RejectedBy = rejectedBy;
    prematriculation.RejectedAt = DateTime.UtcNow;
    prematriculation.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();
    
    // Enviar notificación al acudiente/estudiante
    // ...
    
    return prematriculation;
}

// En CancelPrematriculationAsync:
// Similar pero libera cupos y permite reembolso si hay pagos
```

### 7. **Historial y Auditoría** ❌
**Faltante:** No se registra quién hizo qué cambios.

**Código faltante:**
```csharp
// Crear modelo PrematriculationHistory:
public class PrematriculationHistory
{
    public Guid Id { get; set; }
    public Guid PrematriculationId { get; set; }
    public string PreviousStatus { get; set; }
    public string NewStatus { get; set; }
    public Guid ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    
    public virtual Prematriculation Prematriculation { get; set; }
    public virtual User ChangedByUser { get; set; }
}

// Método para registrar cambios:
private async Task RecordHistoryAsync(Guid prematriculationId, string previousStatus, string newStatus, Guid changedBy, string? reason = null)
{
    var history = new PrematriculationHistory
    {
        Id = Guid.NewGuid(),
        PrematriculationId = prematriculationId,
        PreviousStatus = previousStatus,
        NewStatus = newStatus,
        ChangedBy = changedBy,
        ChangedAt = DateTime.UtcNow,
        Reason = reason
    };
    
    _context.PrematriculationHistories.Add(history);
    await _context.SaveChangesAsync();
}

// Llamar en cada cambio de estado:
// En ConfirmMatriculationAsync:
await RecordHistoryAsync(prematriculationId, prematriculation.Status, "Matriculado", currentUserId, "Matrícula confirmada");

// En RejectPrematriculationAsync:
await RecordHistoryAsync(prematriculationId, prematriculation.Status, "Rechazado", rejectedBy, reason);
```

### 8. **Validación de Duplicados en Confirmación** ❌
**Faltante:** No valida si el estudiante ya está matriculado en otro grupo/grado.

**Código faltante:**
```csharp
// En ConfirmMatriculationAsync, ANTES de crear StudentAssignment:
// Verificar si ya está matriculado en otro grupo/grado del mismo período académico
var existingMatriculation = await _context.Prematriculations
    .Where(p => p.StudentId == prematriculation.StudentId
        && p.Id != prematriculationId
        && p.Status == "Matriculado"
        && p.PrematriculationPeriodId == prematriculation.PrematriculationPeriodId)
    .FirstOrDefaultAsync();

if (existingMatriculation != null)
{
    throw new Exception($"El estudiante ya está matriculado en otro grupo/grado para este período académico (Grupo: {existingMatriculation.Group?.Name}, Grado: {existingMatriculation.Grade?.Name})");
}
```

---

## 🟢 MEJORAS RECOMENDADAS - BAJA PRIORIDAD

### 9. **Reportes y Estadísticas** ❌
- Reporte de prematrículas por período
- Estadísticas de cupos ocupados/disponibles por grupo
- Reporte de estudiantes pendientes de matrícula
- Reporte de estudiantes rechazados/cancelados

### 10. **Notificaciones Automáticas** ❌
- Notificar cuando el período está por vencer
- Recordatorios de pago pendiente
- Notificar cuando se asigna grupo automáticamente
- Notificar cuando se libera un cupo (si hay lista de espera)

### 11. **Lista de Espera** ❌
- Cuando no hay cupos, agregar a lista de espera
- Notificar cuando se libera un cupo
- Priorizar por fecha de solicitud

### 12. **Mejoras en Asignación Automática de Grupo** ❌
- Considerar balance de género (50/50 si es posible)
- Considerar necesidades especiales (distribuir estudiantes con inclusión)
- Considerar historial académico (mezclar niveles)

---

## 📋 RESUMEN POR PRIORIDAD

### 🔴 **ALTA PRIORIDAD** (Implementar PRIMERO para estar al 100%):
1. ✅ Validación de duplicados de prematrícula
2. ✅ Gestión correcta de StudentAssignment (inactivar anteriores)
3. ✅ Validación de cupos considerando prematrículas reservadas
4. ✅ Validación de pago completo (monto total)
5. ⚠️ Gestión de estados con enum (opcional pero recomendado)

### 🟡 **MEDIA PRIORIDAD** (Para funcionalidad completa):
6. ✅ Métodos de rechazo/cancelación
7. ✅ Historial y auditoría
8. ✅ Validación de duplicados en confirmación

### 🟢 **BAJA PRIORIDAD** (Mejoras y optimizaciones):
9. Reportes y estadísticas
10. Notificaciones automáticas
11. Lista de espera
12. Mejoras en asignación automática

---

## 🔧 CAMBIOS NECESARIOS EN MODELOS

### Prematriculation:
```csharp
// Agregar campos:
public Guid? ConfirmedBy { get; set; }
public Guid? RejectedBy { get; set; }
public DateTime? RejectedAt { get; set; }
public Guid? CancelledBy { get; set; }
public DateTime? CancelledAt { get; set; }
// Opcional: public PrematriculationStatus StatusEnum { get; set; }
```

### StudentAssignment:
```csharp
// Agregar campos:
public bool IsActive { get; set; } = true;
public DateTime? EndDate { get; set; }
// ShiftId ya existe pero verificar que se use
```

### PrematriculationPeriod:
```csharp
// Agregar campos:
public decimal? RequiredAmount { get; set; }
public int? MaxPrematriculations { get; set; }
public bool AllowMultiplePrematriculations { get; set; } = false;
```

### Nuevo Modelo - PrematriculationHistory:
```csharp
public class PrematriculationHistory
{
    public Guid Id { get; set; }
    public Guid PrematriculationId { get; set; }
    public string PreviousStatus { get; set; }
    public string NewStatus { get; set; }
    public Guid ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    
    public virtual Prematriculation Prematriculation { get; set; }
    public virtual User ChangedByUser { get; set; }
}
```

---

## ✅ ESTADO ACTUAL DEL MÓDULO

### Funcionalidad Base: ~70% ✅
- Creación y gestión básica ✅
- Validaciones básicas ✅
- Integración con pagos ✅

### Validaciones Críticas: ~60% ⚠️
- Validación de duplicados ❌
- Gestión de StudentAssignment ❌
- Validación de cupos completa ❌
- Validación de pago completo ❌

### Funcionalidades Avanzadas: ~30% ❌
- Rechazo/cancelación ❌
- Historial/auditoría ❌
- Reportes ❌
- Notificaciones automáticas ❌

---

## 🎯 PARA LLEGAR AL 100%:

**Mínimo necesario (ALTA PRIORIDAD):**
1. Validación de duplicados
2. Gestión correcta de StudentAssignment
3. Validación de cupos completa
4. Validación de pago completo

**Deseable (MEDIA PRIORIDAD):**
5. Métodos de rechazo/cancelación
6. Historial y auditoría

**Mejoras (BAJA PRIORIDAD):**
7. Reportes y estadísticas
8. Notificaciones automáticas
9. Lista de espera

