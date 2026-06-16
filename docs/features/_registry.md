# Feature Registry — Wardkitten

Tabla maestra de funcionalidades. Cada una tiene su código `FXX.YY` y, si procede, ficha en
`docs/features/FXX-nombre/overview.md`. Ver `DOCUMENTATION-DIRECTIVES.md`.

| Código | Feature | Estado | Ficha |
|--------|---------|--------|-------|
| F01.01 | Registro y cuenta de usuario | implementada | — |
| F01.02 | Login + refresh tokens rotatorios | implementada | — |
| F01.03 | Verificación de email y teléfono (OTP) | implementada | — |
| F02.01 | Watch (tarea vigilada) | implementada | [F02-watches](F02-watches/overview.md) |
| F02.02 | Schedule timezone-aware (interval/cron/calendar) | implementada | [F02-watches](F02-watches/overview.md) |
| F02.03 | Channel bindings apilables por tarea | implementada | [F02-watches](F02-watches/overview.md) |
| F03.01 | Check-in por ping HTTP (start/success/fail) | implementada | — |
| F03.02 | Check-in manual (app/magic link) | implementada | — |
| F04.01 | Incidentes con idempotencia de alertas | implementada | [F04-evaluation](F04-evaluation/overview.md) |
| F04.02 | Escalado por bindings / políticas | implementada | [F04-evaluation](F04-evaluation/overview.md) |
| F04.03 | Motor de evaluación + leader election | implementada | [F04-evaluation](F04-evaluation/overview.md) |
| F05.01 | Canales Email/Telegram/Push/SMS/WhatsApp | implementada | — |
| F05.03 | ACK/Hecho/Snooze por magic link firmado | implementada | — |
| F06.01 | Wallet de créditos (cobro metered) | implementada | [F06-wallet](F06-wallet/overview.md) |
| F06.02 | Movimientos de créditos (asientos) | implementada | [F06-wallet](F06-wallet/overview.md) |
| F07.01 | Suscripciones Stripe (Free/Pro/Team) | implementada | — |
| F07.02 | Recargas de créditos vía Stripe | implementada | — |
| F08.01 | Web (dashboard, alta de watch, wallet) | implementada | — |
| F08.02 | Estado en vivo (SignalR) | implementada (web por polling) | — |
| F09 | App móvil (MAUI Blazor Hybrid) + push | scaffold | — |
| F10.01 | Streaks / gamificación | implementada | — |
| F12.01 | Equipos (miembros, plan Team) | implementada | — |
| F12.02 | Guardias on-call (rotación por turnos + overrides) | implementada | — |
| F12.03 | Escalado de incidente a la persona de guardia | implementada | — |
| F11.01 | Crear tareas con lenguaje natural (**sin IA**, parser determinista) | planificada (futuro) | — |

> **Decisión de proyecto:** Wardkitten no incorpora funcionalidades de IA (ver `ToDo.md` → Decisiones de proyecto y `AGENTS.md`). La feature F11.01 es a futuro y deberá implementarse sin LLM.
