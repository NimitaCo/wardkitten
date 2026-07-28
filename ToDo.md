# ToDo — Wardkitten

<!-- ==== PENDIENTE anadido el 2026-07-27 (feed NuGet NimitaCo) ==== -->

## Pendiente - acceso al feed NuGet de NimitaCo

> Seccion anadida el 2026-07-27 al poner en marcha la publicacion de los paquetes
> `Es.Nimita.*` desde `NimitaCo/Domain`. Actualizada el 2026-07-28: la adopcion por
> `PackageReference` ya esta hecha; quedan las **acciones humanas** de credenciales.

Punto de partida ya resuelto: `nuget.config` declara el feed `nimitaco`
(`https://nuget.pkg.github.com/NimitaCo/index.json`) con *package source mapping*
limitado a `Es.Nimita.*`. Sin credenciales el feed queda anonimo. **OJO: desde el
2026-07-28 el repo referencia paquetes `Es.Nimita.*`, asi que el restore NECESITA la
credencial** (verificado en local contra un feed de directorio con los .nupkg 26.7.5).

Acciones humanas pendientes:

- [ ] **Crear un PAT clasico con scope `read:packages`** en <https://github.com/settings/tokens>.
      Sin el no se pueden descargar los paquetes. **La descarga contra el feed real nunca llego
      a verificarse**: el unico intento devolvio `403 Forbidden` por falta de scope en el token.
- [ ] **Local** - definir la variable de entorno `NuGetPackageSourceCredentials_nimitaco`
      con el formato `Username=<usuario>;Password=<PAT>`. En PowerShell:

      [Environment]::SetEnvironmentVariable('NuGetPackageSourceCredentials_nimitaco','Username=<usuario>;Password=<PAT>','User')

- [ ] **CI** - crear el secreto de organizacion `NIMITACO_NUGET_TOKEN`
      (`gh secret set NIMITACO_NUGET_TOKEN --org NimitaCo --visibility all`, requiere org admin).
      Hasta que exista, el CI y los builds de imagen fallaran en el restore de `Es.Nimita.*`
      (feed anonimo -> 401): es el estado esperado tras la adopcion.

Hecho el 2026-07-28 (adopcion de los paquetes):

- [x] **Workflows preparados y guardados** (2026-07-28) - `ci.yml` compone
      `NuGetPackageSourceCredentials_nimitaco` en un paso condicionado por
      `NIMITACO_TOKEN_PRESENT` (evaluado en el `env` del job: el contexto `secrets` no se
      permite en el `if` de un paso). Sin el secreto creado NO se define la variable, evitando
      el `Value cannot be null or empty string (Parameter 'password')` que ya dejo en rojo el
      CI de cuatro repos. Sigue vigente: **nunca** declarar `<packageSourceCredentials>` en
      `nuget.config` apuntando a variables de entorno (NuGet las valida al parsear el fichero).
- [x] **Docker con secreto BuildKit** (2026-07-28) - los dos Dockerfiles montan la credencial
      con `--mount=type=secret,id=nimitaco_nuget` alrededor del `dotnet publish` (que hace el
      restore) y solo la exportan si el secreto trae `Password` no vacio. `build-api.yml` y
      `build-worker.yml` la pasan via `secrets:` de `docker/build-push-action`. **Nunca
      `ARG`/`--build-arg`**: quedaria grabada en las capas de la imagen.
- [x] **Adoptados `Es.Nimita.Domain.Primitives` + `Es.Nimita.Infra.Mongo` 26.7.5 por
      `PackageReference`** (2026-07-28) - sin `vendor/`. Sustituidos MongoDbConfigurator
      (wrapper sobre `MongoConventions.Register(MongoConventionOptions.Default)`, juego 1:1 con
      el historico), MongoSettings (conservando el default de BBDD "Wardkitten"), y el leasing
      completo (`Lease`/`ILeaseStore`/`MongoLeaseStore` -> `Es.Nimita.Infra.Mongo.Leasing`,
      misma coleccion `leases`). AuthService valida email con `EmailAddress` y el telefono del
      OTP con `PhoneNumber.TryParseSpanish`. Candado de la forma BSON de produccion en
      `MongoConventionsGuardTests`. SharpCompress alineado a 1.0.0 (pin del paquete).

<!-- ==== FIN de lo anadido el 2026-07-27 . Lo de abajo ya existia ==== -->

---

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
