# Especificación de interfaz — pastepad

Documento de referencia para implementar la UI en Flet contra las
maquetas de `docs/mockups/`. Todos los valores de aquí deben coincidir
con `pastepad/estilo.py` — si no coinciden, `estilo.py` es el que
está mal y hay que corregirlo, no este documento ni las maquetas.

---

## 1. Mapa de maquetas

| Archivo | Qué muestra | Estado del código |
|---|---|---|
| `01_panel_reciente.svg` | Panel completo, pestaña Reciente, una fila fijada y activa | Implementado |
| `02_panel_guardados.svg` | Panel completo, pestaña Guardados, carpeta desplegable | Implementado |
| `03_panel_seleccionar.svg` | Modo selección múltiple, casillas y pie con Borrar (n) | Implementado |
| `04_dialogo_texto_nuevo.svg` | Diálogo para crear/editar un texto guardado | Implementado |
| `05_dialogo_carpeta.svg` | Diálogo de una sola línea (nombre de carpeta) | Implementado |
| `06_dialogo_campos.svg` | Diálogo de plantilla con `[[campos]]` | Implementado |
| `07_dialogo_lista.svg` | Diálogo de importación masiva | Implementado |
| `08_dialogo_apariencia.svg` | Color, tamaño, estilo de carpetas, atajo | Implementado |
| `09_dialogo_confirmar.svg` | Confirmación de borrado | Implementado |
| `10_estado_vacio_reciente.svg` | "Copia algo y aparecerá aquí" | Implementado |
| `11_estado_vacio_guardados.svg` | "Vacío. Usa Nuevo para guardar un texto" | Implementado |
| `12_busqueda_sin_resultados.svg` | "Nada coincide con esa búsqueda" | Implementado |
| `13_fila_enlace.svg` | Icono de enlace + dominio en vez de contador | Implementado |
| `14_menu_tres_puntos.svg` | Menú contextual de una fila | Implementado |
| `15_menu_carpetas.svg` | Menú desplegable de carpetas | Implementado |
| `16_tamano_mini.svg` | Panel en 300×380 | Implementado |
| `17_tamano_grande.svg` | Panel en 470×700 | Implementado |
| `18_tema_claro.svg` | Paleta clara completa | Implementado |
| `19_pausa_activa.svg` | Cabecera con captura pausada | Implementado |
| `20_estirando_ventana.svg` | **Especulativo** — no verificado en ejecución | Sin verificar |

**El 20 es distinto a los otros 19.** Los primeros diecinueve describen
estados que el código ya define explícitamente (textos, colores,
condiciones en `app.py`). El 20 dibuja una intención de cómo debería
verse el redimensionado nativo de Flet, pero nadie lo ha visto correr
en Windows todavía. Trátalo como hipótesis a validar, no como spec.

---

## 2. Paleta de colores

Fuente de verdad: `pastepad/estilo.py`, diccionarios `OSCURA` y
`CLARA`. **No** `pastepad/config.py` — ese archivo tiene una copia
vieja con valores distintos (`borde` difiere: `#1F1F23` en config vs
`#242429` en estilo; `T_MENOR` es 11 vs 12). Es peso muerto pendiente
de borrar. Si algo no coincide entre los dos, gana `estilo.py`.

### Oscura (por defecto)

| Token | Valor | Uso |
|---|---|---|
| `fondo` | `#0B0B0D` | Fondo del panel |
| `elevado` | `#141417` | Buscador, diálogos |
| `tarjeta` | `#1B1B1F` | Filas no activas, botones secundarios |
| `hover` | `#26262B` | Fila bajo el cursor |
| `borde` | `#242429` | Borde de 1px del panel |
| `texto` | `#F4F4F5` | Texto principal |
| `medio` | `#9C9CA6` | Iconos, texto secundario |
| `tenue` | `#6B6B75` | Subtítulos, texto deshabilitado |

### Clara

| Token | Valor |
|---|---|
| `fondo` | `#F6F6F4` |
| `elevado` / `tarjeta` | `#FFFFFF` |
| `hover` | `#EFEFEC` |
| `borde` | `#E6E6E2` |
| `texto` | `#141416` |
| `medio` | `#5C5C66` |
| `tenue` | `#8E8E98` |

En tema claro, las tarjetas no activas llevan un borde de 1px en vez
de fondo diferenciado, y una sombra sutil (`#00000012` desplazada 2px
abajo) para separarse del fondo sin depender solo del color. Ver
`18_tema_claro.svg`.

### Acentos (elegibles desde Apariencia)

| Nombre | Color | Texto encima |
|---|---|---|
| `menta` (por defecto) | `#2DD4A7` | `#052E23` |
| `azul` | `#4B8DF8` | `#FFFFFF` |
| `violeta` | `#9B7BF7` | `#FFFFFF` |
| `ambar` | `#F5A524` | `#3A2606` |
| `coral` | `#F76B5C` | `#FFFFFF` |
| `rosa` | `#F472B6` | `#3A1128` |

El color "texto encima" cambia según el acento porque algunos son
claros y otros oscuros — nunca usar blanco fijo sobre ámbar o rosa,
falla el contraste.

