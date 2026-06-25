# Hardcodeos activos en Wardkitten

Registro de valores, IDs o ramas de comportamiento codificados a fuego que deberían ser configuración.

**Regla:** cualquier hardcodeo que introduzcas se anota aquí y se marca en el código con
`// HARDCODE (ver HARDCODED.md): …`. Se elimina de esta tabla cuando se retira del código. Cada entrada
indica: autor, fecha, motivo, condición de retirada y ubicación (`archivo:línea` o símbolo).

| # | Elemento | Autor | Fecha | Motivo | Condición de retirada | Ubicación |
|---|----------|-------|-------|--------|-----------------------|-----------|
| 1 | Secretos del despliegue de pruebas (cluster C) hardcodeados en el manifiesto: `MONGOSETTINGS_CONNECTION`, `JWT_SECRET`, `MAGICLINK_SECRET`, `INTERNAL_TOKEN` | Claude (dan) | 2026-06-25 | Despliegue rápido en entorno de pruebas sin gestor de secretos (sin sealed-secrets/vault); convención de la casa (igual que IntegraSystem) | Sustituir por secretos reales gestionados (sealed-secrets o vault) antes de producción | `Avanware/infra` → `Clusters/C/misc/wardkitten/wardkitten.yaml` (Secret `wardkitten-secrets`) |

## Detalle de los secretos hardcodeados (entorno de PRUEBAS)

Valores fijados en el Secret `wardkitten-secrets` del manifiesto de infra (`Avanware/infra/Clusters/C/misc/wardkitten/wardkitten.yaml`). El mismo Secret lo comparten la API/web y el worker vía `envFrom`.

| Clave | Valor | Origen |
|---|---|---|
| `MONGOSETTINGS_CONNECTION` | `mongodb://crm:e4B6Q2F!tKsnduJ5@10.200.0.10:2717,10.200.0.19:2717,10.200.0.68:2717,10.200.0.69:2717/?replicaSet=MongoReplica0` | Replica set de IntegraSystem (usuario `crm`). BBDD propia `Wardkitten` (en el ConfigMap `wardkitten-config`). |
| `JWT_SECRET` | `60907af0735214f0693522453eaa45ae2469d054bd84488d4f01548434037f0b` | Aleatorio inventado (`openssl rand -hex 32`). |
| `MAGICLINK_SECRET` | `e7f2316d4578af1548ce49e22346bb30634915c94d0a9b8512143b1d8f468cba` | Aleatorio inventado (`openssl rand -hex 32`). |
| `INTERNAL_TOKEN` | `e726906a9825044f9479d28f121a1283c2af1d9bdace171e` | Aleatorio inventado (`openssl rand -hex 24`). Compartido worker↔API. |

**Pendientes (vacíos a propósito, canales deshabilitados):** `SMTP_*`, `TELEGRAM_BOT_TOKEN`, `FCM_SERVICE_ACCOUNT_JSON`, `TWILIO_*`, `STRIPE_*`. No bloquean el arranque; cada canal queda inactivo hasta que se rellene con credenciales reales.
