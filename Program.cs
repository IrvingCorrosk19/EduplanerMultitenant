using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using SchoolManager.Mappings;
using SchoolManager.Models;
using AutoMapper;
using SchoolManager.Services.Implementations;
using SchoolManager.Options;
using SchoolManager.Services.Interfaces;
using SchoolManager.Application.Interfaces;
using SchoolManager.Infrastructure.Services;
using SchoolManager.Services;
using SchoolManager.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using BCrypt.Net;
using SchoolManager.Middleware;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using SchoolManager.Repositories.Implementations;
using SchoolManager.Repositories.Interfaces;
using SchoolManager.Services.Background;
using SchoolManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Render / docs de Cloudinary suelen usar CLOUDINARY_CLOUD_NAME, CLOUDINARY_API_KEY, CLOUDINARY_API_SECRET.
// Tambi�n vale Cloudinary__CloudName en el entorno. Hay que sobrescribir placeholders de appsettings (TU_? / ?AQUI?).
static void ApplyCloudinaryEnvironmentAliases(ConfigurationManager config)
{
    static bool IsPlaceholderValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var t = value.Trim();
        return t.StartsWith("TU_", StringComparison.OrdinalIgnoreCase)
            || t.Contains("AQUI", StringComparison.OrdinalIgnoreCase);
    }

    void MapFromEnv(string configKey, string envKey)
    {
        var fromEnv = Environment.GetEnvironmentVariable(envKey);
        if (string.IsNullOrWhiteSpace(fromEnv)) return;
        var current = config[configKey];
        if (IsPlaceholderValue(current))
            config[configKey] = fromEnv.Trim();
    }

    MapFromEnv("Cloudinary:CloudName", "CLOUDINARY_CLOUD_NAME");
    MapFromEnv("Cloudinary:ApiKey", "CLOUDINARY_API_KEY");
    MapFromEnv("Cloudinary:ApiSecret", "CLOUDINARY_API_SECRET");
    // Mismo criterio si usan nombres .NET en Render (Cloudinary__CloudName, etc.)
    MapFromEnv("Cloudinary:CloudName", "Cloudinary__CloudName");
    MapFromEnv("Cloudinary:ApiKey", "Cloudinary__ApiKey");
    MapFromEnv("Cloudinary:ApiSecret", "Cloudinary__ApiSecret");
}

ApplyCloudinaryEnvironmentAliases(builder.Configuration);

// CRIT-02: Sobrescribir claves secretas con variables de entorno si est�n definidas.
// En producci�n NUNCA deben usarse los valores hardcodeados de appsettings.json.
// Configurar: QrSecurity__SecretKey y ApiToken__SecretKey en Render / Docker / etc.
static void ApplySecretKeyEnvironmentOverrides(ConfigurationManager config)
{
    static bool IsPlaceholder(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var t = value.Trim();
        return t.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
            || t.StartsWith("REEMPLAZAR", StringComparison.OrdinalIgnoreCase)
            || t.Contains("EduPlaner-", StringComparison.OrdinalIgnoreCase); // detectar valor dev hardcodeado
    }

    void MapSecret(string configKey, params string[] envKeys)
    {
        foreach (var envKey in envKeys)
        {
            var fromEnv = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                config[configKey] = fromEnv.Trim();
                return;
            }
        }
        // Si el valor actual sigue siendo placeholder y estamos en producci�n, advertir en stderr
        if (IsPlaceholder(config[configKey]))
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (!env.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"[SEGURIDAD] La clave '{configKey}' tiene un valor placeholder inseguro. " +
                    $"Defina una de las siguientes variables de entorno: {string.Join(", ", envKeys)}");
            }
        }
    }

    MapSecret("QrSecurity:SecretKey", "QrSecurity__SecretKey", "QR_SECRET_KEY");
    MapSecret("ApiToken:SecretKey",   "ApiToken__SecretKey",   "API_TOKEN_SECRET_KEY");
    MapSecret("StudentIdCard:PublicBaseUrl", "StudentIdCard__PublicBaseUrl", "PUBLIC_BASE_URL", "RENDER_EXTERNAL_URL");
}

