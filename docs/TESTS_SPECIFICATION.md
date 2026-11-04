# Especificación de Calidad de Software y Suite de Pruebas Automatizadas
### Sistema Integral de Control de Laboratorio y Aforo — BVE FAMURP

---

## 1. Introducción y Marco de Calidad

El presente documento detalla la especificación funcional y técnica de la suite de pruebas automatizadas desarrollada para el ecosistema de la **Biblioteca Virtual y Especializada de la Facultad de Medicina Humana (BVE-FAMURP)**.

La suite sigue los principios de la pirámide de pruebas de software, combinando pruebas unitarias de lógica de negocio, pruebas de integración de API sobre controladores con aislamiento de base de datos en memoria, y pruebas de componentes visuales en frontend:
- **Backend (.NET 8 LTS):** 78 pruebas implementadas con `xUnit`, `Moq`, `FluentAssertions` y `Microsoft.EntityFrameworkCore.InMemory`.
- **Frontend (Vue 3 / Vite):** 8 pruebas implementadas con `Vitest`, `@vue/test-utils` y `jsdom`.
- **Total de Pruebas Automatizadas:** 86 casos de prueba ejecutados en cada ciclo de integración continua (CI/CD Quality Gate).

Cada prueba se encuentra estructurada bajo el estándar ágil de **Historias de Usuario (HU)**, descrita mediante **Criterios de Aceptación Gherkin BDD** (`Dado` - `Cuando` - `Entonces`) y vinculada directamente con su método y archivo de prueba física correspondiente.

---

## 2. Matriz General de Trazabilidad

| Módulo Funcional | Código HU | Descripción de la Historia de Usuario | Pruebas Asociadas | Archivo de Prueba |
| :--- | :--- | :--- | :---: | :--- |
| **Control de Acceso y Cuotas** | `HU-LAB-01` | Autenticación de estudiantes en estación y control de cuota diaria | 12 | `AuthControllerLoginTests.cs` |
| **Ciclo de Vida de Sesión** | `HU-LAB-02` | Monitoreo de latido, extensión y cierre de sesión | 16 | `AuthControllerSessionTests.cs` |
| **Monitoreo y Comandos Remotos** | `HU-LAB-03` | Gestión de 36 computadoras, desbloqueo y apagado remoto | 11 | `AuthControllerRemoteTests.cs` |
| **Padrón de Estudiantes (CRUD)** | `HU-ALU-01` | Mantenimiento, unicidad, eliminación y bono de reactivación | 17 | `AlumnosControllerCrudTests.cs` |
| **Importación Masiva de Padrón** | `HU-ALU-02` | Carga de archivos, sanitización y deduplicación masiva | 7 | `AlumnosControllerBulkTests.cs` |
| **Seguridad Admin y Analítica** | `HU-SEC-01` | Login administrativo, clave de acción para mapa y reportes | 15 | `AuthControllerAdminLoginTests.cs`, `StatsControllerSecurityTests.cs` |
| **Infraestructura de Aislamiento** | `HU-INF-01` | Fábrica de contexto de base de datos en memoria para pruebas | 2 | `TestDbContextFactoryTests.cs` |
| **Interfaz de Autenticación UI** | `HU-UI-01` | Formulario de login administrativo y control de estado reactivo | 6 | `AdminLogin.spec.js` |
| **Panel de Sesiones Activas UI** | `HU-UI-02` | Monitoreo en tiempo real de estaciones y estados vacíos | 2 | `ActiveSessions.spec.js` |
| **Total** | | | **86** | |

---

## 3. Módulo 1: Control de Acceso y Cuotas de Laboratorio

### HU-LAB-01: Autenticación de Alumnos en Estación y Control de Cuota Diaria
*Como estudiante acreditado de la facultad, quiero identificarme con mi Código Universitario y DNI en una computadora de laboratorio para iniciar mi sesión de estudio regulada.*

#### Escenario 1.1: Inicio de sesión exitoso con credenciales válidas
- **Dado:** Un estudiante activo registrado y un equipo disponible.
- **Cuando:** Se envían código universitario y DNI correctos al endpoint de login.
- **Entonces:** Retorna `HTTP 200 Ok` con los datos completos de la sesión, hora de inicio y hora límite.
- **Prueba física:** [`AuthControllerLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerLoginTests.cs) -> `Login_WithValidCredentials_Returns200WithFullPayload()`

#### Escenario 1.2: Rechazo por estudiante no registrado
- **Dado:** Un código universitario que no existe en el padrón.
- **Cuando:** Se intenta iniciar sesión con dicho código.
- **Entonces:** Retorna `HTTP 404 NotFound` indicando que el estudiante no está registrado.
- **Prueba física:** [`AuthControllerLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerLoginTests.cs) -> `Login_WithNonExistentCodigo_Returns404NotFound()`

#### Escenario 1.3: Rechazo por estudiante inhabilitado
- **Dado:** Un estudiante registrado pero con estado inactivo (`Estado = false`).
- **Cuando:** Se solicita el inicio de sesión.
- **Entonces:** Retorna `HTTP 401 Unauthorized` denegando el acceso.
- **Prueba física:** [`AuthControllerLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerLoginTests.cs) -> `Login_WithInactiveStudent_Returns401Unauthorized()`

#### Escenario 1.4: Rechazo por DNI incorrecto
- **Dado:** Un estudiante activo registrado.
- **Cuando:** Se ingresa un DNI que no coincide con el registrado en base de datos.
- **Entonces:** Retorna `HTTP 401 Unauthorized` por fallo de autenticación.
- **Prueba física:** [`AuthControllerLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerLoginTests.cs) -> `Login_WithMismatchedDni_Returns401Unauthorized()`

