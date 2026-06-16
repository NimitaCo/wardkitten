# Wardkitten.Mobile (MAUI Blazor Hybrid)

App iOS/Android que **reutiliza `Wardkitten.Shared.UI`** (cliente de API, estado de auth, componentes)
vía .NET MAUI Blazor Hybrid. El token se guarda en `SecureStorage` (no localStorage).

## Estado

Scaffold funcional: shell con login + dashboard (lista de watches + "Hecho"), reusando la capa de
cliente compartida. Pendiente (ver `tech-debt.md`):

- **Workload + assets**: `dotnet workload install maui`, Android SDK/JDK (y macOS + Xcode para iOS).
  Faltan iconos/splash/fonts de tienda y la firma.
- **Push (F09)**: `FcmTokenRegistrar` registra el token en la API, pero la obtención del token FCM por
  plataforma (Firebase SDK) está como TODO.
- Pantallas completas (alta/edición de watch, wallet) reutilizando las del proyecto web.

## Build (con la workload instalada)

```bash
dotnet build wardkitten.mobile.slnx -t:Run -f net10.0-android
```

No forma parte de `wardkitten.slnx` ni del CI del núcleo.
