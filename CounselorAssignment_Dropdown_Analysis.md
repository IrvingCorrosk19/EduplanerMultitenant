# ANÁLISIS COMPLETO - DROPDOWN GRADOS Y GRUPOS

## 1. Estado real en base de datos

### grade_levels (tabla completa)
| id | name |
|----|------|
| 171563c1-3c14-486f-9d00-a7cf1b67f9a1 | 12 |
| 7769dfbc-1ce6-4584-b581-0345943b1192 | 8 |
| 9811c9ae-8e25-441c-b7f6-41e2e7cabdef | 9 |
| c3180447-1afd-4ba8-9ebb-4de5bc9eb5c3 | 10 |
| e921f5af-cbb1-4507-ac89-f4749569c28e | 11 |
| eb42cd8b-8d58-4098-8b7d-1494cdc6b312 | 7 |

Total: **6 grados** (7, 8, 9, 10, 11, 12)

### groups (tabla completa)
| name |
|------|
| A, A1, A2, B, C, C1, C2, D, E, E1, E2, F, G, H, I, J, K, L, M, N, Ñ, O, P, S, S1, S2, TM1 |

Total: **27 grupos**

### Combinaciones posibles con estudiantes activos (desde student_assignments WHERE is_active=true)
Total: **66 combinaciones únicas** (confirmado con SELECT DISTINCT)

Distribución:
- Grado 7: 13 combinaciones (A, B, C, D, E, F, G, H, I, J, K, L, M, N, Ñ — nota: 15 grupos con estudiantes activos, ver query)
- Grado 8: 14 combinaciones (A, B, C, D, E, F, G, H, I, J, K, L, M, N, Ñ, O)
- Grado 9: 11 combinaciones (A, B, C, D, E, F, G, H, I, J, K, L)
- Grado 10: 7 combinaciones (A1, A2, C1, C2, E1, E2, S1)
- Grado 11: 9 combinaciones (A1, A2, C1, C2, E1, E2, S2, TM1)
- Grado 12: 8 combinaciones (A1, A2, C1, C2, E1, E2, S1, S2)

### Combinaciones en counselor_assignments (todas las existentes)
| grade_name | group_name | is_active |
|------------|------------|-----------|
| 11 | E1 | true |
| 10 | C1 | true |
| 10 | A1 | true |
| 10 | A2 | true |
| 10 | C2 | true |
| 10 | E2 | true |
| 7 | A | true |
| 7 | B | true |
| 7 | C | true |
| 7 | D | true |
| 7 | E | true |
| 7 | F | true |
| 7 | G | true |

Total: **13 asignaciones activas**

Grupos cubiertos por al menos una asignación activa (sin importar grado):
**A, A1, A2, B, C, C1, C2, D, E, E1, E2, F, G** — 13 group_ids únicos con is_active=true.

### Combinaciones SIN registros en counselor_assignments (ESTAS deberían aparecer en el dropdown)
Las combinaciones que no tienen consejero asignado para esa pareja (grade_id, group_id) exacta:
- 10-E1, 11-A1, 11-A2, 11-C1, 11-C2, 11-E2
- 12-A1, 12-A2, 12-C1, 12-C2, 12-E1, 12-E2
- 8-A, 8-B, 8-C, 8-D, 8-E, 8-F, 8-G
- 9-A, 9-B, 9-C, 9-D, 9-E, 9-F, 9-G

Total: **26 combinaciones sin consejero específico asignado**

---

## 2. Qué está filtrando el backend

### Controller method
**Archivo:** `Controllers/CounselorAssignmentController.cs`, líneas 40-76

```csharp
public async Task<IActionResult> Index()
{
    // ...
    var validCombinations = await _counselorAssignmentService.GetValidGradeGroupCombinationsAsync(currentUser.SchoolId.Value);
    ViewBag.ValidCombinations = validCombinations;
    // ...
    return View(assignments);
}
```

El controller llama a `GetValidGradeGroupCombinationsAsync(schoolId)` y coloca el resultado en `ViewBag.ValidCombinations`.

### Service method
**Archivo:** `Services/Implementations/CounselorAssignmentService.cs`, líneas 720-767

