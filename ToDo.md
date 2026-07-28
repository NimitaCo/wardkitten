# ToDo — Wardkitten

<!-- ==== PENDIENTE anadido el 2026-07-27 (feed NuGet NimitaCo) ==== -->

## Pendiente - acceso al feed NuGet de NimitaCo

> Seccion anadida el 2026-07-27 al poner en marcha la publicacion de los paquetes
> `Es.Nimita.*` desde `NimitaCo/Domain`. **Nada de esta seccion esta hecho todavia.**

Punto de partida ya resuelto: `nuget.config` declara el feed `nimitaco`
(`https://nuget.pkg.github.com/NimitaCo/index.json`) con *package source mapping*
limitado a `Es.Nimita.*`. Sin credenciales el feed queda anonimo y no se consulta,
por lo que el repo compila con normalidad. Lo que falta:

- [ ] **Crear un PAT clasico con scope `read:packages`** en <https://github.com/settings/tokens>.
      Sin el no se pueden descargar los paquetes. **La descarga nunca llego a verificarse**:
      el unico intento devolvio `403 Forbidden` por falta de scope en el token.
- [ ] **Local** - definir la variable de entorno `NuGetPackageSourceCredentials_nimitaco`
      con el formato `Username=<usuario>;Password=<PAT>`. En PowerShell:

      [Environment]::SetEnvironmentVariable('NuGetPackageSourceCredentials_nimitaco','Username=<usuario>;Password=<PAT>','User')

- [ ] **CI** - crear el secreto de organizacion `NIMITACO_NUGET_TOKEN`
      (`gh secret set NIMITACO_NUGET_TOKEN --org NimitaCo --visibility all`, requiere org admin)
      y **solo despues** anadir al job del workflow:

      env:
        NuGetPackageSourceCredentials_nimitaco: Username=${{ github.actor }};Password=${{ secrets.NIMITACO_NUGET_TOKEN }}

      No anadir esa linea antes de que el secreto exista: si la variable esta definida pero
      vacia, NuGet aborta el restore con
      `Value cannot be null or empty string (Parameter 'password')`. Ya ocurrio una vez y
      dejo en rojo el CI de cuatro repos.
- [ ] **Nunca** declarar `<packageSourceCredentials>` en `nuget.config` apuntando a variables
      de entorno: NuGet valida las credenciales **al parsear el fichero**, aunque el source no
      llegue a consultarse nunca, asi que un valor vacio rompe el build entero.
- [ ] **Docker** (`src/Wardkitten.Api/Dockerfile`, `src/Wardkitten.Worker/Dockerfile`) - estos Dockerfiles ejecutan
      `dotnet restore`. Cuando el repo pase a `PackageReference`, la credencial debe
      pasarse con `--mount=type=secret` de BuildKit, **nunca con `ARG`/`--build-arg`**,
      que la dejaria grabada de forma permanente en las capas de la imagen.
- [ ] Adoptar `Es.Nimita.Domain.Primitives` + `Es.Nimita.Infra.Mongo` cuando el feed
      este verificado (hoy no hay `vendor/` ni referencias).

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
