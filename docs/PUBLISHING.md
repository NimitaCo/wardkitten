# Guía de publicación — Wardkitten (web + apps móviles)

Esta guía explica, paso a paso, cómo publicar la **web** (Blazor WASM) y las **apps móviles**
(.NET MAUI Blazor Hybrid: Android e iOS).

> **Supuesto de partida:** **no** tienes cuentas de Google Play ni de la App Store. Por eso esta guía
> prioriza las vías de **prueba privada** (distribución directa y programas de testers) y deja la
> publicación en tiendas como paso opcional posterior. Resumen rápido de qué se puede hacer sin cuenta:
>
> | Plataforma | Sin cuenta de tienda | Para distribuir a testers/tienda |
> |---|---|---|
> | **Web** | ✅ Total (es tu infraestructura: Docker + Kubernetes) | — |
> | **Android** | ✅ APK firmado directo **o** Firebase App Distribution (gratis) | Google Play Console = **25 $ pago único** |
> | **iOS** | ⚠️ Solo build de desarrollo en **tu propio** dispositivo (Xcode + Apple ID gratis, caduca a los 7 días) | **Apple Developer Program = 99 $/año** (obligatorio para TestFlight/ad-hoc) |
>
> Es decir: **iOS para otros testers requiere sí o sí los 99 $/año de Apple.** No hay forma de
> distribuir un build de iOS a terceros sin ese programa. Lo detallo en la sección 4.

---

## 0. Estado actual del proyecto (qué falta antes de un release real)

- **Web**: lista para producción (Dockerfile + nginx + manifiestos K8s ya existen).
- **Móvil** (`src/Wardkitten.Mobile`, solución aparte `wardkitten.mobile.slnx`): es un **scaffold** que
  reutiliza `Wardkitten.Shared.UI`. **Antes de compilar** necesita: la *workload* de MAUI, los SDKs, y
  unos **assets** (icono, splash, fuente) que el scaffold no incluye. Ver §2.
- Pendientes funcionales del móvil (ver `tech-debt.md`): integración real del token **FCM** por
  plataforma (push) y los assets/firma de tienda.

---

## 1. WEB (Blazor WASM) — la más sencilla

La web es WASM estática servida por **nginx**. La `ApiBaseUrl` se inyecta en **arranque** del contenedor
desde la variable `API_BASE_URL` (una sola imagen sirve todos los entornos).

### 1.1 Probar en local

```bash
# Opción A: solo Mongo en Docker y la web/API desde el IDE
docker compose up -d
dotnet run --project src/Wardkitten.Api      # API en http://localhost:5080
dotnet run --project src/Wardkitten.Web      # Web en su puerto de dev

# Opción B: todo el stack en contenedores
docker compose --profile app up --build      # Web :8080, API :5080
```

### 1.2 Publicar la imagen a GHCR (automático con CI)

Al hacer push a `main`, el workflow **Build Web** (`.github/workflows/build-web.yml`) construye y publica
`ghcr.io/avanware/wardkitten-web:<nº-de-build>`. Requiere dos *secrets* en el repo de GitHub
(`Settings → Secrets and variables → Actions`):

- `GHCR_USERNAME` — usuario con permiso de escritura en `ghcr.io/avanware`.
- `GHCR_TOKEN` — PAT con scope `write:packages`.

Build manual (si quieres construir a mano):

```bash
docker build -f src/Wardkitten.Web/Dockerfile -t ghcr.io/avanware/wardkitten-web:test .
docker push ghcr.io/avanware/wardkitten-web:test
```

### 1.3 Desplegar en Kubernetes

Los manifiestos están en `K8S/produccion/` y `K8S/preproduccion/`.

1. **Pull secret** de GHCR (una vez por namespace), para que el clúster pueda bajar la imagen privada:

   ```bash
   kubectl create namespace wardkitten
   kubectl -n wardkitten create secret docker-registry avanware.ghcr.io \
     --docker-server=ghcr.io \
     --docker-username=<usuario> \
     --docker-password=<PAT read:packages>
   ```

2. **Secretos de la app** (Mongo, JWT, etc.) — los `K8S/**` usan placeholders `REPLACE_ME`. Sustitúyelos
   por valores reales **fuera de git** (sealed-secrets o `kubectl edit secret`). Como mínimo:
   `MONGOSETTINGS_CONNECTION`, `JWT_SECRET`, `MAGICLINK_SECRET`, `INTERNAL_TOKEN`.

3. **DNS + TLS**: apunta `app.wardkitten.com` y `api.wardkitten.com` al ingress. Para HTTPS, instala
   `cert-manager` y añade un `ClusterIssuer` (Let's Encrypt) + anotación TLS al `Ingress` (el ingress ya
   enruta `app.*` → web y `api.*` → API).

4. **Aplicar** (o dejar que **ArgoCD** lo sincronice):

   ```bash
   kubectl apply -f K8S/produccion/
   ```

5. **Subir versión** (deploy de una imagen nueva): ver `CLAUDE.md` → "Publicar nueva versión".

### 1.4 `API_BASE_URL` por entorno