#### Escenario 1.5: Rechazo por estación de cómputo inexistente
- **Dado:** Un identificador de equipo que no existe en el inventario.
- **Cuando:** Se envía una solicitud de inicio de sesión para dicho equipo.
- **Entonces:** Retorna `HTTP 404 NotFound`.
- **Prueba física:** [`AuthControllerLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerLoginTests.cs) -> `Login_WithNonExistentEquipment_Returns404NotFound()`

#### Escenario 1.6: Rechazo por equipo fuera de servicio
- **Dado:** Una estación de cómputo deshabilitada por mantenimiento (`Estado = false`).
- **Cuando:** El alumno intenta ingresar en esa estación.
- **Entonces:** Retorna `HTTP 400 BadRequest` con mensaje de equipo fuera de servicio.
- **Prueba física:** [`AuthControllerLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerLoginTests.cs) -> `Login_WhenEquipmentOutOfService_Returns400BadRequest()`

#### Escenario 1.7: Reasignación de equipo ocupado
- **Dado:** Un equipo que figura ocupado por un estudiante previo.
- **Cuando:** Un nuevo estudiante válido inicia sesión en dicho equipo.
- **Entonces:** El sistema reasigna la estación y crea la nueva sesión correctamente.
- **Prueba física:** [`AuthControllerLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerLoginTests.cs) -> `Login_WhenEquipmentOccupiedByAnotherStudent_ReassignsEquipment()`

#### Escenario 1.8: Liberación de sesión previa en otra estación
- **Dado:** Un estudiante con una sesión activa en una máquina A.
- **Cuando:** Inicia sesión en una máquina B.
- **Entonces:** Cierra la sesión en la máquina A y apertura la nueva sesión en la máquina B.
- **Prueba física:** [`AuthControllerLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerLoginTests.cs) -> `Login_WhenStudentHasActiveSessionElsewhere_ReleasesOldAndAssignsNew()`

#### Escenario 1.9: Idempotencia en la misma estación
- **Dado:** Un estudiante con una sesión activa en el equipo X.
- **Cuando:** Envía un reintento de login en el mismo equipo X.
- **Entonces:** Retorna la sesión existente sin duplicar registros ni alterar tiempos.
- **Prueba física:** [`AuthControllerLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerLoginTests.cs) -> `Login_WhenSessionAlreadyActiveOnSameEquipment_ReturnsExistingSession()`

#### Escenario 1.10: Control de cuota diaria agotada
- **Dado:** Un estudiante con límite diario de segundos en cero o negativo.
- **Cuando:** Intenta autenticarse para abrir sesión.
- **Entonces:** Retorna `HTTP 400 BadRequest` notificando el consumo total de la cuota diaria.
- **Prueba física:** [`AuthControllerLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerLoginTests.cs) -> `Login_WhenDailyQuotaExhausted_Returns400BadRequest()`

#### Escenario 1.11: Ajuste de cuota por debajo del umbral mínimo
- **Dado:** Un estudiante con saldo remanente menor al umbral mínimo de minutos.
- **Cuando:** Se procesa el cálculo de asignación de tiempo.
- **Entonces:** Ajusta la duración al valor mínimo permitido sin exceder el remanente.
- **Prueba física:** [`AuthControllerLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerLoginTests.cs) -> `Login_WithRemainingSecondsBelowThreshold_ClampsToAssignedMinutes()`

#### Escenario 1.12: Cálculo proporcional de minutos asignados
- **Dado:** Un estudiante con tiempo remanente fraccionario.
- **Cuando:** Se calcula la hora límite de la sesión.
- **Entonces:** Asigna con exactitud los minutos correspondientes a los segundos disponibles.
- **Prueba física:** [`AuthControllerLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerLoginTests.cs) -> `Login_CalculatesAssignedMinutesAccuratelyFromRemainingSeconds()`

---

## 4. Módulo 2: Ciclo de Vida de Sesión y Telemetría

### HU-LAB-02: Monitoreo de Latido, Extensión y Cierre de Sesión
*Como administrador de sala, quiero supervisar el latido en vivo de las estaciones y controlar el cierre o extensión de sesiones para garantizar el uso ordenado de las computadoras.*

#### Escenario 2.1: Cierre de sesión inexistente
- **Dado:** Un identificador de sesión que no existe en el sistema.
- **Cuando:** Se invoca la finalización de sesión.
- **Entonces:** Retorna `HTTP 404 NotFound`.
- **Prueba física:** [`AuthControllerSessionTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerSessionTests.cs) -> `Logout_WithNonExistentSession_Returns404NotFound()`

#### Escenario 2.2: Finalización normal de sesión activa
- **Dado:** Una sesión activa en ejecución.
- **Cuando:** El alumno solicita el cierre de sesión.
- **Entonces:** Asigna la hora de finalización, remueve el ping y calcula los segundos consumidos.
- **Prueba física:** [`AuthControllerSessionTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerSessionTests.cs) -> `Logout_WithActiveSession_SetsHoraFinAndClearsPing()`

#### Escenario 2.3: Desactivación por cuota diaria superada
- **Dado:** Un alumno que durante su sesión agota el total de su cuota diaria.
- **Cuando:** Se registra el logout.
- **Entonces:** Cambia el estado del estudiante a inactivo para el resto de la jornada.
- **Prueba física:** [`AuthControllerSessionTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerSessionTests.cs) -> `Logout_WhenQuotaExceeded_SetsStudentEstadoFalse()`

#### Escenario 2.4: Preservación de estado con cuota disponible
- **Dado:** Un alumno que cierra sesión con tiempo remanente en su saldo diario.
- **Cuando:** Se procesa el logout.
- **Entonces:** Mantiene al estudiante en estado activo para permitir ingresos posteriores.
- **Prueba física:** [`AuthControllerSessionTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerSessionTests.cs) -> `Logout_WhenQuotaRemaining_LeavesStudentEstadoTrue()`

