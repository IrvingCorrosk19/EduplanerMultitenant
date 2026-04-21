# Flujo Funcional: Matrícula y Prematrícula

## 📋 Resumen del Proceso

El sistema gestiona el proceso completo desde la prematrícula hasta la matrícula final, con validaciones automáticas y asignación de grupos.

---

## 🔄 Flujo Completo del Proceso

### 1. CONFIGURACIÓN INICIAL (Administrador)

**¿Quién lo hace?** Administrador / Coordinador Académico

**Acciones:**
- Define el período de prematrícula (fecha inicio y fecha fin)
- Configura el cupo máximo de estudiantes por grupo
- Habilita o desactiva la asignación automática por turno (mañana/tarde)

**Resultado:**
- Durante el período activo: los acudientes pueden acceder a la opción de prematrícula
- Fuera del período: se muestra el mensaje "El período de prematrícula no está disponible"

---

### 2. PREMATRÍCULA (Acudiente/Estudiante)

**¿Quién lo hace?** Acudiente o Estudiante

**Paso 1: Acceso**
- El acudiente/estudiante ingresa al portal
- El sistema verifica si hay un período de prematrícula activo

**Paso 2: Validación de Condición Académica**
- **Regla:** El estudiante solo puede prematricularse si tiene **máximo 3 materias reprobadas**
- El sistema calcula automáticamente:
  - Obtiene todas las calificaciones del estudiante
  - Calcula el promedio por materia
  - Cuenta las materias con promedio menor a 3.0 (reprobadas)

**Si NO cumple:**
- ❌ Se muestra el mensaje: *"El estudiante no puede participar en la prematrícula por exceder el límite de materias reprobadas"*
- ❌ El proceso se detiene

**Si cumple:**
- ✅ Continúa al siguiente paso

**Paso 3: Selección de Grado y Grupo**
- El acudiente selecciona:
  - El estudiante (si es acudiente con varios hijos)
  - El grado al que quiere matricular
  - El grupo deseado (opcional, puede dejarse en blanco)

**Paso 4: Verificación de Cupos**
- Si se seleccionó un grupo específico:
  - El sistema verifica si el grupo tiene cupos disponibles
  - Si no hay cupos: ❌ Error "El grupo seleccionado no tiene cupos disponibles"
  - Si hay cupos: ✅ Continúa

**Paso 5: Asignación Automática de Grupo (Opcional)**
- **Si el período tiene asignación automática habilitada:**
  - El sistema busca grupos disponibles con la misma jornada del estudiante (mañana/tarde)
  - Si encuentra grupos con la misma jornada: Asigna automáticamente
  - Si no encuentra grupos con la misma jornada: Asigna el grupo con menos estudiantes

**Paso 6: Creación de Prematrícula**
- Se genera un código único de prematrícula (formato: `PRE-YYYYMMDD-HHMMSS-RANDOM`)
- Estado inicial: **"Prematriculado"**
- Se registra:
  - Estudiante
  - Grado
  - Grupo asignado
  - Cantidad de materias reprobadas
  - Fecha de creación

**Resultado:**
- ✅ Estado: **"Prematriculado"**
- ✅ Código de prematrícula generado
- ✅ Notificación al acudiente/estudiante

---

### 3. REGISTRO DE PAGO

**¿Quién lo hace?** Acudiente (pago en línea) o Contabilidad (pago manual)

#### Opción A: Pago en Línea (Acudiente)

**Flujo:**
1. Acudiente selecciona el concepto de pago (ej: Matrícula)
2. Selecciona el método de pago:
   - **Tarjeta de crédito/débito:** 
     - Pago se confirma automáticamente
     - Estado del pago: **"Confirmado"**
     - Estado de prematrícula cambia a: **"Pagado"**
   - **Transferencia/Depósito/Yappy:**
     - Debe adjuntar comprobante del pago
     - Estado del pago: **"Pendiente de verificación"**
     - Estado de prematrícula: **"Prematriculado"** (aún no cambia)

#### Opción B: Pago Manual (Contabilidad)

**Flujo:**
1. Contabilidad recibe el pago en caja
2. Busca la prematrícula del estudiante (por código o nombre)
3. Registra el pago:
   - Monto pagado
   - Número de recibo oficial
   - Método de pago
4. Cambia el estado del pago a: **"Confirmado"**
5. El sistema automáticamente:
   - Cambia el estado de prematrícula a: **"Pagado"**
   - Registra la fecha de pago

---

### 4. CONFIRMACIÓN DE MATRÍCULA (Automática)

**¿Cuándo ocurre?** Automáticamente cuando se confirma un pago

**Proceso Automático:**

1. **Verificación de Pago:**
   - El sistema verifica que exista al menos un pago con estado **"Confirmado"**
   - Si no hay pago confirmado: ❌ No se puede matricular

2. **Cambio de Estado:**
   - Estado de prematrícula: **"Prematriculado"** → **"Pagado"** → **"Matriculado"**
   - Se registra la fecha de matrícula

3. **Asignación del Estudiante:**
   - Se crea automáticamente un registro en `StudentAssignment`
   - El estudiante queda asignado al grupo y grado seleccionado
   - Ahora el estudiante puede:
     - Ver sus calificaciones
     - Asistir a clases
     - Participar en actividades académicas