```csharp
public async Task<List<GradeGroupCombinationDto>> GetValidGradeGroupCombinationsAsync(Guid schoolId)
{
    var combinations = await _context.StudentAssignments
        .Include(sa => sa.Student)
        .Where(sa => sa.Student != null && sa.Student.SchoolId == schoolId && sa.IsActive)
        .GroupBy(sa => new { sa.GradeId, sa.GroupId })
        .Where(g => !_context.CounselorAssignments
            .Any(ca => (ca.GradeId == g.Key.GradeId && ca.GroupId == g.Key.GroupId) ||
                       (ca.GroupId == g.Key.GroupId && ca.IsActive)))
        .Select(g => new GradeGroupCombinationDto { ... })
        .OrderBy(c => c.GradeName)
        .ThenBy(c => c.GroupName)
        .ToListAsync();
}
```

### Repository/LINQ query (la query que genera el problema)

El `WHERE` de exclusión (líneas 734-736) contiene dos condiciones unidas por OR:

```csharp
.Where(g => !_context.CounselorAssignments
    .Any(ca => (ca.GradeId == g.Key.GradeId && ca.GroupId == g.Key.GroupId)   // Condición 1: par exacto
               ||
               (ca.GroupId == g.Key.GroupId && ca.IsActive)))                   // Condición 2: solo group_id + is_active
```

**La SQL equivalente generada por EF Core:**

```sql
WHERE NOT EXISTS (
  SELECT 1 FROM counselor_assignments ca
  WHERE (ca.grade_id = sa.grade_id AND ca.group_id = sa.group_id)
     OR (ca.group_id = sa.group_id AND ca.is_active = true)
)
```

La Condición 2 **no está condicionada al grade_id**. Evalúa si existe cualquier fila en `counselor_assignments` con el mismo `group_id` Y `is_active=true`, sin importar a qué grado corresponda esa asignación.

---

## 3. Qué recibe la vista

### ViewModel / ViewBag / SelectList
La lista `validCombinations` es `List<GradeGroupCombinationDto>` y se pasa como `ViewBag.ValidCombinations`.

**Archivo:** `Dtos/GradeGroupCombinationDto.cs`, líneas 1-15

```csharp
public class GradeGroupCombinationDto
{
    public Guid GradeId { get; set; }
    public string GradeName { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string GroupGrade { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public string DisplayText => $"{GradeName} - {GroupName} ({StudentCount} estudiantes)";
}
```

### Vista Razor (foreach/select)
**Archivo:** `Views/CounselorAssignment/Index.cshtml`, líneas 73-91

```razor
<select id="gradeGroupCombination" class="form-select" required>
    <option value="">Seleccione una combinación válida</option>
    @if (ViewBag.ValidCombinations != null)
    {
        @foreach (var combination in ViewBag.ValidCombinations)
        {
            <option value="@combination.GradeId|@combination.GroupId"
                    data-grade-id="@combination.GradeId"
                    data-group-id="@combination.GroupId">
                @combination.DisplayText
            </option>
        }
    }
</select>
<small class="form-text text-muted">
    Solo se muestran las combinaciones que tienen estudiantes asignados
</small>
```

La vista itera `ViewBag.ValidCombinations` directamente sin ningún filtro adicional. Si `validCombinations` devuelve 27 combinaciones, el dropdown mostraría 27. En la práctica, el servicio devuelve solo las que pasan el filtro erróneo.

### JavaScript adicional
**Archivo:** `Views/CounselorAssignment/Index.cshtml`, líneas 288-296

```javascript
$('#gradeGroupCombination').on('change', function() {
    var selectedValue = $(this).val();
    if (selectedValue) {
        var parts = selectedValue.split('|');
        $('#gradeId').val(parts[0]);
        $('#groupId').val(parts[1]);
    } else {
        $('#gradeId, #groupId').val('');
    }
});
```

No hay lógica JS adicional que filtre el dropdown. El contenido del `<select>` es generado íntegramente server-side en Razor.

---

## 4. Diferencias detectadas

