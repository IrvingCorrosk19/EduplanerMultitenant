# Flujo: Estudiante de Nuevo Ingreso

## 📋 Descripción del Flujo Actual

### 1. **CREACIÓN DE PREMATRÍCULA** (CreatePrematriculationAsync)

#### ✅ Lo que SÍ funciona:
- **Detecta estudiante nuevo**: Si no tiene `StudentAssignments` previos, se le muestran todos los grados disponibles
- **Permite seleccionar cualquier grado**: No hay restricción de "siguiente nivel"
- **Valida condición académica**: ✅ `GetFailedSubjectsCountAsync` retorna `0` si no tiene calificaciones (válido para nuevo ingreso)
- **Puede crear prematrícula sin grado/grupo**: El grado y grupo son opcionales en la creación

#### ⚠️ Problemas Identificados:
1. **No hay validación específica de "nuevo ingreso"**
   - No diferencia si es estudiante nuevo vs estudiante existente
   - La validación de condición académica se aplica igual para ambos casos

2. **Validación de condición académica para nuevo ingreso**
   - Para nuevo ingreso, `GetFailedSubjectsCountAsync` retorna `0` (no tiene calificaciones)
   - Esto es correcto, pero podría ser más explícito

3. **No valida documentos requeridos**
   - No verifica si el estudiante tiene documentos completos (cédula, certificados, etc.)
   - Para nuevo ingreso, esto debería ser obligatorio

---

### 2. **CONFIRMACIÓN DE MATRÍCULA** (ConfirmMatriculationAsync)

#### ✅ Lo que SÍ funciona:
- **Asignación automática de grado para nuevo ingreso**:
  ```csharp
  // Si no tiene grado actual (estudiante nuevo), usar el primer grado disponible
  var firstGrade = allGrades.OrderBy(g => {
      var num = ExtractGradeNumber(g.Name);
      return num ?? int.MaxValue; // Ordenar por número
  }).FirstOrDefault();
  ```
  - Asigna el primer grado ordenado por número (ej: "1°", "2°", etc.)

- **Asignación automática de grupo**:
  - Si no tiene grupo asignado, usa `AutoAssignGroupAsync`
  - Considera jornada del estudiante si está configurada
  - Asigna grupo con menos estudiantes

#### ⚠️ Problemas Identificados:
1. **No verifica si es estudiante nuevo antes de asignar**
   - Siempre intenta obtener `currentGrade` de `StudentAssignments`
   - Si es null, asume que es nuevo y asigna primer grado
   - Esto es correcto pero podría ser más explícito

2. **No valida requisitos de nuevo ingreso**
   - Edad mínima/máxima por grado
   - Documentos completos
   - Información del acudiente

---

## 🔄 FLUJO COMPLETO ACTUAL

### **Escenario: Estudiante Nuevo Ingreso**

