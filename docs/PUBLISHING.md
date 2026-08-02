# Guía de publicación — Wardkitten (web + apps móviles)

Esta guía explica cómo publicar la **web** (Blazor WASM) y las **cuatro apps móviles nativas**:
iOS, watchOS, Android y Wear OS.

> **Punto de partida actual.** Las cuentas de desarrollador **ya existen**, a nombre de
> **Nimita Consulting S.L.**:
>
> | Tienda | Estado | Identificador |
> |---|---|---|
> | Google Play | Cuenta de organización creada y pagada | ID `5859106165437175812` |
> | App Store | Alta en revisión | Enrollment `U5KZN59GNR` |
>
> Identificador base de las apps: **`es.nimita.wardkitten`** (sustituye al antiguo
> `com.danwave.wardkitten`). **No se puede cambiar una vez publicada la app.**
>
> Al ser cuenta de **organización**, Google Play exime del test cerrado obligatorio de
> 12 testers durante 14 días que sí se aplica a las cuentas personales nuevas.

---

## 0. Estado actual del proyecto

- **Web**: lista para producción. La **sirve la propia API** (Blazor WASM empaquetado en la imagen
  `wardkitten`, same-origin); no hay imagen `wardkitten-web` separada. Manifiestos K8s ya existen.
- **Móvil**: cuatro apps nativas en `mobile/`. Se retiró la app MAUI; ver
  [ADR](architecture/ADR-mobile-nativo.md).

| App | Ubicación | Estado |
|---|---|---|
| Android | `mobile/android/app` | Esqueleto compilable |
| Wear OS | `mobile/android/wear` | Esqueleto compilable, standalone |
| iOS | `mobile/ios` | Paquete `WardkittenKit` listo; targets de app pendientes |
| watchOS | `mobile/ios` | Pendiente |

Pendientes en `tech-debt.md`: contratos duplicados a mano, push por plataforma (APNs y FCM),
y assets e iconos de tienda.

---
## 1. WEB (Blazor WASM) — la más sencilla

La web es **Blazor WASM servido por la propia API** (`UseBlazorFrameworkFiles`): un **único** despliegue
(imagen `wardkitten`) sirve el WASM y la API en el **mismo origen**, así que la web no necesita CORS ni
una `ApiBaseUrl` cruzada (`appsettings.Production.json` lleva `ApiBaseUrl` vacío → mismo origen).

### 1.1 Probar en local

```bash
# Opción A: solo Mongo en Docker y la API (que ya sirve la web) desde el IDE
docker compose up -d
dotnet run --project src/Wardkitten.Api      # API + web en http://localhost:5080

# Opción B: todo el stack en contenedores
docker compose --profile app up --build      # API+web :5080, worker, Mongo
```

> Para iterar solo en la web con hot-reload puedes seguir levantando el dev-server del WASM aparte
> (`dotnet run --project src/Wardkitten.Web`), que usa `appsettings.json` (apunta a la API en `:5080`).

### 1.2 Publicar la imagen a GHCR (automático con CI)

Al hacer push a `main`, el workflow **Build** (`.github/workflows/build-api.yml`) construye y publica
`ghcr.io/nimitaco/wardkitten:<nº-de-build>` — **esa imagen ya incluye el WASM**. El workflow se dispara
también con cambios en `src/Wardkitten.Web/**` y `src/Wardkitten.Shared.UI/**`. Autenticación vía
`GITHUB_TOKEN` (no requieren secrets extra).

Build manual (si quieres construir a mano):

```bash
docker build -f src/Wardkitten.Api/Dockerfile -t ghcr.io/nimitaco/wardkitten:test .
docker push ghcr.io/nimitaco/wardkitten:test
```

### 1.3 Desplegar en Kubernetes

Los manifiestos están en `K8S/produccion/` y `K8S/preproduccion/`.

1. **Pull secret** de GHCR (una vez por namespace), para que el clúster pueda bajar la imagen privada
   (si el paquete `ghcr.io/nimitaco` es público, este paso no hace falta):

   ```bash
   kubectl create namespace wardkitten
   kubectl -n wardkitten create secret docker-registry nimitaco.ghcr.io \
     --docker-server=ghcr.io \
     --docker-username=<usuario con acceso a ghcr.io/nimitaco> \
     --docker-password=<PAT read:packages>
   ```

2. **Secretos de la app** (Mongo, JWT, etc.) — los `K8S/**` usan placeholders `REPLACE_ME`. Sustitúyelos
   por valores reales **fuera de git** (sealed-secrets o `kubectl edit secret`). Como mínimo:
   `MONGOSETTINGS_CONNECTION`, `JWT_SECRET`, `MAGICLINK_SECRET`, `INTERNAL_TOKEN`.

