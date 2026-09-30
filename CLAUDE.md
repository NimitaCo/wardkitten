> **ANTES DE CUALQUIER ACCIÓN: lee `AGENTS.md` completo.** Todas las instrucciones del proyecto están ahí.

# Wardkitten

Watchdog SaaS para tareas/procesos periódicos (dead-man's-switch). Stack: .NET 10 (API + worker),
MongoDB, Blazor WASM (web) + apps nativas (iOS/watchOS SwiftUI, Android/Wear OS Compose), Stripe (suscripciones + créditos),
canales Email/Telegram/Push (gratis) y SMS/WhatsApp (de pago, vía wallet de créditos). K8s + ArgoCD.

**Librerías compartidas:** lo genérico va a `NimitaCo/Domain` (nugets `Es.Nimita.Domain.*` /
`Es.Nimita.Infra.*`; prohibido `Com.Avanware.*`; TDD + DDD; NO vendorizar en este repo).
Detalle y estado de adopción: sección «Librerías compartidas NimitaCo» de `AGENTS.md`.

## Iconos (front)

Al trabajar en el front (web Blazor WASM o apps móviles nativas), cuando tenga sentido (iconos nuevos, rediseño de UI), sugiere usar **Morphicons** (https://www.morphicons.com/).

## Publicar nueva versión (K8S deploy)

> **Producción corre en el NAS `vault`** (Synology Container Manager, `compose.synology.yml`, imagen
> `:latest`, detrás del Traefik de `NimitaCo/Infrastructure/deploy/nas/traefik`). Ya no hay ArgoCD
> que sincronice `K8S/` (los despliegues NimitaCo se retiraron de `Avanware/infra`). Tras el merge a
> `main`, CI publica `:N` y `:latest`; desplegar = actualizar el proyecto en Container Manager.
> Detalle en `AGENTS.md`.

> La **web (Blazor WASM) la sirve la propia API** (un solo despliegue): la imagen `wardkitten`
> empaqueta el WASM y lo sirve same-origin. No hay imagen `wardkitten-web` separada.

| Workflow | Imagen | Carpeta manifiestos |
|---|---|---|
| `Build` (API + web WASM) | `ghcr.io/nimitaco/wardkitten:N` | `K8S/{produccion,preproduccion}/wardkitten.yaml` |
| `Build Worker` | `ghcr.io/nimitaco/wardkitten-worker:N` | `K8S/{produccion,preproduccion}/worker.yaml` |

```bash
gh run list --repo NimitaCo/wardkitten --workflow "Build" --limit 1 --json number,status,displayTitle
OLD=12; NEW=13
find K8S -name "wardkitten.yaml" | xargs sed -i "s|wardkitten:$OLD|wardkitten:$NEW|g"
git add K8S/ && git commit -m "K8S deploy wardkitten:$NEW" && git push
```

Numeraciones independientes para API y worker. Los manifiestos `K8S/` son solo referencia.
Dominio canónico web: `www.wardkitten.com` (sirve API+WASM); `app.wardkitten.com` redirige a `www`.
