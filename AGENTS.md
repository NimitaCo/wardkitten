# Instrucciones para agentes — Wardkitten

> Adaptado de las directivas de IntegraSystem/Avanware. Léelo entero antes de actuar en este repo.

Wardkitten es un **watchdog SaaS** para tareas/procesos periódicos (automáticos y manuales). Núcleo:
**monitorización inversa / dead-man's-switch** — se espera un *check-in* antes de un deadline; si no
llega dentro de `deadline + tolerancia`, se alerta por los canales configurados en la tarea.

## Estructura del repositorio

Monolito modular .NET 10, arquitectura limpia. Compila siempre desde `wardkitten.slnx`. Cuando añadas
un proyecto nuevo, vincúlalo también a la solución.

```
src/Wardkitten.Domain          # entidades, value objects, reglas, interfaces (sin dependencias de infra)
src/Wardkitten.Application      # casos de uso y servicios (scheduling, evaluación, wallet, billing, alertas, auth)
src/Wardkitten.Infrastructure   # Mongo, Stripe, Twilio, Telegram, FCM, SMTP (implementa interfaces de Application/Domain)
src/Wardkitten.Shared.Contracts # DTOs compartidos API <-> clientes
src/Wardkitten.Shared.UI        # componentes Razor compartidos (web + móvil)
src/Wardkitten.Api              # ASP.NET Core API + SignalR + hosting del WASM
src/Wardkitten.Worker           # motor de evaluación (BackgroundService)
src/Wardkitten.Web              # Blazor WebAssembly
src/Wardkitten.Mobile           # .NET MAUI Blazor Hybrid (solución aparte: wardkitten.mobile.slnx)
test/Wardkitten.Tests           # unit + integration
```

## Ramas y entornos

- `main` → preproducción (staging).
- `Release` → producción.
- Nombres de rama ≤ 15 caracteres (el prefijo `codex/` no computa).
- **Commitea y pushea con frecuencia**: un commit/push sobrevive a cualquier reset del working tree;
  los cambios sin commitear, no. Si trabajas en otra rama sobre el mismo directorio, usa `git worktree`.

## Publicar nueva versión (K8S deploy)

La imagen Docker se etiqueta con el número de build del workflow de CI. `wardkitten` y
`wardkitten-worker` tienen numeraciones independientes. **La imagen `wardkitten` empaqueta y sirve
también el Blazor WASM** (un solo despliegue; no hay imagen `wardkitten-web` separada).

```bash
# 1. Número de build actual
gh run list --repo NimitaCo/wardkitten --workflow "Build" --limit 1 --json number,status,displayTitle

# 2. Actualizar manifiestos (los entornos a la vez)
OLD=12; NEW=13
find K8S -name "wardkitten.yaml" | xargs sed -i "s|ghcr.io/nimitaco/wardkitten:$OLD|ghcr.io/nimitaco/wardkitten:$NEW|g"

# 3. Commit y push
git add K8S/ && git commit -m "K8S deploy wardkitten:$NEW" && git push
```

Imágenes: `ghcr.io/nimitaco/wardkitten` y `ghcr.io/nimitaco/wardkitten-worker`. Pull secret:
`avanware.ghcr.io` (nombre histórico del `dockerconfigjson`; debe tener acceso de lectura a
`ghcr.io/nimitaco`). Entornos en `K8S/produccion/` y `K8S/preproduccion/`. Despliegue por ArgoCD;
se considera completo con `sync == Synced` y `health == Healthy`.

Dominio canónico de la web: `www.wardkitten.com` (la API sirve WASM + API same-origin);
`app.wardkitten.com` redirige (308) a `www`. `api.wardkitten.com` sigue sirviendo la API.

## Despliegue Linux vs. desarrollo Windows

Web y worker corren en **Kubernetes (Linux)**; el desarrollo es en **Windows**. Cuidado con:

- **Certificados cliente TLS:** usa `HttpClient` con `ClientCertificateOption.Manual` (nunca `Automatic`,
  no envía el cert en Linux/OpenSSL).
