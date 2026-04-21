# Bitácora — App Flutter (SchoolManager ID Scanner)

**Fecha:** 2026-02-12  
**Objetivo:** Integrar / trabajar con la app móvil Flutter que consume el backend SchoolManager.

---

## Ubicación de la app Flutter

| Proyecto | Ruta |
|----------|------|
| **App Flutter (ID Scanner)** | `C:\src\schoolmanager_id_scanner` |
| **Backend (API)** | `C:\Proyectos\EduplanerIIC\SchoolManager` |

---

## Endpoints del backend disponibles para la app

- **Login:** `POST /api/auth/login`  
  Body: `{ "email": "...", "password": "..." }`  
  Respuesta: `token`, `userId`, `email`, `name`, `role`.

- **Escanear carné:** `POST /StudentIdCard/api/scan`  
  Body: `{ "token": "..." }` (token del QR).  
  `[AllowAnonymous]`.

- **Generar carné:** `POST /StudentIdCard/api/generate/{studentId}`  
  Requiere autenticación (Admin, SuperAdmin, Director).

- **Listar estudiantes (JSON):** `GET /StudentIdCard/api/list-json`  
  Requiere autenticación.

---

---

## Evaluación: ¿La app ya consume las APIs?

**Conclusión: Sí. La app Flutter está preparada para consumir las APIs del backend.**

### 1. Login — `POST /api/auth/login`

| Aspecto | Estado | Detalle |
|--------|--------|---------|
| URL | OK | `$baseUrl/api/auth/login` en `lib/services/id_card_api.dart` |
| Body | OK | `jsonEncode({'email': email, 'password': password})` — coincide con backend |
| Uso del token | OK | La app guarda `json['token']` (backend devuelve `token`). También contempla `accessToken` |
| Pantalla | OK | `login_screen.dart` llama a `IdCardApi.login()` y guarda el token en `Storage` |

### 2. Escaneo — `POST /StudentIdCard/api/scan`

| Aspecto | Estado | Detalle |
|--------|--------|---------|
| URL | OK | `$baseUrl/StudentIdCard/api/scan` |
| Body | OK | `token`, `scanType`, `scannedBy` — coincide con `ScanRequestDto` (Token, ScanType, ScannedBy) |
| Respuesta | OK | `ScanResult.fromJson()` espera `allowed`, `message`, `studentName`, `grade`, `group` — el backend devuelve eso en camelCase |
| Pantalla | OK | `scan_screen.dart` obtiene el token del QR, llama a `IdCardApi.scan(token, jwt)` y navega a `ResultScreen` |

### 3. Configuración de base URL

- **Local:** `http://192.168.1.18:5172` (IP fija; hay que ajustarla a la IP de tu PC en la red).
- **Producción:** `https://eduplaner.net`.
- **Flag:** `useLocalhost = true` — actualmente apunta a local.

### 4. Observaciones / mejoras sugeridas

1. **IP local:** En `id_card_api.dart` la IP `192.168.1.18` debe ser la de la PC donde corre el backend. Si cambia la red o la PC, hay que actualizarla (o hacerla configurable).
2. **Emulador Android:** El comentario indica usar `http://10.0.2.2:5172` para emulador; con `useLocalhost = true` y IP fija, en emulador no funcionará a menos que se cambie a 10.0.2.2.
3. **Scan es [AllowAnonymous]:** El backend no exige auth para `/StudentIdCard/api/scan`. La app envía `Authorization: Bearer $jwt` de todas formas; no rompe nada, pero el backend no lo usa para este endpoint.
4. **ScannedBy:** La app envía `'00000000-0000-0000-0000-000000000000'`. Si se quiere registrar quién escaneó, habría que enviar el `userId` del login (el backend ya tiene el campo en el DTO).

### 5. Resumen

| API | ¿La app la consume? | ¿Formato compatible? |
|-----|---------------------|----------------------|
| Login | Sí | Sí |
| Scan | Sí | Sí |

La app está integrada con las APIs; para que funcione en desarrollo solo hace falta que la IP en `localUrl` sea la correcta y que el backend esté corriendo en el puerto 5172.

---

## Pendiente para mañana

- [x] Revisar proyecto Flutter en `C:\src\schoolmanager_id_scanner`.
- [x] Verificar configuración de URL base del API en la app.
- [ ] Probar login desde la app contra el backend (IP correcta).
- [ ] Probar escaneo de carné (endpoint `/StudentIdCard/api/scan`).
- [ ] (Opcional) Enviar `userId` real en `scannedBy` y/o hacer la URL base configurable.

---

## Notas

- Backend corre en `http://localhost:5172` (desarrollo). En dispositivo/emulador usar la IP de la máquina o túnel si aplica.
- Este archivo sirve de referencia para continuar el trabajo.
