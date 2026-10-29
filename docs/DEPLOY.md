# Puesta en producción

Guía para publicar AgendaSaaS y dar de alta al primer cliente.

## Opción A (recomendada): un VPS con Docker Compose

Todo en un servidor (DigitalOcean, Hetzner, Contabo, etc. — con 1 GB de RAM alcanza):
MySQL + API + frontend + [Caddy](https://caddyserver.com/) con HTTPS automático.

1. **Dominios**: crear dos registros DNS tipo `A` apuntando a la IP del servidor, por ejemplo
   `agenda.midominio.com` (frontend) y `api.midominio.com` (API).
2. **Servidor**: instalar Docker y clonar el repo.
3. **Configuración**:
   ```bash
   cp .env.example .env
   openssl rand -base64 48   # usar como JWT_KEY
   nano .env                 # completar dominios, claves y superadmin
   ```
4. **Levantar**:
   ```bash
   docker compose up -d --build
   docker compose ps          # los 4 servicios en "Up"
   curl https://api.midominio.com/health   # {"status":"ok"}
   ```
   La API crea las tablas sola al arrancar (base vacía) y siembra el superadmin.
5. **Actualizar a una versión nueva**: `git pull && docker compose up -d --build`.

## Opción B: servicios administrados (PaaS)

- **API**: cualquier hosting de contenedores (Render, Railway, Fly.io, Azure App Service)
  usando `backend/Dockerfile`. Health check: `GET /health`. Puerto: `8080`.
- **Base de datos**: MySQL 8 administrado (Railway, Aiven, PlanetScale-compatible, AWS RDS…).
  Si vas a importar la base de desarrollo de Windows, configurar `lower_case_table_names=1`
  (ver más abajo).
- **Frontend**: sitio estático (Vercel, Netlify, Cloudflare Pages) con
  `npm run build` → carpeta `dist`, y `VITE_API_URL` definida **al compilar**.
  Es una SPA: hay que redirigir toda ruta a `index.html`
  (Netlify: archivo `public/_redirects` con `/* /index.html 200`;
  Vercel: `vercel.json` con `{"rewrites":[{"source":"/(.*)","destination":"/"}]}`).

### Variables de entorno de la API

| Variable | Obligatoria | Ejemplo / descripción |
|---|---|---|
| `ConnectionStrings__Default` | sí | `Server=host;Database=agendasaas;Uid=user;Pwd=...;Charset=utf8mb4;` |
| `Jwt__Key` | sí | ≥ 32 caracteres aleatorios (`openssl rand -base64 48`). La app no arranca si es más corta. |
| `SuperAdmin__Email` / `SuperAdmin__Password` | sí | Se crea al primer arranque si no existe. |
| `Cors__AllowedOrigins` | sí | URL pública del frontend, ej. `https://agenda.midominio.com` (varias separadas por coma). |
| `App__TimeZone` | no | Zona horaria del negocio. Default `America/Argentina/Buenos_Aires`. |
| `ReverseProxy__Enabled` | no | `true` si hay un proxy que termina TLS adelante (la imagen Docker ya lo trae en `true`). |
| `Jwt__ExpiresMinutes` | no | Duración de la sesión. Default 10080 (7 días). |
| `RateLimiting__LoginPorMinuto` | no | Intentos de login por IP y minuto. Default 10. |
| `RateLimiting__ReservasPorHora` | no | Reservas públicas por IP y hora. Default 20. |
| `Swagger__Enabled` | no | `true` para exponer Swagger en producción (por defecto solo en Development). |

## Migrar datos de desarrollo (opcional)

Si ya cargaste datos reales en tu base local:

```bash
mysqldump -u root -p --single-transaction agendasaas > agendasaas.sql
# en el servidor (Opción A):
docker compose exec -T mysql mysql -uroot -p"$MYSQL_ROOT_PASSWORD" agendasaas < agendasaas.sql
```

MySQL en Windows guarda los nombres de tabla en minúsculas (`turnos`), y en Linux los
nombres distinguen mayúsculas. El `docker-compose.yml` ya arranca MySQL con
`--lower-case-table-names=1` para que el dump funcione tal cual. En un MySQL administrado,
configurar ese mismo parámetro **antes** de crear la base, o empezar con una base vacía.

## Backups

La base es lo único que no se puede reconstruir. Ejemplo de backup diario con cron en el VPS:

```bash
0 4 * * * cd /ruta/AgendaSaas && docker compose exec -T mysql sh -c 'mysqldump -uroot -p"$MYSQL_ROOT_PASSWORD" --single-transaction agendasaas' | gzip > /backups/agendasaas-$(date +\%F).sql.gz
```

Copiar los backups fuera del servidor (S3, Google Drive, etc.) y probar restaurar uno.

## Alta del primer cliente

1. Entrar a `https://agenda.midominio.com/login` con el superadmin.
2. **Nueva manicurista**: nombre, email (es su usuario), WhatsApp, duración de turno y
   contraseña inicial. Se crea con horario Lunes a Viernes 9 a 18.
3. Entrar con la cuenta de la manicurista (o pasarle los datos) y en **Configuración**:
   ajustar horarios y pausas, cargar servicios y precios, bloquear feriados/vacaciones.
4. Compartirle su link público: `https://agenda.midominio.com/agenda/<slug>`
   (para la bio de Instagram / estado de WhatsApp).

## Checklist antes de anunciarlo

- [ ] `GET /health` responde `{"status":"ok"}`.
- [ ] Login de superadmin y de la manicurista funcionan; contraseña incorrecta muestra error.
- [ ] Desde un celular (fuera de tu red): abrir el link público, reservar un turno.
- [ ] El turno aparece como pendiente en el panel; confirmarlo; aparece en la agenda a la hora correcta.
- [ ] El horario mostrado coincide con la hora local (no corrido 3 horas).
- [ ] Backup diario configurado y probado.

## Limitaciones conocidas

- **Una sola instancia de la API**: la protección contra doble reserva simultánea es un lock en
  memoria. Si en el futuro se escala a varias instancias, hay que moverla a la base
  (ej. `SELECT ... FOR UPDATE` o un índice único).
- **Sin notificaciones**: la manicurista se entera de las reservas entrando al panel. Los turnos
  pendientes sin confirmar se liberan solos a las 24 h.
- **Cambiar la contraseña**: hoy solo la puede resetear el superadmin.
