# F03.03 — Banco de pruebas de la URL de ping (dry-run)

## Metadata
- Estado: implementada
- Módulo: F03

## Descripción
Permite comprobar, **mientras se está creando o editando** una vigilancia, que las solicitudes del sistema
remoto llegan de verdad — sin que esas solicitudes cuenten como confirmación. La URL con la que se ensaya
es **exactamente la misma** que después resetea los contadores en producción: en el alta se reserva el
token y la vigilancia lo adopta al guardar, así que lo que el usuario deje configurado en su cron, su CI o
su script ya no hay que tocarlo.

Dos modos, según haya o no URL todavía:

| Modo | Cuándo | Qué pasa con la URL |
|---|---|---|
| `Draft` | Alta sin guardar, o vigilancia manual que se está convirtiendo a ping | Aún no pertenece a ninguna vigilancia: las solicitudes solo se registran en el banco. Se abre solo al elegir el tipo «Ping». |
| `DryRun` | Vigilancia ya guardada con URL | Ventana corta (15 min por defecto, 60 máx.) en la que su URL real **deja de contar** y la vigilancia **no se evalúa**, para que el ensayo no dispare alertas. Requiere pulsarlo explícitamente. |

## Elementos UI
- `Pages/WatchEdit.razor`: tarjeta «🧪 Comprobar que llegan las solicitudes» con la URL (+ copiar y ejemplo
  `curl`), la hora de la última solicitud («hace N s») y un desplegable con el historial. Refresco cada 3 s
  mientras hay prueba en curso; cada 15 s si solo se mira el histórico real.
- `Pages/Home.razor`: distintivo 🧪 en las vigilancias con un ensayo en curso (no están contando).

## Endpoints
- `POST /api/ping-tests` — abre la prueba (`watchId` opcional, `minutes` opcional). Devuelve token, URL,
  modo, caducidad e historial.
- `GET /api/ping-tests/{probeId}` — estado + historial. Renueva la caducidad de los borradores.
- `DELETE /api/ping-tests/{probeId}` — termina la prueba (cierra el dry-run y borra el banco).
- El ping público no cambia de ruta: `GET/POST /p/{token}` (+ `/start`, `/fail`). Responde `200` en ambos
  casos, con `mode: "live"` o `mode: "test", counted: false`.

## Modelo de datos (MongoDB, `pingProbes`)
`PingProbe`: `userId`, `token`, `mode`, `watchId?`, `expiresAtUtc`, `lastHitAtUtc`, `hitCount`,
`hits[]` (últimas 50: `receivedAtUtc`, `method`, `kind`, `remoteIp`, `userAgent`, `payload` ≤ 2 KB).
Índices: `ux_pingprobe_token` (único), `ix_pingprobe_watch`, `ix_pingprobe_user` y **`ttl_pingprobe_expiry`**
(TTL con caducidad inmediata sobre `expiresAtUtc`).

En `watches` se añade `testModeUntilUtc`: fin de la ventana de ensayo (null = operación normal).

## Reglas de negocio
- **Auto-borrado.** El banco caduca solo: borrador 2 h (renovadas en cada refresco mientras la pantalla
  siga abierta), dry-run = ventana + 1 h de cortesía para el historial. El índice TTL de Mongo lo borra; las
  consultas filtran además por caducidad porque el barrido TTL corre cada ~60 s. Salir de la pantalla o
  guardar cierra la prueba explícitamente; el TTL es solo la red de seguridad.
- **Nunca se pierde un check-in real.** El ping resuelve primero la vigilancia (camino de producción, una
  sola consulta) y solo consulta el banco si está en modo prueba o si el token aún no es de nadie. Si la
  ventana está marcada pero el banco ya no existe, el ping **cuenta**.
- **El ensayo no genera falsas alarmas.** Mientras dura, `IsActiveForEvaluation` es falso. Al terminarlo
  (manual, al guardar, o al expirar: lo cierra el motor de evaluación) se reprograma el próximo vencimiento
  si el deadline pasó durante la prueba.
- **Adopción del token.** `WatchRequest.pingProbeId` → la vigilancia nace con el token ensayado. Si el
  borrador caducó o se guarda como manual, se descarta y se genera token nuevo.
- Máximo 5 bancos vivos por usuario (se descartan los más antiguos). Los tokens son de 128 bits y comparten
  el rate-limit `ping` (ver `SECURITY.md`).

## Dependencias / Sub-features
F02.01 (watch y su `pingToken`), F03.01 (check-in por ping), F04.03 (motor de evaluación: cierra los
ensayos caducados), F08.01 (pantalla de alta/edición).
