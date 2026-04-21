# Exporta solo la estructura (DDL) de la base de datos PostgreSQL.
# Uso: desde la raíz del proyecto SchoolManager, o pasando la conexión:
#   .\Scripts\ExportDbSchema.ps1
#   .\Scripts\ExportDbSchema.ps1 -ConnectionString "Host=...;Database=...;Username=...;Password=..."
# Requiere: pg_dump en el PATH (PostgreSQL client tools).

param(
    [string]$ConnectionString = $env:DefaultConnection,
    [string]$OutputFile = "Docs\ESTRUCTURA_BD_SCHEMA.sql"
)

if (-not $ConnectionString) {
    foreach ($configPath in @("appsettings.json", "appsettings.Development.json", "..\appsettings.json", "..\SchoolManager\appsettings.json")) {
        if (Test-Path $configPath) {
            $json = Get-Content $configPath -Raw | ConvertFrom-Json
            $ConnectionString = $json.ConnectionStrings.DefaultConnection
            if ($ConnectionString) { break }
        }
    }
}
if (-not $ConnectionString) {
    Write-Host "No se encontró ConnectionString. Use -ConnectionString o variable DefaultConnection." -ForegroundColor Red
    exit 1
}

# Parsear Host, Database, User, Password, Port para pg_dump
$host = ($ConnectionString -split 'Host=([^;]+)')[1]
$db   = ($ConnectionString -split 'Database=([^;]+)')[1]
$user = ($ConnectionString -split 'Username=([^;]+)')[1]
$pass = ($ConnectionString -split 'Password=([^;]+)')[1]
$port = ($ConnectionString -split 'Port=([^;]+)')[1]
if (-not $port) { $port = "5432" }

$outDir = Split-Path $OutputFile -Parent
if ($outDir -and -not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

$env:PGPASSWORD = $pass
& pg_dump -h $host -p $port -U $user -d $db --schema-only --no-owner --no-privileges -f $OutputFile
Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue

if ($LASTEXITCODE -eq 0) {
    Write-Host "Esquema exportado en: $OutputFile" -ForegroundColor Green
} else {
    Write-Host "Error al ejecutar pg_dump. Compruebe que pg_dump esté en el PATH y la conexión sea correcta." -ForegroundColor Red
    exit 1
}
