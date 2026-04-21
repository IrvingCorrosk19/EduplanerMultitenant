# QA exhaustivo – Módulo Aprobados/Reprobados

**Fecha:** 2026-02-12  
**Módulo:** Cuadro de Aprobados y Reprobados por Grado  
**Ruta principal:** GET /AprobadosReprobados/Index  
**Alcance:** Funcionalidad, reglas de negocio, seguridad, multi-tenant, integridad, performance. Sin modificación de código.

---

## 1. Resumen ejecutivo

El módulo permite a Admin/Director/Teacher generar reportes de aprobados y reprobados por trimestre y nivel (Premedia/Media), con filtros opcionales por grado, especialidad, área y materia. Las consultas de datos se atan al **SchoolId del usuario actual** en el controlador; el servicio recibe siempre ese `schoolId` y filtra grupos y listas por escuela. Se identifican **hallazgos medios** (CSRF no validado en POST, posible N+1, filtro por escuela faltante en calificaciones) y **bajos** (mensaje sin datos, parámetro `grupo` no enviado a Vista Previa, exposición de mensaje de error en JSON). No se detectan fugas multi-tenant directas (no se acepta `schoolId` ni IDs de otra escuela en la request para datos); el reporte es de solo lectura. **Estado general:** utilizable en producción con correcciones recomendadas. **Riesgos:** CSRF en POST GenerarReporte; performance con muchos estudiantes por grupo; endurecer multi-tenant filtrando calificaciones por escuela.

---

## 2. Matriz de escenarios

| # | Escenario | Resultado | Severidad |
|---|-----------|-----------|-----------|
| A1 | Carga inicial Index sin errores JS | OK | - |
| A2 | Combos se llenan (trimestre, nivel, área, especialidad, materia) | OK | - |
| A3 | Trimestre vacío + Generar → mensaje claro | OK (cliente + servidor) | - |
| A4 | Nivel vacío + Generar → mensaje claro | OK (cliente + servidor) | - |
| A5 | Cambiar nivel → grados se actualizan (7°–9° o 10°–12°) | OK | - |
| A6 | Cambiar área/especialidad → materias se actualizan vía AJAX | OK | - |
| A7 | Limpiar → form reset, reporte oculto, grados/materias a "Todos" | OK (materia options pueden quedar de filtro anterior hasta nuevo change) | Baja |
| A8 | Sin datos → se muestra tabla con totales en 0; no hay mensaje explícito "Sin resultados" | FAIL (UX) | Baja |
| B1 | Criterio aprobado: promedio ≥ 3.0 y &lt; 3 materias reprobadas | OK (NOTA_MINIMA_APROBACION = 3.0m; reprobado si ≥3 materias &lt; 3.0) | - |
| B2 | Conteo total = aprobados + reprobados + sin calificaciones + retirados | OK | - |
| B3 | Filtro por trimestre: solo notas de actividades con ese Trimester | OK | - |
| B4 | Sin nota → contado como "Sin calificaciones" (no reprobado) | OK | - |
| B5 | Estudiante retirado/inactivo → excluido del total efectivo (Retirados) | OK | - |
| C1 | Solo Admin/Director/Teacher pueden acceder | OK ([Authorize(Roles = "admin,director,teacher")]) | - |
| C2 | POST GenerarReporte no valida antiforgery token | FAIL | Media |
| C3 | Respuesta JSON con error devuelve ex.Message | FAIL (info interna) | Media |
| D1 | Todas las consultas usan SchoolId del usuario actual | OK (controller) | - |
| D2 | Listados (trimestres, niveles, especialidades, materias) filtrados por escuela donde aplica | OK (ObtenerAreas sin schoolId: Area es global por modelo) | - |
| D3 | Calificaciones no filtradas explícitamente por SchoolId en servicio | FAIL (endurecimiento) | Media |
| E1 | Módulo solo lectura (no escribe BD en flujo de reporte) | OK | - |
| F1 | N+1: por cada estudiante se hace FindAsync + query de calificaciones | FAIL | Media |
| F2 | Sin paginación en tabla (datos en memoria) | OK para tamaños típicos | Baja |

