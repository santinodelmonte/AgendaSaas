import type { ReactNode } from 'react';

// Datos del responsable del servicio. Completar antes de salir a producción.
const RESPONSABLE = 'AgendaSaaS';
const CONTACTO = 'contacto@agendasaas.uy';
const ACTUALIZADO = 'noviembre de 2026';

function Seccion({ titulo, children }: { titulo: string; children: ReactNode }) {
  return (
    <section className="space-y-2">
      <h2 className="text-lg font-semibold text-slate-900">{titulo}</h2>
      <div className="space-y-2 text-sm leading-relaxed text-slate-600">{children}</div>
    </section>
  );
}

function Documento({ titulo, children }: { titulo: string; children: ReactNode }) {
  return (
    <article className="mx-auto max-w-3xl space-y-6 rounded-3xl border border-slate-200 bg-white p-5 shadow-soft sm:p-8">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900 sm:text-3xl">{titulo}</h1>
        <p className="mt-1 text-xs text-slate-500">Última actualización: {ACTUALIZADO}</p>
      </header>
      {children}
    </article>
  );
}

export function PrivacidadPage() {
  return (
    <Documento titulo="Política de privacidad">
      <Seccion titulo="1. Quién es responsable de tus datos">
        <p>
          Cuando reservás un turno, la manicurista con la que reservás es la responsable de la base de
          datos de sus clientas. {RESPONSABLE} actúa como encargado de tratamiento: brinda la plataforma y
          guarda los datos por cuenta de ella. Esta política se rige por la Ley N.º 18.331 de Protección de
          Datos Personales de Uruguay y su Decreto reglamentario N.º 414/009.
        </p>
      </Seccion>

      <Seccion titulo="2. Qué datos recolectamos">
        <p>
          <strong>Clientas que reservan:</strong> nombre, número de teléfono (WhatsApp), servicio elegido
          y fecha y hora del turno.
        </p>
        <p>
          <strong>Manicuristas que usan la plataforma:</strong> nombre, email, WhatsApp, datos del
          negocio, horarios y servicios.
        </p>
        <p>No recolectamos datos sensibles ni datos de pago.</p>
      </Seccion>

      <Seccion titulo="3. Para qué los usamos">
        <ul className="list-disc space-y-1 pl-5">
          <li>Gestionar tu turno: solicitarlo, confirmarlo, reprogramarlo o cancelarlo.</li>
          <li>Contactarte por WhatsApp sobre tu turno (confirmaciones y recordatorios).</li>
          <li>Que la manicurista lleve su agenda y su historial de atención.</li>
        </ul>
        <p>No vendemos tus datos ni los usamos para publicidad de terceros.</p>
      </Seccion>

      <Seccion titulo="4. Con quién se comparten">
        <p>
          Solo con la manicurista con la que reservaste. Para enviar mensajes por WhatsApp se usan los
          servicios de Meta (WhatsApp), y los datos se alojan en un proveedor de hosting que puede estar
          fuera de Uruguay, con medidas de seguridad adecuadas.
        </p>
      </Seccion>

      <Seccion titulo="5. Cuánto tiempo se guardan">
        <p>
          Mientras la manicurista mantenga su cuenta activa y sea necesario para llevar su agenda e
          historial. Podés pedir que se borren antes en cualquier momento.
        </p>
      </Seccion>

      <Seccion titulo="6. Tus derechos">
        <p>
          Tenés derecho a acceder, rectificar, actualizar, incluir o suprimir tus datos. Para ejercerlos,
          escribile a la manicurista o a {CONTACTO}. Respondemos dentro de los plazos legales (5 días
          hábiles para el acceso). Si no quedás conforme, podés reclamar ante la Unidad Reguladora y de
          Control de Datos Personales (URCDP), www.gub.uy/urcdp.
        </p>
      </Seccion>

      <Seccion titulo="7. Seguridad">
        <p>
          Las contraseñas se guardan cifradas, la conexión usa HTTPS y cada manicurista solo puede ver los
          datos de sus propias clientas.
        </p>
      </Seccion>
    </Documento>
  );
}

export function TerminosPage() {
  return (
    <Documento titulo="Términos y condiciones">
      <Seccion titulo="1. El servicio">
        <p>
          {RESPONSABLE} es una plataforma para que manicuristas gestionen su agenda y reciban solicitudes de
          turnos online. Al usarla aceptás estos términos.
        </p>
      </Seccion>

      <Seccion titulo="2. Reservas">
        <p>
          Reservar desde la plataforma es una <strong>solicitud</strong>: el turno queda pendiente hasta
          que la manicurista lo confirma. Las condiciones del servicio (precio, duración, cancelaciones,
          tolerancia de demora) las define cada manicurista y son responsabilidad de ella.
        </p>
        <p>Te pedimos ingresar datos reales y avisar si no vas a poder asistir.</p>
      </Seccion>

      <Seccion titulo="3. Cuentas de manicuristas">
        <p>
          Cada manicurista es responsable de mantener segura su contraseña, de la información que publica
          y del trato con sus clientas. Debe usar los datos de sus clientas solo para gestionar sus turnos.
        </p>
        <p>
          Durante el período beta el servicio puede cambiar, tener interrupciones o nuevas condiciones
          comerciales, que se avisarán con anticipación.
        </p>
      </Seccion>

      <Seccion titulo="4. Responsabilidad">
        <p>
          Hacemos lo posible para que la plataforma funcione sin interrupciones, pero no garantizamos que
          esté siempre disponible. {RESPONSABLE} no es parte de la relación entre la manicurista y sus
          clientas ni responde por los servicios que se prestan.
        </p>
      </Seccion>

      <Seccion titulo="5. Datos personales">
        <p>
          El tratamiento de datos se rige por nuestra{' '}
          <a href="/privacidad" className="font-medium text-brand-600 underline">política de privacidad</a>.
        </p>
      </Seccion>

      <Seccion titulo="6. Ley aplicable">
        <p>
          Estos términos se rigen por las leyes de la República Oriental del Uruguay. Consultas:{' '}
          {CONTACTO}.
        </p>
      </Seccion>
    </Documento>
  );
}
