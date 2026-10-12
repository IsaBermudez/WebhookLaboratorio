# WebhookLaboratorio — Pruebas de integración basadas en Webhooks

Solución .NET 8: `gestionUsuariosApi` (emisor) → webhook HTTP + JSON + HMAC-SHA256 → `recepcionEventosApi` (receptor) → BD SQL Server compartida.

## Estructura
| Proyecto | Rol |
|---|---|
| `gestionUsuariosApi` | Emisor. Recibe `POST /crearUsuario`, construye y firma el webhook y lo envía. Consulta usuarios (solo lectura). |
| `recepcionEventosApi` | Receptor. Valida firma y estructura, detecta duplicados, persiste Usuarios y Eventos. |
| `Shared` | DTOs, modelos de webhook, `HmacHelper`, constantes y middleware global de errores. |
| `Tests` | xUnit + FluentAssertions + WebApplicationFactory (unitarias, integración y seguridad). |

## Instalación y ejecución del laboratorio

### 1. Prerrequisitos
| Herramienta | Para qué | Cómo comprobarla |
|---|---|---|
| .NET 8 SDK | Compilar y ejecutar las APIs y las pruebas | `dotnet --version` debe mostrar `8.x` |
| SQL Server (LocalDB, Express o Developer) | Base de datos compartida | `sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "SELECT @@VERSION"` (ajusta el servidor) |
| Postman | Prueba manual de los casos (opcional si solo corres `dotnet test`) | — |

Se necesita conexión a internet la primera vez, para descargar los paquetes NuGet.

### 2. Obtener el proyecto
Descomprime `WebhookLaboratorio.zip` y abre una terminal dentro de la carpeta `WebhookLaboratorio` (donde está `WebhookLaboratorio.sln`). Todos los comandos siguientes se ejecutan desde ahí.

### 3. Restaurar y compilar
```
dotnet restore
dotnet build
```
Debe terminar con `Build succeeded` y 0 errores.

### 4. Configurar la conexión a SQL Server
Edita `ConnectionStrings:Default` en **los dos** archivos, con el mismo servidor y la misma base:
- `gestionUsuariosApi/appsettings.json`
- `recepcionEventosApi/appsettings.json`

| Tu servidor | Cadena de conexión |
|---|---|
| LocalDB (por defecto, solo Windows) | `Server=(localdb)\\MSSQLLocalDB;Database=WebhookLaboratorio;Trusted_Connection=True;TrustServerCertificate=True` |
| SQL Server Express | `Server=localhost\\SQLEXPRESS;Database=WebhookLaboratorio;Trusted_Connection=True;TrustServerCertificate=True` |
| Usuario y contraseña (Docker, Linux) | `Server=localhost,1433;Database=WebhookLaboratorio;User Id=sa;Password=<tu_clave>;TrustServerCertificate=True` |

En el JSON la barra invertida va doble (`\\`). Si no cambias nada, se usa LocalDB.

`Webhook:Secret` (en los mismos archivos) debe ser **idéntico** en ambas APIs: con él se firma y se valida el HMAC.

### 5. Crear la base de datos
Opción A (recomendada): ejecutar el script `Database/crear_bd.sql` en SQL Server Management Studio, o por consola:
```
sqlcmd -S "(localdb)\MSSQLLocalDB" -i Database/crear_bd.sql
```
Opción B: no hacer nada. Si la base no existe, `recepcionEventosApi` la crea junto con las tablas al arrancar.

En ambos casos los `Id` los genera SQL Server (`IDENTITY(1,1)`).

### 6. Ejecutar las APIs
Abre **dos terminales** en la carpeta del proyecto. Levanta el receptor primero:

| Terminal | Comando | URL |
|---|---|---|
| 1 | `dotnet run --project recepcionEventosApi` | http://localhost:5002 |
| 2 | `dotnet run --project gestionUsuariosApi` | http://localhost:5001 |

Déjalas abiertas mientras pruebas. Para detenerlas, `Ctrl + C` en cada terminal.

### 7. Verificar que todo funciona
1. `GET http://localhost:5002/consultarEventos` → `200` con `[]`.
2. `GET http://localhost:5001/consultarUsuarios` → `200` con `[]`.
3. Crear un usuario (en Postman, o por consola):
   ```
   curl -X POST http://localhost:5001/crearUsuario -H "Content-Type: application/json" -d "{\"nombre\":\"Carolina\",\"email\":\"carolina@gmail.com\"}"
   ```
   Debe responder `200` con `id`, `nombre` y `email`.
4. Repetir los dos GET: ahora deben mostrar el usuario y un evento `USUARIO_CREADO` en estado `PROCESADO`.

Si algo falla, revisa los problemas frecuentes; si todo responde bien, sigue con la reproducción de los casos en Postman o con las pruebas automáticas (`dotnet test`, ver sección *Pruebas*).

### 8. Problemas frecuentes
| Síntoma | Causa y solución |
|---|---|
| `dotnet` no se reconoce | Falta el SDK de .NET 8 o hay que abrir una terminal nueva tras instalarlo. |
| `Cannot open database` / error de conexión al arrancar | SQL Server no está iniciado o `ConnectionStrings:Default` apunta a otro servidor. Revisa el paso 4. |
| Error de certificado SSL con SQL Server | Confirma que la cadena incluye `TrustServerCertificate=True`. |
| `address already in use` en 5001 o 5002 | Otro proceso usa el puerto. Ciérralo o cambia `applicationUrl` en `Properties/launchSettings.json` (y `RecepcionEventos:BaseUrl` del emisor si cambias el del receptor). |
| `crearUsuario` responde `503 RECEPTOR_NO_DISPONIBLE` | `recepcionEventosApi` no está corriendo (o es el CP-004). Levántalo primero. |
| El receptor responde `401 FIRMA_INVALIDA` | `Webhook:Secret` distinto entre las APIs, o el script de Postman usa otro secreto. |
| `crearUsuario` responde `409` | Ese email ya existe: el duplicado se detecta por email. Usa otro. |