El deployment de la web (`K8S/**/web.yaml`) define `API_BASE_URL`. La web apunta a esa URL de la API.
Producción: `https://api.wardkitten.com`; preproducción: `https://api-pre.wardkitten.com`. (CORS ya está
configurado en la API para esos orígenes; si cambias dominios, actualiza `CORS_ORIGINS` en el ConfigMap.)

### 1.5 Verificación

- `https://app.wardkitten.com` carga la web.
- `https://api.wardkitten.com/health` responde `{"status":"ok"}`.
- `https://api.wardkitten.com/swagger` muestra la documentación de la API.

---

## 2. MÓVIL — preparación común (Android + iOS)

### 2.1 Instalar herramientas

```bash
# Workload de MAUI (descarga grande)
dotnet workload install maui

# Android: necesita el Android SDK + un JDK (17+). Opciones:
#  - Visual Studio 2022 / VS Code con la extensión .NET MAUI (instala SDK/JDK por ti), o
#  - dotnet android sdk:  dotnet workload install android  + Android Studio para el SDK manager.
# iOS: SOLO compila en macOS con Xcode instalado (no se puede compilar iOS en Windows/Linux).
```

Comprueba que la workload está:

```bash
dotnet workload list      # debe aparecer "maui" (o maui-android / maui-ios)
```

### 2.2 Assets que debes añadir antes de compilar

El scaffold **no** trae icono/splash/fuente. Crea estos archivos y referéncialos en
`src/Wardkitten.Mobile/Wardkitten.Mobile.csproj`:

```
src/Wardkitten.Mobile/Resources/AppIcon/appicon.svg        # icono base
src/Wardkitten.Mobile/Resources/AppIcon/appiconfg.svg      # primer plano del icono
src/Wardkitten.Mobile/Resources/Splash/splash.svg          # splash
src/Wardkitten.Mobile/Resources/Fonts/OpenSans-Regular.ttf # fuente referenciada en MauiProgram
```

Y añade al `.csproj` (dentro de un `<ItemGroup>`):

```xml
<MauiIcon Include="Resources/AppIcon/appicon.svg" ForegroundFile="Resources/AppIcon/appiconfg.svg" Color="#0f172a" />
<MauiSplashScreen Include="Resources/Splash/splash.svg" Color="#0f172a" BaseSize="128,128" />
<MauiFont Include="Resources/Fonts/*" />
```

> Si prefieres no añadir la fuente todavía, elimina la línea `.ConfigureFonts(...)` de `MauiProgram.cs`.

### 2.3 Configuración de la app

- **URL de la API**: en `src/Wardkitten.Mobile/MauiProgram.cs`, `apiBaseUrl` está fijado a
  `https://api.wardkitten.com`. Cámbialo si tu API está en otro host.
- **ApplicationId**: `com.danwave.wardkitten` (en el `.csproj`). Debe ser único y estable.
- **Versión**: `ApplicationDisplayVersion` (1.0) y `ApplicationVersion` (entero incremental) en el `.csproj`.

---

## 3. ANDROID

### 3.1 Crear el keystore de firma (una vez)

```bash
keytool -genkeypair -v -keystore wardkitten.keystore \
  -alias wardkitten -keyalg RSA -keysize 2048 -validity 10000
# Guarda la contraseña y el .keystore en lugar seguro (NO en git).
```

### 3.2 Compilar el release firmado

**APK** (un solo archivo, ideal para distribución directa / Firebase):

```bash
dotnet publish src/Wardkitten.Mobile -c Release -f net10.0-android \
  -p:AndroidPackageFormat=apk \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=$PWD/wardkitten.keystore \
  -p:AndroidSigningKeyAlias=wardkitten \
  -p:AndroidSigningStorePass=<storepass> \
  -p:AndroidSigningKeyPass=<keypass>
# Salida: src/Wardkitten.Mobile/bin/Release/net10.0-android/publish/com.danwave.wardkitten-Signed.apk
```

**AAB** (Android App Bundle, formato para Google Play): igual pero `-p:AndroidPackageFormat=aab`.

### 3.3 Prueba privada SIN cuenta de Play

**Opción A — APK directo (lo más simple).** Envía el `.apk` a los testers (link, email, etc.). En el
teléfono Android: ajustes → permitir "instalar apps de orígenes desconocidos" para tu navegador/gestor de
archivos, y abrir el APK. O por cable:

```bash
adb install -r com.danwave.wardkitten-Signed.apk
```

**Opción B — Firebase App Distribution (recomendada para varios testers, gratis, sin Play).**