ApplySecretKeyEnvironmentOverrides(builder.Configuration);

// Render: usar PORT si est� definido (producci�n en Render.com)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Tabla email_queues (cola de env�o de contrase�as por correo). Idempotente.
if (args.Length > 0 && args[0] == "--apply-email-queues-table")
{
    var connStr = PostgresConnectionResolver.Resolve(builder.Configuration);
    if (string.IsNullOrEmpty(connStr)) { Console.WriteLine("Falta conexi�n: DefaultConnection, ConnectionStrings__DefaultConnection o DATABASE_URL."); Environment.Exit(1); return; }
    var opts = new DbContextOptionsBuilder<SchoolDbContext>().UseNpgsql(connStr).Options;
    using var ctx = new SchoolDbContext(opts);
    await SchoolManager.Scripts.ApplyEmailQueuesTable.RunAsync(ctx);
    return;
}

// Aplicar columna schools.is_active sin arrancar la app (evita usar Schools antes de que exista la columna)
if (args.Length > 0 && args[0] == "--apply-email-jobs")
{
    var connStr = PostgresConnectionResolver.Resolve(builder.Configuration);
    if (string.IsNullOrEmpty(connStr)) { Console.WriteLine("Falta conexi�n: DefaultConnection, ConnectionStrings__DefaultConnection o DATABASE_URL."); Environment.Exit(1); return; }
    var opts = new DbContextOptionsBuilder<SchoolDbContext>().UseNpgsql(connStr).Options;
    using var ctx = new SchoolDbContext(opts);
    await SchoolManager.Scripts.ApplyEmailJobsAndQueueColumns.RunAsync(ctx);
    Console.WriteLine("? email_jobs y columnas de email_queues aplicados. Saliendo...");
    return;
}

if (args.Length > 0 && args[0] == "--apply-school-is-active")
{
    var connStr = PostgresConnectionResolver.Resolve(builder.Configuration);
    if (string.IsNullOrEmpty(connStr)) { Console.WriteLine("Falta conexi�n: DefaultConnection, ConnectionStrings__DefaultConnection o DATABASE_URL."); Environment.Exit(1); return; }
    var opts = new DbContextOptionsBuilder<SchoolDbContext>().UseNpgsql(connStr).Options;
    using var ctx = new SchoolDbContext(opts);
    await SchoolManager.Scripts.ApplySchoolIsActive.RunAsync(ctx);
    Console.WriteLine("? Columna schools.is_active aplicada y migraci�n registrada. Saliendo...");
    return;
}

// Crear tablas Plan de Trabajo Trimestral (teacher_work_plans, teacher_work_plan_details)
if (args.Length > 0 && args[0] == "--apply-teacher-work-plan-tables")
{
    var connStr = PostgresConnectionResolver.Resolve(builder.Configuration);
    if (string.IsNullOrEmpty(connStr)) { Console.WriteLine("Falta conexi�n: DefaultConnection, ConnectionStrings__DefaultConnection o DATABASE_URL."); Environment.Exit(1); return; }
    var opts = new DbContextOptionsBuilder<SchoolDbContext>().UseNpgsql(connStr).Options;
    using var ctx = new SchoolDbContext(opts);
    await SchoolManager.Scripts.ApplyTeacherWorkPlanTables.RunAsync(ctx);
    return;
}

// Columnas gobernanza + tabla teacher_work_plan_review_logs (Direcci�n Acad�mica)
if (args.Length > 0 && args[0] == "--apply-director-work-plan-governance")
{
    var connStr = PostgresConnectionResolver.Resolve(builder.Configuration);
    if (string.IsNullOrEmpty(connStr)) { Console.WriteLine("Falta conexi�n: DefaultConnection, ConnectionStrings__DefaultConnection o DATABASE_URL."); Environment.Exit(1); return; }
    var opts = new DbContextOptionsBuilder<SchoolDbContext>().UseNpgsql(connStr).Options;
    using var ctx = new SchoolDbContext(opts);
    await SchoolManager.Scripts.ApplyDirectorWorkPlanGovernance.RunAsync(ctx);
    return;
}