#### Escenario 2.5: Verificación de estado sobre sesión inexistente
- **Dado:** Una consulta de estado con id de sesión inválido.
- **Cuando:** Se solicita el estatus de sesión.
- **Entonces:** Retorna `HTTP 404 NotFound`.
- **Prueba física:** [`AuthControllerSessionTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerSessionTests.cs) -> `GetSessionStatus_WithNonExistentSession_Returns404NotFound()`

#### Escenario 2.6: Actualización de ping en sesión activa
- **Dado:** Una sesión activa reportando actividad regular.
- **Cuando:** La estación consulta su estado de sesión.
- **Entonces:** Actualiza la marca temporal de latido y devuelve los segundos restantes.
- **Prueba física:** [`AuthControllerSessionTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerSessionTests.cs) -> `GetSessionStatus_WithActiveSession_UpdatesPingAndCalculatesRemainingSeconds()`

#### Escenario 2.7: Ajuste a cero en sesión con tiempo expirado
- **Dado:** Una sesión que sobrepasó su hora límite establecida.
- **Cuando:** Se consulta su estado.
- **Entonces:** Retorna cero segundos restantes impidiendo extensiones no autorizadas.
- **Prueba física:** [`AuthControllerSessionTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerSessionTests.cs) -> `GetSessionStatus_WhenExpired_ClampsRemainingSecondsToZero()`

#### Escenario 2.8: Consulta de sesión activa en equipo inexistente
- **Dado:** Un número de máquina no registrado.
- **Cuando:** Se consulta la sesión activa.
- **Entonces:** Retorna `HTTP 404 NotFound`.
- **Prueba física:** [`AuthControllerSessionTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerSessionTests.cs) -> `GetActiveSession_WithNonExistentEquipment_Returns404NotFound()`

#### Escenario 2.9: Consulta en equipo sin sesión activa
- **Dado:** Un equipo libre en el laboratorio.
- **Cuando:** Se solicita la sesión activa del equipo.
- **Entonces:** Retorna respuesta exitosa indicando que no cuenta con sesión activa (`hasActiveSession = false`).
- **Prueba física:** [`AuthControllerSessionTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerSessionTests.cs) -> `GetActiveSession_WhenNoActiveSession_ReturnsHasActiveSessionFalse()`

#### Escenario 2.10: Consulta detallada de equipo ocupado
- **Dado:** Un equipo con una sesión en curso.
- **Cuando:** Se consulta su sesión activa.
- **Entonces:** Retorna el detalle completo: datos del estudiante, hora inicio y segundos restantes.
- **Prueba física:** [`AuthControllerSessionTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerSessionTests.cs) -> `GetActiveSession_WhenOccupied_ReturnsFullDetails()`

#### Escenario 2.11: Extensión de sesión sobre registro inexistente
- **Dado:** Un id de sesión inexistente.
- **Cuando:** Se intenta extender tiempo de uso.
- **Entonces:** Retorna `HTTP 404 NotFound`.
- **Prueba física:** [`AuthControllerSessionTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerSessionTests.cs) -> `ExtendSession_WithNonExistentSession_Returns404NotFound()`

#### Escenario 2.12: Rechazo de extensión sobre sesión ya finalizada
- **Dado:** Una sesión que ya fue cerrada con anterioridad.
- **Cuando:** Se intenta extender minutos.
- **Entonces:** Retorna `HTTP 400 BadRequest`.
- **Prueba física:** [`AuthControllerSessionTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerSessionTests.cs) -> `ExtendSession_WhenSessionFinished_Returns400BadRequest()`

#### Escenario 2.13: Extensión efectiva con adición de cuota
- **Dado:** Una sesión activa.
- **Cuando:** El administrador autoriza minutos extraordinarios.
- **Entonces:** Incrementa la hora límite y actualiza el saldo de cuota disponible.
- **Prueba física:** [`AuthControllerSessionTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerSessionTests.cs) -> `ExtendSession_WithActiveSession_AddsQuotaAndExtendsHoraLimite()`

#### Escenario 2.14: Sobrescritura de límite sobre sesión inexistente
- **Dado:** Un id de sesión inválido.
- **Cuando:** Se intenta sobrescribir la hora límite.
- **Entonces:** Retorna `HTTP 404 NotFound`.
- **Prueba física:** [`AuthControllerSessionTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerSessionTests.cs) -> `SetSessionLimit_WithNonExistentSession_Returns404NotFound()`

#### Escenario 2.15: Rechazo de límite manual sobre sesión cerrada
- **Dado:** Una sesión que ya concluyó.
- **Cuando:** Se intenta ajustar su hora límite.
- **Entonces:** Retorna `HTTP 400 BadRequest`.
- **Prueba física:** [`AuthControllerSessionTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerSessionTests.cs) -> `SetSessionLimit_WhenSessionFinished_Returns400BadRequest()`

#### Escenario 2.16: Sobrescritura de límite sin alterar cuota base
- **Dado:** Una sesión activa.
- **Cuando:** Se fija un nuevo límite horario de forma directa.
- **Entonces:** Actualiza la hora límite manteniendo intacta la cuota del estudiante.
- **Prueba física:** [`AuthControllerSessionTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerSessionTests.cs) -> `SetSessionLimit_WithActiveSession_OverridesHoraLimiteWithoutModifyingQuota()`

---

## 5. Módulo 3: Monitoreo y Comandos Remotos de Sala

### HU-LAB-03: Control Centralizado de las 36 Computadoras
*Como responsable de biblioteca, quiero ejecutar acciones remotas sobre la sala (desbloqueo, apagado individual o masivo) para optimizar el cierre del laboratorio y resolver incidencias técnicas.*

