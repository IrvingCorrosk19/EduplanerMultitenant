# Despliega Eduplaner MultiTenant en el VPS sin afectar otras apps.
# Puertos: Eduplaner=8087 | RestBar=8084 | FixHub=8081 | Travel=8082 | n8n=8083 | CarnetQR=8080(local)

$ErrorActionPreference = "Stop"
$plink = "C:\Program Files\PuTTY\plink.exe"
$pscp = "C:\Program Files\PuTTY\pscp.exe"
$hostname = "root@164.68.99.83"
$password = $env:RESTBAR_SSH_PASSWORD
if ([string]::IsNullOrWhiteSpace($password)) {
    throw "Defina la variable de entorno RESTBAR_SSH_PASSWORD con la clave SSH del VPS."
}
$hostkey = "ssh-ed25519 SHA256:fXnxiWr5sqazM3xRId7HtcseAZ0XHcJ2BBIuPsLt2J0"
$remoteDir = "/opt/apps/eduplaner"
$localRoot = "C:\Proyectos\EduplanerMultitenant"
$schoolDir = Join-Path $localRoot "SchoolManager"
$staging = Join-Path $env:TEMP "eduplaner_vps_deploy"
$archive = Join-Path $env:TEMP "eduplaner_multitenant_deploy.tgz"

function Invoke-Remote([string]$cmd) {
    & $plink -ssh -pw $password -batch -hostkey $hostkey $hostname $cmd 2>&1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DESPLIEGUE EDUPLANER MULTITENANT - VPS" -ForegroundColor Cyan
Write-Host "  Puerto 8087 | dir $remoteDir" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if (-not (Test-Path $plink)) { throw "PuTTY plink no encontrado: $plink" }
if (-not (Test-Path $pscp)) { throw "PuTTY pscp no encontrado: $pscp" }

Write-Host "`nPASO 0: Compilar Release local..." -ForegroundColor Yellow
Push-Location $schoolDir
try {
    dotnet publish -c Release -o (Join-Path $schoolDir "publish_vps") --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish falló" }
    Write-Host "  Publish OK" -ForegroundColor Green
}
finally { Pop-Location }

Write-Host "`nPASO 1: Empaquetar código (sin bin/obj)..." -ForegroundColor Yellow
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Force -Path (Join-Path $staging "SchoolManager") | Out-Null
Copy-Item (Join-Path $localRoot "docker-compose.vps.yml") (Join-Path $staging "docker-compose.yml") -Force

# Robocopy source excluding heavy/noise dirs
$excludeDirs = @("bin","obj","publish_vps",".e2e_publish","compile_verify_out","_verify_build_*","_pwtemp","Backups","node_modules",".git","qa_bulk_upload","migration_artifacts","analysis_db")
$xd = ($excludeDirs | ForEach-Object { "/XD"; $_ })
& robocopy $schoolDir (Join-Path $staging "SchoolManager") /MIR /NFL /NDL /NJH /NJS /nc /ns /np @xd | Out-Null
# robocopy exit codes 0-7 are success-ish
if ($LASTEXITCODE -ge 8) { throw "robocopy falló con código $LASTEXITCODE" }

if (Test-Path $archive) { Remove-Item $archive -Force }
tar -czf $archive -C $staging .
if (-not (Test-Path $archive)) { throw "No se creó el archivo $archive" }
$sizeMb = [math]::Round((Get-Item $archive).Length / 1MB, 1)
Write-Host "  Archivo: $archive ($sizeMb MB)" -ForegroundColor Green

Write-Host "`nPASO 2: Estado VPS (otras apps)..." -ForegroundColor Yellow
Invoke-Remote "docker ps --format 'table {{.Names}}\t{{.Ports}}' | head -25"

Write-Host "`nPASO 3: Subir paquete..." -ForegroundColor Yellow
Invoke-Remote "mkdir -p $remoteDir /tmp"
& $pscp -pw $password -batch -hostkey $hostkey $archive "${hostname}:/tmp/eduplaner_multitenant_deploy.tgz" 2>&1 | Out-Host

Write-Host "`nPASO 4: Extraer en $remoteDir..." -ForegroundColor Yellow
Invoke-Remote "mkdir -p $remoteDir && tar -xzf /tmp/eduplaner_multitenant_deploy.tgz -C $remoteDir && rm -f /tmp/eduplaner_multitenant_deploy.tgz && ls -la $remoteDir && ls -la $remoteDir/SchoolManager | head -20"

Write-Host "`nPASO 5: Crear .env..." -ForegroundColor Yellow
$envContent = @"
POSTGRES_DB=eduplaner
POSTGRES_USER=eduplaneruser
POSTGRES_PASSWORD=Eduplaner2024!SecureVps

PUBLIC_BASE_URL=http://164.68.99.83:8087
ASPNETCORE_ENVIRONMENT=Production

QR_SECRET_KEY=EduplanerVpsQrSecretKey_Min32Chars_2024!
API_TOKEN_SECRET_KEY=EduplanerVpsApiTokenSecret_Min32Chars_2024!
"@
$envB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($envContent))
Invoke-Remote "cd $remoteDir && echo $envB64 | base64 -d > .env && echo '.env OK' && cat .env | sed 's/PASSWORD=.*/PASSWORD=***/'"