---

## 3. Hallazgos críticos

**Ninguno** que impida el uso del módulo o provoque fuga de datos entre escuelas en el flujo actual. Los puntos más sensibles se tratan como medios.

---

## 4. Hallazgos medios

### M1 – POST GenerarReporte sin validación antiforgery

- **Archivo:** `Controllers/AprobadosReprobadosController.cs`  
- **Método / ruta:** `GenerarReporte` (POST /AprobadosReprobados/GenerarReporte)  
- **Evidencia:** El controlador no tiene `[ValidateAntiForgeryToken]` ni `[AutoValidateAntiforgeryToken]`. La vista envía `__RequestVerificationToken` en el AJAX (línea 262, Index.cshtml), pero el controlador no lo valida.  
- **Impacto:** Riesgo de CSRF: un sitio externo podría hacer que un usuario autenticado genere reportes sin intención.  
- **Fix sugerido:** Añadir `[ValidateAntiForgeryToken]` al método `GenerarReporte` (o `[AutoValidateAntiforgeryToken]` a nivel controlador). No modificar la vista; el token ya se envía.

---

### M2 – Respuesta de error expone ex.Message

- **Archivo:** `Controllers/AprobadosReprobadosController.cs`  
- **Método:** `GenerarReporte` (catch), y otros endpoints que devuelven `Json(..., message: ex.Message)`.  
- **Evidencia:** Línea 113: `return Json(new { success = false, message = $"Error: {ex.Message}" });`  
- **Impacto:** En producción, mensajes de excepción pueden revelar rutas, nombres de tablas o detalles internos.  
- **Fix sugerido:** En producción devolver un mensaje genérico (ej. "Error al generar el reporte") y registrar `ex` con el logger. No exponer `ex.Message` ni stack en JSON.

---

### M3 – Calificaciones sin filtro explícito por escuela

- **Archivo:** `Services/Implementations/AprobadosReprobadosService.cs`  
- **Método:** `CalcularEstadisticasGrupoAsync`  
- **Evidencia:** La consulta de `StudentActivityScores` (líneas 168–174) filtra por `StudentId` y `Activity.Trimester` (y materia/área/especialidad), pero **no** por `Activity.SchoolId` ni `StudentActivityScore.SchoolId`. El `grupoId` proviene de grupos ya filtrados por `schoolId` en `GenerarReporteAsync`, por lo que en práctica los estudiantes son de la escuela; sin embargo, si hubiera datos incorrectos (scores/actividades de otra escuela), podrían mezclarse.  
- **Impacto:** Riesgo bajo con datos correctos; endurecimiento multi-tenant recomendado.  
- **Fix sugerido:** Pasar `schoolId` a `CalcularEstadisticasGrupoAsync` y añadir en la query de calificaciones, por ejemplo:  
  `sas.SchoolId == schoolId` o `sas.Activity!.SchoolId == schoolId` (según modelo y que SchoolId esté poblado). Asegurar que solo se consideren actividades/scores de la escuela.

---

### M4 – N+1 en cálculo por grupo

- **Archivo:** `Services/Implementations/AprobadosReprobadosService.cs`  
- **Método:** `CalcularEstadisticasGrupoAsync`  
- **Evidencia:** Por cada `estudianteId` en `estudiantesDelGrupo` se ejecuta:  
  - `_context.Users.FindAsync(estudianteId)` (línea 164)  
  - Una query de `StudentActivityScores` con filtros (líneas 168–204)  
  Para muchos estudiantes y varios grupos, el número de consultas crece mucho.  
- **Impacto:** Con grupos grandes (ej. 30+ estudiantes) y varios grados, la generación del reporte puede ser lenta.  
- **Fix sugerido:** (1) Cargar en una sola consulta los usuarios del conjunto de `estudianteId` (por ejemplo con `Where(u => ids.Contains(u.Id))`) y usar un diccionario en memoria para el estado retirado. (2) Valorar cargar todas las calificaciones del grupo/trimestre en una o pocas consultas y agrupar en memoria por estudiante, en lugar de una query por estudiante.

