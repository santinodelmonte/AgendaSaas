# Deploy en SmarterASP.NET

Un solo sitio: el backend ASP.NET Core sirve la API (`/api/...`) y el frontend
(React compilado en `wwwroot`). Base de datos: MySQL de SmarterASP.

## Primera vez (en el panel de SmarterASP)

1. **Sitio**: crear el sitio y verificar que esté en **.NET Core / ASP.NET Core 8**
   (Website → ASP.NET version).
2. **MySQL**: Databases → MySQL → crear base. Anotar servidor, nombre, usuario y
   contraseña. Las tablas se crean solas al primer arranque.
3. **Dominio + SSL**: apuntar el dominio y activar el certificado SSL gratuito
   (Let's Encrypt) desde el panel.
4. **Configuración de producción**: crear en la carpeta del sitio (por FTP o el
   File Manager) un `appsettings.Production.json`. **No va al repo** (tiene secretos):

```json
{
  "ConnectionStrings": {
    "Default": "Server=MYSQLxxxx.site4now.net;Database=db_xxx_agenda;Uid=xxx_agenda;Pwd=CONTRASEÑA;Charset=utf8mb4;"
  },
  "Jwt": {
    "Key": "UNA-CLAVE-ALEATORIA-DE-64-CARACTERES-O-MAS"
  },
  "Cors": {
    "Origins": "https://tudominio.com"
  },
  "SuperAdmin": {
    "Email": "tu-email@ejemplo.com",
    "Password": "UNA-CONTRASEÑA-LARGA-Y-UNICA"
  }
}
```

   Para generar la clave JWT: `[Convert]::ToBase64String((1..48 | % { Get-Random -Max 256 }))`
   en PowerShell.

5. **Entorno**: el `web.config` que genera el publish ya corre en `Production`.

## Cada deploy

```powershell
powershell -ExecutionPolicy Bypass -File .\publicar.ps1
```

Corre los tests, compila el front, lo copia a `wwwroot` y deja todo en `deploy/`.
Subir el contenido de `deploy/` por FTP o Web Deploy, **sin pisar**
`appsettings.Production.json` en el servidor.

Si el sitio queda tomado al subir (DLL bloqueada), detenerlo desde el panel o subir
primero un `app_offline.htm`, y borrarlo al terminar.

## Después de subir

- `https://tudominio.com/health` → `{"status":"ok"}` (API y base conectadas).
- Entrar a `/login` con el superadmin y crear las manicuristas desde `/superadmin`.
- Probar una reserva en `https://tudominio.com/agenda/<slug>` desde el celular.

## Pendientes conocidos

- **Backups**: activar los backups automáticos de MySQL del panel y bajar uno manual
  antes de cada deploy importante.
- **Monitoreo**: dar de alta `https://tudominio.com/health` en UptimeRobot (gratis)
  con aviso por email o Telegram.
- **App pool inactivo**: IIS apaga la app tras ~20 min sin visitas. El primer request
  tarda unos segundos y el servicio que vence turnos pendientes no corre mientras está
  apagada. El ping de UptimeRobot cada 5 min la mantiene despierta.
- **Textos legales**: completar responsable y email de contacto en
  `frontend/src/pages/LegalPages.tsx` antes de abrir al público.