Write-Host "`nPASO 6: Docker compose build+up (puede tardar varios minutos)..." -ForegroundColor Yellow
Invoke-Remote "cd $remoteDir && docker compose up -d --build 2>&1"

Write-Host "`nPASO 6b: Bootstrap BD si falta tabla schools (dump local)..." -ForegroundColor Yellow
$bootstrapLocal = Join-Path $env:TEMP "eduplaner_vps_bootstrap.tgz"
$pgDump = "C:\Program Files\PostgreSQL\18\bin\pg_dump.exe"
if ((Test-Path $pgDump) -and $env:EDUPLANER_BOOTSTRAP_DB -eq "1") {
    $env:PGPASSWORD = "Panama2020$"
    $sqlDump = Join-Path $env:TEMP "eduplaner_vps_bootstrap.sql"
    & $pgDump -h localhost -U postgres -d eduplaner --no-owner --no-acl -f $sqlDump
    tar -czf $bootstrapLocal -C $env:TEMP eduplaner_vps_bootstrap.sql
    & $pscp -pw $password -batch -hostkey $hostkey $bootstrapLocal "${hostname}:/tmp/eduplaner_vps_bootstrap.tgz" 2>&1 | Out-Host
    Invoke-Remote @"
set -e
cd /tmp && tar -xzf eduplaner_vps_bootstrap.tgz
HAS=`$(docker exec eduplaner_postgres psql -U eduplaneruser -d eduplaner -tAc `"SELECT COUNT(*) FROM information_schema.tables WHERE table_name='schools'`" | tr -d ' ')
if [ "`$HAS" = "0" ]; then
  echo 'Aplicando bootstrap SQL...'
  docker stop eduplaner_web || true
  docker exec eduplaner_postgres psql -U eduplaneruser -d eduplaner -c 'DROP SCHEMA public CASCADE; CREATE SCHEMA public; GRANT ALL ON SCHEMA public TO eduplaneruser; GRANT ALL ON SCHEMA public TO public;'
  docker exec -i eduplaner_postgres psql -U eduplaneruser -d eduplaner < /tmp/eduplaner_vps_bootstrap.sql
  docker start eduplaner_web
else
  echo "schools ya existe (HAS=`$HAS), no se toca la BD"
fi
"@
} else {
    Write-Host "  Skip bootstrap (set EDUPLANER_BOOTSTRAP_DB=1 para forzar dump local → VPS)" -ForegroundColor Gray
}

Write-Host "`nPASO 7: Esperar arranque y verificar..." -ForegroundColor Yellow
Start-Sleep -Seconds 12
Invoke-Remote "docker ps --filter name=eduplaner --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'"
Invoke-Remote "docker logs eduplaner_web --tail 40 2>&1"
Invoke-Remote "curl -s -o /dev/null -w 'HTTP %{http_code}\n' http://127.0.0.1:8087/Auth/Login || curl -s -o /dev/null -w 'HTTP %{http_code}\n' http://127.0.0.1:8087/"

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  DESPLIEGUE EDUPLANER COMPLETADO" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "URL: http://164.68.99.83:8087" -ForegroundColor Green
Write-Host "Otras apps NO modificadas (RestBar 8084, FixHub 8081, etc.)" -ForegroundColor Gray
Write-Host "Logs: docker logs -f eduplaner_web" -ForegroundColor Yellow
