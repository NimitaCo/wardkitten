# ToDo — Wardkitten

Seguimiento de construcción por fases (ver plan aprobado). `[x]` hecho · `[ ]` pendiente.

## Fases

- [x] **F0** Scaffolding + gobernanza + solución + CI
- [x] **F1** Domain + Infrastructure (Mongo)
- [x] **F2** Application (scheduling, evaluación, wallet, billing, alertas, auth)
- [x] **F3** Canales de notificación
- [x] **F4** API (auth, watches, ping, check-in, wallet, webhooks, SignalR, health)
- [x] **F5** Worker (evaluación, leader election, escalado, self-monitoring)
- [x] **F6** Web (Blazor WASM + Shared.UI)
- [x] **F7** Móvil (MAUI Blazor Hybrid) — scaffold
- [x] **F8** K8S + Docker + CI
- [x] **F9** Tests + docs/features

## Decisiones de proyecto

- **Sin IA.** Wardkitten **no** incorpora funcionalidades de inteligencia artificial. No añadir
  dependencias de modelos/LLM ni servicios de IA. Cualquier idea que implique IA queda fuera de alcance.

## Pendientes funcionales (post-v1)

- [x] Status pages públicas/privadas
- [x] Gamificación / streaks (habit tracker)
- [x] Plantillas de watch
- [x] Integraciones salientes: Webhook / Slack / Discord (Microsoft Teams pendiente)
- [x] Equipos y guardias (on-call rotations + overrides)
- [ ] **Crear tareas con lenguaje natural** — *feature a futuro*. Permitir describir una tarea en texto
  ("recuérdame regar cada 3 días") y derivar su schedule/tolerancia. Debe implementarse **sin IA**
  (p. ej. parser de reglas/patrones deterministas) conforme a la decisión de proyecto; no usar LLM.
- [ ] Build firmado iOS/Android + workload MAUI en CI
- [ ] Plantillas WhatsApp aprobadas en Meta