---

## 5. Hallazgos bajos

### B1 – Sin mensaje explícito "Sin resultados"

- **Archivo:** `Views/AprobadosReprobados/Index.cshtml`  
- **Evidencia:** Cuando `data.estadisticas` está vacío, `mostrarReporte` pinta la tabla con totales en 0. No hay texto tipo "No hay datos para los filtros seleccionados".  
- **Impacto:** El usuario puede dudar si no hay datos o si falló la carga.  
- **Fix sugerido:** Si `data.estadisticas.length === 0`, mostrar un mensaje claro en lugar de (o además de) la tabla con ceros.

---

### B2 – Parámetro "grupo" no enviado a Vista Previa

- **Archivo:** `Views/AprobadosReprobados/Index.cshtml`  
- **Evidencia:** En el click de "Vista Completa" (líneas 292–307) se construye la URL con `trimestre`, `nivelEducativo`, `grado`, `especialidadId`, `areaId`, `materiaId`. No se añade `grupo` (equivalente a `GrupoEspecifico`). El controlador `VistaPrevia` acepta `grupo` (string) en la firma.  
- **Impacto:** Si en el futuro se expone filtro por grupo en la UI, la vista previa abierta desde el reporte no lo reflejaría. Hoy el filtro por grupo no está en la vista Index (solo grado), por lo que el impacto es bajo.  
- **Fix sugerido:** Si se agrega filtro por grupo en Index, incluir en la URL de vista previa:  
  `if (grupoEspecifico) url += '&grupo=' + encodeURIComponent(grupoEspecifico);`

---

### B3 – Roles con minúsculas en Authorize

- **Archivo:** `Controllers/AprobadosReprobadosController.cs`  
- **Evidencia:** `[Authorize(Roles = "admin,director,teacher")]` (línea 9). En otros controladores del proyecto se usan variantes con mayúscula ("Admin", "Director").  
- **Impacto:** Si el proveedor de identidad guarda roles con distinta capitalización, podría haber denegación inesperada.  
- **Fix sugerido:** Confirmar cómo se almacenan los roles en la BD/claims y alinear (p. ej. "Admin,Director,Teacher" o el valor que use el resto de la app).

---

## 6. Seguridad – Checklist y pruebas

| Prueba | Resultado | Notas |
|--------|-----------|--------|
| Solo roles permitidos acceden | OK | [Authorize(Roles = "admin,director,teacher")]. Student no puede acceder. |
| POST con token antiforgery | FAIL | Token se envía pero no se valida (M1). |
| Respuesta de error no expone detalles internos | FAIL | Se devuelve ex.Message (M2). |
| Entradas no concatenadas a SQL | OK | Uso de EF y parámetros; no hay SQL crudo con entrada de usuario. |
| Filtros (trimestre, nivel, Guids) validados | OK | Trimestre/nivel son strings acotados; Guids se usan en filtros EF. No se valida que área/especialidad/materia sean de la escuela en GET ObtenerMaterias (solo se filtra por schoolId en materias); riesgo bajo. |
| VistaPrevia / Exportar con IDs por query | OK | Reciben trimestre, nivel, grado, grupo, especialidadId, areaId, materiaId. El servicio usa siempre `currentUser.SchoolId`; no se puede pedir datos de otra escuela. |

**Resumen seguridad:** Aceptable con mejoras: validar antiforgery en POST y no devolver `ex.Message` al cliente.

---

## 7. Multi-tenant – Checklist y pruebas

| Prueba | Resultado | Notas |
|--------|-----------|--------|
| Index / GenerarReporte usan solo currentUser.SchoolId | OK | Líneas 38–39, 74–76, 92–103. |
| VistaPrevia / ExportarPdf / ExportarExcel usan currentUser.SchoolId | OK | Líneas 124–129, 164–167, 208–211. |
| ObtenerTrimestresDisponiblesAsync filtra por schoolId | OK | Activities.Where(a => a.SchoolId == schoolId). |
| ObtenerEspecialidadesAsync filtra por schoolId (o null) | OK | Specialties.Where(s => s.SchoolId == schoolId \|\| s.SchoolId == null). |
| ObtenerAreasAsync | OK | Area no tiene SchoolId en el modelo (áreas globales). |
| ObtenerMateriasAsync filtra por schoolId | OK | Subjects.Where(s => s.SchoolId == schoolId). |
| Grupos filtrados por schoolId en GenerarReporteAsync | OK | Groups.Where(g => g.SchoolId == schoolId && g.Grade == grado). |
| Calificaciones filtradas por escuela | FAIL (endurecimiento) | No se filtra por SchoolId en Activity/StudentActivityScore (M3). |