4. **Notificaciones:**
   - 📧 Email automático al acudiente/estudiante con confirmación
   - 💬 Notificación en la plataforma
   - 📄 Comprobante de matrícula disponible para descarga

**Resultado:**
- ✅ Estado: **"Matriculado"**
- ✅ Estudiante asignado al grupo
- ✅ Fecha de matrícula registrada
- ✅ Notificaciones enviadas

---

## 📊 Estados del Proceso

```
PENDIENTE
    ↓
PREMATRICULADO ← (Después de crear prematrícula)
    ↓
PAGADO ← (Después de confirmar pago)
    ↓
MATRICULADO ← (Confirmación automática)
```

**Estados posibles:**
- **Pendiente:** Inicial, antes de validar
- **Prematriculado:** Prematrícula creada exitosamente
- **Pagado:** Pago confirmado, listo para matricular
- **Matriculado:** Matrícula confirmada y activa
- **Rechazado:** (No implementado en el flujo actual)

---

## 🔐 Roles y Permisos

### Acudiente/Padre
- ✅ Ver sus propias prematrículas
- ✅ Crear nueva prematrícula
- ✅ Realizar pago en línea
- ✅ Ver comprobante de matrícula
- ❌ No puede confirmar matrícula manualmente

### Estudiante
- ✅ Ver sus propias prematrículas
- ✅ Crear prematrícula para sí mismo
- ✅ Ver comprobante de matrícula
- ❌ No puede realizar pagos (debe ser acudiente)

### Administrador
- ✅ Ver todas las prematrículas
- ✅ Confirmar matrícula manualmente (si es necesario)
- ✅ Ver reportes por período, grupo, etc.
- ✅ Configurar períodos de prematrícula

### Contabilidad
- ✅ Ver pagos pendientes
- ✅ Confirmar pagos manuales
- ✅ Ver reportes de pagos
- ❌ No puede crear prematrículas

### Docente
- ✅ Ver estudiantes prematriculados/matriculados de sus grupos
- ✅ Consultar listados por grupo
- ❌ No puede crear o confirmar prematrículas

---

## ⚙️ Reglas de Negocio

### 1. Validación Académica
- **Regla:** Máximo 3 materias reprobadas
- **Cálculo:** Promedio por materia menor a 3.0 = reprobada
- **Aplicación:** Automática al crear prematrícula

### 2. Período de Prematrícula
- **Regla:** Solo se puede prematricular durante el período activo
- **Validación:** Fecha actual debe estar entre fecha inicio y fecha fin
- **Mensaje:** "El período de prematrícula no está disponible"

### 3. Cupos por Grupo
- **Regla:** No se puede asignar más estudiantes que el cupo máximo
- **Validación:** Al seleccionar grupo o asignar automáticamente
- **Mensaje:** "El grupo seleccionado no tiene cupos disponibles"

### 4. Asignación Automática
- **Regla:** Mantener la misma jornada (mañana/tarde) si está habilitada
- **Prioridad:** 
  1. Grupos con la misma jornada del estudiante
  2. Si no hay, grupo con menos estudiantes

### 5. Confirmación de Matrícula
- **Regla:** Requiere pago confirmado
- **Validación:** Al menos un pago con estado "Confirmado"
- **Mensaje:** "No se puede confirmar la matrícula sin un pago confirmado"

---

## 📝 Ejemplo de Flujo Completo

### Escenario: Acudiente prematricula a su hijo

1. **Día 1 - Configuración (Admin):**
   - Admin configura período: 01/01/2025 - 31/01/2025
   - Cupo máximo por grupo: 30 estudiantes

2. **Día 5 - Prematrícula (Acudiente):**
   - Acudiente ingresa al portal
   - Selecciona su hijo (estudiante)
   - Sistema valida: 2 materias reprobadas ✅
   - Selecciona grado: 10° y grupo: A
   - Sistema verifica: Grupo A tiene 25 estudiantes (5 cupos disponibles) ✅
   - Crea prematrícula: Estado "Prematriculado", Código: PRE-20250105-143022-1234

3. **Día 10 - Pago (Acudiente):**
   - Acudiente realiza pago en línea con tarjeta
   - Sistema confirma pago automáticamente
   - Estado de prematrícula: "Prematriculado" → "Pagado"

4. **Día 10 - Matrícula Automática (Sistema):**
   - Sistema detecta pago confirmado
   - Cambia estado a "Matriculado"
   - Crea `StudentAssignment` (estudiante asignado al grupo 10° A)
   - Envía email de confirmación al acudiente
   - Envía notificación en plataforma

5. **Resultado:**
   - ✅ Estudiante matriculado en 10° A
   - ✅ Puede acceder a sus clases
   - ✅ Comprobante disponible para descargar

---

## 🔍 Puntos Importantes

1. **La matrícula es automática** una vez confirmado el pago
2. **No se puede matricular sin pago confirmado**
3. **La validación académica es obligatoria** (máximo 3 materias reprobadas)
4. **El período debe estar activo** para poder prematricular
5. **Los cupos se verifican automáticamente** antes de asignar
6. **Las notificaciones se envían automáticamente** al acudiente/estudiante

---

**Última actualización:** 2025-01-XX

