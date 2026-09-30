# F05.05 — Vinculación de Telegram por deep link

## Metadata
- Estado: implementada
- Módulo: F05

## Descripción
Permite que el usuario reciba las alertas en su Telegram sin copiar a mano ningún `chat_id`. Desde la web
pide un código de un solo uso y abre `https://t.me/<bot>?start=<código>`; al pulsar **Start**, Telegram
entrega `/start <código>` al webhook de Wardkitten, que guarda el chat como destino por defecto del canal
Telegram (`User.TelegramChatId`) y responde en el chat «✅ Cuenta vinculada con Wardkitten».

## Elementos UI
- `Pages/Welcome.razor` (paso «Cómo avisarte»): «Vincular» → botón «Abrir Telegram» + código
  (`/start <código>`, por si se abre en otro dispositivo); consulta `telegram/status` cada 3 s hasta que queda
  vinculado. «Enviar prueba» y «Desvincular».

## Endpoints
- `POST /api/auth/telegram/link-code` (sesión, rate-limit `auth`) → `{ code, deepLink, expiresAtUtc }`.
- `GET /api/auth/telegram/status` (sesión) → `{ linked }` (no expone el chat).
- `POST /api/auth/telegram/unlink` (sesión): borra el chat y quita Telegram de los canales por defecto.
- `POST /telegram/webhook` (**público**, rate-limit `ping`): exige la cabecera
  `X-Telegram-Bot-Api-Secret-Token` igual a `TELEGRAM_WEBHOOK_SECRET` (comparación en tiempo constante; sin
  secreto configurado rechaza todo con 401). Procesa `message.chat.id` + `message.text`:
  - `/start <código>` (o `/start@bot <código>`) válido → vincula y confirma.
  - Código desconocido, usado o caducado → responde «❌ No he podido vincular tu cuenta…».
  - `/start` sin código → responde con instrucciones.
  - Cualquier otro mensaje → se ignora. Responde `200` a todo update válido para que Telegram no reintente
    (`400` si el cuerpo no es JSON).

## Modelo de datos (MongoDB, `users`)
`telegramLinkCodeHash` (hash del código; índice disperso `ix_users_telegramlink`), `telegramLinkExpiresUtc`,
`telegramChatId`.

## Reglas de negocio
- Código de 12 caracteres sin ambiguos (`a-z` sin `i/l/o`, `2-9`; ≈ 59 bits), guardado **hasheado** (mismo
  hash que los códigos de verificación de email) y con **15 minutos** de vida (ver `HARDCODED.md`). Un solo uso.
- Generar un código nuevo invalida el anterior.

## Configuración (variables de entorno)
| Variable | Dónde | Uso |
|---|---|---|
| `TELEGRAM_BOT_TOKEN` | Secret | Envío de mensajes (canal F05.01) y respuestas del bot. |
| `TELEGRAM_BOT_USERNAME` | ConfigMap | Usuario del bot sin `@`, para el deep link. Vacío = vinculación deshabilitada (la UI lo indica). |
| `TELEGRAM_WEBHOOK_SECRET` | Secret | Secreto de la cabecera del webhook (1–256 caracteres `A-Za-z0-9_-`). |

**Alta del webhook (una vez por bot y entorno).** Telegram solo entrega updates tras registrar la URL con el
mismo secreto:

```bash
curl -sS "https://api.telegram.org/bot${TELEGRAM_BOT_TOKEN}/setWebhook" \
  -d "url=https://www.wardkitten.com/telegram/webhook" \
  -d "secret_token=${TELEGRAM_WEBHOOK_SECRET}" \
  -d 'allowed_updates=["message"]'

# Comprobar el estado:
curl -sS "https://api.telegram.org/bot${TELEGRAM_BOT_TOKEN}/getWebhookInfo"
```

En preproducción, con su propio bot, la URL es `https://app-pre.wardkitten.com/telegram/webhook`.

## Dependencias / Sub-features
F05.01 (canal Telegram, `TelegramChannel`), F01.04 (asistente), F02.05 (canales por defecto).

---

# F05.06 — Envío de prueba por un canal

## Metadata
- Estado: implementada
- Módulo: F05

## Descripción
Botón «Enviar prueba» del asistente: manda «🐾 Wardkitten · mensaje de prueba» por un canal, con **la misma
implementación de canal (`INotificationChannel`) y la misma resolución de destino** que las alertas
(`ChannelDestinations`, compartida con `NotificationDispatcher`), para comprobar que llegan antes de depender
de ellas.

## Elementos UI
- `Pages/Welcome.razor` (`Onboarding/ChannelTestButton.razor` y botones junto a cada URL).

## Endpoints
- `POST /api/onboarding/channels/test` (sesión, rate-limit `auth`) con `{ binding }` → siempre `200` con
  `{ success, message }` (mensaje listo para enseñar).

## Reglas de negocio
- El binding pasa por las reglas de F02.05 (`ChannelBindingRules`) antes de enviar: por eso no se puede usar
  para sondear URLs internas (loopback, rangos privados).
- SMS/WhatsApp **no** se prueban: consumen créditos.
- Cada envío queda en `notificationLogs` con `kind = "test"`.

## Dependencias
F05.01 (canales), F02.05 (reglas de validación).
