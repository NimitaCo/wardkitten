# F04 — Motor de evaluación e incidentes

## Metadata
- Estado: implementada
- Módulo: F04

## Descripción
El worker evalúa periódicamente los watches vencidos, abre incidentes cuando se agota la tolerancia y
dispara/escala las alertas. Es el corazón del watchdog.

## Componentes
- `EvaluationEngine` (Application): barrido recovery-safe e idempotente.
- `EvaluationWorker` (Worker): `BackgroundService` con leader election.
- `NotificationDispatcher` (Application): entrega por los canales del watch.

## Reglas de negocio
- **F04.03 Recovery-safe**: al arrancar tras una caída se recuperan los deadlines perdidos a partir de
  `nextDueAtUtc` (no depende de timers en memoria); el bucle "pone al día" los ciclos perdidos.
- **F04.01 Idempotencia**: un único incidente abierto por watch (índice parcial único en Mongo) y cada
  `(canal, escalón)` se entrega una sola vez. Los "skipped" (quiet hours / saldo) se reintentan.
- **F04.02 Escalado**: los `channelBindings` con `escalationDelay` se activan escalonadamente mientras el
  incidente siga abierto y sin ACK.
- **Leader election**: lease con TTL en Mongo (`MongoLeaseStore`) para que solo una réplica evalúe y no se
  dupliquen alertas al escalar en Kubernetes.

## Modelo de datos
`incidents` (estado, deliveries[], escalón), `leases` (id = recurso, holder, expiresAtUtc).

## Verificación
Tests: `EvaluationEngineTests` (breach abre incidente y alerta una vez; dentro de gracia no alerta).