// Crear superadmin inicial (superadmin@schoolmanager.com / Admin123!). Usa la conexi�n configurada.
if (args.Length > 0 && args[0] == "--create-initial-superadmin")
{
    var connStr = PostgresConnectionResolver.Resolve(builder.Configuration);
    if (string.IsNullOrEmpty(connStr)) { Console.WriteLine("Falta conexi�n: DefaultConnection, ConnectionStrings__DefaultConnection o DATABASE_URL."); Environment.Exit(1); return; }
    var opts = new DbContextOptionsBuilder<SchoolDbContext>().UseNpgsql(connStr).Options;
    using var ctx = new SchoolDbContext(opts);
    await SchoolManager.Scripts.CreateInitialSuperAdminScript.RunAsync(ctx);
    return;
}

// Crear admin local (admin@local.com / Admin123!). Crea escuela si no existe.
if (args.Length > 0 && args[0] == "--create-local-admin")
{
    var connStr = PostgresConnectionResolver.Resolve(builder.Configuration);
    if (string.IsNullOrEmpty(connStr)) { Console.WriteLine("Falta conexi�n: DefaultConnection, ConnectionStrings__DefaultConnection o DATABASE_URL."); Environment.Exit(1); return; }
    var opts = new DbContextOptionsBuilder<SchoolDbContext>().UseNpgsql(connStr).Options;
    using var ctx = new SchoolDbContext(opts);
    await SchoolManager.Scripts.CreateLocalAdminScript.RunAsync(ctx);
    return;
}

// Crear tabla student_payment_access en Render (m�dulo Club de Padres). No arranca la app.
if (args.Length > 0 && args[0] == "--apply-render-student-payment-access")
{
    await SchoolManager.Scripts.ApplyRenderStudentPaymentAccess.RunAsync();
    return;
}

// Homologar BD LOCAL con Render. Solo para desarrollo local.
if (args.Length > 0 && args[0] == "--homologate-local")
{
    Console.WriteLine("?????????????????????????????????????????????????");
    Console.WriteLine("   COMANDO --homologate-local DESACTIVADO");
    Console.WriteLine("?????????????????????????????????????????????????\n");
    return;
}

// Cultura oficial del sistema (est�ndar corporativo de fechas)
var culture = new CultureInfo("es-PA");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// Add services to the container (una sola cadena: vistas + JSON camelCase para Ok()/fetch)
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableDateTimeJsonConverter());
    })
    .AddMvcOptions(options =>
    {
        // Requiere autenticaci�n por defecto; use [AllowAnonymous] en login, APIs p�blicas y enlaces firmados.
        options.Filters.Add(new AuthorizeFilter(
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
        options.Filters.Add<SchoolManager.Attributes.DateTimeConversionAttribute>();
        options.Filters.Add<SchoolManager.Filters.PlatformAccessGuardFilter>();
    });

// Configurar Antiforgery para aceptar el token desde header (usado por fetch en Schedule y otros m�dulos AJAX)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// Conexi�n a la base de datos PostgreSQL (appsettings, ConnectionStrings__DefaultConnection o DATABASE_URL en Render)
var npgsqlConnectionString = PostgresConnectionResolver.Resolve(builder.Configuration)
    ?? throw new InvalidOperationException(
        "Falta cadena de base de datos. Configure ConnectionStrings:DefaultConnection, la variable de entorno ConnectionStrings__DefaultConnection o DATABASE_URL (Render PostgreSQL).");
builder.Services.AddDbContext<SchoolDbContext>(options =>
{
    options.UseNpgsql(npgsqlConnectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
        npgsql.CommandTimeout(60);
    });

    // Configurar Entity Framework para manejar DateTime autom�ticamente
    options.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.RowLimitingOperationWithoutOrderByWarning));
});

