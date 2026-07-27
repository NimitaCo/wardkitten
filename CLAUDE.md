> **ANTES DE CUALQUIER ACCIÓN: lee `AGENTS.md` completo.** Todas las instrucciones del proyecto están ahí.

# Wardkitten

Watchdog SaaS para tareas/procesos periódicos (dead-man's-switch). Stack: .NET 10 (API + worker),
MongoDB, Blazor WASM (web) + .NET MAUI Blazor Hybrid (móvil), Stripe (suscripciones + créditos),
canales Email/Telegram/Push (gratis) y SMS/WhatsApp (de pago, vía wallet de créditos). K8s + ArgoCD.

**Librerías compartidas:** lo genérico va a `NimitaCo/Domain` (nugets `Es.Nimita.Domain.*` /
`Es.Nimita.Infra.*`; prohibido `Com.Avanware.*`; TDD + DDD; NO vendorizar en este repo).
Detalle y estado de adopción: sección «Librerías compartidas NimitaCo» de `AGENTS.md`.

## Publicar nueva versión (K8S deploy)

> **⚠️ Sincronización de manifiestos K8S (temporal, hasta nueva orden):** los YAML de `K8S/` deben mantenerse **a la vez** en este repo **y** en el repo de infraestructura (`Avanware/infra/Clusters/C/misc/wardkitten/wardkitten.yaml`). Temporalmente es **infra** quien los publica (ArgoCD app `infra`, sync recursivo de `Clusters/C/`); todo cambio en un manifiesto de `K8S/` hay que replicarlo en su copia de infra o no se desplegará.

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

Numeraciones independientes para API y worker. Despliegue por ArgoCD (Synced + Healthy).
Dominio canónico web: `www.wardkitten.com` (sirve API+WASM); `app.wardkitten.com` redirige a `www`.