| Combinación (Grado-Grupo) | En DB (student_assignments activos) | En Dropdown (resultado servicio) | ¿Falta? |
|---------------------------|--------------------------------------|----------------------------------|---------|
| 7-A | SI (37 estudiantes) | NO | SI |
| 7-B | SI (37 estudiantes) | NO | SI |
| 7-C | SI (38 estudiantes) | NO | SI |
| 7-D | SI (41 estudiantes) | NO | SI |
| 7-E | SI (38 estudiantes) | NO | SI |
| 7-F | SI (39 estudiantes) | NO | SI |
| 7-G | SI (37 estudiantes) | NO | SI |
| 7-H | SI (33 estudiantes) | SI | NO |
| 7-I | SI (34 estudiantes) | SI | NO |
| 7-J | SI (34 estudiantes) | SI | NO |
| 7-K | SI (35 estudiantes) | SI | NO |
| 7-L | SI (33 estudiantes) | SI | NO |
| 7-M | SI (33 estudiantes) | SI | NO |
| 7-N | SI (34 estudiantes) | SI | NO |
| 7-Ñ | SI (34 estudiantes) | SI | NO |
| 8-A | SI (35 estudiantes) | NO | SI |
| 8-B | SI (36 estudiantes) | NO | SI |
| 8-C | SI (31 estudiantes) | NO | SI |
| 8-D | SI (34 estudiantes) | NO | SI |
| 8-E | SI (38 estudiantes) | NO | SI |
| 8-F | SI (36 estudiantes) | NO | SI |
| 8-G | SI (32 estudiantes) | NO | SI |
| 8-H | SI (36 estudiantes) | SI | NO |
| 8-I | SI (31 estudiantes) | SI | NO |
| 8-J | SI (28 estudiantes) | SI | NO |
| 8-K | SI (30 estudiantes) | SI | NO |
| 8-L | SI (26 estudiantes) | SI | NO |
| 8-M | SI (32 estudiantes) | SI | NO |
| 8-N | SI (32 estudiantes) | SI | NO |
| 8-Ñ | SI (30 estudiantes) | SI | NO |
| 8-O | SI (26 estudiantes) | SI | NO |
| 9-A | SI (35 estudiantes) | NO | SI |
| 9-B | SI (37 estudiantes) | NO | SI |
| 9-C | SI (38 estudiantes) | NO | SI |
| 9-D | SI (36 estudiantes) | NO | SI |
| 9-E | SI (37 estudiantes) | NO | SI |
| 9-F | SI (38 estudiantes) | NO | SI |
| 9-G | SI (35 estudiantes) | NO | SI |
| 9-H | SI (32 estudiantes) | SI | NO |
| 9-I | SI (34 estudiantes) | SI | NO |
| 9-J | SI (32 estudiantes) | SI | NO |
| 9-K | SI (36 estudiantes) | SI | NO |
| 9-L | SI (32 estudiantes) | SI | NO |
| 10-A1 | SI (21 estudiantes) | NO | SI |
| 10-A2 | SI (22 estudiantes) | NO | SI |
| 10-C1 | SI (21 estudiantes) | NO | SI |
| 10-C2 | SI (17 estudiantes) | NO | SI |
| 10-E1 | SI (20 estudiantes) | NO | SI |
| 10-E2 | SI (18 estudiantes) | NO | SI |
| 10-S1 | SI (21 estudiantes) | SI | NO |
| 11-A1 | SI (13 estudiantes) | NO | SI |
| 11-A2 | SI (18 estudiantes) | NO | SI |
| 11-C1 | SI (14 estudiantes) | NO | SI |
| 11-C2 | SI (26 estudiantes) | NO | SI |
| 11-E1 | SI (17 estudiantes) | NO | SI |
| 11-E2 | SI (14 estudiantes) | NO | SI |
| 11-S2 | SI (7 estudiantes) | SI | NO |
| 11-TM1 | SI (11 estudiantes) | SI | NO |
| 12-A1 | SI (18 estudiantes) | NO | SI |
| 12-A2 | SI (14 estudiantes) | NO | SI |
| 12-C1 | SI (18 estudiantes) | NO | SI |
| 12-C2 | SI (12 estudiantes) | NO | SI |
| 12-E1 | SI (17 estudiantes) | NO | SI |
| 12-E2 | SI (9 estudiantes) | NO | SI |
| 12-S1 | SI (13 estudiantes) | SI | NO |
| 12-S2 | SI (9 estudiantes) | SI | NO |