// Proveedor de tenant (school_id) ? lee del claim, sin hit de BD
builder.Services.AddScoped<SchoolManager.Infrastructure.ITenantProvider, SchoolManager.Infrastructure.TenantProvider>();

// Data Protection Keys ? almacenamiento persistente en PostgreSQL
// Contexto separado del SchoolDbContext para evitar conflictos con TenantProvider y GQF.
// Resuelve: [IgnoreAntiforgeryToken] en Login era workaround de este problema.
builder.Services.AddDbContext<DataProtectionKeyDbContext>(options =>
    options.UseNpgsql(npgsqlConnectionString));

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<DataProtectionKeyDbContext>()
    .SetApplicationName("EduPlaner");

// Registrando todos los servicios con inyecci�n de dependencias
builder.Services.AddScoped<ISchoolService, SchoolService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<ITeacherAssignmentService, TeacherAssignmentService>();
builder.Services.AddScoped<ITrimesterService, TrimesterService>();
builder.Services.AddScoped<IActivityTypeService, ActivityTypeService>();
builder.Services.AddScoped<ITeacherGroupService, TeacherGroupService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IStudentActivityScoreService, StudentActivityScoreService>();

builder.Services.AddSingleton<IFileStorage, LocalFileStorage>(); // o tu propio servicio

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddSingleton<IDocumentStorageService, DocumentStorageService>();

builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IDisciplineReportService, DisciplineReportService>();
builder.Services.AddScoped<IOrientationReportService, OrientationReportService>();
builder.Services.AddScoped<ISecuritySettingService, SecuritySettingService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

builder.Services.AddAutoMapper(_ => { }, typeof(AutoMapperProfile).Assembly);
builder.Services.AddScoped<IStudentReportService, StudentReportService>();
builder.Services.AddScoped<IGradeLevelService, GradeLevelService>();
builder.Services.AddScoped<IAcademicAssignmentService, AcademicAssignmentService>();
builder.Services.AddScoped<IStudentAssignmentService, StudentAssignmentService>();
builder.Services.AddScoped<IAreaService, AreaService>();
builder.Services.AddScoped<ISpecialtyService, SpecialtyService>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<ISubjectAssignmentService, SubjectAssignmentService>();
builder.Services.AddScoped<IDirectorService, DirectorService>();
builder.Services.AddScoped<ISuperAdminService, SuperAdminService>();
builder.Services.AddScoped<IDateTimeHomologationService, DateTimeHomologationService>();
builder.Services.AddScoped<IEmailConfigurationService, EmailConfigurationService>();
builder.Services.AddScoped<IEmailApiConfigurationService, EmailApiConfigurationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICounselorAssignmentService, CounselorAssignmentService>();
builder.Services.AddScoped<IStudentProfileService, StudentProfileService>();
builder.Services.AddScoped<IMessagingService, MessagingService>();
builder.Services.AddScoped<IAprobadosReprobadosService, AprobadosReprobadosService>();
builder.Services.AddScoped<IReportesInstitucionalesService, ReportesInstitucionalesService>();
builder.Services.AddScoped<IInformeInstitucionalHtmlPdfService, InformeInstitucionalHtmlPdfService>();
builder.Services.AddScoped<IInformeInstitucionalRazorRenderService, InformeInstitucionalRazorRenderService>();
builder.Services.AddScoped<IPrematriculationPeriodService, PrematriculationPeriodService>();
builder.Services.AddScoped<IPrematriculationService, PrematriculationService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentConceptService, PaymentConceptService>();
builder.Services.AddScoped<IAcademicYearService, AcademicYearService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IScheduleConfigurationService, ScheduleConfigurationService>();
builder.Services.Configure<SchoolManager.Services.Security.QrSecurityOptions>(
    builder.Configuration.GetSection(SchoolManager.Services.Security.QrSecurityOptions.SectionName));