#### Escenario 3.1: Desbloqueo remoto en equipo inexistente
- **Dado:** Un número de máquina no registrado.
- **Cuando:** Se envía orden de desbloqueo remoto.
- **Entonces:** Retorna `HTTP 404 NotFound`.
- **Prueba física:** [`AuthControllerRemoteTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerRemoteTests.cs) -> `TriggerRemoteUnlock_WithNonExistentEquipment_Returns404NotFound()`

#### Escenario 3.2: Encolamiento de desbloqueo en equipo libre
- **Dado:** Una computadora disponible.
- **Cuando:** Se emite la orden de desbloqueo.
- **Entonces:** Encola el comando remoto para ser consumido por el agente de escritorio.
- **Prueba física:** [`AuthControllerRemoteTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerRemoteTests.cs) -> `TriggerRemoteUnlock_WithAvailableEquipment_EnqueuesCommandSuccessfully()`

#### Escenario 3.3: Desbloqueo forzado con liberación de sesión
- **Dado:** Un equipo ocupado con bloqueo anómalo.
- **Cuando:** El personal envía el comando de desbloqueo remoto desde la consola.
- **Entonces:** Cierra la sesión asociada, encola el desbloqueo y libera el equipo.
- **Prueba física:** [`AuthControllerRemoteTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerRemoteTests.cs) -> `TriggerRemoteUnlock_WithOccupiedEquipment_ReleasesSessionAndEnqueuesCommand()`

#### Escenario 3.4: Consumo y drenado de comando de desbloqueo
- **Dado:** Un comando de desbloqueo pendiente para un equipo.
- **Cuando:** El agente de la computadora consulta si existen comandos pendientes.
- **Entonces:** Retorna verdadero (`true`) y drena la cola para que no se reejecute.
- **Prueba física:** [`AuthControllerRemoteTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerRemoteTests.cs) -> `CheckRemoteUnlock_WhenCommandPending_ReturnsTrueAndDrains()`

#### Escenario 3.5: Consulta de desbloqueo sin comandos pendientes
- **Dado:** Una máquina sin acciones encoladas.
- **Cuando:** El agente consulta su estado de desbloqueo.
- **Entonces:** Retorna falso (`false`) confirmando ausencia de comandos.
- **Prueba física:** [`AuthControllerRemoteTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerRemoteTests.cs) -> `CheckRemoteUnlock_WhenNoCommandPending_ReturnsFalse()`

#### Escenario 3.6: Apagado remoto sobre equipo inexistente
- **Dado:** Un identificador de máquina fuera de catálogo.
- **Cuando:** Se solicita su apagado.
- **Entonces:** Retorna `HTTP 404 NotFound`.
- **Prueba física:** [`AuthControllerRemoteTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerRemoteTests.cs) -> `TriggerRemoteShutdown_WithNonExistentEquipment_Returns404NotFound()`

#### Escenario 3.7: Encolamiento de apagado para máquina individual
- **Dado:** Una computadora válida en el laboratorio.
- **Cuando:** Se envía la orden individual de apagado.
- **Entonces:** Encola la solicitud de shutdown exitosamente.
- **Prueba física:** [`AuthControllerRemoteTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerRemoteTests.cs) -> `TriggerRemoteShutdown_EnqueuesCommandSuccessfully()`

#### Escenario 3.8: Consumo y drenado de comando de apagado
- **Dado:** Una máquina con orden de apagado en espera.
- **Cuando:** El agente local consulta el endpoint de shutdown.
- **Entonces:** Retorna verdadero (`true`) y consume la orden de apagado.
- **Prueba física:** [`AuthControllerRemoteTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerRemoteTests.cs) -> `CheckRemoteShutdown_WhenCommandPending_ReturnsTrueAndDrains()`

#### Escenario 3.9: Consulta de apagado sin comandos pendientes
- **Dado:** Un equipo sin orden de apagado activa.
- **Cuando:** El agente consulta su estado.
- **Entonces:** Retorna falso (`false`) manteniendo la máquina encendida.
- **Prueba física:** [`AuthControllerRemoteTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerRemoteTests.cs) -> `CheckRemoteShutdown_WhenNoPendingCommand_ReturnsFalse()`

#### Escenario 3.10: Apagado masivo de todas las estaciones
- **Dado:** Todas las estaciones del laboratorio registradas en la base de datos.
- **Cuando:** El bibliotecario ejecuta el comando de apagado general de sala.
- **Entonces:** Encola la orden de apagado individualmente para cada una de las 36 máquinas.
- **Prueba física:** [`AuthControllerRemoteTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerRemoteTests.cs) -> `TriggerRemoteShutdownAll_EnqueuesShutdownForAllEquipos()`

