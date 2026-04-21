# Ejecuta psql usando la cadena de DefaultConnection desde appsettings.
# Orden: appsettings.Development.json (local, no versionado) -> appsettings.json.
# Ejemplo:
#   .\Invoke-PsqlWithAppSettings.ps1 -f .\backfill_activities_school_trimester.sql
#   .\Invoke-PsqlWithAppSettings.ps1 -c "SELECT COUNT(*) FROM activities;"

param(
    [string] $PsqlPath = "C:\Program Files\PostgreSQL\18\bin\psql.exe",
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $PsqlArgs
)

$ErrorActionPreference = "Stop"
# PSScriptRoot = ...\SchoolManager\Scripts
$schoolDir = Split-Path $PSScriptRoot -Parent
$devJson = Join-Path $schoolDir "appsettings.Development.json"
$mainJson = Join-Path $schoolDir "appsettings.json"

$configPath = if (Test-Path $devJson) { $devJson } elseif (Test-Path $mainJson) { $mainJson } else {
    throw "No se encontró appsettings en $schoolDir"
}

$json = Get-Content $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
$cs = $json.ConnectionStrings.DefaultConnection
if ([string]::IsNullOrWhiteSpace($cs)) {
    throw "ConnectionStrings:DefaultConnection está vacío en $configPath. Copia appsettings.Development.template.json a appsettings.Development.json y completa la cadena, o define la variable de entorno ConnectionStrings__DefaultConnection."
}

$map = @{}
foreach ($segment in ($cs -split ';')) {
    $t = $segment.Trim()
    if (-not $t) { continue }
    $eq = $t.IndexOf('=')
    if ($eq -lt 1) { continue }
    $k = $t.Substring(0, $eq).Trim()
    $v = $t.Substring($eq + 1).Trim()
    $map[$k] = $v
}

$hostName = $map['Host']
$db = $map['Database']
$user = $map['Username']
$pass = $map['Password']
$port = if ($map['Port']) { $map['Port'] } else { '5432' }

if (-not $hostName -or -not $db -or -not $user) {
    throw "La cadena de conexión debe incluir al menos Host, Database y Username."
}

$ssl = $map['SSL Mode']
if ($ssl -match 'Require|require') {
    $env:PGSSLMODE = 'require'
}

if ($pass) {
    $env:PGPASSWORD = $pass
}

if (-not (Test-Path $PsqlPath)) {
    throw "No existe psql en: $PsqlPath. Ajusta -PsqlPath o instala PostgreSQL 18."
}

& $PsqlPath -h $hostName -p $port -U $user -d $db @PsqlArgs
exit $LASTEXITCODE
