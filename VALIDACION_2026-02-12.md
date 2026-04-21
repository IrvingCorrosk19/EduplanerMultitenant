# Validación general — 12 feb 2026

## Resumen

| Componente | Estado | Notas |
|------------|--------|--------|
| **Backend (SchoolManager)** | ✅ Compila | `dotnet build` OK |
| **Tablas BD** | ✅ Creadas/ajustadas al arranque | `student_qr_tokens`, `scan_logs`, `student_id_cards`; `shift_id` en `student_assignments` si falta; `scan_logs.student_id` nullable |
| **API Login** | ✅ Funciona | `POST /api/auth/login` responde; 200 con credenciales válidas, 401 si incorrectas |
| **API Scan** | ✅ Funciona | `POST /StudentIdCard/api/scan` responde 200 con `allowed`, `message`, `studentName`, `grade`, `group` |
| **App Flutter** | ✅ Build OK | `flutter build windows` correcto. URL local: `192.168.0.8:5172` |

---

## 1. Backend

- **Compilación:** correcta.
- **Ejecución:** el backend que estaba en marcha escucha en `http://localhost:5172`.
- **Conexión a BD:** según entorno:
  - Con `ASPNETCORE_ENVIRONMENT=Development` se usa `appsettings.Development.json` → BD local `schoolmanagement`.
  - Sin Development se usa `appsettings.json` → Render (producción).

Para que login y scan funcionen contra tu BD local, arranca así:

```powershell
cd C:\Proyectos\EduplanerIIC\SchoolManager
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run
```

---

## 2. Base de datos local

En la BD **localhost / schoolmanagement** se ejecutó:

- `Scripts/EnsureStudentQrTokensTable.sql` (vía psql).
- Se crearon: `scan_logs`, `student_id_cards`, `student_qr_tokens`.

Además, en el arranque de la app (cuando corre con la BD que no tiene esas tablas), `EnsureIdCardTables.cs` ahora también crea estas tablas si no existen. Tras un cambio de código, hay que **reiniciar el backend** para que ese script se ejecute en el próximo arranque.

---

## 3. APIs

### Login — `POST /api/auth/login`

- **URL:** `http://localhost:5172/api/auth/login`
- **Body:** `{"email":"...","password":"..."}`
- **Respuesta 200:** `token`, `userId`, `email`, `name`, `role`.
- **401:** credenciales inválidas o usuario inexistente en la BD que esté usando el backend.

Credenciales de prueba (según `CREDENCIALES_LOGIN.md` en la app Flutter):  
`superadmin@schoolmanager.com` / `Admin123!`  
Deben existir en la BD a la que apunta el backend (local o Render).

### Scan — `POST /StudentIdCard/api/scan`

- **URL:** `http://localhost:5172/StudentIdCard/api/scan`
- **Body:** `{"token":"...","scannedBy":"...","scanType":"entry"}`
- **Respuesta 200:** `allowed`, `message`, `studentName`, `grade`, `group`.
- **400 con "student_qr_tokens does not exist":** el backend está conectado a una BD donde no se han creado esas tablas (p. ej. Render). Usar BD local con las tablas creadas y, si hace falta, reiniciar con `ASPNETCORE_ENVIRONMENT=Development`.

---

## 4. App Flutter (schoolmanager_id_scanner)

- **Ruta:** `C:\src\schoolmanager_id_scanner`
- **Build Windows:** correcto (`flutter build windows`).
- **URL local en código:** `http://192.168.0.8:5172` (IP actual de la PC).
- **Producción:** `https://eduplaner.net`.

Para probar en local:

1. Backend en marcha con BD local (Development).
2. Misma red WiFi en el celular si pruebas en dispositivo.
3. Ejecutar app: `flutter run` (dispositivo) o `flutter run -d windows`.

---

## 5. Qué hacer para que “todo funcione correctamente”

1. **Backend con BD local**  
   - Asegurar que el backend use la BD donde ya están las tablas (p. ej. `schoolmanagement` en local con `ASPNETCORE_ENVIRONMENT=Development`).  
   - Reiniciar el backend después de cualquier cambio en `EnsureIdCardTables` o en la BD.

2. **Login**  
   - Tener al menos un usuario en esa BD (p. ej. superadmin con la contraseña de `CREDENCIALES_LOGIN.md`).  
   - Probar desde la app o con PowerShell/Postman contra `POST /api/auth/login`.

3. **Scan**  
   - Que el backend esté conectado a la BD que tiene `student_qr_tokens`.  
   - Probar con un token que exista en esa tabla (p. ej. generando un carné desde el panel web) o comprobar que la API responde 200 con `allowed: false` cuando el token no existe.

4. **Flutter**  
   - Build ya validado. Solo falta probar login y escaneo desde la app contra el backend en local (IP `192.168.0.8:5172` o la que corresponda).

---

## Archivos tocados en esta validación

| Archivo | Cambio |
|---------|--------|
| `Scripts/EnsureIdCardTables.cs` | Creación de `student_qr_tokens`, `scan_logs`, `student_id_cards` al arranque si no existen |
| `Scripts/EnsureStudentQrTokensTable.sql` | Script SQL para crear las mismas tablas manualmente (psql) |
| `VALIDACION_2026-02-12.md` | Este informe |