**Prueba conceptual (no ejecutada):**  
- **Given:** Usuario Admin de School A autenticado.  
- **When:** Llama a GenerarReporte (POST) o VistaPrevia (GET) con cualquier combinación de parámetros.  
- **Then:** El servicio recibe siempre `currentUser.SchoolId` (School A) y obtiene grupos y datos de esa escuela. No existe parámetro `schoolId` en la request; no hay forma de pedir datos de School B.

**Conclusión multi-tenant:** Sin fugas en el diseño actual. Recomendable endurecer filtrando calificaciones por escuela (M3).

---

## 8. Performance – Análisis y recomendaciones

- **N+1 (M4):** En `CalcularEstadisticasGrupoAsync`, por cada estudiante del grupo se hacen al menos 2 consultas (User, StudentActivityScores). Con 10 grupos de 25 estudiantes son 500+ consultas.  
  **Recomendación:** Cargar usuarios por lista de IDs y, si es posible, calificaciones por grupo/trimestre en batch; reducir round-trips a BD.

- **Índices sugeridos (si no existen):**  
  - `student_assignments (group_id, is_active)` para la query de estudiantes del grupo.  
  - `student_activity_scores (student_id, ...)` y/o `activities (trimester, school_id)` para las consultas de calificaciones por trimestre.  
  Revisar en migraciones/BD actuales antes de añadir.

- **Carga y paginación:** El reporte devuelve todos los grupos del nivel/grado en memoria. Para niveles con muchos grupos (ej. 20+) el payload y el tiempo de respuesta pueden crecer; en tamaños típicos es aceptable. No hay paginación en la tabla; si en el futuro se amplía el alcance, valorar paginación o límites.

---

## 9. Reglas de negocio – Criterios confirmados en código

- **Nota mínima de aprobación:** `NOTA_MINIMA_APROBACION = 3.0m` (escala 0–5).  
  Archivo: `AprobadosReprobadosService.cs`, línea 13.

- **Aprobado:** Promedio general ≥ 3.0 y ninguna materia con promedio &lt; 3.0 (o &lt; 3 materias reprobadas).  
  Líneas 226–240: si `materiasReprobadas >= 3` → reprobado; si `materiasReprobadas == 0` y `promedioGeneral >= 3.0` → aprobado.

- **Reprobado hasta la fecha:** Al menos una materia con promedio &lt; 3.0.  
- **Reprobado definitivo:** 3 o más materias con promedio &lt; 3.0.

- **Sin calificaciones:** Estudiante activo sin `StudentActivityScores` para el trimestre (o sin actividades que pasen filtros de materia/área/especialidad) → contado en "Sin calificaciones", no como aprobado ni reprobado. Líneas 206–211.

- **Retirados:** `User.Status` "inactive" o "retirado" → excluido del conteo efectivo y sumado a "Retirados". Líneas 164–169.

- **Trimestre:** Solo se consideran actividades con `Activity.Trimester == trimestre`. Línea 173.

- **Totales:** TotalesGenerales = suma de las estadísticas por grupo; porcentajes coherentes con totales. Líneas 264–287.

---

## 10. Checklist "Listo para producción"

