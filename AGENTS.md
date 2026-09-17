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
mobile/ios                      # Apps nativas iOS + watchOS (SwiftUI)
mobile/android                  # Apps nativas Android + Wear OS (Kotlin/Compose)
test/Wardkitten.Tests           # unit + integration
```

## Ramas y entornos

- `main` → preproducción (staging).
- `Release` → producción.
- Nombres de rama ≤ 15 caracteres (el prefijo `codex/` no computa).
- **Commitea y pushea con frecuencia**: un commit/push sobrevive a cualquier reset del working tree;
  los cambios sin commitear, no. Si trabajas en otra rama sobre el mismo directorio, usa `git worktree`.

## Publicar nueva versión (K8S deploy)

> **⚠️ Sincronización de manifiestos K8S (temporal, hasta nueva orden):** los YAML de `K8S/` (`produccion/` y `preproduccion/`) deben mantenerse **a la vez** en este repo **y** en el repo de infraestructura (`Avanware/infra/Clusters/C/misc/wardkitten/wardkitten.yaml`). Temporalmente es **infra** quien los publica (ArgoCD app `infra`, sync recursivo de `Clusters/C/`); todo cambio en un manifiesto de `K8S/` hay que replicarlo en su copia de infra o no se desplegará.

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
`nimitaco.ghcr.io` (el `dockerconfigjson`; debe tener acceso de lectura a `ghcr.io/nimitaco`). Entornos en `K8S/produccion/` y `K8S/preproduccion/`. Despliegue por ArgoCD;
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
- No subas **credenciales** a `nuget.config`. El único feed privado permitido es `nimitaco`
  (Es.Nimita.*, ya declarado con *source mapping*); su credencial viaja SOLO por la variable de
  entorno `NuGetPackageSourceCredentials_nimitaco` (ver `ToDo.md`).

## Librerías compartidas NimitaCo (Es.Nimita.*) — política TDD + DDD

- Las librerías comunes viven en el repo **NimitaCo/Domain** y se distribuyen como paquetes
  NuGet `Es.Nimita.Domain.*` (dominio) y `Es.Nimita.Infra.*` (infraestructura), publicados
  en GitHub Packages (`nuget.pkg.github.com/NimitaCo`).
- **Prohibido `Com.Avanware.*`** (nugets de IntegraSystem/Avanware) en este repo.
- Campos del lenguaje común (NIF, email, teléfono, IBAN, dinero…) → value objects de
  `Es.Nimita.Domain.Primitives`, no strings sueltos. En el borde (API/BD) puede persistirse
  el valor plano; la validación/normalización se hace siempre con el value object.
- Funcionalidad genérica duplicada en dos repos → se promueve a Domain (con tests) y se consume
  desde allí; no se mantiene N veces.
- **TDD**: tests primero para funcionalidad nueva; cada bug se reproduce con un test antes del
  fix. **DDD**: puertos en dominio, adaptadores en infraestructura.
- Convenciones y glosario de toda la organización: repo `NimitaCo/Domain` →
  `docs/CONVENTIONS.md` y `docs/GLOSSARY.md`.

### Estado en wardkitten: **adoptado por `PackageReference` 26.7.5** (2026-07-28)

`Es.Nimita.Domain.Primitives` (en `Wardkitten.Application`) y `Es.Nimita.Infra.Mongo` (en
`Wardkitten.Infrastructure`) se consumen como paquetes NuGet del feed `nimitaco` declarado en
`nuget.config` (sin credenciales en el repo: se leen de la variable de entorno
`NuGetPackageSourceCredentials_nimitaco`; ver `ToDo.md` para las credenciales pendientes). Detalle:

- (a) `MongoDbConfigurator` es un wrapper fino sobre
  `MongoConventions.Register(MongoConventionOptions.Default)` — juego 1:1 con el histórico
  (camelCase + IgnoreExtra + IgnoreIfNull + enum como string + Decimal128, fechas UTC nativas).
  El `MongoSettings` local se sustituyó por el del paquete; su default de `DatabaseName` es
  cadena vacía, así que el registro DI conserva el default histórico **"Wardkitten"**.
  **Candado**: `MongoConventionsGuardTests` y `MongoSettingsRegistrationTests` protegen la forma
  BSON y el nombre de BBDD de producción — si fallan, PARAR.
- (b) El leasing local se sustituyó por `Es.Nimita.Infra.Mongo.Leasing`
  (`Lease`/`ILeaseStore`/`MongoLeaseStore`; el paquete los promovió desde este repo, semántica
  idéntica). Misma colección `leases` (vía `MongoContext.Leases`); el `MongoLeaseStore` del
  paquete recibe `TimeProvider`, adaptado desde `IClock` con
  `Wardkitten.Infrastructure.Time.ClockTimeProvider`.
- (c) `AuthService` valida el email del REGISTRO con `EmailAddress` y el teléfono del OTP con
  `PhoneNumber.TryParseSpanish` (normaliza a E.164). La normalización para buscar usuarios
  existentes sigue siendo trim + lowercase, sin pasar por el value object, para no bloquear
  logins de cuentas antiguas con emails raros.

**NO vendorizar** las librerías en este repo (copiar sus fuentes aquí): solo NBill y oildiagnosis
llevan copia vendored; wardkitten consume los paquetes NuGet del feed.

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
- `CheckIn` es una colección normal indexada por `watchId` + `receivedAtUtc` (antes time-series, que exigía MongoDB 5.0+).

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

## Bloqueos de MongoDB (CPU alta / COLLSCAN)

Ante saturación de MongoDB (CPU de `mongod` al 100%, timeouts `MaxTimeMSExpired`, `COLLSCAN` en el log del primario del replica set), sigue el runbook de auditoría y arreglo: [`RUNBOOK-Auditoria-Bloqueos-MongoDB.md`](RUNBOOK-Auditoria-Bloqueos-MongoDB.md).

## Explorar el código: `graphify` antes de barrer con `grep`

El repositorio lleva un **grafo de conocimiento** en `graphify-out/`. Para una pregunta amplia —quién
llama a X, qué se rompe si toco Y, por dónde pasa un flujo— consúltalo antes de encadenar `grep` y
lecturas: da la lista completa de llamadores en una sola llamada.

**Actualízalo antes de usarlo.** No hay hooks instalados, así que el grafo solo avanza cuando alguien
lanza el comando, y uno desfasado engaña dos veces: los símbolos añadidos después "no existen", y los
`fichero:Lnnn` que devuelve son los del commit con el que se construyó, así que apuntan a código que
ya se ha movido. La reextracción es determinista y no gasta LLM (cubre código, no documentos):

```powershell
graphify update .    # reescribe graph.json, manifest.json, GRAPH_REPORT.md y graph.html
```

De qué commit viene el grafo: campo `built_at_commit` en la raíz de `graph.json`.

```powershell
graphify explain "<Símbolo>"                          # vecindario del nodo: entrantes, salientes, fichero y línea
graphify affected "<id>" --relation calls --depth 1   # quién lo llama / qué se ve afectado
graphify path "<idA>" "<idB>"                         # camino más corto entre dos símbolos
graphify god-nodes --top 20                           # hubs arquitectónicos
graphify query "<pregunta>"                           # travesía a partir de una pregunta en lenguaje natural
```

- **Resolver el id es medio trabajo.** Si la etiqueta es única basta con ella; si se repite, `explain`
  responde `Ambiguous:` y te imprime los ids candidatos, mientras que `affected` solo dice `No unique
  node match`, así que saca antes el id con `explain`. El id de un método es el de su clase más el
  nombre en minúsculas. Pasar la ruta de un fichero devuelve el nodo *fichero*, no el de la clase.
- **`graphify query` rinde mal en grafos grandes** (más de ~10.000 nodos): trunca por presupuesto de
  tokens y devuelve ruido. Ahí prefiere `explain` y `affected`.
- **Para lo que la CLI no dé, ataca el JSON.** Viene indentado, así que `grep` va fino:
  `grep -B8 '"norm_label": "<clase-en-minúsculas>"' graphify-out/graph.json | grep '"id"'` lista los
  candidatos. Para recorridos, `Get-Content graphify-out\graph.json -Raw | ConvertFrom-Json` deja
  `.nodes` y `.links` como objetos. Los nodos traen `id` (lo que pide `affected`), `label`,
  `norm_label`, `source_file`, `source_location`, `community` y `file_type`; los enlaces tienen
  `relation`: los frecuentes son `calls`, `references`, `method`, `contains` e `imports`, y para
  jerarquías están `inherits`, `implements` y `extends`.
- **El grafo complementa a `grep`, no lo sustituye.** Solo extrae símbolos, llamadas y referencias: no
  ve asignaciones a propiedades ni literales, así que "quién escribe tal campo" sigue siendo un
  `grep`. Contrasta con el código antes de concluir.
- **Excepción a "no incluyas archivos generados en los commits": el grafo sí se versiona**, porque es
  el índice del que parten los agentes. Solo se ignoran `graphify-out/cache/` y las copias de
  seguridad con fecha que deja la reextracción. Sube los ficheros juntos (`git add graphify-out/`) y
  en un commit aparte de los cambios funcionales.
- `graphify-out/GRAPH_REPORT.md` resume comunidades, hubs y conexiones sorprendentes: es el mapa de
  partida cuando aterrizas en una zona del código que no conoces.

## Mutation testing obligatorio al escribir tests

Siempre que escribas o modifiques tests, mide su calidad con mutación antes de cerrar el cambio. La
cobertura no vale para esto: un test puede ejecutar una línea sin comprobar nada. La herramienta
introduce mutaciones en el código (invierte condiciones, cambia operadores, vacía métodos) y comprueba
si algún test falla; el porcentaje de mutantes matados es la medida real de si la suite te protege.

**Veredicto, igual en todos los lenguajes:** **≥80 %** los tests protegen el fichero; **60–80 %** mata
los supervivientes antes de seguir; **<60 %** los tests no protegen y el cambio pasa a ser un PR de
solo tests. No persigas el 100 %: hay mutantes equivalentes (los bordes `<=`/`<` contra la hora actual
son indistinguibles sin un reloj inyectable); cuando un superviviente sea equivalente, dilo en el PR y
sigue.

**Acota siempre la mutación** a los ficheros que tus tests ejercitan. Mutar un proyecto entero
recompila y ejecuta la suite una vez por mutante, y tarda horas.

### C# — Stryker.NET

```powershell
dotnet tool install -g dotnet-stryker     # una vez por máquina

