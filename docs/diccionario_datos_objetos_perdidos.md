# Diccionario de Datos y Estados: Objetos Perdidos y Custodia

Guía técnica y normativa de referencia para interpretar los valores numéricos, categorías, reglas de retención y restricciones de consistencia del módulo de Objetos Perdidos de la Biblioteca Central - Facultad de Medicina Humana (URP).

---

## 1. Códigos Numéricos de Estado (`EstadoObjeto`)

Columna `EstadoObjeto` (tipo `TINYINT` en base de datos, enum `EstadoObjeto : byte` en backend):

| Valor Numérico | Identificador Enum | Etiqueta Visual | Significado Operativo | Campos Requeridos |
| :---: | :--- | :--- | :--- | :--- |
| **`1`** | `EnCustodia` | **En Custodia** | El objeto fue hallado en instalaciones de la facultad/biblioteca y se encuentra resguardado físicamente en el módulo de recepción. Es visible en el catálogo de búsqueda pública. | `FechaHoraDevolucion = NULL`, `FechaBaja = NULL`, campos de receptor en `NULL`. |
| **`2`** | `Devuelto` | **Devuelto** | El propietario acreditó fehacientemente su pertenencia y recibió el bien. Se genera constancia con la identidad del receptor y del bibliotecario responsable. | `FechaHoraDevolucion != NULL`, `IdUsuarioEntrega != NULL`, `NombrePersonalEntrega != NULL`, (`AlumnoID != NULL` o `DocumentoReceptor != NULL`). |
| **`4`** | `Dispuesto` | **Dispuesto / Baja** | Transcurrió el periodo reglamentario de 30 días naturales sin reclamo o el bien fue transferido según directivas institucionales. Deja de mostrarse en la vitrina pública. | `FechaBaja != NULL`, `FechaHoraDevolucion = NULL`. |

---

## 2. Clasificación por Categorías (`Categoria`)

Columna `Categoria` (tipo `TINYINT` en base de datos, enum `CategoriaObjeto : byte` en backend):

| Valor | Identificador Enum | Ejemplos de Artículos Comprendidos |
| :---: | :--- | :--- |
| **`1`** | `Electronica` | Laptops, tablets, cargadores, audífonos, memorias USB, calculadoras científicas, punteros láser. |
| **`2`** | `Documentos` | Carnés universitarios URP, DNI, pasaportes, licencias, tarjetas bancarias, fotochecks clínicos. |
| **`3`** | `Estudio` | Cuadernos de apuntes, libros de texto, guías de práctica médica, estuches de disección, estetoscopios, tensiómetros. |
| **`4`** | `Prendas` | Batas médicas, mandiles de laboratorio, casacas, polerones, mochilas, cartucheras, bufandas. |
| **`5`** | `Otros` | Termos, botellas de agua, paraguas, llaves, lentes de lectura o sol, accesorios varios. |

---

## 3. Regla Institucional de los 30 Días Naturales de Custodia

1. **Plazo de Retención Activa:**
   Todo objeto reportado permanece en estado `EnCustodia` (`1`) durante un plazo máximo de **30 días naturales** contados a partir de `FechaHoraHallazgo`.
2. **Alertas en Panel Administrativo:**
   El panel de gestión resalta visualmente en ámbar o rojo aquellos registros cuya permanencia supere o esté próxima a los 30 días sin haber sido reclamados.
3. **Cierre de Ciclo de Vida (Baja / Disposición):**
   Al cumplirse el plazo reglamentario sin reclamo por parte del titular, el personal autorizado ejecuta la acción de disposición final (`EstadoObjeto = 4`), sellando automáticamente `FechaBaja` y consignando en `ObservacionesEntrega` el destino institucional asignado (donación, archivo o destrucción según normativa universitaria).

---

## 4. Restricciones de Consistencia en Base de Datos (CHECK Constraints)

Para garantizar la integridad transaccional ante cualquier operación directa o mediante API, la tabla `OBJETOS_PERDIDOS` implementa las siguientes reglas a nivel motor SQL Server:

### A. Consistencia de Devolución (`CK_OBJETOS_PERDIDOS_ConsistenciaDevolucion`)
```sql
CONSTRAINT CK_OBJETOS_PERDIDOS_ConsistenciaDevolucion CHECK (
    (EstadoObjeto = 2 AND FechaHoraDevolucion IS NOT NULL AND IdUsuarioEntrega IS NOT NULL AND NombrePersonalEntrega IS NOT NULL AND (AlumnoID IS NOT NULL OR DocumentoReceptor IS NOT NULL))
    OR
    (EstadoObjeto <> 2 AND FechaHoraDevolucion IS NULL AND IdUsuarioEntrega IS NULL AND NombrePersonalEntrega IS NULL AND DocumentoReceptor IS NULL AND CodigoURPReceptor IS NULL AND AlumnoID IS NULL)
)
```
- **Garantía:** Es imposible marcar un objeto como devuelto sin identificar al receptor y al personal que entrega. Recíprocamente, un objeto en custodia o dispuesto no puede tener datos residuales de entrega.

### B. Consistencia de Baja (`CK_OBJETOS_PERDIDOS_ConsistenciaBaja`)
```sql
CONSTRAINT CK_OBJETOS_PERDIDOS_ConsistenciaBaja CHECK (
    (EstadoObjeto = 4 AND FechaBaja IS NOT NULL)
    OR
    (EstadoObjeto <> 4 AND FechaBaja IS NULL)
)
```
- **Garantía:** Exige fecha de baja obligatoria si el estado es `Dispuesto` (`4`) y prohibe registrar fecha de baja en objetos en custodia o devueltos.

---

## 5. Protocolo de Entrega Segura y Privacidad de Datos

1. **Catálogo Público Informativo (`GET /api/v1/objetos-perdidos/publico`):**
   - Muestra exclusivamente: `IdObjeto`, `Titulo`, `Categoria`, `LugarHallazgo` y `FechaHoraHallazgo`.
   - **Blindaje de Privacidad:** No se publican números completos de documentos encontrados, contenido interno de billeteras/mochilas, ni señas particulares críticas (como claves de bloqueo, marcas internas o fotografías detalladas). Esto previene reclamos fraudulentos por terceros.
2. **Verificación Presencial de Titularidad:**
   - El estudiante o usuario acude a la recepción de la biblioteca.
   - Debe describir detalles particulares del bien no visibles en la ficha pública (por ejemplo: color del fondo de pantalla, stickers, contenidos internos o mostrar la clave de desbloqueo).
3. **Formalización de la Devolución (`POST /api/v1/objetos-perdidos/{id}/entregar`):**
   - El operador valida el código de alumno o documento de identidad en el sistema.
   - El sistema almacena la relación con `AlumnoID` (si es alumno URP registrado en padrón), `CodigoURPReceptor` o `DocumentoReceptor` (si es externo o visitante), vinculando la firma electrónica del usuario administrativo en sesión (`IdUsuarioEntrega`, `NombrePersonalEntrega`).