**Resumen:**
- Combinaciones totales en DB con estudiantes activos: 66
- Combinaciones que el servicio DEVUELVE (aparecen en dropdown): 27
- Combinaciones que el servicio ELIMINA incorrectamente: **39**

---

## 5. Causa raíz (ROOT CAUSE)

Tipo: **B — Lógica de filtrado excesivamente amplia: la segunda condición del OR en el subquery de exclusión opera sobre group_id sin scope de grade_id**

Descripción:

En `CounselorAssignmentService.cs` líneas 734-736, el filtro de exclusión contiene:

```csharp
.Any(ca => (ca.GradeId == g.Key.GradeId && ca.GroupId == g.Key.GroupId)    // par exacto (correcto)
           ||
           (ca.GroupId == g.Key.GroupId && ca.IsActive))                     // SOLO group_id (erróneo)
```

La segunda condición del OR — `(ca.GroupId == g.Key.GroupId && ca.IsActive)` — **no incluye ninguna restricción sobre `ca.GradeId`**. Esto significa que si un grupo (por ejemplo, grupo "A1") tiene un consejero asignado activo en **cualquier grado** (por ejemplo, 10-A1), entonces **todas las combinaciones con ese mismo group_id en cualquier otro grado** también quedan excluidas del dropdown (11-A1, 12-A1, etc.).

El efecto en datos: los 13 group_ids con asignación activa en algún grado actúan como "lista negra global de grupos". Cualquier par (grade_id, group_id) donde el group_id aparezca en esa lista — sin importar si el par exacto (grade_id, group_id) tiene o no consejero asignado — es eliminado del dropdown.

---

## 6. Evidencia SQL

### Query que reproduce el problema (replica exactamente el LINQ del servicio)

```sql
SELECT sa.grade_id, gl.name as grade_name, sa.group_id, g.name as group_name, COUNT(*) as student_count
FROM student_assignments sa
JOIN grade_levels gl ON gl.id = sa.grade_id
JOIN groups g ON g.id = sa.group_id
WHERE sa.is_active = true
GROUP BY sa.grade_id, gl.name, sa.group_id, g.name
HAVING NOT EXISTS (
  SELECT 1 FROM counselor_assignments ca
  WHERE (ca.grade_id = sa.grade_id AND ca.group_id = sa.group_id)
     OR (ca.group_id = sa.group_id AND ca.is_active = true)
)
ORDER BY gl.name, g.name;
-- Resultado: 27 filas
```

### Query que muestra los datos faltantes (combinaciones excluidas solo por la segunda condición del OR)

```sql
SELECT sa.grade_id, gl.name as grade_name, sa.group_id, g.name as group_name, COUNT(*) as student_count
FROM student_assignments sa
JOIN grade_levels gl ON gl.id = sa.grade_id
JOIN groups g ON g.id = sa.group_id
WHERE sa.is_active = true
  AND NOT EXISTS (
    SELECT 1 FROM counselor_assignments ca
    WHERE ca.grade_id = sa.grade_id AND ca.group_id = sa.group_id
  )
  AND EXISTS (
    SELECT 1 FROM counselor_assignments ca
    WHERE ca.group_id = sa.group_id AND ca.is_active = true
  )
GROUP BY sa.grade_id, gl.name, sa.group_id, g.name
ORDER BY gl.name, g.name;
-- Resultado: 26 filas — estas son las combinaciones sin consejero propio
-- que el dropdown DEBERÍA mostrar pero NO muestra
```

Nota adicional: hay 13 combinaciones más que tienen consejero asignado para el par exacto (grade_id, group_id) — esas son excluidas correctamente por la primera condición del OR. 26 + 13 = 39 combinaciones excluidas en total de 66.

---

## 7. Evidencia en código

### Archivo: `Services/Implementations/CounselorAssignmentService.cs`
### Líneas: 730-757

