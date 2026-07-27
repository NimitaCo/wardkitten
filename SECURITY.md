# Política de seguridad — Wardkitten

Wardkitten gestiona **PII** (emails, teléfonos), **pagos** (Stripe) y **saldo monetario** (wallet de
créditos). La seguridad es requisito de primera clase, no un extra. Este documento recoge las reglas
que todo cambio debe respetar.

## 1. Gestión de secretos

- **Ningún secreto en el repo.** Connection strings, claves Stripe/Twilio, JWT signing keys, service
  accounts de Firebase, etc. se inyectan por **variables de entorno** (dev) o **K8s Secret** (prod).
- Los manifiestos de `K8S/**/*.yaml` versionados usan **placeholders**; los valores reales se aplican
  fuera de git (`kubectl`/sealed-secrets/ArgoCD). `.gitignore` excluye `.env`, `*.pem`, `*.pfx`,
  `serviceAccountKey.json`, `appsettings.Secrets.json`.
- Rotación: las claves comprometidas se rotan de inmediato y se documenta el incidente en `docs/devlog/`.

## 2. Autenticación y autorización

- Passwords con **BCrypt** (work factor ≥ 12). Nunca en claro ni en logs.
- **JWT** de vida corta + **refresh tokens** rotatorios y revocables (persistidos hasheados).
- Verificación de **email** obligatoria; verificación de **teléfono por OTP** obligatoria antes de
  habilitar canales SMS/WhatsApp.
- Autorización por **rol y plan**: los límites de plan (nº de watches, intervalos mínimos, canales) se
  comprueban en el servidor, nunca solo en el cliente.

## 3. Endpoints públicos (sin sesión)

Tres familias de endpoints son públicas por diseño; cada una tiene su propia defensa:

| Endpoint | Defensa |
|---|---|
| **Ping** `/p/{token}` (+ `/start`, `/fail`) | `pingToken` = UUIDv4 **no adivinable** (128 bits). Rate-limit por token e IP. No revela existencia del watch a terceros. Un token en pruebas (F03.03) responde igual que uno real (`200`), sin distinguir para terceros si la vigilancia existe. |
| **Webhooks** Stripe/Twilio | **Verificación de firma** obligatoria (`Stripe-Signature`, `X-Twilio-Signature`) antes de procesar. Idempotencia por event id. |
| **Magic links** (ACK/Snooze/Done) | Token **firmado** (HMAC) con expiración corta y un solo uso; acción acotada al watch/incident. |

- **Rate-limiting** global y por endpoint (ASP.NET Core RateLimiter). Límites más estrictos en
  login, registro, OTP, ping y recargas.

## 4. Wallet / canales metered (anti-abuso)

- SMS y WhatsApp **descuentan créditos** de la wallet de forma **transaccional** (no se envía si el
  débito no se confirma; el envío y el `CreditTransaction` deben ser consistentes).
- Si `Wallet.balance < rate` del mensaje, el canal metered se **desactiva** para ese envío y se notifica
  al usuario por un canal gratis. No se permite saldo negativo.
- Verificación de teléfono (OTP) y límites de envío por minuto/día para evitar uso de Wardkitten como
  pasarela de spam SMS. Las recargas pasan por Stripe (no se acreditan créditos sin webhook firmado).

## 5. Datos y privacidad (GDPR)

- Minimización: se guarda solo lo necesario. El historial de check-ins/notificaciones tiene
  **retención configurable** (TTL en Mongo) por plan.
- Derechos del usuario: **export** y **borrado** de sus datos (cuenta, watches, historial).
- PII no se escribe en logs. Los logs no incluyen tokens, OTPs ni payloads sensibles.
- Datos en tránsito por **TLS**; en reposo según la configuración de Mongo del clúster.

## 6. Dependencias y supply chain

- Solo `nuget.org` como fuente (ver `nuget.config`). Revisa avisos de seguridad de NuGet/Dependabot.
- No introducir dependencias sin necesidad clara.

## 7. Reporte de vulnerabilidades

Las vulnerabilidades se reportan en privado al equipo (no en issues públicos) y se registran como
incidente en `docs/devlog/` con causa raíz y mitigación.