Rojo de peligro (fijo, no cambia con el tema): `#DC2626`, hover
`#B91C1C` — reservado para el botón "Borrar" y estados de error.

---

## 3. Medidas

### Tamaños de ventana

| Nombre | Ancho × Alto | Notas |
|---|---|---|
| `mini` | 300 × 380 | Filas de una sola línea, sin subtítulo |
| `chico` | 340 × 460 | |
| `mediano` | 380 × 560 | **Por defecto** |
| `grande` | 470 × 700 | |

Límites absolutos: mínimo 300×340, máximo 720×1100 (`config.MIN_ANCHO`
/ `MAX_ANCHO` / `MIN_ALTO` / `MAX_ALTO`).

### Radios

| Elemento | Radio |
|---|---|
| Panel completo | 20px |
| Diálogos | 20px |
| Filas / tarjetas | 14px |
| Botones, campos de texto | 10–12px |
| Pestañas, fichas de carpeta | 15–18px (cápsula) |
| Botones circulares de icono | círculo completo (radio = mitad del lado) |

### Espaciado

Grid de 4px. Los valores que se repiten en el código:
`4, 8, 12, 16, 24`. No usar números sueltos como 15 o 18 para
padding — redondear al múltiplo de 4 más cercano.

### Tipografía

Fuente: `Segoe UI Variable Display` si está disponible (Windows 11),
si no `Segoe UI` (Windows 10). Cuatro tamaños, nada intermedio:

| Token | Tamaño | Uso |
|---|---|---|
| `T_TITULO` | 15px | Títulos de diálogo |
| `T_CUERPO` | 13px | Texto de fila, campos de entrada |
| `T_MENOR` | 12px | Botones, pestañas |
| `T_MINI` | 11px | Subtítulos, contador de caracteres |

### Filas de la lista

- Altura normal: **56px**
- Altura en modo mini: **42px**, una sola línea de texto (sin
  subtítulo)
- Separación entre filas: 6px
- Padding interno: 16px izquierda cuando no hay icono de enlace/marca,
  22px cuando sí

---

## 4. Comportamiento (no solo visual)

Estas reglas están en el código y las maquetas las asumen; si la
implementación no las cumple, no es un problema de estilo sino de
lógica.

**Barra de acento.** Solo la fila seleccionada lleva una barrita
blanca vertical de 3px a la izquierda (ver `01`, `13`, `17`). No es
decorativa nada más — es la señal de foco que no depende solo del
color de fondo, para quienes tienen dificultad para distinguir
colores.

**Iconos por fila, solo al pasar el cursor.** El botón de tres puntos
(`···`) y el alfiler de "fijar" no están siempre visibles — aparecen
al hover o cuando la fila está activa. Excepción: si algo ya está
fijado, el alfiler se queda visible siempre, para saber qué está
fijado sin pasar el mouse por cada fila.

**Fila de enlace.** Solo si el texto completo copiado es una URL
(`modelo.es_enlace()`) — no si el texto *contiene* una URL en medio de
una oración. Cuando aplica: icono de enlace en vez del icono normal,
y el dominio (`modelo.dominio_de()`) en vez del contador de
caracteres. Un clic abre el navegador en vez de pegar.

**Carpetas: dos modos intercambiables.** `menu` (desplegable, un solo
botón) o `fichas` (una cápsula por carpeta en fila horizontal). Se
elige desde Apariencia y se recuerda entre sesiones
(`config.CARPETAS_MENU` / `CARPETAS_FICHAS`). El desplegable es el
modo por defecto porque con muchas carpetas las fichas no caben en el
ancho del panel.

**Pausa.** El botón cambia de icono (pausa ↔ play) y de color (gris ↔
rojo `#EF4444`) según el estado. Aparece además un texto "En pausa" en
rojo a la izquierda de la cabecera quimten está activo. Ver `19`.

---

## 5. Qué comparar primero

Si vas a revisar la implementación contra las maquetas, este orden da
más señal por menos esfuerzo:

1. **Colores exactos** — abre `pastepad/estilo.py` al lado de
   cualquier maqueta y compara valores hex uno a uno. Es mecánico y
   encuentra errores rápido.
2. **`13`, `14`, `15`** — son las que muestran comportamiento
   (detección de enlace, menú contextual, menú de carpetas), no solo
   estructura. Ahí es más probable que el código real diverja de la
   intención.
3. **`10`, `11`, `12`** — confirma que los tres mensajes de estado
   vacío están exactamente donde dice `app.py.refrescar()`, no
   inventados.
4. **`20` al final, con escepticismo** — es la única maqueta sin
   verificar en ejecución real. Si el redimensionado se ve distinto a
   esto, confía en lo que Windows realmente hace, no en el dibujo.

---

## 6. Lo que este documento no cubre

- Animaciones y transiciones (duración, curva) — están en el código
  como `ft.Animation(...)`, no hay maqueta estática que las capture.
- Comportamiento del atajo global — ver `CLAUDE.md`, sección
  "Problema abierto".
- Accesibilidad más allá de la barra de acento — no se ha auditado
  contraste de color ni navegación por teclado de forma sistemática.
