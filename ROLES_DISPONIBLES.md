# Roles Disponibles en el Sistema

## Roles Válidos en la Base de Datos

La tabla `users` tiene una constraint que valida los siguientes roles:

### Roles Administrativos
1. **superadmin** - Super Administrador (máximo nivel de acceso)
2. **admin** - Administrador (acceso administrativo general)
3. **director** - Director (acceso de dirección)

### Roles Académicos
4. **teacher** - Docente/Profesor
5. **student** - Estudiante (en inglés)
6. **estudiante** - Estudiante (en español)

### Roles de Padres/Acudientes
7. **parent** - Padre/Acudiente (en inglés)
8. **acudiente** - Acudiente (en español) - **NUEVO**

### Roles de Contabilidad
9. **contable** - Contable - **NUEVO**
10. **contabilidad** - Contabilidad - **NUEVO**

---

## Roles Usados en los Controladores

### Super Admin
- `SuperAdminController` - Requiere: `superadmin`

### Administración
- `UserController` - Requiere: `admin`
- `EmailConfigurationController` - Requiere: `superadmin,admin`
- `CounselorAssignmentController` - Requiere: `superadmin,admin`
- `PrematriculationPeriodController` - Requiere: `Admin,SuperAdmin`

### Director
- `DirectorController` - Requiere: `director`

### Docentes
- `TeacherGradebookController` - Requiere: `teacher`
- `TeacherGradebookDuplicateController` - Requiere: `teacher`
- `OrientationReportController` - Requiere: `teacher`

### Estudiantes
- `StudentController` - Requiere: `student,estudiante`
- `StudentProfileController` - Requiere: `student,estudiante`
- `StudentOrientationController` - Requiere: `student,estudiante`

### Prematrícula y Pagos
- `PrematriculationController` (MyPrematriculations, Create) - Requiere: `Parent,Acudiente,Student,Estudiante`
- `PrematriculationController` (Index, Details, etc.) - Requiere: `Admin,SuperAdmin` o `Teacher,Docente,Admin,SuperAdmin`
- `PaymentController` - Requiere: `Admin,SuperAdmin,Contabilidad`

### Reportes
- `AprobadosReprobadosController` - Requiere: `admin,director,teacher`

---

## Roles Usados en el Menú (_AdminLayout.cshtml)

### Prematrícula
- Visible para: `parent`, `acudiente`, `student`, `estudiante`

### Mis Calificaciones
- Visible para: `student`, `estudiante`

### Libro de Calificaciones
- Visible para: `teacher`

### Reportes
- Visible para: `director`

### Gestión de Estudiantes
- Visible para: `admin`, `director`, `teacher`

### Prematrícula y Matrícula (Admin)
- Visible para: `admin`, `superadmin`

### Pagos (Admin)
- Visible para: `admin`, `superadmin`

### Pagos (Contabilidad)
- Visible para: `contabilidad`

### Gestión de Usuarios
- Visible para: `admin`

---

## Políticas de Autorización (Program.cs)

```csharp
options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin"));
options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
options.AddPolicy("Teacher", policy => policy.RequireRole("Teacher"));
options.AddPolicy("Student", policy => policy.RequireRole("Student"));
options.AddPolicy("Parent", policy => policy.RequireRole("Parent", "Acudiente"));
options.AddPolicy("Accounting", policy => policy.RequireRole("Contabilidad", "Admin", "SuperAdmin"));
```

---

## Notas Importantes

1. **Roles en minúsculas vs mayúsculas**: La base de datos usa roles en minúsculas, pero algunos controladores usan capitalización (ej: `Admin`, `SuperAdmin`). ASP.NET Core hace matching case-insensitive por defecto.

2. **Roles duplicados**: 
   - `student` y `estudiante` son equivalentes
   - `parent` y `acudiente` son equivalentes

3. **Roles nuevos agregados**:
   - `acudiente` - Agregado recientemente
   - `contable` - Agregado recientemente  
   - `contabilidad` - Agregado recientemente

4. **Roles en uso actual**: Según la base de datos, los roles actualmente en uso son:
   - `admin`
   - `estudiante`
   - `superadmin`
   - `teacher`

---

**Última actualización**: 2025-11-02

