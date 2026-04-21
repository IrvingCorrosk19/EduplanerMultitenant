# ✅ Guía: Cómo Confirmar un Pago Pendiente (Admin)

## 🎯 Requisitos

- ✅ Rol: **Admin** o **SuperAdmin** o **Contabilidad**
- ✅ Acceso a: `/Payment/Index` o `/Payment/Details/{id}`

---

## 📋 Método 1: Desde la Lista de Pagos

### Paso 1: Acceder a la Gestión de Pagos

**Opción A: Desde el Menú del Layout Admin**
1. Inicia sesión como **Admin**
2. En el menú lateral izquierdo, busca la sección **"Pagos"** 💰
3. Haz clic en **"Pagos"** para expandir el submenú
4. Haz clic en **"Gestión de Pagos"** 📋
5. Serás redirigido a: `/Payment/Index`

**Opción B: Acceso Directo**
- Navega directamente a:
   ```
   http://localhost:5172/Payment/Index
   ```

**Ruta en el Menú:**
```
Menú Lateral → Pagos 💰 → Gestión de Pagos 📋
```

### Paso 2: Identificar Pagos Pendientes
- Los pagos pendientes tienen un **badge amarillo** con el texto: ⚠️ **Pendiente**
- Los pagos confirmados tienen un **badge verde** con el texto: ✅ **Confirmado**

### Paso 3: Confirmar el Pago
1. En la columna **"Acciones"** de la tabla, busca el pago pendiente
2. Verás tres botones:
   - 👁️ **Azul** (Ver detalles)
   - 🖼️ **Gris** (Ver comprobante) - Solo si hay imagen adjunta
   - ✅ **Verde** (Confirmar pago) - Solo para pagos pendientes

3. Haz clic en el botón **verde** con el icono ✓
4. Aparecerá un diálogo de confirmación: **"¿Confirma este pago?"**
5. Haz clic en **"Aceptar"** o **"Confirmar"**

### Paso 4: Verificar Confirmación
- El pago cambia de estado: **"Pendiente"** → **"Confirmado"**
- El badge cambia de amarillo a verde
- Se registra la **fecha de confirmación**
- El botón verde desaparece (ya no se puede confirmar de nuevo)

---

## 📋 Método 2: Desde Detalles del Pago

### Paso 1: Acceder a Detalles del Pago
1. Desde la lista de pagos (`/Payment/Index`)
2. Haz clic en el botón **👁️ Azul** (Ver detalles) del pago que quieres confirmar
3. O navega directamente a:
   ```
   http://localhost:5172/Payment/Details/{id-del-pago}
   ```

### Paso 2: Revisar Detalles del Pago
En la página de detalles verás:
- 📄 Número de recibo
- 💰 Monto
- 📅 Fecha de pago
- 📊 Estado (Pendiente o Confirmado)
- 🖼️ Comprobante (si está adjunto)
- 📝 Notas
- 👤 Información del estudiante/prematrícula

### Paso 3: Confirmar el Pago
1. Si el pago está **Pendiente**, verás un botón verde en la parte inferior:
   ```
   [🔙 Volver] [✅ Confirmar Pago]
   ```
2. Haz clic en el botón **"Confirmar Pago"**
3. Aparecerá un diálogo de confirmación: **"¿Confirma este pago?"**
4. Haz clic en **"Aceptar"** o **"Confirmar"**

### Paso 4: Verificar Confirmación
- El pago cambia de estado: **"Pendiente"** → **"Confirmado"**
- El badge cambia de amarillo a verde
- Se registra la **fecha de confirmación**
- El botón "Confirmar Pago" desaparece
- Se muestra mensaje de éxito: **"Pago confirmado exitosamente..."**

---

## 🔄 ¿Qué ocurre al confirmar?

Cuando confirmas un pago pendiente, el sistema realiza automáticamente:

1. ✅ **Actualiza el estado del pago**: "Pendiente" → "Confirmado"
2. ✅ **Registra la fecha de confirmación**: Se establece automáticamente
3. ✅ **Actualiza la prematrícula**: "Prematriculado" → "Pagado"
4. ✅ **Activa la matrícula**: Si corresponde, se activa automáticamente
5. ✅ **Envía notificación**: Al acudiente/estudiante sobre la confirmación

---

## 🖼️ Visualización de la Interfaz

### Vista de Lista de Pagos (`/Payment/Index`)

```
┌─────────────────────────────────────────────────────────────┐
│  Gestión de Pagos de Prematrícula                           │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  [🔍 Buscar Prematrícula para Pago]                        │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐ │
│  │ Lista de Pagos                                        │ │
│  ├──────────────────────────────────────────────────────┤ │
│  │ Recibo │ Concepto │ Monto │ Estado │ Acciones        │ │
│  ├──────────────────────────────────────────────────────┤ │
│  │ 12345  │ Matrícula│ $100  │ ⚠️ Pendiente│ [👁️][✅] │ │
│  │ 12346  │ Mensual. │ $50   │ ✅ Confirmado│ [👁️]    │ │
│  └──────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Vista de Detalles del Pago (`/Payment/Details/{id}`)

```
┌─────────────────────────────────────────────────────────────┐
│  Detalles del Pago                                          │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Número de Recibo: 12345                                    │
│  Monto: $100.00                                             │
│  Estado: ⚠️ Pendiente                                       │
│  Fecha de Pago: 05/11/2025                                  │
│  Comprobante: [🖼️ Ver Comprobante]                        │
│                                                              │
│  [🔙 Volver] [✅ Confirmar Pago]                          │
└─────────────────────────────────────────────────────────────┘
```

---

## ⚠️ Notas Importantes

1. **Solo pagos pendientes** pueden ser confirmados
2. **No se puede confirmar** un pago ya confirmado
3. **No se puede eliminar** un pago confirmado
4. **La confirmación es permanente** - No se puede deshacer
5. **Se requiere confirmación** antes de confirmar (diálogo de seguridad)

---

## 🎯 Resumen Rápido

### Opción A: Desde Lista
```
/Payment/Index → Buscar pago pendiente → Clic en botón ✅ verde
```

### Opción B: Desde Detalles
```
/Payment/Index → Clic en 👁️ (Ver detalles) → Clic en "Confirmar Pago"
```

---

## ✅ Pasos Visuales

1. **🔍 Buscar** el pago pendiente en la lista
2. **👁️ Ver** detalles (opcional) o **✅ Confirmar** directamente
3. **✋ Confirmar** en el diálogo de seguridad
4. **✅ Verificar** que el estado cambió a "Confirmado"

---

**Última actualización**: 2025-01-XX
**Estado**: ✅ Funcional y listo para usar