builder.Services.Configure<StudentIdCardPdfPrintOptions>(
    builder.Configuration.GetSection(StudentIdCardPdfPrintOptions.SectionName));
builder.Services.Configure<StudentIdCardOptions>(
    builder.Configuration.GetSection(StudentIdCardOptions.SectionName));
builder.Services.AddSingleton<SchoolManager.Services.Security.IQrSignatureService, SchoolManager.Services.Security.QrSignatureService>();

// SEG-2: Rate limiting para el endpoint p�blico de escaneo QR.
// L�mite por IP: 60 peticiones/minuto en ventana fija.
// Previene brute force de tokens, enumeraci�n masiva y DoS de scan_logs.
builder.Services.AddRateLimiter(options =>
{
    // Escaneo QR: 60 req/min por IP
    options.AddFixedWindowLimiter("ScanApiPolicy", limiter =>
    {
        limiter.PermitLimit = 60;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });

    // Login web: 10 intentos/min por IP ? previene fuerza bruta
    options.AddFixedWindowLimiter("LoginPolicy", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });

    // Login API m�vil: 20 intentos/min por IP
    options.AddFixedWindowLimiter("ApiLoginPolicy", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
builder.Services.AddScoped<IStudentIdCardService, StudentIdCardService>();
builder.Services.AddScoped<IStudentIdCardImageService, StudentIdCardImageService>();
builder.Services.AddScoped<IStudentIdCardPdfService, StudentIdCardPdfService>();
builder.Services.AddScoped<IStudentIdCardHtmlCaptureService, StudentIdCardHtmlCaptureService>();
builder.Services.Configure<InstitutionalCredentialOptions>(
    builder.Configuration.GetSection(InstitutionalCredentialOptions.SectionName));
builder.Services.AddScoped<IStaffInstitutionalProfileService, StaffInstitutionalProfileService>();
builder.Services.AddScoped<IInstitutionalCredentialService, InstitutionalCredentialService>();
builder.Services.AddScoped<IInstitutionalCredentialPdfService, InstitutionalCredentialPdfService>();
builder.Services.AddScoped<IInstitutionalCredentialImageService, InstitutionalCredentialImageService>();
builder.Services.AddScoped<IInstitutionalCredentialHtmlCaptureService, InstitutionalCredentialHtmlCaptureService>();
builder.Services.AddScoped<ITeacherGradebookPdfService, TeacherGradebookPdfService>();
builder.Services.AddScoped<ITeacherWorkPlanService, TeacherWorkPlanService>();
builder.Services.AddScoped<ITeacherWorkPlanPdfService, TeacherWorkPlanPdfService>();
builder.Services.AddScoped<IDirectorWorkPlanService, DirectorWorkPlanService>();
builder.Services.AddScoped<IUserPasswordManagementService, UserPasswordManagementService>();
builder.Services.AddScoped<IBulkPasswordEmailService, BulkPasswordEmailService>();
builder.Services.AddScoped<IEmailQueueRepository, EmailQueueRepository>();
builder.Services.AddScoped<IEmailQueueService, EmailQueueService>();
builder.Services.AddScoped<IEmailSender, ResendEmailSender>();
builder.Services.AddScoped<IEmailJobService, EmailJobService>();
builder.Services.AddHostedService<EmailQueueWorker>();
// M�dulo Club de Padres (pagos carnet y plataforma)
builder.Services.AddScoped<IClubParentsPaymentService, ClubParentsPaymentService>();
builder.Services.AddScoped<IQlServicesCarnetService, QlServicesCarnetService>();
builder.Services.AddScoped<IPlatformAccessGuardService, PlatformAccessGuardService>();
builder.Services.AddScoped<SchoolManager.Filters.PlatformAccessGuardFilter>();

// HttpClient (p. ej. descarga de fotos en Cloudinary para PDFs)
builder.Services.AddHttpClient();

builder.Services.Configure<UserPhotoCacheOptions>(
    builder.Configuration.GetSection(UserPhotoCacheOptions.SectionName));
var userPhotoCacheBootstrap = builder.Configuration.GetSection(UserPhotoCacheOptions.SectionName)
    .Get<UserPhotoCacheOptions>() ?? new UserPhotoCacheOptions();
builder.Services.AddMemoryCache(o =>
{
    o.SizeLimit = Math.Clamp(userPhotoCacheBootstrap.MemoryCacheSizeLimitBytes, 16 * 1024 * 1024, 512 * 1024 * 1024);
});
builder.Services.AddSingleton<IHttpBytesDownloadCache, HttpBytesDownloadCache>();

// Cloudinary: credenciales reales en producci�n (variables de entorno / Render) para que las fotos sobrevivan al deploy
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// Fotos de usuario: solo Cloudinary (LocalFileStorageService; sin copia en disco al subir).
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IUserPhotoService, UserPhotoService>();

// Agregar servicios de autenticaci�n
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        // MED-03: 4h en lugar de 24h ? reduce ventana de sesi�n secuestrada
        options.ExpireTimeSpan = TimeSpan.FromHours(4);
        options.SlidingExpiration = true;
    });