#### Escenario 3.11: Manejo concurrente de órdenes de desbloqueo y apagado
- **Dado:** Un equipo con comandos simultáneos encolados.
- **Cuando:** El agente sondea ambos endpoints en secuencia.
- **Entonces:** Resuelve y drena ambos comandos de forma independiente.
- **Prueba física:** [`AuthControllerRemoteTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerRemoteTests.cs) -> `CheckRemoteUnlock_WhenUnlockAndShutdownBothPending_ReturnsBothAndDrains()`

---

## 6. Módulo 4: Padrón de Estudiantes e Importación Masiva

### HU-ALU-01: Mantenimiento CRUD de Alumnos y Reactivación con Bono
*Como personal administrativo, quiero gestionar el padrón estudiantil, garantizando unicidad de registros y permitiendo la asignación controlada de bonos de reactivación.*

#### Escenario 4.1: Consulta con base de datos vacía
- **Dado:** El sistema sin alumnos registrados.
- **Cuando:** Se solicita la lista completa de alumnos.
- **Entonces:** Retorna una colección vacía sin generar errores de nulidad.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `GetAlumnos_WhenDatabaseIsEmpty_ReturnsEmptyList()`

#### Escenario 4.2: Consulta ordenada descendentemente por identificador
- **Dado:** Una base de datos con múltiples estudiantes.
- **Cuando:** Se invoca la consulta de padrón.
- **Entonces:** Retorna todos los alumnos ordenados descendentemente por `AlumnoId`.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `GetAlumnos_WhenPopulated_ReturnsAllRecordsOrderedByAlumnoIdDescending()`

#### Escenario 4.3: Obtención de estudiante existente por id
- **Dado:** Un alumno registrado con datos completos.
- **Cuando:** Se busca por su `AlumnoId`.
- **Entonces:** Retorna `HTTP 200 Ok` con los datos del estudiante.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `GetAlumno_WhenStudentExists_Returns200WithAlumno()`

#### Escenario 4.4: Búsqueda de alumno inexistente
- **Dado:** Un `AlumnoId` que no coincide con ningún registro.
- **Cuando:** Se realiza la consulta.
- **Entonces:** Retorna `HTTP 404 NotFound`.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `GetAlumno_WhenStudentDoesNotExist_Returns404NotFound()`

#### Escenario 4.5: Creación exitosa de nuevo alumno
- **Dado:** Datos válidos de un nuevo alumno con código y DNI únicos.
- **Cuando:** Se envía al endpoint de creación.
- **Entonces:** Retorna `HTTP 201 CreatedAtAction` y persiste en la base de datos.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `PostAlumno_WithValidData_Returns201CreatedAtActionAndPersists()`

#### Escenario 4.6: Conflicto por código universitario duplicado
- **Dado:** Un nuevo registro con código universitario previamente asignado a otro estudiante.
- **Cuando:** Se intenta persistir.
- **Entonces:** Retorna `HTTP 409 Conflict` protegiendo la integridad.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `PostAlumno_WithDuplicateCodigoUniversitario_Returns409Conflict()`

#### Escenario 4.7: Conflicto por DNI duplicado
- **Dado:** Un nuevo registro con documento nacional de identidad repetido.
- **Cuando:** Se envía la solicitud de creación.
- **Entonces:** Retorna `HTTP 409 Conflict`.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `PostAlumno_WithDuplicateDni_Returns409Conflict()`

#### Escenario 4.8: Rechazo por discrepancia de identificadores en actualización
- **Dado:** Un id de ruta distinto al id incluido en el cuerpo de la petición.
- **Cuando:** Se solicita la modificación de datos.
- **Entonces:** Retorna `HTTP 400 BadRequest`.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `PutAlumno_WithIdMismatch_Returns400BadRequest()`

#### Escenario 4.9: Actualización sobre estudiante inexistente
- **Dado:** Un id no registrado en el sistema.
- **Cuando:** Se envía la petición de actualización.
- **Entonces:** Retorna `HTTP 404 NotFound`.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `PutAlumno_WhenStudentDoesNotExist_Returns404NotFound()`

#### Escenario 4.10: Rechazo de actualización por colisión de código universitario
- **Dado:** Un intento de asignar a un estudiante el código perteneciente a otro.
- **Cuando:** Se procesa la actualización.
- **Entonces:** Retorna `HTTP 409 Conflict`.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `PutAlumno_WithDuplicateCodigoOnAnotherStudent_Returns409Conflict()`

#### Escenario 4.11: Rechazo de actualización por colisión de DNI
- **Dado:** Un intento de asignar a un estudiante el DNI perteneciente a otro alumno.
- **Cuando:** Se procesa la actualización.
- **Entonces:** Retorna `HTTP 409 Conflict`.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `PutAlumno_WithDuplicateDniOnAnotherStudent_Returns409Conflict()`

#### Escenario 4.12: Actualización idempotente de datos propios
- **Dado:** Un estudiante que actualiza campos manteniendo su propio código y DNI.
- **Cuando:** Se envía la actualización.
- **Entonces:** Retorna `HTTP 204 NoContent` guardando los cambios sin falsos conflictos.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `PutAlumno_WhenUpdatingOwnDataWithSameCodeAndDni_Returns204NoContent()`

#### Escenario 4.13: Reactivación de alumno inactivo con asignación de bono
- **Dado:** Un estudiante en estado inactivo (`Estado = false`).
- **Cuando:** El administrador reactiva al alumno cambiando su estado a activo.
- **Entonces:** Suma el bono de reactivación reglamentario a su saldo disponible.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `PutAlumno_WhenReactivatingInactiveStudent_AddsReactivationBonus()`

#### Escenario 4.14: Normalización de cuota agotada y bono en reactivación
- **Dado:** Un estudiante con saldo en cero o negativo al momento de ser reactivado.
- **Cuando:** Se procesa su reactivación.
- **Entonces:** Normaliza la cuota base y añade los minutos del bono de reactivación.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `PutAlumno_WhenReactivatingStudentWithDepletedQuota_NormalizesBaseAndAddsBonus()`

#### Escenario 4.15: Modificación de alumno ya activo sin bono indebido
- **Dado:** Un estudiante que ya figuraba en estado activo.
- **Cuando:** Se modifican sus datos generales.
- **Entonces:** Actualiza la información sin incrementar bono de forma indebida.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `PutAlumno_WhenUpdatingAlreadyActiveStudent_DoesNotAddBonus()`

#### Escenario 4.16: Eliminación de alumno existente
- **Dado:** Un alumno registrado en el sistema.
- **Cuando:** Se ejecuta la eliminación por su id.
- **Entonces:** Retorna `HTTP 204 NoContent` y lo retira de la base de datos.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `DeleteAlumno_WhenStudentExists_RemovesFromDatabaseAndReturns204NoContent()`

#### Escenario 4.17: Eliminación sobre registro inexistente o secuencial
- **Dado:** Una segunda ejecución de eliminación sobre un registro previamente borrado.
- **Cuando:** Se envía la solicitud.
- **Entonces:** Retorna `HTTP 404 NotFound` previniendo operaciones corruptas.
- **Prueba física:** [`AlumnosControllerCrudTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerCrudTests.cs) -> `DeleteAlumno_WhenCalledSequentially_SecondCallReturns404NotFound()`

