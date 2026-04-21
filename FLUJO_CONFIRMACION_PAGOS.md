# 🔄 Flujo de Confirmación de Pagos

## 📋 Estado: Pendiente

Cuando un pago se registra con estado **"Pendiente"**, significa que:

1. ✅ El pago fue registrado en el sistema
2. ⏳ Está esperando confirmación por parte de contabilidad
3. ❌ La matrícula NO se activa automáticamente
4. 📧 El acudiente/estudiante recibe notificación de registro pendiente

### ¿Cuándo un pago queda en "Pendiente"?

- **Método de pago manual**: Transferencia, Depósito, Yappy
- **Requiere comprobante**: Debe adjuntar imagen del comprobante
- **Requiere verificación**: Contabilidad debe validar el comprobante

### ¿Cuándo un pago se confirma automáticamente?

- **Método de pago**: Tarjeta de Crédito/Débito
- **Estado inicial**: "Confirmado" (simulado)
- **Sin verificación manual**: Se activa automáticamente

---

## ✅ Proceso de Confirmación

### 1. **Quién puede confirmar pagos**

- ✅ **Admin** (`admin`)
- ✅ **SuperAdmin** (`superadmin`)
- ✅ **Contabilidad** (`contabilidad`, `contable`)

### 2. **Dónde confirmar pagos**

#### **Opción A: Desde la lista de pagos** (`/Payment/Index`)
- Ver todos los pagos de la escuela
- Botón verde con icono ✓ para cada pago pendiente
- Click en "Confirmar" → Confirma el pago

#### **Opción B: Desde detalles del pago** (`/Payment/Details/{id}`)
- Ver detalles completos del pago
- Botón "Confirmar Pago" si está pendiente
- Ver comprobante adjunto antes de confirmar

### 3. **Qué ocurre al confirmar**

Cuando se confirma un pago pendiente, el sistema realiza automáticamente:

#### **A. Actualización del Pago**
```csharp
- Estado: "Pendiente" → "Confirmado"
- Fecha de confirmación: Se establece automáticamente
- Usuario que confirmó: Se registra (registered_by)
```

#### **B. Actualización de la Prematrícula**
```csharp
- Estado: "Prematriculado" → "Pagado"
- Fecha de pago: Se establece automáticamente
- PaymentDate: Se actualiza con la fecha de confirmación
```

#### **C. Activación Automática de Matrícula**
```csharp
- Si la prematrícula está en estado "Pagado"
- Se ejecuta: ConfirmMatriculationAsync()
- Estado final: "Prematriculado" → "Pagado" → "Matriculado"
- Fecha de matrícula: Se establece automáticamente
```

#### **D. Notificación al Acudiente/Estudiante**
```csharp
- Se envía mensaje automático
- Asunto: "✅ Pago Confirmado - [Nombre Estudiante]"
- Contenido: Detalles del pago confirmado
- Información: Si corresponde a matrícula, se activa automáticamente
```

---

## 🔄 Flujo Completo Visual

```
┌─────────────────────────────────────────────────────────────┐
│ 1. REGISTRO DE PAGO                                         │
│    - Usuario registra pago con método manual                │
│    - Estado inicial: "Pendiente"                            │
│    - Comprobante adjuntado                                  │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. PAGO PENDIENTE                                           │
│    - Estado: "Pendiente"                                    │
│    - Badge amarillo: ⚠️ Pendiente                          │
│    - Matrícula NO activada                                  │
│    - Notificación enviada a contabilidad                     │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ [Usuario con rol: admin/contabilidad]
                       │ [Click en botón "Confirmar"]
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. CONFIRMACIÓN DE PAGO                                     │
│    - Estado: "Pendiente" → "Confirmado"                     │
│    - Fecha de confirmación: Se establece                    │
│    - Usuario que confirmó: Se registra                     │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. ACTUALIZACIÓN DE PREMATRÍCULA                            │
│    - Estado: "Prematriculado" → "Pagado"                    │
│    - PaymentDate: Se actualiza                              │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. ACTIVACIÓN AUTOMÁTICA DE MATRÍCULA                      │
│    - Se ejecuta: ConfirmMatriculationAsync()                │
│    - Estado final: "Matriculado"                            │
│    - MatriculationDate: Se establece                        │
│    - Notificación enviada al acudiente/estudiante           │
└─────────────────────────────────────────────────────────────┘
```

---

## 📧 Notificaciones

### **Al Registrar Pago (Pendiente)**
- 📨 **Destinatario**: Usuarios de contabilidad de la escuela
- 📋 **Asunto**: "💰 Pago Pendiente de Verificación - [Nombre Estudiante]"
- 📝 **Contenido**: 
  - Detalles del pago registrado
  - Monto y método de pago
  - Número de recibo
  - Solicitud de verificación

### **Al Confirmar Pago**
- 📨 **Destinatario**: Acudiente/Estudiante
- 📋 **Asunto**: "✅ Pago Confirmado - [Nombre Estudiante]"
- 📝 **Contenido**:
  - Confirmación del pago
  - Detalles del pago (recibo, monto, fecha)
  - Información sobre activación de matrícula (si corresponde)

---

## 🔍 Dónde Ver Pagos Pendientes

### **1. Vista de Administración** (`/Payment/Index`)
- Lista todos los pagos de la escuela
- Columna "Estado" muestra: ⚠️ Pendiente o ✅ Confirmado
- Botón verde para confirmar pagos pendientes
- Solo usuarios con rol: admin, superadmin, contabilidad

### **2. Vista de Detalles** (`/Payment/Details/{id}`)
- Muestra detalles completos del pago
- Información del comprobante (si existe)
- Botón para confirmar si está pendiente
- Historial de pagos de la prematrícula

### **3. Vista de Registro** (`/Payment/Register/{prematriculationId}`)
- Muestra lista de pagos registrados para la prematrícula
- Solo muestra información (no permite confirmar)
- Botones para ver detalles o comprobante

---

## ⚙️ Configuración y Validaciones

### **Validaciones al Confirmar**
1. ✅ El pago debe existir
2. ✅ El pago NO debe estar ya confirmado
3. ✅ El usuario debe tener rol de admin/contabilidad
4. ✅ La prematrícula debe existir (si está asociada)

### **Protecciones**
- ❌ No se puede eliminar un pago confirmado
- ❌ No se puede confirmar un pago ya confirmado
- ✅ Se puede ver historial completo de pagos

---

## 📊 Estados del Pago

| Estado | Badge | Descripción | Acción Disponible |
|--------|-------|-------------|-------------------|
| **Pendiente** | ⚠️ Amarillo | Esperando confirmación | Confirmar |
| **Confirmado** | ✅ Verde | Pago verificado y procesado | Ver detalles |

---

## 🎯 Resumen: ¿Qué pasa cuando se paga un pago pendiente?

1. ✅ **Estado del pago cambia**: "Pendiente" → "Confirmado"
2. ✅ **Fecha de confirmación**: Se registra automáticamente
3. ✅ **Prematrícula actualizada**: "Prematriculado" → "Pagado"
4. ✅ **Matrícula activada**: Si corresponde, se activa automáticamente
5. ✅ **Notificación enviada**: Al acudiente/estudiante sobre confirmación
6. ✅ **Badge actualizado**: De amarillo (Pendiente) a verde (Confirmado)
7. ✅ **Botón de confirmar**: Ya no aparece (solo para pendientes)

---

**Última actualización**: 2025-01-XX
**Estado**: ✅ Flujo completo implementado y funcionando

