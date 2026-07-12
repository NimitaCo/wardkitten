# Runbook: Auditoría y arreglo de bloqueos de MongoDB (CPU alta / COLLSCAN)

Guía para diagnosticar y resolver la saturación de MongoDB causada por **consultas sin índice
(COLLSCAN)** que sobrecargan el **primario** del replica set `MongoReplica0`.

## Cuándo aplicar (síntomas)
- CPU del proceso `mongod` al ~100% (varios núcleos) en un nodo.
- App lenta / timeouts (`MaxTimeMSExpired`).
- En el log de mongod: muchas líneas `"Slow query"` con `"planSummary":"COLLSCAN"`.
- Heartbeats con `"backpressure": true` (el servidor pide a los clientes que aflojen).

## Contexto del entorno
- Replica set **MongoReplica0** (MongoDB 6.0), puerto **2717**, auth con keyFile.
- Nodos: `mongo-a-0` (10.200.0.10), `mongo-b-0` (10.200.0.19), `mongo-c-0` (10.200.0.69).
- Cliente `mongosh` disponible en **mongo-b-0**: `C:\mongodb\mongosh\...\bin\mongosh.exe`.
- Clave: `logRotate` es **local a cada nodo**, y `logRotate`/lecturas van al **primario** si la app
  usa `primaryPreferred`.

---

## 1. Diagnóstico

### 1.1 Identificar el primario y conectarse al nodo correcto
```js
db.hello().primary   // host:puerto del primario actual
```
Inspecciona SIEMPRE con conexión directa al nodo concreto:
```
mongosh "mongodb://<user>:<pass>@<nodo>:2717/?authSource=admin&directConnection=true"
```

### 1.2 Confirmar que el culpable es mongod (en el nodo, PowerShell)
```powershell
Get-CimInstance Win32_PerfFormattedData_PerfProc_Process |
  Where-Object Name -eq 'mongod' | Select-Object Name, PercentProcessorTime   # >100 = varios nucleos
```

### 1.3 Conexiones y concurrencia
```js
db.serverStatus().connections               // current / active
db.serverStatus().globalLock.activeClients  // readers / writers concurrentes
```

### 1.4 Ver las queries REALES en curso (excluyendo heartbeats)
El `currentOp` por defecto está lleno de `hello`/`isMaster` (heartbeats aparcados, **no** consumen CPU).
Fíltralos y agrupa por plan:
```js
var co = db.getSiblingDB("admin").runCommand({ currentOp: 1, active: true });
var real = co.inprog.filter(o => { var c = o.command || {}; return !(c.hello || c.isMaster || c.ismaster) && o.op !== "none"; });
print("ops reales: " + real.length);
var byPlan = {}; real.forEach(o => { var p = o.planSummary || "-"; byPlan[p] = (byPlan[p] || 0) + 1; });
print(JSON.stringify(byPlan));   // cuantos COLLSCAN
real.filter(o => o.planSummary === "COLLSCAN").slice(0, 15)
    .forEach(o => print(o.ns + " | " + JSON.stringify(o.command).slice(0, 160)));
```
Cada línea `COLLSCAN` = colección + filtro que **escanea entera**. Anota el `ns` (base.colección) y los
campos de `filter` y `sort`.

### 1.5 Confirmar el plan de una query concreta
```js
db.getSiblingDB("<BD>").<coleccion>.find(<filtro>).explain("executionStats").executionStats
// COLLSCAN o totalDocsExamined >> nReturned  =>  falta indice
```

---

## 2. Arreglo

### 2.1 Crear el índice que falta (orden de campos ESR)
Equality → Sort → Range.
```js
// filtro {a:x, b:y} + sort {c:-1}   =>
db.getSiblingDB("<BD>").<coleccion>.createIndex({ a: 1, b: 1, c: -1 })
```
⚠️ En 6.0 el build corre en **todos** los miembros a la vez y **añade carga**. Con el primario ya
saturado, hazlo en **ventana de menos tráfico**.

> Ojo al "whack-a-mole": tras crear un índice, la CPU puede seguir alta porque el trabajo se
> desplaza a la **siguiente** colección sin índice. Repite el diagnóstico (1.4) hasta que no queden
> COLLSCAN relevantes.

### 2.2 Alivio inmediato SIN build: repartir lecturas
Si la app lee del primario (`primaryPreferred`), cámbialo en la cadena de conexión a:
```
...&readPreference=secondaryPreferred
```
Descarga el primario hacia los secundarios (normalmente ociosos). No requiere índices.

### 2.3 Detectar queries con filtro vacío (bug de app)
Filtros como `{ campo: {} }` o `{}` no filtran nada → COLLSCAN garantizado. **Es un bug de código**,
no se arregla con índice: corrige la construcción de la query en la app.

### 2.4 Bases de datos temporales
Una BD de copia/staging temporal que recibe carga en el primario: si ya no se usa, **dropéala**
(elimina la carga de golpe):
```js
db.getSiblingDB("<BD_temporal>").dropDatabase()   // DESTRUCTIVO: confirmar antes
```

---

## 3. Prevención
- **Índices no usados** (ocupan disco y ralentizan escrituras):
  ```js
  db.<coleccion>.aggregate([{ $indexStats: {} }])   // accesses.ops bajo = candidato a revisar
  ```
- **Umbral de slow query** en `mongod.cfg` para vigilar sin inundar el log:
  ```yaml
  operationProfiling:
    slowOpThresholdMs: 500
  ```
- En cada release, revisar las nuevas queries y asegurar que tienen índice de soporte.
- No dirigir **todas** las lecturas al primario.
- Alertas de CPU de `mongod` y de nº de conexiones.

## Checklist rápida
1. ¿Qué nodo tiene la CPU alta? ¿Es el primario? (`db.hello().primary`)
2. `currentOp` filtrado (1.4) → ¿cuántos COLLSCAN y en qué colecciones?
3. Por cada colección: `createIndex` (ESR) o corregir la query vacía.
4. Alivio transversal: `secondaryPreferred`.
5. BDs temporales sin uso: dropear.
6. Re-medir CPU y **repetir** (el trabajo se desplaza a la siguiente colección sin índice).
