# Apps móviles nativas

Sustituyen a `src/Wardkitten.Mobile` (MAUI Blazor Hybrid). Ver [ADR](../docs/architecture/ADR-mobile-nativo.md).

Identificador base: **`es.nimita.wardkitten`**

## Estado

| Plataforma | Ubicación | Estado |
|---|---|---|
| iOS | `ios/` | Paquete `WardkittenKit` creado · targets de app pendientes del asistente de Xcode |
| watchOS | `ios/` | Pendiente del asistente |
| Android | `android/app` | Esqueleto compilable |
| Wear OS | `android/wear` | Esqueleto compilable, `standalone` |

## Android

Abrir `android/` con Android Studio. Falta el Gradle wrapper, que genera el propio IDE al abrir.

```
android/
├── app/    Teléfono · minSdk 26
├── wear/   Reloj · minSdk 30 · standalone
└── core/   Cliente de API compartido
```

## iOS + watchOS

`ios/WardkittenKit` ya existe con el contrato de API. Los targets de app se crean con el asistente:

**File → New → Project → iOS → App** · Product Name `Wardkitten` · Organization Identifier `es.nimita` · SwiftUI · Swift.

Después **File → New → Target → watchOS → App**, y en su `Info.plist`:

```
WKRunsIndependentlyOfCompanionApp = YES
```

Añadir `WardkittenKit` como paquete local a ambos targets.

## Contratos

Los DTO viven en `src/Wardkitten.Shared.Contracts` (.NET). Kotlin y Swift los replican **a mano**: no hay generación automática. Cualquier cambio en la API hay que reflejarlo en los tres sitios.

Es la deuda que asumimos al pasar de MAUI —que compartía contratos por referencia de proyecto— a nativo.
