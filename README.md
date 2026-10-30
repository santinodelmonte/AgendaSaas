# AgendaSaaS

**Plataforma SaaS multi-tenant de agenda y gestión de turnos.**
En desarrollo activo, con una clienta real (servicio de manicura) como primer tenant.

<!-- TODO: reemplazar por un GIF o screenshot de la agenda funcionando en mobile.
     Es lo primero que ve cualquiera que abra el repo. -->
![AgendaSaaS](docs/demo.gif)

---

## El problema

Las profesionales independientes (manicuristas, peluqueras, estudios chicos) gestionan sus turnos por WhatsApp: mensajes sueltos, un cuaderno, dobles reservas y horas perdidas coordinando. No usan software de agenda porque el que existe está pensado para escritorio, para clínicas grandes, o cuesta más de lo que facturan en un día.

AgendaSaaS apunta a eso: una agenda que se maneja **desde el celular, con una mano, entre cliente y cliente**.

## Qué resuelve

- **Reserva pública** — la clienta final elige servicio y horario disponible sin instalar nada ni crear cuenta.
- **Panel de gestión** — la profesional ve su día en un timeline vertical y crea, mueve o cancela turnos con un tap.
- **Panel superadmin** — alta y administración de tenants.
- **Multi-tenant** — cada negocio tiene sus servicios, su horario, su marca y sus datos aislados. Un solo deploy, N negocios.

## Stack

| Capa | Tecnología |
|---|---|
| Frontend | React · TypeScript · Vite · Tailwind CSS v4 |
| Backend | C# · ASP.NET (API REST) |
| Base de datos | MySQL |
| Auth | JWT + BCrypt |
| Estructura | Monorepo (`/frontend`, `/backend`) |

---

## Decisiones de diseño

### Aislamiento multi-tenant: no confiar en el cliente

La primera versión resolvía el tenant leyendo el header `X-Tenant-Id` que enviaba el frontend. Funcionaba — y era **inseguro**: una usuaria autenticada del salón A podía cambiar ese header por el ID del salón B y acceder a sus datos con su propio token válido. Un IDOR de manual.

El fix: para requests autenticadas, **el tenant sale del claim `tenantId` del JWT**, y el header se ignora por completo. El header solo sobrevive en el flujo público de reserva, donde no hay sesión y no hay nada que suplantar.

```csharp
// TenantMiddleware
var claim = context.User.FindFirst("tenantId")?.Value;
tenantProvider.TenantId = tenantIdClaim;   // el header del cliente no decide nada
```

El filtro por tenant vive además en la **capa de repositorios**, no en cada endpoint: así un endpoint nuevo no puede "olvidarse" de filtrar y filtrar datos de otro negocio.

### Mobile-first, no mobile-compatible

El primer diseño de la agenda usaba drag & drop para mover turnos. Andaba perfecto con mouse e **inutilizable con el pulgar** sobre franjas de 15 minutos. Lo reemplacé por un modelo **tap-to-modal**: tocás el turno, se abre un modal con las acciones. Menos vistoso en la demo, mucho más usable en la realidad. Todos los targets táctiles son ≥ 44×44px.

### Dos vistas distintas para dos usuarias distintas

La clienta final ve bloques de disponibilidad (simple, sin ruido). La profesional ve un timeline vertical continuo con los huecos reales. Compartir un mismo componente entre ambas hubiera ahorrado código y empeorado las dos experiencias.

### Acceso a datos sin ORM

ADO.NET con **SQL parametrizado** en todos los repositorios. Decisión consciente: más código, control total sobre las queries, y cero superficie de SQL injection.

---

## Estado

En desarrollo activo.

- [x] Reserva pública multi-tenant
- [x] Panel de gestión con timeline vertical (tap-to-modal)
- [x] Panel superadmin
- [x] Aislamiento de tenant atado al JWT
- [ ] Parsing de reservas en lenguaje natural (IA)
- [x] Avisos de reservas nuevas por notificación push (PWA instalable, iPhone y Android)
- [x] Aviso a la clienta por WhatsApp al confirmar/rechazar/cancelar (mensaje prearmado)
- [ ] Recordatorios automáticos por WhatsApp
- [ ] Reportes de ingresos y clientes recurrentes

## Probar en tu computadora (un comando)

Con [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado:

```bash
docker compose -f docker-compose.local.yml up --build
```

- Abrí **http://localhost:8080** → entrá con `admin@local.test` / `admin1234` (superadmin de prueba).
- Creá una manicurista, cerrá sesión y entrá con su email para ver el panel.
- La agenda pública queda en `http://localhost:8080/agenda/<slug>`. Swagger: `http://localhost:8080/swagger`.
- **Desde el celular** (misma red Wi‑Fi): `http://<IP-de-tu-PC>:8080`. Reservas y panel funcionan;
  las notificaciones push no, porque requieren HTTPS (en la PC funcionan por `localhost` en Chrome/Edge).
- Para borrar los datos de prueba: `docker compose -f docker-compose.local.yml down -v`.

## Correr local (desarrollo)

Al correr con `dotnet run` (perfiles de desarrollo) se cargan datos de demo la primera vez:
manicurista **`caro@demo.local` / `demo1234`**, con horarios, servicios y turnos alrededor
de hoy. Agenda pública: `http://localhost:5173/agenda/unas-de-caro`. Se controla con
`Demo:Seed` (apagado por defecto y en producción).

```bash
# Backend
cd backend/AgendaSaaS
dotnet restore
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Database=agendasaas;Uid=root;Pwd=;Charset=utf8mb4;"
dotnet user-secrets set "Jwt:Key" "<una clave larga y aleatoria>"
dotnet run

# Frontend
cd frontend
cp .env.example .env
npm install
npm run dev
```

Ver `appsettings.Example.json` para la configuración completa. Con una base vacía, la API
crea las tablas sola al arrancar.

## Tests

```bash
# Backend: tests de integración (API completa contra un MySQL real; crean y borran
# una base descartable). Sin la variable se omiten.
export AGENDASAAS_TEST_MYSQL="Server=localhost;Uid=root;Pwd=;"
dotnet test backend/AgendaSaaS.Tests/AgendaSaaS.Tests.csproj

# Frontend
cd frontend && npm test
```

Cubren reserva pública, anticipación mínima, reservas simultáneas, gestión de turnos,
aislamiento entre tenants, superadmin, horarios y zona horaria. Corren en CI
(`.github/workflows/ci.yml`) en cada push y PR.

## Deploy

Ver [`docs/DEPLOY.md`](docs/DEPLOY.md): stack con Docker Compose (MySQL + API + frontend +
HTTPS automático), variables de entorno, backups y checklist para el alta del primer cliente.

---

*Desarrollado por [Santino Delmonte](https://github.com/santinodelmonte).*
