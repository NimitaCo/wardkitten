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

## Modelo de datos (MongoDB, `watches`)
`Watch`: `type` (Ping/Manual), `schedule`, `tolerance`, `channelBindings[]`, `status`, `nextDueAtUtc`,
`consecutiveMisses`, `pingToken`, `maintenanceWindows[]`, `currentIncidentId`.

## Reglas de negocio
- **F02.02 Schedule** timezone-aware (IANA) y correcto con DST: `Interval` | `Cron` (NCrontab) | `Calendar`.
- **Tolerancia bidimensional**: `gracePeriod` (retraso permitido) + `skipTolerance` (fallos consecutivos
  permitidos). Se alerta cuando `consecutiveMisses > skipTolerance`.
- **F02.03 Channel bindings apilables**: varios canales por tarea, cada uno con destino, orden de
  escalado y quiet hours propios. Los límites del plan (nº de watches, intervalo mínimo) se validan en
  servidor (`PlanCatalog`).

## Dependencias
F03 (check-ins), F04 (evaluación), F05 (canales), F06 (wallet para metered).