1. Crea un proyecto en [Firebase](https://console.firebase.google.com) (gratis) y registra una app
   **Android** con el package `com.danwave.wardkitten`. Copia su **App ID** (formato `1:123...:android:abc`).
2. Instala el CLI y entra:

   ```bash
   npm install -g firebase-tools
   firebase login
   ```

3. Sube el APK y repártelo a un grupo de testers (por email):

   ```bash
   firebase appdistribution:distribute com.danwave.wardkitten-Signed.apk \
     --app <FIREBASE_APP_ID> \
     --groups "testers" \
     --release-notes "Build de prueba Wardkitten"
   ```

   Los testers reciben un email con un enlace para instalar. **No requiere cuenta de Google Play.**

### 3.4 (Opcional, futuro) Google Play

- Alta en **Google Play Console** (25 $, pago único).
- Sube el **AAB** al *track* **Internal testing** (hasta 100 testers por email, sin revisión completa) o
  **Closed testing**. Producción requiere la revisión de Google.

---

## 4. iOS

> **Lo importante primero:** para distribuir a **otras** personas (TestFlight o ad-hoc) necesitas el
> **Apple Developer Program (99 $/año)**. **No existe** una vía gratuita para repartir un build de iOS a
> terceros. Lo único posible sin cuenta de pago es instalar la app en **tu propio** dispositivo.

### 4.1 Sin cuenta de pago — solo en tu dispositivo (Apple ID gratis)

Requiere **macOS + Xcode**. Con un Apple ID normal (gratis), Xcode crea un "Personal Team":

1. Abre el proyecto en Xcode (o compila con `dotnet` y despliega a un dispositivo conectado):

   ```bash
   dotnet build src/Wardkitten.Mobile -t:Run -f net10.0-ios \
     -p:RuntimeIdentifier=ios-arm64 -p:_DeviceName=<udid-del-dispositivo>
   ```

2. En Xcode, selecciona tu Apple ID como *Signing Team* (Personal Team).

**Limitaciones del modo gratuito:** la app **caduca a los 7 días**, máximo ~3 apps por dispositivo, solo
en dispositivos que controles físicamente, **sin push notifications** y **sin** distribución a otros.
Sirve para probar tú, no para repartir.

### 4.2 Con Apple Developer Program (99 $/año) — TestFlight (pruebas privadas reales)

1. Enrólate en el **Apple Developer Program**.
2. En el **Apple Developer portal**: crea un **App ID** (`com.danwave.wardkitten`), un **certificado de
   distribución** y un **perfil de aprovisionamiento** (App Store / TestFlight).
3. En **App Store Connect**: crea la ficha de la app.
4. Compila el archivo firmado en **macOS**:

   ```bash
   dotnet publish src/Wardkitten.Mobile -c Release -f net10.0-ios \
     -p:ArchiveOnBuild=true \
     -p:CodesignKey="Apple Distribution: TU NOMBRE (TEAMID)" \
     -p:CodesignProvision="Wardkitten AppStore"
   # Genera un .ipa
   ```

5. Sube el `.ipa` con **Transporter** (app de Apple) o `xcrun altool`/`notarytool`.
6. En **TestFlight**: añade testers (internos = miembros del equipo, sin revisión; externos = hasta 10.000,
   con una revisión ligera). Los testers instalan con la app **TestFlight**. Esto **sí** es prueba privada
   distribuible — pero requiere el programa de pago.

### 4.3 Firebase App Distribution en iOS

También distribuye iOS, **pero** el `.ipa` debe ir firmado **ad-hoc** con los **UDID** de los dispositivos
registrados, y registrar dispositivos/crear perfiles ad-hoc requiere igualmente el **Apple Developer
Program de pago**. Es decir: no evita el coste de Apple.

---

## 5. (Opcional) Automatizar el móvil en CI

Cuando tengas la firma resuelta, puedes añadir workflows de GitHub Actions:

- **Android**: runner `ubuntu-latest`, `dotnet workload install maui-android`, publicar el APK/AAB con la
  firma desde *secrets* (`ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEY_ALIAS`,
  `ANDROID_KEY_PASS`), y subir a **Firebase App Distribution** con `FIREBASE_TOKEN` + `FIREBASE_APP_ID`.
- **iOS**: runner `macos-latest` (incluye Xcode), `dotnet workload install maui-ios`, firma con los
  certificados/perfiles importados desde *secrets*, y subida a TestFlight con `altool`/Fastlane.

No se incluyen en el repo porque la firma depende de credenciales que aún no existen.

---

## 6. Checklist de "primera publicación de prueba"

**Web**
- [ ] `GHCR_USERNAME` / `GHCR_TOKEN` configurados → CI publica `wardkitten-web`.
- [ ] Pull secret `avanware.ghcr.io` creado en el namespace.
- [ ] Secretos reales sustituyendo los `REPLACE_ME`.
- [ ] DNS `app.*` y `api.*` + TLS (cert-manager).
- [ ] `kubectl apply -f K8S/produccion/` (o ArgoCD synced/healthy).

**Android (prueba privada, sin cuenta)**
- [ ] `dotnet workload install maui` + Android SDK/JDK.
- [ ] Assets (icono/splash/fuente) añadidos al `.csproj`.
- [ ] `apiBaseUrl` correcto en `MauiProgram.cs`.
- [ ] Keystore creado y APK firmado compilado.
- [ ] Repartido por APK directo **o** Firebase App Distribution.

**iOS (prueba en tu dispositivo, sin cuenta)**
- [ ] macOS + Xcode.
- [ ] Apple ID (gratis) como Personal Team; instalar en dispositivo (caduca 7 días).
- [ ] Para repartir a otros: enrolarse en Apple Developer Program (99 $/año) → TestFlight.
