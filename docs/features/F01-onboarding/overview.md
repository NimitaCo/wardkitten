# F01.04 — Asistente de bienvenida (onboarding)

## Metadata
- Estado: implementada
- Módulo: F01

## Descripción
Primer contacto de una cuenta nueva: tras registrarse, el usuario llega a `/welcome`, un asistente de
cuatro pasos que deja la cuenta lista para vigilar algo de verdad. Cada paso se puede saltar (explicando qué
pasa si lo salta) y «Saltar todo e ir al panel» está siempre visible.

| Paso | Carácter | Qué hace | Si se salta |
|---|---|---|---|
| 1. Tu perfil | esencial (prellenado) | Nombre, zona horaria (la guardada al registrarse, que es la del navegador) e idioma. | Se mantienen los datos del registro. |
| 2. Cómo avisarte | esencial | Canales por defecto ([F02.05](../F02-watches/overview.md)): Email (verificación opcional), Telegram ([F05.05](../F05-canales/overview.md)), Slack/Discord/Teams/webhook por URL, SMS/WhatsApp (teléfono con OTP; de pago). Botón «Enviar prueba» ([F05.06](../F05-canales/overview.md)). | Los monitores nuevos avisan solo por email. |
| 3. Tu primer monitor | opcional | Nombre, tipo Ping/Manual, periodicidad en lenguaje humano filtrada por el plan, margen. Con Ping se ensaya la URL en vivo ([F03.03](../F03-checkins/overview.md)) y el monitor adopta esa misma URL. | Se crea después desde «Nuevo» o «Plantillas». |
| 4. Listo | — | Resumen (canales, monitor) e «Ir al panel» (cierra el asistente). | — |

Las cuentas que no han cerrado el asistente ven en el panel un aviso «Termina de configurar tu cuenta →
Continuar» (descartarlo cuenta como «saltado»). El estado vacío del panel ofrece «Configurar paso a paso» y
«Crear monitor».

## Elementos UI
- `Layout/OnboardingLayout.razor`: sin navbar; cabecera de marca, stepper (se puede volver a pasos ya
  visitados) y pie «Saltar todo e ir al panel» (`POST /api/onboarding/skip`).
- `Pages/Welcome.razor` + `Welcome.razor.cs` (lógica; mutada con Stryker).
- `Onboarding/`: `OnboardingProgress` (estado del stepper compartido página ↔ layout), `IntervalPresets`,
  `TimeZoneOptions`, `StepActions.razor`, `ChannelTestButton.razor`.
- `Shared.UI/Components/PingTestBench` (paso 3; el mismo componente que usa `WatchEdit`).
- `Pages/Register.razor` navega a `/welcome`. `Pages/Home.razor`: aviso y acciones del estado vacío.

## Endpoints
Todos con sesión, bajo `/api/onboarding`:
- `GET state` → `OnboardingStateDto`: cerrado o no, perfil, email/teléfono verificados, Telegram vinculado
  y disponible, canales por defecto, nº de watches y límites del plan.
- `PUT profile` (`displayName`, `timeZoneId` IANA, `locale` `es`|`en`).
- `PUT channels` (canales por defecto, F02.05) y `POST channels/test` (F05.06).
- `POST complete` / `POST skip`: ambos fijan `onboardingCompletedAtUtc`; se mantienen separados para poder
  distinguirlos en analítica.

`UserDto` (`/api/auth/me`, login, registro) incluye `onboardingCompleted` y `telegramLinked`.

## Modelo de datos (MongoDB, `users`)
- `onboardingCompletedAtUtc` (null = pendiente). Idempotente: conserva la primera fecha.
- `defaultChannelBindings[]` (F02.05), `telegramLinkCodeHash` / `telegramLinkExpiresUtc` (F05.05).

## Reglas de negocio
- El perfil se valida en servidor: nombre obligatorio (≤ 80), zona horaria existente, idioma soportado.
- El primer monitor se crea **sin bindings**: el servidor le aplica los canales por defecto del usuario.
- Las periodicidades del asistente son un catálogo fijo (ver `HARDCODED.md`) filtrado por
  `PlanLimits.MinIntervalSeconds`: en Free no aparecen las de minutos. Cron e intervalos a medida siguen en la
  pantalla completa de alta.
- Las cuentas anteriores a esta feature tienen `onboardingCompletedAtUtc = null`: verán el aviso del panel
  hasta que lo descarten o completen el asistente.
- Estado en vivo por **polling** (Telegram cada 3 s, banco de pruebas cada 2 s), no SignalR: producción tiene
  varias réplicas de la API sin backplane.
- El registro envía el idioma del navegador (`es`/`en`; cualquier otro cae en `es`).

## Dependencias / Sub-features
F01.01 (registro), F01.03 (verificación de email/teléfono), F02.05, F03.03, F05.05, F05.06.
