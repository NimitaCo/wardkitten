# Futura mejora — NSwag: operationIds estables

> Nota de conocimiento trasladada desde **Avanware/IntegraSystem** (rama `DBM_NSWAG_NAMES`).
> **Aplica solo si este repo expone APIs ASP.NET Core consumidas por clientes TypeScript
> generados con NSwag.** Si no genera clientes nswag, ignórala.

## El problema
NSwag deriva el nombre de cada método del cliente TS del **path+verbo** del endpoint. Con rutas
repetidas entre controllers (p.ej. `[HttpGet("all")]` en varios), NSwag genera sufijos numéricos
`all`, `all2`, `all3` que **se desplazan al añadir/quitar un endpoint** → renombra métodos y **rompe
silenciosamente al front** (que llama al nombre viejo). El back compila; el front no.

## La idea
Fijar los `operationId` del documento OpenAPI a un patrón **estable** `Controller`+`Action` (no del
path) y que NSwag genere **un cliente por microservicio** con métodos nombrados por ese `operationId`.

## Cómo ejecutarlo
1. En cada `Program.cs`, dentro de `AddSwaggerGen(c => { … })`:
   ```csharp
   c.CustomOperationIds(api =>
       api.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor cad
           ? $"{appname.ToLowerInvariant()}_{cad.ControllerName}{cad.ActionName}"
           : null);
   ```
2. En cada `*.TS.nswag`: `"className": "$(api)Client"`, `"generateClientClasses": true`,
   `"operationGenerationMode": "MultipleClientsFromOperationId"`.
3. **Regenerar el nswag** (`nswag run <svc>.TS.nswag`) **y actualizar TODOS los llamadores del front
   en el mismo cambio**. ⚠️ Paso crítico: cambiar los operationId + regenerar SIN actualizar los
   `.service.ts` deja el front sin compilar (errores *"Property 'x' does not exist on type 'yClient'"*).
   Hazlo como **un único cambio coordinado**: config + regeneración + callers.
4. Al añadir un microservicio nuevo, replica el bloque `CustomOperationIds` + `className: "$(api)Client"`.

## Cuándo
Como paso final antes de publicar, regenerando el nswag **una sola vez** con el API ya estabilizado.
