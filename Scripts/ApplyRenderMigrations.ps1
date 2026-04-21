# Script PowerShell para aplicar migraciones a Render usando el cliente de .NET
# Este script ejecuta los comandos de migración usando dotnet run

Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "   APLICACIÓN DE MIGRACIONES A RENDER" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "📋 Opciones disponibles:" -ForegroundColor Yellow
Write-Host "   1. Probar conexión solamente (--test-render)" -ForegroundColor White
Write-Host "   2. Aplicar todas las migraciones (--apply-render-all)" -ForegroundColor White
Write-Host "   3. Aplicar solo prematriculación (--apply-render-prematriculation)" -ForegroundColor White
Write-Host "   4. Aplicar solo año académico (--apply-render-academic-year)" -ForegroundColor White
Write-Host ""

$option = Read-Host "Selecciona una opción (1-4)"

switch ($option) {
    "1" {
        Write-Host ""
        Write-Host "🔍 Probando conexión a Render..." -ForegroundColor Yellow
        dotnet run -- --test-render
    }
    "2" {
        Write-Host ""
        Write-Host "⚠️  ADVERTENCIA: Esto aplicará TODAS las migraciones a la base de datos de PRODUCCIÓN" -ForegroundColor Red
        $confirm = Read-Host "¿Estás seguro? (escribe 'SI' para confirmar)"
        if ($confirm -eq "SI") {
            Write-Host ""
            Write-Host "🔧 Aplicando todas las migraciones..." -ForegroundColor Yellow
            dotnet run -- --apply-render-all
        } else {
            Write-Host "❌ Operación cancelada" -ForegroundColor Yellow
        }
    }
    "3" {
        Write-Host ""
        Write-Host "⚠️  ADVERTENCIA: Esto aplicará migraciones de PREMATRICULACIÓN a la base de datos de PRODUCCIÓN" -ForegroundColor Red
        $confirm = Read-Host "¿Estás seguro? (escribe 'SI' para confirmar)"
        if ($confirm -eq "SI") {
            Write-Host ""
            Write-Host "🔧 Aplicando migraciones de prematriculación..." -ForegroundColor Yellow
            dotnet run -- --apply-render-prematriculation
        } else {
            Write-Host "❌ Operación cancelada" -ForegroundColor Yellow
        }
    }
    "4" {
        Write-Host ""
        Write-Host "⚠️  ADVERTENCIA: Esto aplicará migraciones de AÑO ACADÉMICO a la base de datos de PRODUCCIÓN" -ForegroundColor Red
        $confirm = Read-Host "¿Estás seguro? (escribe 'SI' para confirmar)"
        if ($confirm -eq "SI") {
            Write-Host ""
            Write-Host "🔧 Aplicando migraciones de año académico..." -ForegroundColor Yellow
            dotnet run -- --apply-render-academic-year
        } else {
            Write-Host "❌ Operación cancelada" -ForegroundColor Yellow
        }
    }
    default {
        Write-Host "❌ Opción inválida" -ForegroundColor Red
    }
}

Write-Host ""

