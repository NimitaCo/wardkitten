# C4 — Diagrama de contexto

## Descripción
Wardkitten es un watchdog SaaS. Los usuarios definen tareas; los procesos automáticos confirman por ping
y las manuales por la app/Telegram/email. Si una tarea incumple su programación, Wardkitten alerta por los
canales configurados.

## Diagrama (Mermaid)

```mermaid
C4Context
  Person(user, "Usuario", "Define tareas y recibe alertas")
  System_Boundary(wk, "Wardkitten") {
    System(api, "API + Web", "ASP.NET Core + Blazor WASM")
    System(worker, "Worker", "Motor de evaluación")
    SystemDb(mongo, "MongoDB", "Watches, check-ins, incidentes, wallet")
  }
  System_Ext(proc, "Procesos automáticos", "Backups, facturación…")
  System_Ext(stripe, "Stripe", "Suscripciones + créditos")
  System_Ext(channels, "Email/Telegram/FCM/Twilio", "Notificaciones")

  Rel(user, api, "Gestiona watches / recibe alertas")
  Rel(proc, api, "Ping a /p/{token}")
  Rel(api, mongo, "Lee/escribe")
  Rel(worker, mongo, "Evalúa (leader election)")
  Rel(worker, channels, "Envía alertas")
  Rel(api, stripe, "Checkout / webhooks")
```

## Notas
- API y Worker comparten Domain/Application/Infrastructure y la misma BBDD Mongo.
- El Worker evalúa bajo leader election (lease en Mongo) para no duplicar alertas al escalar.
- Despliegue en Kubernetes (ver `K8S/`), imágenes en GHCR, ArgoCD por entorno.
