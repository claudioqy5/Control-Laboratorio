# UNIVERSIDAD RICARDO PALMA
### FACULTAD DE MEDICINA HUMANA "MANUEL VELASCO SUÁREZ"
### BIBLIOTECA VIRTUAL Y ESPECIALIZADA (BVE-FAMURP)

---

# SISTEMA INTEGRAL DE CONTROL DE LABORATORIO, GESTIÓN DE AFORO Y SERVICIOS ACADÉMICOS

## Documento Descriptivo del Sistema y Arquitectura Institucional

---

**Supervisión y Jefatura Institucional:**  
- **Lic. Francisca Valero Villaizan**  
  *Jefa de la Biblioteca Virtual y Especializada de la Facultad de Medicina Humana*  
  *Universidad Ricardo Palma*  

**Equipo de Desarrollo:**  
- [Claudio Fernando Quello Yapu](https://github.com/claudioqy5)  
- [Joshue Sebastian Correa Giron](https://github.com/Cjoshue18)  

**Ámbito de Aplicación:**  
Biblioteca Virtual y Especializada de la Facultad de Medicina Humana  
Campus Central URP, Santiago de Surco, Lima, Perú  

---

## 1. DESCRIPCIÓN DEL PROYECTO

### 1.1 Objetivos
El presente proyecto tiene como propósito principal la consolidación de un ecosistema tecnológico unificado, interactivo y autogestionado para la Biblioteca Virtual y Especializada de la Facultad de Medicina Humana (BVE-FAMURP).

La iniciativa persigue los siguientes fines:
1. **Maximizar el retorno de la inversión institucional:** Facilitar y promover activamente el aprovechamiento de las suscripciones a bases de datos biomédicas internacionales y bibliografía científica especializada contratadas por la universidad.
2. **Equidad en el uso del equipamiento:** Regular y optimizar la rotación de las 36 computadoras del laboratorio de cómputo, garantizando que cada estudiante disponga de un tiempo diario justo (cuota asignada de 3 horas) para sus actividades de estudio e investigación.
3. **Automatización del acceso y aforo:** Sustituir los registros analógicos en cuadernos de papel por mecanismos de identificación inmediata mediante cámaras de escaneo óptico del carnet universitario.
4. **Agilización en los servicios académicos:** Proveer herramientas visuales e intuitivas para el acceso a recursos digitales, consulta de actividades formativas y orientación académica.
5. **Canal centralizado de comunicación:** Facilitar la difusión de convocatorias y actividades académicas mediante herramientas que permitan la distribución de comunicados masivos sin saturar los servicios de mensajería institucional.

### 1.2 Alcance del Sistema
El alcance abarca tanto la operatividad presencial en las instalaciones físicas como la extensión académica digital:
- **Protección y supervisión física:** Bloqueo automatizado de las 36 estaciones del laboratorio de cómputo para asegurar que solo alumnos acreditados hagan uso de los equipos informáticos.
- **Acceso rápido y autónomo:** Consulta de disponibilidad de recursos electrónicos y servicios bibliotecarios desde terminales táctiles del campus o dispositivos personales.
- **Telemetría para la gestión bibliotecaria:** Monitoreo en vivo para que el personal administrativo conozca el aforo real, sesiones activas y horas de mayor afluencia.
- **Integración de servicios web y presenciales:** Articulación entre la consola administrativa para la gestión física en sala y el portal público institucional para la comunidad médica.

### 1.3 Justificación
Históricamente, la biblioteca ha enfrentado una brecha operativa entre la inversión en recursos académicos y su aprovechamiento real:
- El registro de ingreso a las computadoras era manual, generando demoras en horas punta y una nula trazabilidad sobre el tiempo real de uso.
- Los estudiantes desconocían en gran medida los procedimientos para conectar sus cuentas personales a las redes institucionales para acceder a las bases de datos biomédicas fuera del campus.
- La gestión de actividades y control de asistencia demandaba esfuerzos administrativos manuales que restaban tiempo a la atención formativa de los alumnos.

La digitalización integral elimina estas tareas operativas repetitivas, permitiendo al equipo de biblioteca concentrarse en la orientación académica y dotando a la facultad de evidencias y estadísticas directas para procesos de acreditación universitaria y licenciamiento institucional ante SUNEDU.

---

## 2. SOBRE EL SISTEMA Y ESTRUCTURA DE COMPONENTES

El diseño de la arquitectura y la integración modular del sistema han sido concebidos y desarrollados en conjunto por **Joshue Sebastian Correa Giron** y **Claudio Fernando Quello Yapu**, articulando soluciones de software de control de hardware de laboratorio con servicios web modernos para toda la comunidad universitaria:

### 2.1 Monitoreo y Gestión Remota de Estaciones (Las 36 Computadoras)
Este módulo administra visualmente la sala de cómputo de la biblioteca. Mediante una consola interactiva que replica la distribución física real del laboratorio en una cuadrícula de 36 computadoras, el personal bibliotecario puede:
- Conocer al instante qué equipos están ocupados, cuáles están disponibles y la identidad del estudiante en sesión.
- Supervisar el tiempo consumido y el saldo restante de la cuota diaria asignada a cada alumno.
- Ejecutar comandos centralizados de asistencia, tales como el desbloqueo remoto en caso de incidencias o el apagado general de todos los equipos al finalizar la jornada.
- Asignar alias personalizados y registrar notas técnicas sobre el estado operativo del hardware (por ejemplo, advertencias de mantenimiento o fallas de periféricos).

### 2.2 Bloqueo de Seguridad y Agente Local de Estación
Cada una de las 36 computadoras cuenta con un componente de software que toma el control de la pantalla al iniciar el sistema operativo:
- Muestra una pantalla institucional de bloqueo que impide el acceso a Windows hasta que el estudiante ingrese sus credenciales válidas (Código Universitario y DNI) o presente su carnet frente a la cámara.
- Bloquea combinaciones de teclado que pudieran saltarse la pantalla de acceso, garantizando que ninguna computadora se utilice sin previa identificación.
- Incorpora un mecanismo de auto-protección que previene el cierre forzado del sistema desde el administrador de tareas.
- Al iniciar sesión, despliega una pequeña ventana flotante y no invasiva que informa al alumno el tiempo disponible que le queda en su turno de estudio.
- Reporta de manera continua su estado de conexión al servidor central para que, si un equipo sufre una desconexión o corte eléctrico, la sesión se cierre adecuadamente sin perjudicar el saldo de tiempo del estudiante.

### 2.3 Identificación y Registro Automatizado por Escaneo Óptico
Diseñado para acelerar el ingreso al laboratorio y reducir las filas en recepción:
- Permite que el estudiante acerque su carnet universitario frente a la cámara web instalada en el punto de atención.
- El sistema analiza la imagen capturada, reconoce automáticamente el texto impreso (código institucional, documento de identidad, nombres completos y carrera profesional) y valida los datos contra la base de datos de la universidad.
- Si el estudiante está habilitado y cuenta con saldo de tiempo disponible, autoriza su ingreso de manera inmediata sin que el personal deba tipear ningún dato en el teclado.

### 2.4 Panel de Control, Analítica y Comunicación Administrativa
Consola web de supervisión exclusiva para la jefatura y el personal de biblioteca:
- **Tablero Estadístico:** Métricas en tiempo real que reflejan el volumen de ingresos diarios, duración promedio de permanencia, porcentaje de ocupación del laboratorio y un gráfico comparativo de afluencia por carreras profesionales (analizando la demanda entre estudiantes de Medicina Humana e Ingeniería Informática).
- **Padrón de Estudiantes:** Gestión de los registros de más de 4,100 estudiantes, con capacidad de importación masiva de padrones académicos semestrales.
- **Asistente de Comunicación Masiva:** Módulo especializado para la redacción de correos informativos institucionales, el cual divide automáticamente las listas de destinatarios en bloques ordenados de 250 correos con dominio universitario (`@urp.edu.pe`). Este mecanismo garantiza que los comunicados puedan enviarse de forma secuencial sin ser bloqueados o catalogados como spam por proveedores como Gmail o Microsoft 365.

---

## 3. ARQUITECTURA TECNOLÓGICA Y DESARROLLO DE INTERFACES

Para garantizar la estabilidad y escalabilidad institucional, el sistema delimita sus responsabilidades tecnológicas entre el núcleo de procesamiento, la consola administrativa y el portal web para la comunidad universitaria:

### 3.1 Infraestructura Central y Panel Administrativo
- **Núcleo de Servicios (Backend API):** Desarrollado sobre la plataforma **C# ASP.NET Core (.NET 8)**, encargado de orquestar la seguridad, las validaciones de acceso, el control de concurrencia y la lógica de negocio.
- **Persistencia de Datos Relacional:** Implementado íntegramente sobre **Microsoft SQL Server** (con soporte en contenedores Docker SQL Server 2022 y compatibilidad con SQL Server Express en estaciones locales), garantizando integridad referencial y transaccionalidad estricta para el registro de estudiantes, equipos y sesiones.
- **Servicios de Visión Artificial:** Integración de servicios avanzados de reconocimiento óptico de caracteres (OCR) para la decodificación de carnets universitarios.
- **Agente de Escritorio y Bloqueo:** Aplicación desarrollada en **C# WPF para Windows**, con instalador automatizado para el despliegue desatendido en las 36 computadoras del laboratorio.
- **Consola de Administración (Backoffice):** Construida como una aplicación web en **Vue 3**, mantenida y optimizada específicamente para las labores del personal técnico y bibliotecario en la gestión operativa del laboratorio.

### 3.2 Portal Web Institucional BVE-FAMURP
La presencia digital y el punto de acceso público para los estudiantes, médicos residentes e investigadores de la facultad se articula a través del portal institucional oficial **BVE-FAMURP**, desarrollado por **Joshue Sebastian Correa Giron**:
- **Stack Tecnológico:** Construido en **React 19**, **TypeScript** y **Tailwind CSS**, implementando una arquitectura modular por dominios (*Vertical Feature Slices*) que asegura alto rendimiento, tipado estricto y mantenibilidad a largo plazo.
- **Servicios Integrados para la Comunidad Universitaria:**
  - **Directorio Especializado de Recursos Biomédicos:** Catálogo interactivo de las bases de datos licenciadas por la universidad (ClinicalKey, UpToDate, Scopus, PubMed, AccessMedicina, etc.) con guías de acceso y fichas técnicas estructuradas.
  - **Instrucción y Acceso Remoto:** Guías y tutoriales multimedia para orientar a los estudiantes sobre el proceso de autenticación institucional mediante la Intranet URP para consultar recursos fuera del campus.
  - **Gestión de Conferencias y Capacitaciones:** Agenda interactiva de actividades formativas, capacitaciones especializadas, calendario de eventos e inscripciones en línea.
  - **Marcación y Control de Asistencia:** Sistema de registro en tiempo real y consolidación de participantes para la emisión de constancias académicas oficiales.
  - **Canal de Avisos y Novedades Institucionales:** Módulo de comunicación y difusión comunitaria, con avisos de objetos extraviados en sala y suscripciones a boletines informativos segmentados por nivel académico.
- **Repositorio Oficial del Portal Web:**  
  [https://github.com/Cjoshue18/MedLib-URP.git](https://github.com/Cjoshue18/MedLib-URP.git)

---

## 4. HARDWARE Y PERIFÉRICOS HOMOLOGADOS

Para aprovechar al máximo las capacidades de automatización del software desarrollado, se definen los siguientes componentes de hardware recomendados para las salas:

1. **Monitores Táctiles de Consulta:** Pantallas de formato interactivo (tales como el monitor HP E24t G5 Touch de 23.8 pulgadas FHD) para habilitar terminales de búsqueda autónoma sin necesidad de teclado ni ratón.
2. **Cámaras Web para Identificación Óptica:** Dispositivos de captura con resolución Full HD (1080p) y enfoque automático veloz (como la serie Logitech C920e), conectados vía USB a las estaciones de control para el escaneo instantáneo de los carnets universitarios.
3. **Quiosco Digital de Gran Formato (Visión a Largo Plazo):** Pantalla interactiva táctil de gran dimensión (43 a 55 pulgadas) ubicada en el vestíbulo de la biblioteca para la bienvenida interactiva y consulta rápida de disponibilidad del laboratorio.

---

## 5. ARQUITECTURA CENTRALIZADA DE BACKEND Y CONSUMO DUAL

El valor del ecosistema radica en la complementariedad de sus partes, articuladas a través de un **backend unificado en C# ASP.NET Core y Microsoft SQL Server** que opera como punto central de verdad y persistencia para toda la biblioteca:

```
┌──────────────────────────────────────┐     ┌──────────────────────────────────────┐
│       ADMINISTRACIÓN / BACKOFFICE     │     │        PORTAL PÚBLICO BVE-FAMURP     │
│             (Vue 3 SPA)              │     │      (React 19 + TypeScript SPA)     │
│                                      │     │                                      │
│ • Monitoreo de 36 computadoras       │     │ • Directorio de bases de datos       │
│ • Comandos remotos (bloqueo/apagado) │     │ • Gestión de conferencias            │
│ • Escaneo y aforo en sala            │     │ • Marcación y control de asistencia  │
│ • Padrón de alumnos y analítica      │     │ • Avisos y novedades institucionales │
└──────────────────┬───────────────────┘     └──────────────────┬───────────────────┘
                   │                                            │
                   │ (HTTP / REST)                              │ (HTTP / REST)
                   ▼                                            ▼
┌───────────────────────────────────────────────────────────────────────────────────┐
│                           API CENTRALIZADA BVE-FAMURP                             │
│                              (C# ASP.NET Core)                                    │
│                                                                                   │
│  Servicios de Control de Sala:            Servicios Académicos y Difusión:        │
│  - Equipos (36 PCs y telemetría)          - Recursos médicos y tutoriales         │
│  - Sesiones y cuotas de uso               - Conferencias y capacitaciones         │
│  - Aforo y escaneo de carnets             - Asistencias y acreditación            │
│  - Alumnos y comunicación masiva          - Novedades y avisos a la comunidad     │
└─────────────────────────────────────────┬─────────────────────────────────────────┘
                                          │
                                          ▼ (Entity Framework Core)
┌───────────────────────────────────────────────────────────────────────────────────┐
│                          BASE DE DATOS INSTITUCIONAL                              │
│                            (Microsoft SQL Server)                                 │
└───────────────────────────────────────────────────────────────────────────────────┘
```

Esta arquitectura de backend centralizado aporta las siguientes ventajas operativas:
1. **Unificación de Datos sin Conflictos:** El modelo relacional integra armoniosamente las entidades operativas del laboratorio físico (equipos, sesiones, registros de aforo) con las entidades académicas (recursos, conferencias, inscripciones, asistencias y avisos), manteniendo relaciones consistentes vinculadas al padrón general de estudiantes.
2. **Especialización de Interfaces:** 
   - El **Backoffice en Vue 3** se mantiene enfocado exclusivamente en las herramientas de supervisión interna que requiere el personal en el mostrador y en la sala de cómputo.
   - El **Portal Web en React 19 + TypeScript** se enfoca en brindar una experiencia pública ágil, accesible y moderna para los estudiantes y docentes, consumiendo los servicios del backend de forma desacoplada y directa.
3. **Mantenimiento Simplificado:** Toda la lógica de negocio, reglas de validación, tiempos de expiración y persistencia relacional residen en un único servicio de backend en ASP.NET Core, evitando duplicidad de código y facilitando las tareas de respaldo, auditoría y despliegue en la infraestructura de la universidad.

---

## 6. CONTACTO Y SEDE INSTITUCIONAL

- **Biblioteca Virtual y Especializada (BVE-FAMURP)**  
  Facultad de Medicina Humana "Manuel Velasco Suárez"  
  Universidad Ricardo Palma  
- **Correo Institucional:** `biblioteca.famurp@urp.edu.pe`  
- **Sede:** Campus Central URP, Av. Benavides 5440, Santiago de Surco, Lima, Perú
