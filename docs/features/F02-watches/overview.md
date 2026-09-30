# F02.01 — Watch (tarea vigilada)

## Metadata
- Estado: implementada
- Módulo: F02

## Descripción
Unidad central del watchdog. Representa algo que debe confirmarse periódicamente (automático o manual).
Si no se confirma dentro de `deadline + tolerancia`, se abre un incidente y se alerta.

## Elementos UI
- `Pages/Home.razor` (panel con tarjetas + estado en vivo), `Pages/WatchEdit.razor` (alta/edición),
  `Components/StatusBadge.razor`.

## Endpoints
- `GET/POST/PUT/DELETE /api/watches`, `POST /api/watches/{id}/pause|resume|checkin`,
  `GET /api/watches/{id}/checkins`, ping público `GET/POST /p/{token}` (+ `/start`, `/fail`).
- Banco de pruebas de la URL durante el alta/edición: `POST|GET|DELETE /api/ping-tests` (ver
  [F03.03](../F03-checkins/overview.md)).

## Modelo de datos (MongoDB, `watches`)
`Watch`: `type` (Ping/Manual), `schedule`, `tolerance`, `channelBindings[]`, `status`, `nextDueAtUtc`,
`consecutiveMisses`, `pingToken`, `testModeUntilUtc`, `maintenanceWindows[]`, `currentIncidentId`.

## Reglas de negocio
- **F02.02 Schedule** timezone-aware (IANA) y correcto con DST: `Interval` | `Cron` (NCrontab) | `Calendar`.
- **Tolerancia bidimensional**: `gracePeriod` (retraso permitido) + `skipTolerance` (fallos consecutivos
  permitidos). Se alerta cuando `consecutiveMisses > skipTolerance`.
- **F02.03 Channel bindings apilables**: varios canales por tarea, cada uno con destino, orden de
  escalado y quiet hours propios. Los límites del plan (nº de watches, intervalo mínimo) se validan en
  servidor (`PlanCatalog`).

## Dependencias
F03 (check-ins), F04 (evaluación), F05 (canales), F06 (wallet para metered).

---

# F02.05 — Canales por defecto del usuario

## Metadata
- Estado: implementada
- Módulo: F02

## Descripción
Cada usuario tiene una lista de canales por defecto (`User.defaultChannelBindings`). Un watch que se crea
**sin bindings propios** nace con una **copia** de ellos (editar el watch no toca los defaults); si el usuario
no tiene, avisa solo por Email, como siempre. La pantalla de alta (`WatchEdit`) también parte de ellos.
Se configuran en el asistente de bienvenida ([F01.04](../F01-onboarding/overview.md)).

## Endpoints
- `PUT /api/onboarding/channels` (`{ bindings }`); lectura en `GET /api/onboarding/state`.

## Reglas de negocio (`ChannelBindingRules`)
Solo se guarda un canal que ya puede entregar:
- Email: siempre (verificado o no).
- SMS/WhatsApp: teléfono verificado por OTP (SECURITY.md §2).
- Telegram: cuenta vinculada ([F05.05](../F05-canales/overview.md)). Desvincular lo quita de los defaults.
- Push: al menos un dispositivo registrado.
- Webhook/Slack/Discord/Teams: URL absoluta `https` que no apunte a loopback ni a rangos privados
  (10/8, 172.16/12, 192.168/16, 169.254/16, 100.64/10, 0/8, ULA/link-local IPv6). No se resuelve DNS.
- Se descartan duplicados exactos (tipo + destino) y se renumera el orden.