---

### HU-ALU-02: Procesamiento Masivo e Importación de Padrones
*Como administrador del sistema, quiero importar listados masivos de estudiantes provenientes de matrícula académica, ignorando duplicados y sanitizando datos.*

#### Escenario 5.1: Inicialización de controlador con servicio OCR opcional
- **Dado:** Un entorno donde el servicio externo de visión OCR no está configurado.
- **Cuando:** Se instancia el controlador de alumnos.
- **Entonces:** Inicializa exitosamente permitiendo la importación masiva de datos.
- **Prueba física:** [`AlumnosControllerBulkTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerBulkTests.cs) -> `Constructor_WithNullVisionService_InitializesSuccessfully()`

#### Escenario 5.2: Rechazo ante envío de lista nula
- **Dado:** Una petición de importación con carga nula.
- **Cuando:** Se envía al endpoint masivo.
- **Entonces:** Retorna `HTTP 400 BadRequest` con mensaje descriptivo.
- **Prueba física:** [`AlumnosControllerBulkTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerBulkTests.cs) -> `BulkImport_WithNullList_Returns400BadRequestWithExpectedMessage()`

#### Escenario 5.3: Rechazo ante envío de colección vacía
- **Dado:** Una lista de estudiantes con cero elementos.
- **Cuando:** Se solicita la importación.
- **Entonces:** Retorna `HTTP 400 BadRequest`.
- **Prueba física:** [`AlumnosControllerBulkTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerBulkTests.cs) -> `BulkImport_WithEmptyList_Returns400BadRequestWithExpectedMessage()`

#### Escenario 5.4: Inserción completa con todos los alumnos nuevos
- **Dado:** Una colección donde ningún estudiante existe previamente en el padrón.
- **Cuando:** Se procesa la carga masiva.
- **Entonces:** Inserta todos los registros y reporta el conteo exacto de altas.
- **Prueba física:** [`AlumnosControllerBulkTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerBulkTests.cs) -> `BulkImport_WithAllNewStudents_InsertsAllAndReturnsExpectedCounts()`

#### Escenario 5.5: Omisión total con todos los alumnos ya existentes
- **Dado:** Un archivo compuesto exclusivamente por estudiantes ya registrados.
- **Cuando:** Se ejecuta la importación.
- **Entonces:** Omite todas las inserciones, inserta cero y no altera registros existentes.
- **Prueba física:** [`AlumnosControllerBulkTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerBulkTests.cs) -> `BulkImport_WithAllExistingStudents_OmitsAllAndInsertsZero()`

#### Escenario 5.6: Discriminación en archivo mixto (nuevos y existentes)
- **Dado:** Un padrón combinado con alumnos nuevos y duplicados preexistentes.
- **Cuando:** Se ejecuta la importación.
- **Entonces:** Da de alta únicamente a los registros nuevos y omite los duplicados.
- **Prueba física:** [`AlumnosControllerBulkTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerBulkTests.cs) -> `BulkImport_WithMixedNewAndExistingStudents_InsertsOnlyNewAndOmitsDuplicates()`

#### Escenario 5.7: Sanitización y recorte de espacios en blanco
- **Dado:** Nombres, códigos o DNI con espacios accidentales al inicio o fin.
- **Cuando:** Se procesa la importación masiva.
- **Entonces:** Aplica limpieza `trim` sobre las cadenas antes de persistir.
- **Prueba física:** [`AlumnosControllerBulkTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AlumnosControllerBulkTests.cs) -> `BulkImport_WithWhitespaceData_TrimsAndPersistsSanitizedStrings()`

---

## 7. Módulo 5: Seguridad Administrativa y Analítica de Uso

### HU-SEC-01: Autenticación Administrativa, Clave de Acción y Métricas
*Como administrador de la biblioteca, quiero autenticarme de forma segura en el backoffice y proteger acciones sensibles con contraseña de autorización.*

#### Escenario 6.1: Login administrativo con credenciales válidas
- **Dado:** Usuario y contraseña de administración correctos configurados en el sistema.
- **Cuando:** Se envían las credenciales al endpoint de login administrativo.
- **Entonces:** Retorna `HTTP 200 Ok` con el token de sesión emitido.
- **Prueba física:** [`AuthControllerAdminLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerAdminLoginTests.cs) -> `AdminLogin_WithValidCredentials_ReturnsOkWithToken()`

#### Escenario 6.2: Rechazo por contraseña administrativa incorrecta
- **Dado:** Usuario correcto pero contraseña errónea.
- **Cuando:** Se solicita el acceso.
- **Entonces:** Retorna `HTTP 401 Unauthorized` denegando la sesión.
- **Prueba física:** [`AuthControllerAdminLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerAdminLoginTests.cs) -> `AdminLogin_WithInvalidPassword_Returns401Unauthorized()`

#### Escenario 6.3: Rechazo por usuario administrativo incorrecto
- **Dado:** Un nombre de usuario inexistente o no autorizado.
- **Cuando:** Se solicita autenticación.
- **Entonces:** Retorna `HTTP 401 Unauthorized`.
- **Prueba física:** [`AuthControllerAdminLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerAdminLoginTests.cs) -> `AdminLogin_WithInvalidUsername_Returns401Unauthorized()`

