# Genera la carpeta lista para subir a SmarterASP (deploy/).
# Uso:  powershell -ExecutionPolicy Bypass -File .\publicar.ps1
$ErrorActionPreference = 'Stop'
$raiz = $PSScriptRoot
$wwwroot = Join-Path $raiz 'backend\AgendaSaaS\wwwroot'
$salida = Join-Path $raiz 'deploy'

Write-Host '1/4 Tests del backend...'
dotnet test (Join-Path $raiz 'backend\AgendaSaaS.slnx')
if ($LASTEXITCODE -ne 0) { throw 'Fallaron los tests: no se publica.' }

Write-Host '2/4 Build del frontend...'
Push-Location (Join-Path $raiz 'frontend')
npm ci
npm run build
if ($LASTEXITCODE -ne 0) { Pop-Location; throw 'Falló el build del frontend.' }
Pop-Location

Write-Host '3/4 Copiando el frontend a wwwroot...'
if (Test-Path $wwwroot) { Remove-Item -Recurse -Force $wwwroot }
Copy-Item -Recurse (Join-Path $raiz 'frontend\dist') $wwwroot

Write-Host '4/4 Publicando el backend...'
if (Test-Path $salida) { Remove-Item -Recurse -Force $salida }
dotnet publish (Join-Path $raiz 'backend\AgendaSaaS\AgendaSaaS.csproj') -c Release -o $salida
if ($LASTEXITCODE -ne 0) { throw 'Falló el publish.' }

# Nunca subir la config de desarrollo.
Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $salida 'appsettings.Development.json')

Write-Host ''
Write-Host "Listo: subí el contenido de $salida a la carpeta del sitio en SmarterASP."
Write-Host 'La primera vez, creá también appsettings.Production.json en el servidor (ver DEPLOY.md).'
