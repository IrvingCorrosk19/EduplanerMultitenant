# Análisis de StudentAssignment/Upload

## Estructura del Formato Excel

El archivo Excel para cargar asignaciones de estudiantes debe tener exactamente **9 columnas** en este orden:

### Columnas Requeridas

1. **ESTUDIANTE (EMAIL)** - `string` (obligatorio)
   - Correo electrónico único del estudiante
   - Formato: `usuario@dominio.com`
   - Validación: Formato de email válido, no duplicado

2. **NOMBRE** - `string` (obligatorio)
   - Nombre del estudiante
   - No puede estar vacío

3. **APELLIDO** - `string` (obligatorio)
   - Apellido del estudiante
   - No puede estar vacío

4. **DOCUMENTO ID** - `string` (obligatorio)
   - Número de documento de identidad
   - Validación: No duplicado en el archivo

5. **FECHA NACIMIENTO** - `string` (obligatorio)
   - Formato: `DD/MM/YYYY` (ejemplo: 15/03/2008)
   - Alternativa: Número de Excel (fecha serial)
   - Validación: Fecha válida entre 1900 y año actual

6. **GRADO** - `string` (obligatorio)
   - Nombre del grado (ejemplo: 6°, 7°, 8°, 9°, 10°, 11°)
   - Si no existe, se crea automáticamente

7. **GRUPO** - `string` (obligatorio)
   - Nombre del grupo (ejemplo: A, B, C, D, E)
   - Si no existe, se crea automáticamente

8. **JORNADA** - `string` (opcional)
   - Valores permitidos: `Mañana`, `Tarde`, `Noche`
   - Si no existe en la tabla, se crea automáticamente
   - Se relaciona directamente con `StudentAssignment` (similar a Grado y Grupo)

9. **INCLUSIÓN** - `string` (opcional)
   - Valores: `si`, `sí`, `yes`, `true` (true) | `no`, `false` (false) | vacío (null)
   - Indica si el estudiante tiene alguna discapacidad

## Validaciones

### Validaciones de Campos Obligatorios
- Email: Formato válido y único
- Nombre: No vacío
- Apellido: No vacío
- Documento ID: No vacío y único
- Fecha Nacimiento: Formato válido (DD/MM/YYYY o número Excel)
- Grado: No vacío
- Grupo: No vacío

### Validaciones de Formato
- **Email**: Regex `/^[^\s@]+@[^\s@]+\.[^\s@]+$/`
- **Fecha**: Formato `DD/MM/YYYY` o número de Excel > 0
- **Jornada**: Se normaliza a `Mañana`, `Tarde`, `Noche` (case-insensitive)

## Proceso de Carga

1. **Lectura del Excel**: Se lee la primera hoja usando XLSX.js
2. **Validación de Encabezados**: Deben ser exactamente los 9 encabezados en orden
3. **Validación de Filas**: Cada fila se valida antes de mostrar
4. **Previsualización**: Se muestra una tabla con los datos y validaciones
5. **Guardado**: Al hacer clic en "Guardar Asignaciones", se envía vía AJAX a `/StudentAssignment/SaveAssignments`

## Lógica de Guardado (SaveAssignments)

Para cada fila del Excel:

1. **Buscar o crear Estudiante**:
   - Si existe por email, se actualiza (Inclusivo, Shift)
   - Si no existe, se crea automáticamente con:
     - Contraseña temporal: `123456`
     - Rol: `estudiante`
     - Status: `active`

2. **Buscar o crear Grado**:
   - `GetByNameAsync(item.Grado)`
   - Si no existe, se crea automáticamente

3. **Buscar o crear Grupo**:
   - `GetByNameAndGradeAsync(item.Grupo)`
   - Si no existe, se crea automáticamente

4. **Buscar o crear Jornada**:
   - `GetOrCreateAsync(item.Jornada)`
   - Si no existe, se crea automáticamente
   - Se asigna al grupo: `group.ShiftId = shift.Id`
   - **Se asigna directamente a StudentAssignment**: `assignment.ShiftId = shift.Id`

5. **Crear StudentAssignment**:
   ```csharp
   new StudentAssignment
   {
       StudentId = student.Id,
       GradeId = grade.Id,
       GroupId = group.Id,
       ShiftId = shift?.Id,  // ✅ Relación directa con jornada
       CreatedAt = DateTime.UtcNow
   }
   ```

## Relaciones

- **StudentAssignment** → `StudentId` (FK a Users)
- **StudentAssignment** → `GradeId` (FK a GradeLevels)
- **StudentAssignment** → `GroupId` (FK a Groups)
- **StudentAssignment** → `ShiftId` (FK a Shifts) ✅ **NUEVO**

## Archivo de Ejemplo

Se ha generado `asignaciones_estudiantes_grado_grupo.csv` con 20 estudiantes de ejemplo que incluye:
- Emails únicos
- Nombres y apellidos variados
- Documentos únicos
- Fechas de nacimiento válidas (2005-2012)
- Grados variados (6° a 11°)
- Grupos variados (A, B, C, D, E)
- Jornadas variadas (Mañana, Tarde, Noche)
- Inclusión variada (si, no, vacío)

**Nota**: El archivo CSV puede abrirse directamente en Excel y guardarse como .xlsx.