```
1. ESTUDIANTE/ACUDIENTE CREA PREMATRÍCULA
   ↓
   - Accede a /Prematriculation/Create
   - Si es estudiante: ve todos los grados (porque no tiene StudentAssignment)
   - Si es acudiente: ve todos los grados para seleccionar
   ↓
2. SELECCIONA ESTUDIANTE (si es acudiente)
   ↓
   - Sistema verifica si estudiante tiene StudentAssignments
   - Si NO tiene → Estudiante nuevo → Muestra todos los grados
   - Si SÍ tiene → Estudiante existente → Filtra grados permitidos
   ↓
3. SELECCIONA GRADO (opcional)
   ↓
   - Puede seleccionar cualquier grado
   - O dejar vacío para asignación automática
   ↓
4. SELECCIONA GRUPO (opcional)
   ↓
   - Puede seleccionar grupo con cupos disponibles
   - O dejar vacío para asignación automática
   ↓
5. SISTEMA VALIDA CONDICIÓN ACADÉMICA
   ↓
   GetFailedSubjectsCountAsync(StudentId):
   - Busca StudentActivityScores del estudiante
   - Si NO hay calificaciones → retorna 0 ✅
   - Si hay calificaciones → cuenta materias reprobadas
   - Valida: failedSubjects <= 3 ✅ (nuevo ingreso pasa)
   ↓
6. CREA PREMATRÍCULA
   ↓
   - Status: "Pendiente" → "Prematriculado"
   - GradeId: null (si no se seleccionó)
   - GroupId: null (si no se seleccionó)
   - FailedSubjectsCount: 0 (nuevo ingreso)
   - AcademicConditionValid: true
   ↓
7. ASIGNACIÓN AUTOMÁTICA DE GRUPO (si está habilitada)
   ↓
   - Si AutoAssignByShift está activo
   - Y no hay grupo asignado
   - Y hay grado asignado
   - Intenta asignar grupo automáticamente
   ↓
8. REALIZA PAGO
   ↓
   - Status: "Prematriculado" → "Pagado"
   - PaymentDate: DateTime.UtcNow
   ↓
9. ADMIN CONFIRMA MATRÍCULA
   ↓
   ConfirmMatriculationAsync:
   ↓
   9.1 VALIDACIONES:
       - Estado válido ✅
       - Tiene pago confirmado ✅
       - Condición académica válida ✅ (nuevo ingreso: 0 materias reprobadas)
   ↓
   9.2 ASIGNACIÓN AUTOMÁTICA DE GRADO (si no tiene):
       - Busca currentGrade en StudentAssignments
       - Si NO existe → Estudiante nuevo
       - Asigna primer grado ordenado por número
       - Ejemplo: Si hay "1°", "2°", "3°" → Asigna "1°"
   ↓
   9.3 ASIGNACIÓN AUTOMÁTICA DE GRUPO (si no tiene):
       - Usa AutoAssignGroupAsync
       - Busca grupos del grado asignado
       - Filtra por cupos disponibles
       - Considera jornada del estudiante (si hay)
       - Asigna grupo con menos estudiantes
   ↓
   9.4 VALIDA CUPOS:
       - Verifica que el grupo tenga cupos
       - Considera prematrículas reservadas
   ↓
   9.5 CREA StudentAssignment:
       - Crea nueva asignación del estudiante al grupo
       - No inactiva asignaciones previas (porque no hay)
   ↓
   9.6 ACTUALIZA ESTADO:
       - Status: "Matriculado"
       - MatriculationDate: DateTime.UtcNow
   ↓
10. ESTUDIANTE MATRICULADO ✅
```

---

## ❌ PROBLEMAS Y MEJORAS NECESARIAS

### 🔴 **CRÍTICO**

#### 1. **Falta Validación de Nuevo Ingreso Explícita**
**Problema:** No hay un campo o flag que identifique claramente a un estudiante nuevo.

**Solución sugerida:**
```csharp
public bool IsNewStudent(Guid studentId)
{
    var hasAssignments = _context.StudentAssignments
        .Any(sa => sa.StudentId == studentId);
    
    var hasScores = _context.StudentActivityScores
        .Any(sas => sas.StudentId == studentId);
    
    return !hasAssignments && !hasScores;
}
```

#### 2. **Validación de Condición Académica para Nuevo Ingreso**
**Problema:** Aunque funciona (retorna 0), debería ser más explícito que para nuevo ingreso no aplica esta validación.

**Solución sugerida:**
```csharp
public async Task<bool> ValidateAcademicConditionAsync(Guid studentId)
{
    // Si es nuevo ingreso, no validar condición académica
    if (IsNewStudent(studentId))
        return true;
    
    var failedSubjects = await GetFailedSubjectsCountAsync(studentId);
    return failedSubjects <= 3;
}
```

#### 3. **Falta Validación de Edad/Grado**
**Problema:** No se valida que la edad del estudiante corresponda al grado seleccionado.

**Solución sugerida:**
```csharp
public bool ValidateAgeForGrade(DateTime? dateOfBirth, Guid gradeId)
{
    if (!dateOfBirth.HasValue)
        return false;
    
    var grade = _context.GradeLevels.Find(gradeId);
    if (grade == null) return false;
    
    var age = DateTime.UtcNow.Year - dateOfBirth.Value.Year;
    var gradeNum = ExtractGradeNumber(grade.Name);
    
    if (!gradeNum.HasValue) return true; // No se puede validar
    
    // Validar edad esperada por grado (ej: 1° = 6 años, 2° = 7 años, etc.)
    var expectedAge = gradeNum.Value + 5; // Aproximación
    return Math.Abs(age - expectedAge) <= 2; // Permitir 2 años de diferencia
}
```

