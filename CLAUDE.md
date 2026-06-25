> **ANTES DE CUALQUIER ACCIÓN: lee `AGENTS.md` completo.** Todas las instrucciones del proyecto están ahí.

# Wardkitten

Watchdog SaaS para tareas/procesos periódicos (dead-man's-switch). Stack: .NET 10 (API + worker),
MongoDB, Blazor WASM (web) + .NET MAUI Blazor Hybrid (móvil), Stripe (suscripciones + créditos),
canales Email/Telegram/Push (gratis) y SMS/WhatsApp (de pago, vía wallet de créditos). K8s + ArgoCD.

## Publicar nueva versión (K8S deploy)

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