```csharp
var combinations = await _context.StudentAssignments
    .Include(sa => sa.Student)
    .Where(sa => sa.Student != null && sa.Student.SchoolId == schoolId && sa.IsActive)
    .GroupBy(sa => new { sa.GradeId, sa.GroupId })
    .Where(g => !_context.CounselorAssignments
        .Any(ca => (ca.GradeId == g.Key.GradeId && ca.GroupId == g.Key.GroupId) ||
                   (ca.GroupId == g.Key.GroupId && ca.IsActive)))     // <-- LÍNEA 736: condición sin grade_id
    .Select(g => new GradeGroupCombinationDto
    {
        GradeId = g.Key.GradeId,
        GradeName = _context.GradeLevels
            .Where(gl => gl.Id == g.Key.GradeId)
            .Select(gl => gl.Name)
            .FirstOrDefault() ?? "Sin grado",
        GroupId = g.Key.GroupId,
        GroupName = _context.Groups
            .Where(gr => gr.Id == g.Key.GroupId)
            .Select(gr => gr.Name)
            .FirstOrDefault() ?? "Sin grupo",
        GroupGrade = _context.Groups
            .Where(gr => gr.Id == g.Key.GroupId)
            .Select(gr => gr.Grade)
            .FirstOrDefault() ?? "",
        StudentCount = g.Count()
    })
    .OrderBy(c => c.GradeName)
    .ThenBy(c => c.GroupName)
    .ToListAsync();
```

### Explicación del filtro problemático

La condición en la línea 736:

```csharp
(ca.GroupId == g.Key.GroupId && ca.IsActive)
```

Esta condición evalúa: "existe alguna fila en `counselor_assignments` donde `group_id` sea igual al `group_id` del par actual, Y esa fila esté activa". Esta evaluación es **independiente del `grade_id`** del par que se está inspeccionando.

Dado que la asignación `11-E1` registra `group_id = 7fd600ad` (grupo E1) con `is_active = true`, la condición `ca.GroupId == g.Key.GroupId && ca.IsActive` es verdadera para **cualquier combinación** donde `g.Key.GroupId = 7fd600ad`, incluyendo `10-E1` y `12-E1`, aunque esos pares no tengan consejero asignado.

La misma lógica aplica a los 13 grupos con asignaciones activas (A, A1, A2, B, C, C1, C2, D, E, E1, E2, F, G): en todos los grados en que esos grupos tienen estudiantes activos, la combinación es excluida del dropdown — ya sea que tenga consejero asignado para ese par específico o no.

El mismo patrón está replicado en `GetValidGradeGroupCombinationsForEditAsync` (líneas 784-788):

```csharp
.Where(g => !_context.CounselorAssignments
    .Any(ca => ((ca.GradeId == g.Key.GradeId && ca.GroupId == g.Key.GroupId) ||
                (ca.GroupId == g.Key.GroupId)) &&                      // <-- sin grade_id
               ca.IsActive &&
               (excludeAssignmentId == null || ca.Id != excludeAssignmentId)))
```

---

## 8. Conclusión técnica

El dropdown `<select id="gradeGroupCombination">` muestra únicamente 27 de las 66 combinaciones posibles con estudiantes activos porque `GetValidGradeGroupCombinationsAsync` aplica un filtro de exclusión cuya segunda cláusula del OR — `(ca.GroupId == g.Key.GroupId && ca.IsActive)` en la línea 736 — opera exclusivamente sobre el `group_id` sin ninguna restricción sobre el `grade_id`. Esto convierte el `group_id` en la clave de exclusión en lugar del par `(grade_id, group_id)`. Existen 13 group_ids distintos que tienen al menos una asignación activa en `counselor_assignments`; como resultado, la condición devuelve verdadero para toda combinación en `student_assignments` que contenga cualquiera de esos 13 group_ids, independientemente de si la pareja (grade_id, group_id) específica tiene o no un consejero asignado. De las 66 combinaciones posibles: 13 son excluidas correctamente (tienen consejero en ese par exacto), 39 son excluidas incorrectamente (no tienen consejero en ese par exacto, pero su group_id aparece en otra asignación activa con diferente grade_id), y 27 pasan el filtro y aparecen en el dropdown.