## Reproducción manual con Postman
Los casos de prueba automatizados hacen las mismas llamadas HTTP, sobre los mismos endpoints y con los mismos JSON, pero **en memoria** (`WebApplicationFactory`), sin red ni Postman. Para reproducirlos a mano:

| Caso | Qué hacer en Postman | Esperado |
|---|---|---|
| CP-001 | `POST http://localhost:5001/crearUsuario`, body JSON `{ "nombre": "abcde", "email": "abcde@gmail.com" }` | 200; el usuario aparece en `GET :5001/consultarUsuarios` y el evento en `GET :5002/consultarEventos` |
| CP-002 | Webhook directo (ver abajo), **con firma**, con `eventType: ""`, `nombre: ""`, `email: "correo-invalido"` | 400; sin registros nuevos (sin firma respondería 401, porque la firma se valida primero) |
| CP-003 | Enviar **dos veces** el mismo webhook válido (o `crearUsuario` con el mismo email) | 200 y luego 409; un solo evento |
| CP-004 | Detener `recepcionEventosApi` y llamar `POST :5001/crearUsuario` | 503 `RECEPTOR_NO_DISPONIBLE` |
| CP-005 | Webhook directo (ver abajo) con `X-Signature` válida y body de Carlos Rodríguez (`carlos.rodriguez@gmail.com`); luego consultar `Usuarios` y `Eventos` en SQL Server | 200; registros correctos con `Id` generado por IDENTITY |
| CP-006 | Webhook directo **sin** la cabecera `X-Signature` | 401; 0 registros |

**¿Quién firma?** Con `POST :5001/crearUsuario` (CP-001, CP-004) **no envías firma**: el emisor la calcula y la manda solo. Al llamar **directo al receptor** (CP-002, CP-003, CP-005, CP-006) Postman hace de emisor, por eso necesita el script de abajo. El script se ejecuta en cada envío de la request donde lo pegues y recalcula la firma sobre el body que tenga en ese momento. **Ponlo en cada request, no a nivel de colección**: si no, también firmaría CP-006.

**Webhook directo al receptor** (CP-002, CP-003, CP-005, CP-006): `POST http://localhost:5002/recibirEventoUsuarioCreado`, Body → raw → JSON (sin variables `{{}}`, la firma se calcula sobre el texto exacto):
```json
{ "eventType": "USUARIO_CREADO", "timestamp": "2026-10-02T10:00:00",
  "data": { "nombre": "Juan Pérez", "email": "juan.perez@gmail.com" } }
```
Pestaña *Pre-request Script* (calcula la firma HMAC-SHA256 en hexadecimal; para CP-006 comenta la última línea):
```js
const secret = "clave-secreta-laboratorio-webhooks-2026"; // = Webhook:Secret
const firma = CryptoJS.HmacSHA256(pm.request.body.raw, secret).toString(CryptoJS.enc.Hex);
pm.request.headers.upsert({ key: "X-Signature", value: firma });
```
Para repetir un caso con éxito (CP-001/CP-003) usa un email que no exista aún: el duplicado se detecta por email.

## Pruebas
`dotnet test` — usa **SQL Server real**: cada prueba crea una BD temporal `WebhookLab_Test_<guid>` (el receptor crea las tablas con IDENTITY(1,1)) y la elimina al terminar.

El servidor se define con la variable de entorno `WEBHOOK_TEST_SQLSERVER` (cadena de conexión **sin** `Database`). Si no existe, se usa LocalDB:
`Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True`

Ejemplos:
- PowerShell: `$env:WEBHOOK_TEST_SQLSERVER="Server=localhost;User Id=sa;Password=<clave>;TrustServerCertificate=True"; dotnet test`
- bash: `WEBHOOK_TEST_SQLSERVER="Server=localhost,1433;User Id=sa;Password=<clave>;TrustServerCertificate=True" dotnet test`

La cuenta debe poder crear y eliminar bases de datos. `CP005` verifica además, consultando SQL Server, que `Usuarios.Id` y `Eventos.Id` son columnas IDENTITY.

| Caso | Prueba | Resultado verificado |
|---|---|---|
| CP-001 | `CP001_RecepcionExitosaDeWebhook` | emisor→receptor→BD, 200, usuario y evento persistidos |
| CP-002 | `CP002_*` | 400 y 0 registros |
| CP-003 | `CP003_EventoDuplicado_*` | 200 y luego 409, un único evento |
| CP-004 | `CP004_ReceptorNoDisponible_*` | 503 `RECEPTOR_NO_DISPONIBLE`, sin persistencia |
| CP-005 | `CP005_*` | campos correctos e IDs generados por la BD |
| CP-006 | `CP006_*` | 401 sin `X-Signature` (también firma incorrecta / no hexadecimal), 0 registros |

## Decisiones de diseño
- El emisor no persiste ni genera IDs: el receptor guarda y devuelve los IDs; el emisor solo lee la BD compartida.
- Duplicados: el esquema aprobado no tiene identificador de evento, por lo que **no se modificó**; un evento es duplicado si el email ya existe en `Usuarios` (409).
- Orden de validación en el receptor: firma HMAC (401) → estructura (400) → duplicado (409).
- Errores uniformes `{ "mensaje", "codigo" }` mediante `Shared.Middleware.ExceptionHandlingMiddleware`.