3. **DNS + TLS**: apunta `www.wardkitten.com` (canónico), `app.wardkitten.com` (redirige a `www`) y
   `api.wardkitten.com` al ingress. Para HTTPS, instala `cert-manager` y añade un `ClusterIssuer`
   (Let's Encrypt) + anotación TLS al `Ingress`. El ingress enruta `www.*` y `api.*` → servicio
   `wardkitten` (mismo proceso) y redirige `app.*` → `www.*`.

4. **Aplicar** (o dejar que **ArgoCD** lo sincronice):

   ```bash
   kubectl apply -f K8S/produccion/
   ```

5. **Subir versión** (deploy de una imagen nueva): ver `CLAUDE.md` → "Publicar nueva versión".

### 1.4 Dominios y orígenes

La web va **same-origin** con la API, así que no hay `API_BASE_URL` que configurar. El ConfigMap define
`PUBLIC_BASE_URL`/`CORS_ORIGINS` = `https://www.wardkitten.com` (prod) y `https://app-pre.wardkitten.com`
(pre). Si cambias dominios, actualízalos ahí. CORS solo es relevante para la **app móvil** (cliente
nativo cross-origin contra `api.wardkitten.com`).

### 1.5 Verificación

- `https://www.wardkitten.com` carga la web (y `https://app.wardkitten.com` redirige 308 a `www`).
- `https://www.wardkitten.com/health` (o `https://api.wardkitten.com/health`) responde `{"status":"ok"}`.
- `https://api.wardkitten.com/swagger` muestra la documentación de la API.

---


## 2. MÓVIL — preparación común

### 2.1 Herramientas

| Herramienta | Para qué | Nota |
|---|---|---|
| **Android Studio** | Android y Wear OS | Incluye SDK y JDK |
| **Xcode** | iOS y watchOS | Solo macOS. En Macs Intel, la última serie compatible es Xcode 26 |

### 2.2 Assets pendientes

Ninguna de las cuatro apps trae icono ni splash. Hacen falta, con los tamaños propios de cada
plataforma, antes de poder subir nada a las tiendas.

---

## 3. ANDROID y WEAR OS

### 3.1 Clave de subida (una sola vez)

Google usa **Play App Signing**: Google custodia la clave que firma la app que instalan los
usuarios, y tú firmas lo que subes con una **clave de subida** propia.

```bash
"/Applications/Android Studio.app/Contents/jbr/Contents/Home/bin/keytool" \
  -genkeypair -v \
  -keystore ~/Documents/nimita-upload.keystore \
  -alias nimita-upload \
  -keyalg RSA -keysize 4096 -validity 10000 \
  -dname "CN=Nimita Consulting S.L., O=Nimita Consulting S.L., L=San Sebastian de los Reyes, ST=Madrid, C=ES"
```

> ⚠️ El fichero **no está en ningún servidor y no se puede regenerar**. Copia de seguridad
> obligatoria y contraseña en el gestor. Si se pierde, hay que pedir a Google un reseteo de la
> clave de subida. `*.keystore` está bloqueado en `.gitignore`.

Las credenciales se referencian desde un `keystore.properties` local, también ignorado por git.

### 3.2 Compilar el bundle

```bash
cd mobile/android
./gradlew :app:bundleRelease     # teléfono
./gradlew :wear:bundleRelease    # reloj
```

Play acepta **AAB**, no APK. Un único listado sirve a ambos form factors: Play entrega a cada
dispositivo el módulo que le corresponde.

### 3.3 Subir

Play Console → **Producción** o **Prueba interna** → subir el `.aab`.

La prueba interna admite hasta 100 testers por correo y está disponible en minutos, sin revisión.
Es la vía natural para las primeras iteraciones.

### 3.4 Requisitos de ficha

- **Política de privacidad accesible por URL** — obligatoria, sin ella no se publica
- **Formulario de Seguridad de los datos** — debe coincidir *exactamente* con lo que declare la
  política de privacidad; una discrepancia es motivo de rechazo
- **URL de eliminación de cuenta** si las apps permiten registro de usuarios
- Capturas por form factor, incluido el reloj

---

## 4. iOS y watchOS

### 4.1 Requisito previo

El alta de Nimita en el Apple Developer Program debe estar **aprobada**. Sin membresía activa no
hay certificados de distribución ni TestFlight.

### 4.2 Certificados

Con la cuenta activa, lo más simple es dejar que Xcode gestione la firma:
**Signing & Capabilities → Automatically manage signing**, eligiendo el equipo de Nimita.

> ⚠️ **Exporta cada certificado como `.p12` en el momento de crearlo** y guárdalo en el gestor de
> contraseñas. La clave privada vive en el Llavero y **no se puede recuperar después**. Es
> imprescindible si vas a cambiar de máquina.
>
> Para varias máquinas conviviendo, considera `fastlane match`: guarda los certificados cifrados
> en un repo git privado y los instala con un comando.

### 4.3 Archivar y subir

Xcode → **Product → Archive** → **Distribute App** → **App Store Connect**.

La app de watchOS es **independiente** (`WKRunsIndependentlyOfCompanionApp`), así que aparece por
separado en la App Store del reloj, pero se sube en el mismo archivo.

### 4.4 TestFlight

Una vez procesado el build, App Store Connect → TestFlight. Hasta 100 testers internos sin
revisión; los grupos externos pasan una revisión ligera.

### 4.5 Requisitos de ficha

- **Etiquetas de privacidad** (App Privacy), coherentes con la política publicada
- Capturas por dispositivo, incluido Apple Watch
- **Paid Applications Agreement** aceptado si va a haber compras o suscripciones

---

## 5. Automatizar en CI

Pendiente. Cuando se aborde, lo razonable es:

- **Android**: Gradle en un runner Linux, con el keystore inyectado como secreto
- **iOS**: runner macOS y una **App Store Connect API Key** (`.p8`), que es portable entre
  máquinas, en lugar de certificados atados a un equipo concreto

---

## 6. Checklist de primera publicación

- [ ] Iconos y splash de las cuatro apps
- [ ] Política de privacidad publicada y accesible por URL
- [ ] Formulario de Seguridad de los datos de Play, coherente con la política
- [ ] Etiquetas de privacidad de App Store, coherentes con la política
- [ ] Clave de subida de Android generada, con copia y contraseña guardadas
- [ ] Alta de Apple aprobada y certificados exportados como `.p12`
- [ ] Push configurado: APNs en Apple, FCM en Google
- [ ] Capturas por plataforma, relojes incluidos
