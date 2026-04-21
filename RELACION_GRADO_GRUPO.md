# Relación entre Grado (GradeLevel) y Grupo (Group)

## Resumen de la Relación

**Grado (`GradeLevel`) y Grupo (`Group`) NO tienen una relación directa uno-a-muchos entre sí.** Son entidades independientes que se relacionan a través de tablas intermedias.

## Estructura de los Modelos

### GradeLevel (Grado)
- **Campos principales**: `Id`, `Name`, `Description`, `SchoolId`
- **No tiene referencia directa a Group**

### Group (Grupo)
- **Campos principales**: `Id`, `Name`, `Description`, `SchoolId`
- **Campo legacy**: `Grade` (string opcional) - Solo texto, NO es una relación con GradeLevel
- **Campos de jornada**: `Shift` (string), `ShiftId` (Guid?) - Relación con tabla `shifts`
- **No tiene referencia directa a GradeLevel**

## Tablas Intermedias (Relaciones)

### 1. StudentAssignment (Asignación de Estudiantes)
```csharp
public class StudentAssignment
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid GradeId { get; set; }      // FK a GradeLevel
    public Guid GroupId { get; set; }       // FK a Group
    public DateTime? CreatedAt { get; set; }
    
    public virtual GradeLevel Grade { get; set; }
    public virtual Group Group { get; set; }
    public virtual User Student { get; set; }
}
```

**Relación**: Un estudiante puede estar asignado a una combinación específica de Grado + Grupo.

### 2. SubjectAssignment (Asignación de Materias)
```csharp
public class SubjectAssignment
{
    public Guid Id { get; set; }
    public Guid GradeLevelId { get; set; }  // FK a GradeLevel
    public Guid GroupId { get; set; }       // FK a Group
    public Guid SubjectId { get; set; }
    // ...
    
    public virtual GradeLevel GradeLevel { get; set; }
    public virtual Group Group { get; set; }
    public virtual Subject Subject { get; set; }
}
```

**Relación**: Una materia se asigna a una combinación específica de Grado + Grupo.

### 3. Otras Tablas que Relacionan Grado y Grupo
- **Attendance**: `GradeId`, `GroupId`
- **DisciplineReport**: `GroupId` (grado implícito)
- **OrientationReport**: `GroupId` (grado implícito)
- **CounselorAssignment**: `GradeId`, `GroupId` (opcionales)
- **Activity**: `GroupId` (grado implícito)

## Búsqueda y Creación

### Búsqueda de Grado
```csharp
// Buscar por nombre
var grade = await _gradeLevelService.GetByNameAsync("10°");

// Buscar o crear (si no existe, se crea automáticamente)
var grade = await _gradeLevelService.GetOrCreateAsync("10°");
```

### Búsqueda de Grupo
```csharp
// Buscar por nombre (NOTA: El método se llama GetByNameAndGradeAsync 
// pero NO usa el grado en la búsqueda, solo compara nombres)
var group = await _groupService.GetByNameAndGradeAsync("A");

// Buscar o crear (si no existe, se crea automáticamente)
var group = await _groupService.GetOrCreateAsync("A");
```

**⚠️ IMPORTANTE**: El método `GetByNameAndGradeAsync` solo busca por nombre del grupo, NO usa el grado para filtrar.

## Proceso de Carga Masiva (Excel)

En `/StudentAssignment/Upload`, cuando se procesa un Excel:

1. **Buscar o crear Grado**:
   ```csharp
   var grade = await _gradeLevelService.GetByNameAsync(item.Grado);
   // Si no existe, se crea automáticamente con GetOrCreateAsync
   ```

2. **Buscar o crear Grupo**:
   ```csharp
   var group = await _groupService.GetByNameAndGradeAsync(item.Grupo);
   // Si no existe, se crea automáticamente con GetOrCreateAsync
   ```

3. **Crear la relación en StudentAssignment**:
   ```csharp
   var assignment = new StudentAssignment
   {
       StudentId = student.Id,
       GradeId = grade.Id,
       GroupId = group.Id,
       CreatedAt = DateTime.UtcNow
   };
   await _studentAssignmentService.InsertAsync(assignment);
   ```

## Características Importantes

### 1. Relación Many-to-Many
- Un **Grado** puede tener estudiantes en **múltiples Grupos**
- Un **Grupo** puede tener estudiantes de **múltiples Grados**
- La combinación única es: **Estudiante + Grado + Grupo** (en `StudentAssignment`)

### 2. Independencia
- Los grados y grupos se crean independientemente
- No hay validación que requiera que un grupo pertenezca a un grado específico
- La relación solo existe en las tablas intermedias

### 3. Campo Legacy `Group.Grade`
- El campo `Grade` en `Group` es un **string opcional**
- **NO es una relación con GradeLevel**
- Parece ser un campo legacy para almacenar el nombre del grado como texto
- No se usa en la lógica de búsqueda o relaciones

### 4. Búsqueda de Combinaciones
```csharp
// Obtener combinaciones únicas de Grado-Grupo desde SubjectAssignment
var combinations = await _subjectAssignmentService.GetDistinctGradeGroupCombinationsAsync();

// Obtener asignaciones de materias por Grado y Grupo
var matches = await _subjectService.GetSubjectAssignmentsByGradeAndGroupAsync(gradeId, groupId);
```

## Ejemplo Práctico

**Escenario**: Un estudiante en 10° grado, grupo A, jornada Mañana

1. **Grado**: Se busca o crea "10°" en `GradeLevels`
2. **Grupo**: Se busca o crea "A" en `Groups`
3. **Jornada**: Se busca o crea "Mañana" en `Shifts` y se asigna a `Group.ShiftId`
4. **Asignación**: Se crea registro en `StudentAssignment`:
   - `StudentId` = ID del estudiante
   - `GradeId` = ID del grado "10°"
   - `GroupId` = ID del grupo "A"

## Conclusión

La relación entre Grado y Grupo es **indirecta** y se establece a través de:
- **StudentAssignment**: Para asignar estudiantes a grado+grupo
- **SubjectAssignment**: Para asignar materias a grado+grupo

No hay una relación directa entre las tablas `GradeLevels` y `Groups`.

