# Despliegue en Render — EduplanerIIC

## Resumen

Para que la aplicación quede lista en Render necesitas:

1. **Repositorio conectado** (push a `main` despliega automáticamente si está configurado).
2. **Variable PORT** — Render la define automáticamente; el código ya la usa.
3. **Connection string** de PostgreSQL — configurado como variable de entorno en Render.
4. **Superadmin** — crearlo una vez en la BD de Render tras el primer deploy.

---

## Datos necesarios (Render Dashboard)

| Variable | Dónde obtenerla | Descripción |
|----------|-----------------|-------------|
| `ConnectionStrings__DefaultConnection` | Render → PostgreSQL → Internal URL (o External) | Cadena de conexión a la BD |
| `ASPNETCORE_ENVIRONMENT` | Fijo | `Production` |

La URL de PostgreSQL en Render suele tener esta forma:
```
Host=xxxx.oregon-postgres.render.com;Database=xxx;Username=xxx;Password=xxx;Port=5432;SSL Mode=Require;Trust Server Certificate=true
```

---

## Pasos para dejar la app lista en Render

### 1. Configuración en Render Dashboard

1. Entra a [render.com](https://render.com) → tu servicio **eduplaneriic** (o el nombre que uses).
2. **Environment** → añade:
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `ConnectionStrings__DefaultConnection` = (pega la connection string de tu BD PostgreSQL en Render)
3. **Build & Deploy**:
   - **Build Command:** (vacío si usas Dockerfile; Render lo detecta)
   - **Start Command:** (vacío si usas Dockerfile)
   - **Docker:** Si usas el `Dockerfile`, configura **Dockerfile Path:** `SchoolManager/Dockerfile` y **Root Directory:** `SchoolManager` (o el path correcto respecto a la raíz del repo).

### 2. Si usas despliegue nativo (sin Docker)

Render no ofrece .NET nativo. Usa el `Dockerfile` incluido en `SchoolManager/`.

### 3. Aplicar esquema de BD en Render (si hay cambios nuevos)

Si agregaste tablas o columnas en local, aplica en Render:

```bash
cd SchoolManager
dotnet run -- --apply-render-all
```

(Requiere que `appsettings.json` use la conexión de Render, o usa la variable de entorno.)

### 4. Crear superadmin en la BD de Render

Tras el primer deploy, en el navegador o con curl:

```
GET https://eduplaneriic-22ol.onrender.com/api/auth/create-superadmin
```

O desde tu PC (con appsettings apuntando a Render):

```bash
cd SchoolManager
dotnet run -- --create-initial-superadmin
```

Credenciales: `superadmin@schoolmanager.com` / `Admin123!`

### 5. App Flutter (producción)

En `C:\src\schoolmanager_id_scanner\lib\services\id_card_api.dart`:

- `useLocalhost = false`
- `productionUrl = "https://tu-url.onrender.com"` (o `https://eduplaner.net` si ese es el dominio final)

---

## Data Protection / Antiforgery en contenedores

En Render, las claves de Data Protection no persisten entre reinicios del contenedor. Para evitar fallos en el login:

- El `Login` POST tiene `[IgnoreAntiforgeryToken]` para que funcione sin las claves.
- Si tras un deploy no puedes entrar: borra las cookies de `eduplaner.net` y recarga.
- (Opcional) Para persistir claves: añade un Disco persistente en Render y configura Data Protection para usarlo.

---

## Puerto 8080

Render asigna un puerto dinámico mediante la variable `PORT`. El código ya usa:

```csharp
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
```

No necesitas configurar 8080 manualmente; Render gestiona el puerto.

---

## Checklist rápido

- [ ] Push del código a `main` (o la rama conectada a Render)
- [ ] `ConnectionStrings__DefaultConnection` configurada en Render
- [ ] `ASPNETCORE_ENVIRONMENT=Production` en Render
- [ ] Dockerfile en `SchoolManager/` (si despliegas con Docker)
- [ ] Crear superadmin: `GET /api/auth/create-superadmin` o `dotnet run -- --create-initial-superadmin` contra la BD de Render
- [ ] Probar login en `https://tu-app.onrender.com`

---

## URL del servicio

Si tu servicio es `eduplaneriic-22ol`, la URL pública típica es:

- `https://eduplaneriic-22ol.onrender.com`

El formato `eduplaneriic-22ol:8080` es la referencia interna (host:puerto) dentro de la red de Render.
