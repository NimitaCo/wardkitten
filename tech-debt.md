# Tech debt — Wardkitten

Registro de elementos obsoletos/deprecados y deuda técnica conocida.

**Regla:** al deprecar algo (`[Obsolete]`, `@deprecated`), añade una fila con fecha de deprecación,
fecha límite de retirada (deprecación **+ 2 meses**) y sustituto. Las entradas se retiran como máximo a
los 2 meses si no quedan consumidores activos.

## Deprecaciones

| Elemento | Tipo | Deprecado | Fecha límite | Sustituto | Notas |
|----------|------|-----------|--------------|-----------|-------|
| — | — | — | — | — | — |

## Advisories de dependencias aceptados

| Advisory | Paquete | Estado | Justificación | Revisión |
|----------|---------|--------|---------------|----------|
| GHSA-6c8g-7p36-r338 | SharpCompress (transitiva de MongoDB.Driver) | Suprimido por ID en `Directory.Build.props` | Zip-slip en `WriteToDirectory`; sin fix upstream (afecta a todas las versiones). **No explotable**: Wardkitten no extrae archivos a disco con SharpCompress. | Reevaluar al actualizar `MongoDB.Driver` o cuando haya versión parcheada. |

## Pendientes bloqueados por terceros / herramientas (no realizables en código)

Estos pendientes no dependen de escribir código en el repo, sino de herramientas, cuentas externas o
secretos. Quedan documentados para retomarlos en el entorno adecuado.

| Tema | Descripción | Bloqueo |
|------|-------------|---------|
| Contratos duplicados a mano | Al pasar de MAUI a nativo, los DTO dejan de compartirse por referencia de proyecto. Hay tres copias: C# (`Shared.Contracts`), Kotlin (`mobile/android/core`) y Swift (`mobile/ios/WardkittenKit`). Nada impide que diverjan. Mitigación: generarlos desde el OpenAPI. | Decisión pendiente |
| Targets de app iOS/watchOS | `mobile/ios/WardkittenKit` existe, pero los targets de aplicación hay que crearlos con el asistente de Xcode. | Requiere macOS + Xcode |
| Assets y firma de tienda | Iconos, splash y material de firma para las cuatro apps. | Assets pendientes |
| Push por plataforma | El endpoint `POST /api/auth/push-tokens` sigue vigente. Hay que obtener el token en cada app nativa: APNs en iOS/watchOS y FCM en Android/Wear OS. El `FcmTokenRegistrar` de MAUI se retiró con el proyecto. | Proyecto Firebase + certificados APNs |
| Plantillas WhatsApp | Las plantillas de mensaje de WhatsApp deben aprobarse en **Meta Business** antes de usarse en prod. | Aprobación externa de Meta |
| Secretos de producción | Los `K8S/**` usan placeholders (`REPLACE_ME`); cargar los secretos reales por canal seguro (sealed-secrets/ArgoCD), nunca en git. | Operativo (no es código) |

> Nota: status pages, equipos/on-call e integraciones salientes (Webhook/Slack/Discord/Microsoft Teams)
> ya están **implementadas** (antes figuraban como esbozo en esta tabla).
