# ADR · Apps móviles nativas en lugar de MAUI

**Estado:** Aceptada · 2 de agosto de 2026
**Sustituye a:** F09 · App móvil (MAUI Blazor Hybrid)

## Contexto

El repositorio tenía `src/Wardkitten.Mobile`, una app .NET MAUI Blazor Hybrid para iOS y Android que reutilizaba `Wardkitten.Shared.UI` con el cliente de API, la autenticación y varios componentes Razor. En el registro de features estaba marcada como **scaffold**, no como implementada.

El requisito nuevo es cubrir **cuatro** plataformas: iOS, watchOS, Android y Wear OS.

## El problema

**MAUI no soporta watchOS ni Wear OS.** Y para un watchdog, el reloj no es un extra: recibir el aviso y confirmar el check-in desde la muñeca es probablemente el gesto más valioso del producto. Obligar a sacar el teléfono para pulsar "hecho" desaprovecha el caso de uso principal.

Con MAUI, los relojes habría que escribirlos igualmente en SwiftUI y Compose for Wear OS. Y si ya vas a mantener dos toolchains nativos, el argumento de la interfaz compartida pierde casi todo su peso: quedaría un único proyecto compartido (los dos teléfonos) frente a dos proyectos nativos, con la complejidad de mantener MAUI viva a cambio.

## Decisión

**Cuatro apps nativas.** SwiftUI para iOS y watchOS, Kotlin y Compose para Android y Wear OS. Se retira `src/Wardkitten.Mobile`.

Identificador base: **`es.nimita.wardkitten`**, sustituyendo a `com.danwave.wardkitten`. El repo está en NimitaCo y las cuentas de App Store y Google Play son de Nimita Consulting S.L. El identificador **no se puede cambiar una vez publicada la app**, así que se corrige antes de la primera publicación.

## Consecuencias

**A favor**

- Los relojes dejan de ser un caso imposible
- Acceso directo a complicaciones, Tiles, notificaciones nativas y push de cada plataforma
- Sin dependencia de que MAUI siga el ritmo de Apple y Google

**En contra, y no es menor**

- **Los contratos se replican a mano.** MAUI compartía los DTO por referencia de proyecto con `Wardkitten.Shared.Contracts`. Ahora hay tres copias —C#, Kotlin y Swift— sin nada que garantice que no divergen. Es la deuda real de esta decisión.
- Cuatro apps que mantener y publicar en lugar de una
- `Wardkitten.Shared.UI` pierde a su segundo consumidor y queda al servicio solo de la web

**Mitigación pendiente de decidir:** generar los DTO de Kotlin y Swift desde el esquema OpenAPI de la API, en lugar de escribirlos a mano. Merece la pena en cuanto los contratos crezcan.

## Alternativa descartada

Mantener MAUI para los teléfonos y añadir solo los relojes en nativo. Habría preservado el trabajo existente, pero deja tres tecnologías de interfaz conviviendo (Blazor, SwiftUI y Compose) y el ahorro es pequeño cuando la app de MAUI está en estado de scaffold.