#### Escenario 6.4: Rechazo ante contraseña compuesta por espacios
- **Dado:** Una contraseña administrativa enviada únicamente con espacios en blanco.
- **Cuando:** Se procesa la validación.
- **Entonces:** Retorna `HTTP 401 Unauthorized`.
- **Prueba física:** [`AuthControllerAdminLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerAdminLoginTests.cs) -> `AdminLogin_WithWhitespacePassword_Returns401Unauthorized()`

#### Escenario 6.5: Rechazo ante carga útil vacía
- **Dado:** Un objeto JSON con propiedades vacías.
- **Cuando:** Se envía al endpoint de login administrativo.
- **Entonces:** Retorna `HTTP 400 BadRequest`.
- **Prueba física:** [`AuthControllerAdminLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerAdminLoginTests.cs) -> `AdminLogin_WithEmptyPayload_Returns400BadRequest()`

#### Escenario 6.6: Rechazo ante envío de cuerpo nulo
- **Dado:** Una petición sin contenido en el cuerpo HTTP.
- **Cuando:** Se envía la solicitud.
- **Entonces:** Retorna `HTTP 400 BadRequest`.
- **Prueba física:** [`AuthControllerAdminLoginTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/AuthControllerAdminLoginTests.cs) -> `AdminLogin_WithNullPayload_Returns400BadRequest()`

#### Escenario 6.7: Rechazo de asignación de slot en mapa con clave errónea
- **Dado:** Una solicitud para ubicar un equipo en la cuadrícula de sala con contraseña incorrecta.
- **Cuando:** Se envía la petición.
- **Entonces:** Retorna `HTTP 401 Unauthorized` protegiendo el layout.
- **Prueba física:** [`StatsControllerSecurityTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/StatsControllerSecurityTests.cs) -> `AssignMapSlot_WithWrongPassword_ReturnsUnauthorized()`

#### Escenario 6.8: Asignación exitosa de slot con clave correcta
- **Dado:** Clave de acción administrativa correcta.
- **Cuando:** Se asigna un slot en el mapa y existía una asignación previa.
- **Entonces:** Actualiza la posición y limpia el slot duplicado de forma consistente.
- **Prueba física:** [`StatsControllerSecurityTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/StatsControllerSecurityTests.cs) -> `AssignMapSlot_WithCorrectPassword_UpdatesPositionAndClearsDuplicateSlot()`

#### Escenario 6.9: Asignación de mapa sobre equipo inexistente
- **Dado:** Un id de máquina fuera de inventario con clave válida.
- **Cuando:** Se intenta posicionar en el mapa.
- **Entonces:** Retorna `HTTP 404 NotFound`.
- **Prueba física:** [`StatsControllerSecurityTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/StatsControllerSecurityTests.cs) -> `AssignMapSlot_WhenEquipmentNotFound_ReturnsNotFound()`

#### Escenario 6.10: Rechazo de modificación de alias con clave incorrecta
- **Dado:** Un intento de cambiar el alias de una estación sin la clave de acción válida.
- **Cuando:** Se ejecuta la orden.
- **Entonces:** Retorna `HTTP 401 Unauthorized`.
- **Prueba física:** [`StatsControllerSecurityTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/StatsControllerSecurityTests.cs) -> `SetAlias_WithWrongPassword_ReturnsUnauthorized()`

#### Escenario 6.11: Modificación exitosa de alias con clave válida
- **Dado:** Clave de acción correcta.
- **Cuando:** Se asigna un alias descriptivo al equipo.
- **Entonces:** Actualiza el registro y persiste el alias satisfactoriamente.
- **Prueba física:** [`StatsControllerSecurityTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/StatsControllerSecurityTests.cs) -> `SetAlias_WithCorrectPassword_UpdatesAliasSuccessfully()`

#### Escenario 6.12: Rechazo de registro de comentario con clave incorrecta
- **Dado:** Un comentario de mantenimiento enviado con clave de seguridad inválida.
- **Cuando:** Se envía al endpoint de observaciones.
- **Entonces:** Retorna `HTTP 401 Unauthorized`.
- **Prueba física:** [`StatsControllerSecurityTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/StatsControllerSecurityTests.cs) -> `SetComentario_WithWrongPassword_ReturnsUnauthorized()`

#### Escenario 6.13: Registro de comentario técnico con clave válida
- **Dado:** Clave de acción correcta ingresada por el bibliotecario.
- **Cuando:** Se envía una observación técnica sobre la máquina.
- **Entonces:** Registra el comentario técnico en la ficha de la estación.
- **Prueba física:** [`StatsControllerSecurityTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/StatsControllerSecurityTests.cs) -> `SetComentario_WithCorrectPassword_UpdatesComentarioSuccessfully()`

#### Escenario 6.14: Generación de reporte de uso agrupado por horas
- **Dado:** Historial de sesiones registradas en la base de datos.
- **Cuando:** Se consulta la analítica de horas de uso.
- **Entonces:** Retorna las sesiones debidamente agrupadas por franjas horarias.
- **Prueba física:** [`StatsControllerSecurityTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/StatsControllerSecurityTests.cs) -> `GetUsageStats_ReturnsSessionsGroupedByHour()`

#### Escenario 6.15: Filtrado de sesiones activas en tiempo real
- **Dado:** Un conjunto de sesiones activas e históricas finalizadas.
- **Cuando:** Se solicita el listado de sesiones en vivo.
- **Entonces:** Retorna únicamente aquellas con `HoraFin == null`.
- **Prueba física:** [`StatsControllerSecurityTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Controllers/StatsControllerSecurityTests.cs) -> `GetActiveSessions_ReturnsOnlySessionsWithNullHoraFin()`

---

## 8. Módulo 6: Infraestructura de Aislamiento para Pruebas

### HU-INF-01: Fábrica de Contexto InMemory Aislado
*Como arquitecto de pruebas, quiero un entorno de base de datos en memoria aislado por ejecución para evitar colisiones entre pruebas concurrentes.*

