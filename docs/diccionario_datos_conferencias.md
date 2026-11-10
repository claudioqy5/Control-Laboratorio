# Diccionario de Códigos y Estados: Conferencias y Asistencias

Guía rápida de referencia para interpretar los valores numéricos, banderas booleanas y clasificaciones de participantes en el sistema.

---

## 1. Códigos Numéricos de Participación (`EstadoParticipacion`)

Columna `EstadoParticipacion` (tipo `TINYINT` en la base de datos):

| Valor Numérico | Código / Etiqueta | Significado | ¿Dónde se origina? |
| :---: | :--- | :--- | :--- |
| **`1`** | **`INSCRITO` / `PRE-INSCRITO`** | El participante completó el formulario público en la página web. Su cupo está reservado, pero **aún no se presenta en la puerta** del auditorio. Su fecha de asistencia está vacía (`FechaHoraAsistencia = NULL`). | Formulario web (`POST /api/v1/conferences/{id}/register`). |
| **`2`** | **`ASISTIÓ`** | El participante **ingresó formalmente al evento**. Su documento o carné fue registrado por el lector y su fecha de asistencia fue sellada con la hora exacta (`FechaHoraAsistencia != NULL`). | Lector de puerta (`POST /api/v1/conferences/{id}/attendances/mark`). |

---

## 2. Clasificación de Asistentes en el Reporte (Deducción por Fechas)

Para no crear columnas extras innecesarias en la base de datos, el sistema deduce si un asistente fue pre-inscrito o espontáneo comparando las dos fechas de su registro:

| Condición | Código de Negocio | Cómo se calcula (Fórmula) | Significado en la Vida Real |
| :--- | :--- | :--- | :--- |
| **Pre-inscrito Acreditado** | `ACREDITADO` | `Estado == 2` y `FechaHoraInscripcion != FechaHoraAsistencia` | Se inscribió días o semanas antes por la web y **llegó al auditorio el día del evento** a marcar su carné en la puerta. |
| **Asistente Espontáneo** | `ESPONTANEO` | `Estado == 2` y `FechaHoraInscripcion == FechaHoraAsistencia` | **No se inscribió antes por la web**. Llegó directo a la puerta, pasó su carné, el sistema lo reconoció en el padrón de alumnos de la facultad y lo registró y acreditó en ese mismo instante. |
| **Inasistencia** | `FALTÓ` | `Estado == 1` y `FechaHoraAsistencia IS NULL` | Se pre-inscribió por la web asegurando su cupo, pero **nunca llegó al evento**. |
| **Doble Marcación** | `YA_REGISTRADO` | `Estado == 2` (al pasar el carné por segunda vez) | La persona ya había entrado minutos antes. El sistema **no altera el aforo**, no duplica el registro y muestra la hora previa en que ya había ingresado. |

---

## 3. Banderas Booleanas (Valores `0` y `1`)

### A. Control de Puerta (`AsistenciaAbierta` en conferencias)
Controla si el lector de código de barras o pistola en la puerta del auditorio acepta ingresos en ese momento:

| Valor | Estado | Comportamiento del Sistema |
| :---: | :--- | :--- |
| **`0`** | **`CERRADO`** | La puerta está cerrada. Si alguien intenta escanear un carné, el sistema rechaza la marcación con el mensaje: *"La asistencia para este evento está cerrada"*. |
| **`1`** | **`ABIERTO`** | La puerta está abierta. El lector de código de barras procesa ingresos en tiempo real. |

### B. Retención y Purgado (`AutoPurgar30Dias` en conferencias)
Determina si la conferencia y sus asistentes se eliminan automáticamente tras finalizar:

| Valor | Estado | Comportamiento del Sistema |
| :---: | :--- | :--- |
| **`0`** | **`PERMANENTE`** | El evento y su historial de asistentes se conservan para siempre en la base de datos. |
| **`1`** | **`AUTOPURGAR`** | El sistema programa la fecha `FechaCaducidadPurge` a 30 días después del evento. Cumplido el plazo, se borra el evento y todas sus participaciones en cascada. |

---

## 4. Códigos de Respuesta del Kiosco / Lector de Puerta

Al disparar la pistola de código de barras en la puerta (`POST /attendances/mark`), el API responde con estos estados para la pantalla del operador:

| Código de Respuesta | Color en Pantalla | Mensaje al Operador | Acción del Sistema |
| :--- | :---: | :--- | :--- |
| **`"ACREDITADO"`** | **Verde** | *"Asistencia confirmada (Pre-inscrito)"* | Pasa de Estado 1 a Estado 2. Suma al contador de aforo. |
| **`"ESPONTANEO_ALUMNO"`** | **Azul / Verde** | *"Asistencia espontánea confirmada (Padrón URP)"* | Crea el registro en Estado 2 con datos de biblioteca. Suma al contador de aforo y al KPI de espontáneos. |
| **`"YA_REGISTRADO"`** | **Ámbar / Amarillo** | *"El participante ya registró su asistencia previamente (XX:XX)"* | **No hace nada.** Mantiene el aforo intacto para evitar conteos inflados. |
| **`"NO_ENCONTRADO"`** | **Rojo / Gris** | *"Participante no registrado en el padrón"* | Abre modal en pantalla para que el operador ingrese los datos manualmente si es externo o docente. |

---

## 5. Resumen de Estados de la Conferencia (`EstadoEvento`)

| Texto de Estado | Significado Operativo |
| :--- | :--- |
| **`"Programado"`** | El evento está en agenda futura. Acepta pre-inscripciones web. |
| **`"En Curso"`** | El evento se está llevando a cabo en este momento. |
| **`"Finalizado"`** | El evento ya concluyó. La asistencia se cierra. |
| **`"Cancelado"`** | El evento se suspendió. No se muestra en la agenda pública. |