- **Rutas:** Linux trata `/ruta` como absoluta. Usa `Path.Combine` o rutas explícitas.
- **Case-sensitivity:** el FS de Linux distingue mayúsculas en nombres de archivo.
- **Fin de línea:** el repo usa **CRLF** (forzado en `.gitattributes`). No conviertas a LF.
- **Variables de entorno:** case-sensitive en Linux.

## Normas generales

- **Sin IA.** Wardkitten no incorpora funcionalidades de inteligencia artificial: no añadas dependencias
  de LLM/modelos ni servicios de IA. Las ideas que impliquen IA quedan fuera de alcance (ver `ToDo.md`).
- Mensajes de commit en **inglés**; descripciones de PR pueden ir en español. Si el commit afecta a una
  feature documentada, incluye su código: `feat(F02.01): add per-task channel bindings`.
- No incluyas en commits archivos generados ni dependencias precompiladas, ni **secretos**.
- Evita parches provisionales («duct tape»): da soluciones definitivas.
- Ejecuta `dotnet test` desde la raíz antes de abrir un PR.
- No subas a `nuget.config` credenciales ni feeds privados: este repo es **autónomo** (solo nuget.org).

## Shell

- Para scripting usa preferentemente **PowerShell** o **dotnet-script**. Recurre a Python solo si es
  imprescindible. No uses `Get-Content` para leer archivos de código (usa las herramientas del agente).

## Autorización

- Protege endpoints con los atributos/policies de auth de Wardkitten (`[Authorize]` con políticas por
  rol/plan). Los endpoints públicos (ping, webhooks, magic links) van **sin** auth pero con verificación
  de token/firma propia y rate-limit. Ver `SECURITY.md`.

## MongoDB

- **PascalCase ↔ camelCase.** El C# usa PascalCase (`PingToken`, `NextDueAt`); al persistir se aplica
  `CamelCaseElementNameConvention`, así que en Mongo los campos van en camelCase (`pingToken`,
  `nextDueAt`). Tenlo en cuenta en queries directas, índices y agregaciones.
- Llama `MongoDbConfigurator.Configure()` **antes** de construir cualquier contexto/`IMongoClient` o de
  registrar convenciones BSON.
- Para colecciones grandes (CheckIns, NotificationLog) itera con cursor/`IAsyncEnumerable`; nunca
  `.ToList()` sobre colecciones de tamaño indeterminado (riesgo de `OutOfMemoryException`). Colecciones
  pequeñas (planes, rate cards) sí pueden cargarse enteras.
- `CheckIn` es **colección time-series**; respeta su clave de tiempo (`receivedAt`).

## Concurrencia del worker

- El motor de evaluación debe ejecutarse en **un único líder** (leader election con lease en Mongo) para
  no duplicar alertas al escalar réplicas. Las alertas son **idempotentes** por incidente/escalón.

## Tests

Ver `dotnet test`. Toda funcionalidad nueva o modificada debe tener cobertura: unit para lógica de
dominio (scheduling, tolerancias, wallet, idempotencia de alertas) e integración para repos Mongo. Las
features usables desde la web deberían tener test E2E (bUnit/Playwright) que comprueben **UI y BBDD**.
Añade `// Feature: FXX.YY` en la cabecera del archivo que implementa una feature documentada.

## Hardcodeos y tech-debt

- Cualquier hardcodeo se anota en `HARDCODED.md` (autor, fecha, motivo, condición de retirada,
  ubicación) y se marca en el código con `// HARDCODE (ver HARDCODED.md): …`.
- Al deprecar algo (`[Obsolete]`, `@deprecated`), añade fila en `tech-debt.md` (fecha límite = +2 meses).

## Documentación

Sigue `DOCUMENTATION-DIRECTIVES.md`. Cada feature tiene código `FXX.YY` y ficha en `docs/features/`.
No asumas la intención de negocio a partir del código: pregunta al desarrollador.