#### Escenario 7.1: Creación de contexto único con base de datos InMemory
- **Dado:** Una solicitud de contexto de datos para un caso de prueba.
- **Cuando:** Se invoca a la fábrica de contexto.
- **Entonces:** Entrega un `DbContext` aislado con base de datos en memoria dedicada.
- **Prueba física:** [`TestDbContextFactoryTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Fixtures/TestDbContextFactoryTests.cs) -> `CreateContext_ReturnsConfiguredInMemoryDatabase()`

#### Escenario 7.2: Sembrado limpio de datos de prueba
- **Dado:** Un escenario que requiere inicializar catálogo de equipos.
- **Cuando:** Se ejecuta el sembrado de prueba.
- **Entonces:** Persiste las estaciones mock sin afectar otros hilos de ejecución.
- **Prueba física:** [`TestDbContextFactoryTests.cs`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Tests/ControlLaboratorio.Tests/Fixtures/TestDbContextFactoryTests.cs) -> `SeedStandardEquipos_PopulatesInitialMachines()`

---

## 9. Módulo 7: Interfaz de Usuario Web (Vue 3 / Vitest)

### HU-UI-01: Acceso y Navegación Reactiva en el Panel de Administración
*Como usuario administrativo, quiero una interfaz ágil en Vue 3 que valide entradas, gestione estados de carga y controle la persistencia del token de autenticación.*

#### Escenario 8.1: Renderizado completo de campos de formulario
- **Dado:** El componente de login montado en el árbol del DOM.
- **Cuando:** La vista se inicializa.
- **Entonces:** Renderiza inputs de usuario, contraseña y el botón de acceso.
- **Prueba física:** [`AdminLogin.spec.js`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Admin/src/components/__tests__/AdminLogin.spec.js) -> `renders form inputs and submit button`

#### Escenario 8.2: Sincronización de enlace bidireccional (v-model)
- **Dado:** El usuario tipeando en los campos de texto.
- **Cuando:** Se emiten eventos de entrada.
- **Entonces:** El modelo reactivo interno actualiza los valores correspondientes.
- **Prueba física:** [`AdminLogin.spec.js`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Admin/src/components/__tests__/AdminLogin.spec.js) -> `updates form model bindings when inputs change`

#### Escenario 8.3: Almacenamiento de token y emisión de evento de éxito
- **Dado:** Credenciales válidas enviadas al backend simulado.
- **Cuando:** Se recibe la respuesta positiva.
- **Entonces:** Almacena el token en `localStorage` y emite el evento `login-success`.
- **Prueba física:** [`AdminLogin.spec.js`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Admin/src/components/__tests__/AdminLogin.spec.js) -> `saves token and emits login-success on successful authentication`

#### Escenario 8.4: Disparo de login mediante tecla Enter
- **Dado:** El cursor ubicado en el campo de contraseña.
- **Cuando:** El usuario presiona la tecla Enter.
- **Entonces:** Dispara el evento de autenticación automáticamente.
- **Prueba física:** [`AdminLogin.spec.js`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Admin/src/components/__tests__/AdminLogin.spec.js) -> `triggers login on enter key in password input`

#### Escenario 8.5: Despliegue de mensaje ante fallo de autenticación
- **Dado:** Una respuesta de rechazo emitida por el backend.
- **Cuando:** Falla la solicitud HTTP.
- **Entonces:** Despliega visualmente el mensaje de alerta informando el error.
- **Prueba física:** [`AdminLogin.spec.js`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Admin/src/components/__tests__/AdminLogin.spec.js) -> `displays error message when login fails`

#### Escenario 8.6: Deshabilitación de botón y estado de carga durante petición
- **Dado:** Una solicitud de autenticación en curso.
- **Cuando:** La petición se encuentra pendiente de respuesta.
- **Entonces:** Deshabilita el botón de envío y presenta texto de carga previniendo envíos duplicados.
- **Prueba física:** [`AdminLogin.spec.js`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Admin/src/components/__tests__/AdminLogin.spec.js) -> `disables submit button and shows loading text during login request`

---

### HU-UI-02: Monitoreo en Tiempo Real de Sesiones Activas
*Como bibliotecario en sala, quiero una tabla reactiva en Vue 3 que despliegue las sesiones en vivo y comunique con claridad cuando el laboratorio esté libre.*

#### Escenario 9.1: Estado vacío cuando no hay sesiones activas
- **Dado:** El componente montado con un arreglo vacío de sesiones.
- **Cuando:** La vista renderiza la tabla.
- **Entonces:** Presenta el mensaje descriptivo informando la ausencia de sesiones en curso.
- **Prueba física:** [`ActiveSessions.spec.js`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Admin/src/components/__tests__/ActiveSessions.spec.js) -> `renders title and empty state when there are no active sessions`

#### Escenario 9.2: Despliegue de listado de sesiones activas con detalle
- **Dado:** Un conjunto de sesiones activas cargadas desde la API.
- **Cuando:** El componente completa la carga de datos.
- **Entonces:** Renderiza cada fila con nombre del estudiante, equipo, hora de inicio y tiempo restante.
- **Prueba física:** [`ActiveSessions.spec.js`](file:///c:/Users/joshu/Desktop/famurp-lib/RClaudio/Admin/src/components/__tests__/ActiveSessions.spec.js) -> `renders list of active sessions with student and computer details`

---

## 10. Comandos de Ejecución Local

### Backend (.NET 8 LTS):
```bash
dotnet test Tests/ControlLaboratorio.Tests/ControlLaboratorio.Tests.csproj --configuration Release --verbosity normal
```

### Frontend (Vue 3 / Vitest):
```bash
cd Admin
npm test
```
