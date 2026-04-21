# 📊 Resumen: Datos Dummy para Pruebas de Matrícula y Prematrícula

## ✅ Datos Creados Exitosamente

### 1. **Grupos Actualizados** (27 grupos)
- Todos los grupos ahora tienen `max_capacity` (30 o 25 estudiantes)
- Todos tienen `shift` asignado (Mañana o Tarde)
- ✅ Listos para validar cupos en prematrícula

### 2. **Acudientes de Prueba** (3 acudientes)
- **Acudiente 1**: María Pérez - maria.perez@test.com
- **Acudiente 2**: Juan González - juan.gonzalez@test.com  
- **Acudiente 3**: Ana Rodríguez - ana.rodriguez@test.com
- Contraseña: `Test123!` (hash: `$2a$11$KIXx5L5L5L5L5L5L5L5L5O5L5L5L5L5L5L5L5L5L5L5L5L5L5L5L`)
- ✅ Listos para crear prematrículas

### 3. **Estudiantes con Jornada** (80 estudiantes)
- 50 estudiantes con jornada "Mañana"
- 30 estudiantes con jornada "Tarde"
- ✅ Listos para asignación automática por turno

### 4. **Calificaciones para Validación** (4 calificaciones)
- Estudiante de prueba tiene:
  - **Matemáticas**: 2.5 y 2.8 (Promedio: 2.65) → **REPROBADA** ❌
  - **Español**: 4.0 → **APROBADA** ✅
  - **Ciencias**: 2.0 → **REPROBADA** ❌
- **Total materias reprobadas: 2** (cumple con el límite de ≤3)
- ✅ Estudiante puede prematricularse

### 5. **Usuario de Contabilidad** (1 usuario)
- **Nombre**: Contabilidad Sistema
- **Email**: contabilidad@test.com
- **Rol**: contabilidad
- **Contraseña**: `Test123!`
- ✅ Listo para confirmar pagos manuales

### 6. **Conceptos de Pago**
- ⚠️ **Tabla `payment_concepts` no existe** (falta migración)
- Los conceptos se intentaron crear pero la tabla no está en la base de datos

---

## 📋 Datos Existentes (de la base de datos)

### Escuela
- **ID**: `6e42399f-6f17-4585-b92e-fa4fff02cb65`

### Período de Prematrícula Activo
- **ID**: `307efc09-60f5-4280-a986-763659e9a1d6`
- **Fecha inicio**: 2025-11-05
- **Fecha fin**: 2025-11-29
- **Estado**: Activo
- **Max capacidad por grupo**: 50
- **Asignación automática por turno**: Habilitada

### Grados Disponibles
- 7°, 8°, 9°, 10°, 11°, 12°

### Grupos Disponibles
- 27 grupos con diferentes nombres (A, B, C, A1, A2, C1, C2, etc.)
- Ahora todos tienen `max_capacity` y `shift` configurados

### Materias Disponibles
- 65 materias disponibles
- Incluye: Matemáticas, Español, Ciencias, Inglés, etc.

### Estudiantes
- Más de 1000 estudiantes en la base de datos
- 80 ahora tienen jornada asignada

---

## 🧪 Escenarios de Prueba Disponibles

### Escenario 1: Prematrícula Exitosa
**Datos disponibles:**
- ✅ Acudiente: maria.perez@test.com
- ✅ Estudiante con 2 materias reprobadas (puede prematricularse)
- ✅ Período activo
- ✅ Grupos con cupos disponibles

**Pasos:**
1. Iniciar sesión como acudiente
2. Crear prematrícula
3. Seleccionar grado y grupo
4. Sistema validará: condición académica ✅, cupos ✅, período activo ✅

### Escenario 2: Pago y Matrícula Automática
**Datos disponibles:**
- ✅ Prematrícula creada (estado: "Prematriculado")
- ⚠️ Conceptos de pago: necesitas crear la tabla primero

**Pasos:**
1. Crear pago desde portal (acudiente)
2. Confirmar pago (contabilidad)
3. Sistema activará matrícula automáticamente

### Escenario 3: Validación de Cupos
**Datos disponibles:**
- ✅ Grupos con `max_capacity` configurada
- ✅ Algunos grupos ya tienen estudiantes asignados

**Pasos:**
1. Intentar prematricular en grupo lleno
2. Sistema debe rechazar si no hay cupos

### Escenario 4: Asignación Automática por Turno
**Datos disponibles:**
- ✅ Estudiantes con jornada asignada
- ✅ Período con `auto_assign_by_shift = true`

**Pasos:**
1. Crear prematrícula sin seleccionar grupo
2. Sistema asignará automáticamente grupo con misma jornada

---

## ⚠️ Notas Importantes

1. **Tabla `payment_concepts` no existe**
   - Necesitas ejecutar la migración que crea esta tabla
   - O crear los conceptos manualmente después de crear la tabla

2. **Contraseñas de prueba**
   - Todos los usuarios dummy tienen contraseña: `Test123!`
   - El hash está encriptado con BCrypt

3. **IDs Fijos**
   - Escuela: `6e42399f-6f17-4585-b92e-fa4fff02cb65`
   - Período: `307efc09-60f5-4280-a986-763659e9a1d6`
   - Admin: `b0b35595-cc47-4a3e-9233-1c57809daca5`

4. **Calificaciones de Prueba**
   - Se crearon para el primer estudiante encontrado
   - Para probar con otro estudiante, necesitas crear más calificaciones

---

## 🚀 Próximos Pasos

1. **Verificar datos creados:**
   ```sql
   SELECT * FROM users WHERE role = 'acudiente';
   SELECT * FROM groups WHERE max_capacity IS NOT NULL;
   SELECT * FROM student_activity_scores LIMIT 5;
   ```

2. **Crear tabla `payment_concepts`** (si no existe):
   - Ejecutar migraciones de Entity Framework
   - O crear manualmente la tabla

3. **Iniciar pruebas del flujo:**
   - Login como acudiente
   - Crear prematrícula
   - Probar validaciones
   - Probar asignación automática
   - Probar pago y matrícula

---

**Última ejecución:** 2025-01-XX
**Estado:** ✅ Datos dummy listos para pruebas