// Agregar configuraci�n de autorizaci�n
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Teacher", policy => policy.RequireRole("Teacher"));
    options.AddPolicy("Student", policy => policy.RequireRole("Student"));
    options.AddPolicy("Parent", policy => policy.RequireRole("Parent", "Acudiente"));
    options.AddPolicy("Accounting", policy => policy.RequireRole("Contabilidad", "Admin", "SuperAdmin"));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<ITimeZoneService, TimeZoneService>();

var app = builder.Build();

// Asegurar que existan las tablas del m�dulo de carnets (por si la migraci�n no se aplic�)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var cloudinary = scope.ServiceProvider.GetRequiredService<ICloudinaryService>();
    if (!cloudinary.IsConfigured)
    {
        // No detenemos el arranque; sin credenciales v�lidas SaveUserPhotoAsync fallar� (solo Cloudinary).
        logger.LogCritical(
            "Cloudinary no est� configurado o las credenciales son placeholders. " +
            "Defina CLOUDINARY_CLOUD_NAME, CLOUDINARY_API_KEY y CLOUDINARY_API_SECRET " +
            "(o Cloudinary__CloudName, Cloudinary__ApiKey, Cloudinary__ApiSecret). " +
            "Sin eso, la subida de fotos de usuario fallar�.");
    }

    // Crear tabla data_protection_keys si no existe (idempotente)
    // Permite que las cookies y antiforgery tokens sobrevivan reinicios de contenedor en Render.
    // Resuelve IMP-01: elimina la necesidad de [IgnoreAntiforgeryToken] en AuthController.Login
    var dpContext = scope.ServiceProvider.GetRequiredService<DataProtectionKeyDbContext>();
    await dpContext.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS data_protection_keys (
            id SERIAL PRIMARY KEY,
            friendly_name TEXT NULL,
            xml TEXT NULL
        );");

    var db = scope.ServiceProvider.GetRequiredService<SchoolDbContext>();
    try
    {
        await db.Database.ExecuteSqlRawAsync(@"CREATE EXTENSION IF NOT EXISTS ""pgcrypto""; CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "No se pudieron crear extensiones pgcrypto/uuid-ossp (puede requerir privilegios).");
    }
    try
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("EF Core migrations aplicadas (Database.MigrateAsync).");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error aplicando migraciones EF. La app continuar� con Ensure* scripts.");
    }
    await SchoolManager.Scripts.EnsureIdCardTables.EnsureAsync(db);
    await SchoolManager.Scripts.EnsureUsersRoleCheck.EnsureAsync(db);
    await SchoolManager.Scripts.EnsureStudentPaymentAccessTable.EnsureAsync(db);
    await SchoolManager.Scripts.EnsureScheduleTables.EnsureAsync(db);
    await SchoolManager.Scripts.EnsureSchoolScheduleConfigurationTable.EnsureAsync(db);
    try
    {
        await SchoolManager.Scripts.EnsureLoginEmailIndex.EnsureAsync(db);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "EnsureLoginEmailIndex fall� (�ndice puede requerir privilegios).");
    }
    try
    {
        await SchoolManager.Scripts.ApplyEmailJobsAndQueueColumns.RunAsync(db);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "ApplyEmailJobsAndQueueColumns fall? (tabla puede no existir a?n).");
    }
    await SchoolManager.Scripts.VerifyAcademicYearsInDb.RunAsync(db, logger);

    // Garantizar que cada escuela tenga al menos un a�o acad�mico (evitar mensaje "No hay a�os acad�micos configurados")
    // Si el esquema base a�n no existe (BD vac�a / MigrateAsync fall�), no tumbar el arranque.
    try
    {
        var academicYearService = scope.ServiceProvider.GetRequiredService<IAcademicYearService>();
        var schools = await db.Schools.Select(s => s.Id).ToListAsync();
        foreach (var schoolId in schools)
        {
            try
            {
                await academicYearService.EnsureDefaultAcademicYearForSchoolAsync(schoolId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo asegurar a�o acad�mico para la escuela {SchoolId}.", schoolId);
            }
        }

        // Garantizar que cada escuela tenga bloques horarios por defecto (8 bloques de 35 min desde 07:00) si no tiene ninguno
        try
        {
            foreach (var schoolId in schools)
            {
                await SchoolManager.Scripts.EnsureDefaultTimeSlots.EnsureForSchoolAsync(db, schoolId);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo asegurar bloques horarios por defecto (tabla time_slots puede no existir a�n).");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "No se pudo consultar schools al arrancar (esquema incompleto). La app arranca igual; aplique el bootstrap SQL del VPS.");
    }
}

// Script temporal para aplicar cambios a la base de datos
// Ejecutar con: 
//   --apply-db-changes: Aplica cambios locales
//   --apply-school-is-active: A�ade columna schools.is_active (Soft Delete) y registra migraci�n
//   --apply-academic-year: Aplica cambios de a�o acad�mico locales
//   --test-render: Prueba conexi�n a Render
//   --apply-render-all: Aplica todas las migraciones a Render
//   --apply-render-prematriculation: Aplica solo prematriculaci�n a Render
//   --apply-render-academic-year: Aplica solo a�o acad�mico a Render
if (args.Length > 0)
{
    if (args[0] == "--test-render")
    {
        await SchoolManager.Scripts.TestRenderConnection.RunAsync();
        return;
    }
    else if (args[0] == "--apply-render-all")
    {
        await SchoolManager.Scripts.ApplyRenderMigrations.ApplyAllMigrationsAsync();
        return;
    }
    else if (args[0] == "--apply-render-prematriculation")
    {
        await SchoolManager.Scripts.ApplyRenderMigrations.ApplyPrematriculationOnlyAsync();
        return;
    }
    else if (args[0] == "--apply-render-academic-year")
    {
        await SchoolManager.Scripts.ApplyRenderMigrations.ApplyAcademicYearOnlyAsync();
        return;
    }
    else if (args[0] == "--compare-db-schemas")
    {
        await SchoolManager.Scripts.CompareDbSchemas.RunAsync();
        return;
    }
    else if (args[0] == "--sync-ef-migrations-history")
    {
        var connStr = PostgresConnectionResolver.Resolve(builder.Configuration);
        if (string.IsNullOrEmpty(connStr)) { Console.WriteLine("Falta conexi�n: DefaultConnection, ConnectionStrings__DefaultConnection o DATABASE_URL."); return; }
        var label = builder.Environment.IsDevelopment() ? "LOCAL" : "RENDER";
        Console.WriteLine($"Sincronizando __EFMigrationsHistory en {label}...\n");
        await SchoolManager.Scripts.SyncEfMigrationsHistory.RunAsync(connStr, label);
        Console.WriteLine("\n? Listo. Comprueba con: dotnet ef migrations list");
        return;
    }
    else if (args[0] == "--sync-ef-migrations-both")
    {
        Console.WriteLine("???????????????????????????????????????????????");
        Console.WriteLine("   COMANDO --sync-ef-migrations-both DESACTIVADO");
        Console.WriteLine("???????????????????????????????????????????????\n");
        return;
    }
    else if (args[0] == "--list-local-tables")
    {
        Console.WriteLine("???????????????????????????????????????????????");
        Console.WriteLine("   COMANDO --list-local-tables DESACTIVADO");
        Console.WriteLine("?????????????????????????????????????????????????\n");
        return;
    }
    else if (args[0] == "--add-render-indexes")
    {
        await SchoolManager.Scripts.AddRenderIndexes.RunAsync();
        return;
    }
    // Comandos locales (usando la conexi�n del appsettings.json)
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<SchoolDbContext>();
    
    if (args[0] == "--apply-db-changes")
    {
        await SchoolManager.Scripts.ApplyDatabaseChanges.ApplyPrematriculationChangesAsync(context);
        Console.WriteLine("? Cambios de prematriculaci�n aplicados. Saliendo...");
        return;
    }
    else if (args[0] == "--apply-academic-year")
    {
        await SchoolManager.Scripts.ApplyAcademicYearChanges.ApplyAsync(context);
        Console.WriteLine("? Cambios de a�o acad�mico aplicados. Saliendo...");
        return;
    }
    else if (args[0] == "--check-users")
    {
        await SchoolManager.Scripts.CheckUsers.RunAsync(context);
        return;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS: fuerza HTTPS por 1 a�o, incluye subdominios
    app.UseHsts();
}

// Headers de seguridad HTTP (MEN-01)
// Protegen contra clickjacking, MIME sniffing, XSS y fugas de referrer
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;

    // Evita que la app sea embebida en iframes (clickjacking)
    headers["X-Frame-Options"] = "SAMEORIGIN";

    // Evita que el navegador adivine el Content-Type (MIME sniffing)
    headers["X-Content-Type-Options"] = "nosniff";

    // No enviar el referrer a sitios externos
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    // XSS: deshabilita el filtro XSS legado (parad�jicamente m�s seguro con CSP)
    headers["X-XSS-Protection"] = "0";

    // Permissions Policy: deshabilitar APIs de hardware no usadas
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

    // Content-Security-Policy: permite recursos propios + CDNs usados en la app
    // MED-01: 'unsafe-eval' es requerido por SheetJS (xlsx) y algunas librer�as legacy.
    // Para eliminarlo: migrar xlsx a versi�n ESM sin eval, o usar un worker isolado.
    // 'unsafe-inline' se puede reemplazar con nonces cuando se adopte Razor TagHelper de nonce.
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' " +
            "https://cdn.jsdelivr.net https://cdnjs.cloudflare.com " +
            "https://cdn.datatables.net https://code.jquery.com; " +
        "style-src 'self' 'unsafe-inline' " +
            "https://cdn.jsdelivr.net https://cdnjs.cloudflare.com " +
            "https://cdn.datatables.net https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
        "img-src 'self' data: blob: https://res.cloudinary.com https://secure.gravatar.com; " +
        "connect-src 'self'; " +
        "frame-ancestors 'self'; " +
        "form-action 'self';";

    await next();
});

app.UseStaticFiles();

app.UseRouting();

// SEG-2: Rate limiter para endpoints con [EnableRateLimiting] (ej: /api/scan)
app.UseRateLimiter();

// Agregar middleware global para DateTime
app.UseMiddleware<DateTimeMiddleware>();

app.UseAuthentication();
app.UseMiddleware<SchoolManager.Middleware.ApiBearerTokenMiddleware>();
app.UseAuthorization();

// Usar el m�todo de extensi�n para el middleware
// app.UseSessionValidation();

app.MapControllers(); // Rutas por atributos (ej. StudentIdCard/ui)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