| Criterio | Sí/No | Comentario |
|----------|--------|------------|
| Funcionalidad principal correcta | Sí | Generación de reporte y filtros operan según reglas. |
| Validaciones requeridas (trimestre, nivel) | Sí | Cliente y servidor. |
| Autorización por rol | Sí | Solo admin, director, teacher. |
| Multi-tenant sin fuga | Sí | Con endurecimiento recomendado (M3). |
| CSRF en POST | No | Añadir validación antiforgery (M1). |
| No exponer ex.Message | No | Mensaje genérico en producción (M2). |
| Performance aceptable | Parcial | N+1 con muchos estudiantes (M4). |
| Manejo de errores controlado | Parcial | Try/catch y redirect/JSON; mejorar mensaje al cliente. |
| Solo lectura sobre datos | Sí | No hay escritura en flujo de reporte. |

**Veredicto:** **Listo para producción con reservas.** El módulo es utilizable y multi-tenant en el flujo actual. Se recomienda aplicar las correcciones de hallazgos medios (antiforgery, mensaje de error, filtro por escuela en calificaciones y mitigación de N+1) antes de considerarlo cerrado para entornos enterprise.

---

## 11. Evidencia de archivos revisados

- `Controllers/AprobadosReprobadosController.cs` – Autorización, endpoints, uso de `currentUser.SchoolId`, ausencia de antiforgery en POST, respuesta de error.
- `Services/Implementations/AprobadosReprobadosService.cs` – Criterios aprobado/reprobado, filtros por escuela, N+1, falta de filtro por escuela en calificaciones.
- `ViewModels/AprobadosReprobadosViewModel.cs` – Filtros requeridos y opcionales.
- `Views/AprobadosReprobados/Index.cshtml` – Validación cliente, cascadas, Limpiar, construcción de URL de vista previa, mensaje sin datos.
- `Views/AprobadosReprobados/VistaPrevia.cshtml` – Solo revisión de estructura.
- Modelos: `Area.cs` (sin SchoolId), `Activity.cs`, `StudentActivityScore.cs` (SchoolId presente).

No se ha modificado código; solo análisis estático y revisión de flujos.

---

## 12. Pasos de reproducción (Given/When/Then)

### M1 – CSRF no validado

- **Given:** Usuario Admin autenticado; página Index de Aprobados/Reprobados cargada.
- **When:** Desde otra pestaña o con una herramienta (Postman/curl) se envía POST a `/AprobadosReprobados/GenerarReporte` sin `__RequestVerificationToken` (o con token inválido), con body válido (Trimestre, NivelEducativo, etc.).
- **Expected:** El servidor rechaza la petición (400 o similar) por token antiforgery inválido.
- **Actual:** Si el controlador no valida el token, la petición es aceptada y se genera el reporte.
- **Root cause:** Falta `[ValidateAntiForgeryToken]` (o equivalente) en el método POST.
- **Fix sugerido:** Añadir validación antiforgery al POST sin cambiar la vista.

### M2 – Mensaje de error expuesto

- **Given:** Servicio o BD en estado que provoque excepción (ej. timeout, FK rota).
- **When:** El usuario hace "Generar Reporte" y el servidor captura una excepción en `GenerarReporte`.
- **Expected:** El cliente recibe un mensaje genérico ("Error al generar el reporte") y el detalle se registra en logs.
- **Actual:** El JSON devuelve `message: "Error: {ex.Message}"`, p. ej. mensaje de SQL o ruta interna.
- **Root cause:** Uso directo de `ex.Message` en la respuesta JSON.
- **Fix sugerido:** En el catch, registrar `ex` y devolver `message: "Error al generar el reporte."` (o mensaje genérico configurado).

### A8 – Sin datos sin mensaje explícito

- **Given:** Trimestre y nivel con grupos sin estudiantes o sin actividades/notas para ese trimestre.
- **When:** El usuario genera el reporte.
- **Expected:** Mensaje claro "No hay datos para los filtros seleccionados" o similar.
- **Actual:** Se muestra la tabla con filas de totales en 0; no hay texto explicativo.
- **Root cause:** La vista siempre pinta la tabla con `data.estadisticas` y `data.totalesGenerales` sin comprobar si hay filas.
- **Fix sugerido:** En `mostrarReporte`, si `!data.estadisticas || data.estadisticas.length === 0`, mostrar un `<p>` o alert con el mensaje y opcionalmente ocultar la tabla o mostrar solo totales con leyenda.