#### 4. **Falta Validación de Documentos Requeridos**
**Problema:** No se verifica que el estudiante tenga documentos completos.

**Solución sugerida:**
```csharp
public bool ValidateRequiredDocuments(Guid studentId)
{
    var student = _context.Users.Find(studentId);
    if (student == null) return false;
    
    // Validar documentos obligatorios
    var hasDocumentId = !string.IsNullOrEmpty(student.DocumentId);
    var hasDateOfBirth = student.DateOfBirth.HasValue;
    var hasName = !string.IsNullOrEmpty(student.Name) && !string.IsNullOrEmpty(student.LastName);
    
    return hasDocumentId && hasDateOfBirth && hasName;
}
```

#### 5. **Falta Validación de Acudiente para Menores**
**Problema:** No se valida que estudiantes menores de edad tengan acudiente asignado.

**Solución sugerida:**
```csharp
public bool ValidateParentRequired(Guid studentId)
{
    var student = _context.Users.Find(studentId);
    if (student == null || !student.DateOfBirth.HasValue)
        return false;
    
    var age = DateTime.UtcNow.Year - student.DateOfBirth.Value.Year;
    
    // Si es menor de 18 años, requiere acudiente
    if (age < 18)
    {
        var hasParent = _context.Students
            .Any(s => s.Id == studentId && s.ParentId.HasValue);
        
        // O verificar en prematrícula
        return hasParent;
    }
    
    return true; // Mayor de edad no requiere acudiente
}
```

---

### 🟡 **IMPORTANTE**

#### 6. **Asignación de Grado para Nuevo Ingreso Mejorable**
**Problema:** Siempre asigna el primer grado disponible, pero podría considerar:
- Edad del estudiante
- Nivel académico previo (si viene de otra institución)
- Preferencias del acudiente

#### 7. **Historial de Estudiantes Nuevos**
**Problema:** No se registra que es un estudiante nuevo, lo cual sería útil para reportes.

**Solución sugerida:**
```csharp
// Agregar campo a Prematriculation
public bool IsNewStudent { get; set; }

// En CreatePrematriculationAsync
prematriculation.IsNewStudent = IsNewStudent(dto.StudentId);
```

---

## ✅ FLUJO MEJORADO PROPUESTO

### **Para Estudiante Nuevo Ingreso:**

```
1. CREAR PREMATRÍCULA
   ↓
   - Validar que es nuevo ingreso ✅
   - Validar documentos requeridos ✅
   - Validar acudiente (si es menor) ✅
   - NO validar condición académica (es nuevo) ✅
   - Permitir seleccionar cualquier grado
   ↓
2. PAGO
   ↓
   - Realizar pago
   - Status: "Pagado"
   ↓
3. CONFIRMAR MATRÍCULA
   ↓
   - Validar edad/grado ✅
   - Asignar grado automático (si no tiene) basado en edad
   - Asignar grupo automático
   - Crear StudentAssignment
   - Status: "Matriculado"
   ↓
4. ESTUDIANTE MATRICULADO ✅
```

---

## 📝 RESUMEN

### **Estado Actual:**
- ✅ Funciona básicamente: Detecta nuevo ingreso y permite prematrícula
- ✅ Asigna grado/grupo automáticamente si no están asignados
- ⚠️ Falta validaciones específicas para nuevo ingreso
- ⚠️ No diferencia claramente entre nuevo y existente
- ⚠️ No valida documentos, edad, acudiente

### **Mejoras Recomendadas:**
1. Agregar flag `IsNewStudent` explícito
2. Validar documentos requeridos
3. Validar edad/grado correspondencia
4. Validar acudiente para menores
5. Mejorar asignación de grado basada en edad
6. Registrar en historial que es nuevo ingreso