cd <carpeta-del-proyecto-de-test>
dotnet stryker `
    --project <proyecto-a-mutar>.csproj `
    --mutate "**/<FicheroBajoTest>.cs" `
    --reporter progress --reporter html
```

- Stryker resuelve por **referencias de proyecto directas**: el `.csproj` de test debe referenciar
  directamente el proyecto a mutar aunque ya le llegue de forma transitiva. Mantén el patrón al crear
  proyectos de test nuevos.
- Revisa los supervivientes en `StrykerOutput/**/reports/mutation-report.html` y mata cada uno con un
  assert o un caso nuevo.
- Cuidado con los dobles de base de datos en memoria que guardan referencias vivas: releer la entidad
  tras la acción devuelve el mismo objeto ya mutado y **no demuestra que se persistió**. Observa la
  escritura por un campo centinela (`LastUpdate` reseteado antes de actuar) y afirma sobre él.

### Blazor — Stryker.NET sobre el code-behind, tests con bUnit

No existe un mutador específico para Blazor y no hace falta: los componentes son C#, así que la
herramienta es la misma Stryker.NET. La pega es que **Stryker.NET no muta el marcado `.razor`** y,
cuando lo intenta, suele dejar el proyecto sin compilar tras la mutación. Regla práctica:

- Saca la lógica del bloque `@code` a un code-behind `Componente.razor.cs` (clase parcial). Eso es lo
  que Stryker muta y lo que de verdad merece tests.
- Excluye el marcado del patrón de mutación: `--mutate "**/<Fichero>.razor.cs" --mutate "!**/*.razor"`.
- Testea los componentes con **bUnit**: son tests de C# normales, así que Stryker los ejecuta como
  cualquier otro y el veredicto de arriba se aplica igual.

### Angular / TypeScript — StrykerJS (no aplica hoy)

Este repositorio no contiene código TypeScript, así que **StrykerJS no es obligatorio aquí**. Si en
algún momento se añade un front Angular/TypeScript con tests unitarios, aplica su homólogo
**StrykerJS** (`@stryker-mutator/core`), acotando la mutación a los ficheros bajo test y con el mismo
veredicto de arriba. La receta completa (instalación y `stryker.config.json`) está en el `AGENTS.md`
de `NimitaCo/TravelInsight` o `NimitaCo/oildiagnosis`.
