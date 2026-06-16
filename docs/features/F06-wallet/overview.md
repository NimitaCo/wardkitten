# F06 — Wallet de créditos (canales metered)

## Metadata
- Estado: implementada
- Módulo: F06

## Descripción
SMS y WhatsApp tienen coste real y se pagan con una **wallet de créditos prepago independiente del plan**
(incluido Free). Sin saldo suficiente, esos canales se desactivan en el envío; los gratuitos siguen.

## Elementos UI
- `Pages/Wallet.razor` (saldo, recargas, movimientos, gestión de plan).

## Endpoints
- `GET /api/wallet`, `GET /api/wallet/transactions`, `POST /api/wallet/topup` (checkout de Stripe).

## Modelo de datos (MongoDB)
- `wallets`: `balanceCredits` (Decimal128), `minThresholdCredits`. `creditTransactions`: asientos con
  signo y `balanceAfter`, `idempotencyKey`. `channelRates`: tarifa por canal y prefijo E.164.

## Reglas de negocio
- **F06.01 Cobro atómico**: `TryDebitAsync` descuenta solo si hay saldo (findOneAndUpdate condicional, sin
  saldo negativo). Si el envío metered falla tras cobrar, se reembolsa.
- **F06.02 Idempotencia de recargas**: las recargas (webhook de Stripe) se aplican una sola vez por
  referencia de pago.
- Requiere teléfono verificado (OTP) antes de habilitar SMS/WhatsApp (ver SECURITY.md).

## Verificación
Tests: `WalletServiceTests` (cobro con saldo registra movimiento; sin saldo no envía ni registra).
