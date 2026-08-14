# Especificación de interfaz — pastepad

Documento de referencia para implementar la UI contra las maquetas de
`docs/mockups/`. Los valores de la paleta deben coincidir con
`pastepad/estilo.py` — si no coinciden, gana `estilo.py`.

**La tecnología ya no es Flet.** Este documento se escribió para la v3;
la reescritura va en C# con WinUI 3 (ver `PLAN.md`). Los números siguen
valiendo todos; lo que cambia es dónde se escriben. `estilo.py` se
conserva como **fuente de datos de la paleta**, no como código vivo: su
traducción es `csharp/Pastepad.App/Estilo.cs`.

**Qué pone el sistema y qué ponemos nosotros.** WinUI 3 trae Mica, el
cambio de tema claro/oscuro en vivo, `Segoe UI Variable`,
`Segoe Fluent Icons` y las esquinas redondeadas de la ventana. Todo eso
se usa tal cual. Este documento manda solo donde dice algo distinto a
propósito: la paleta de acentos, las medidas de fila y la barra blanca
de la fila activa.

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
`CLARA`, **salvo `medio` y `tenue`**, que se corrigieron por contraste
y mandan desde aquí y desde `Estilo.cs` — ver la sección 6. **No** `pastepad/config.py` — ese archivo tiene una copia
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
| `tenue` | `#86868E` | Subtítulos, texto deshabilitado |

### Clara

| Token | Valor |
|---|---|
| `fondo` | `#F6F6F4` |
| `elevado` / `tarjeta` | `#FFFFFF` |
| `hover` | `#EFEFEC` |
| `borde` | `#E6E6E2` |
| `texto` | `#141416` |
| `medio` | `#5C5C66` |
| `tenue` | `#707079` |

En tema claro, las tarjetas no activas llevan un borde de 1px en vez
de fondo diferenciado, y una sombra sutil (`#00000012` desplazada 2px
abajo) para separarse del fondo sin depender solo del color. Ver
`18_tema_claro.svg`.

### Los otros diez fondos

Además de Oscura y Clara, `estilo.py` define diez paletas más. Con
"auto" son doce opciones en el selector de Apariencia, que es lo que
dice el README.

**Cuatro oscuras:** `medianoche` (`#0A0F1E`), `grafito` (`#141414`),
`bosque` (`#0A140F`) y la propia `oscuro`.
**Siete claras:** `papel` (`#F5F1E8`), `niebla` (`#EEF1F5`), `arena`
(`#F7F4EF`), `lila` (`#F4F1FA`), `salvia` (`#EEF7F3`), `rubor`
(`#FBF2F4`) y la propia `claro`.

Los ocho tokens de cada una están en `estilo.py` y traducidos en
`Estilo.cs`. **Oscura y Clara las pone el sistema** (`ElementTheme.Default`
sigue a Windows y cambia en vivo); las otras diez las pone el usuario a
propósito y mandan sobre el sistema.

### Acentos (elegibles desde Apariencia)

Son **dieciocho**, no seis. Las seis de la tabla vieja eran las que
dibuja la maqueta 08; `estilo.py` —que es la fuente de verdad— define
dieciocho, y el README ya hablaba de "18 acentos y 12 fondos".

| Nombre | Color | Texto encima | Contraste |
|---|---|---|---|
| `menta` (por defecto) | `#2DD4A7` | `#052E23` | 7.80 |
| `azul` | `#4B8DF8` | `#04183C` | 5.38 |
| `violeta` | `#9B7BF7` | `#1E1046` | 5.40 |
| `ambar` | `#F5A524` | `#3A2606` | 7.05 |
| `coral` | `#F76B5C` | `#3D0F09` | 5.68 |
| `rosa` | `#F472B6` | `#3A1128` | 6.14 |
| `cian` | `#22D3EE` | `#062E36` | 8.00 |
| `lima` | `#A3E635` | `#1A2E05` | 9.69 |
| `indigo` | `#818CF8` | `#111539` | 5.92 |
| `turquesa` | `#2DD4BF` | `#042F2A` | 7.81 |
| `durazno` | `#FB923C` | `#3B1D06` | 6.80 |
| `lavanda` | `#C084FC` | `#2E1065` | 5.77 |
| `esmeralda` | `#34D399` | `#04291C` | 8.16 |
| `cielo` | `#38BDF8` | `#052F42` | 6.57 |
| `oro` | `#FCD34D` | `#3B2606` | 9.94 |
| `fresa` | `#FB7185` | `#3F0A16` | 6.16 |
| `menta_fria` | `#5EEAD4` | `#032F2A` | 9.83 |
| `arena` | `#D6BC8A` | `#332612` | 8.00 |

El color "texto encima" cambia según el acento porque algunos son
claros y otros oscuros — nunca usar blanco fijo sobre ámbar o rosa,
falla el contraste. La columna de contraste está **calculada** con la
fórmula de WCAG 2.1, no estimada: los dieciocho pasan AA para texto
normal (4.5:1), y el peor es `azul` con 5.38.

Tres pares cambiaron respecto de la tabla vieja de este documento:
`azul`, `violeta` y `coral` llevaban `#FFFFFF` encima y se quedaban
en 3:1. Los valores buenos son los de `estilo.py`, que ya estaban
corregidos.

Rojo de peligro (fijo, no cambia con el tema): `#DC2626`, hover
`#B91C1C` — reservado para el botón "Borrar" y estados de error.

---

## 3. Medidas

### Tamaño de la ventana

**El panel es adaptable y no tiene preajustes.** Se estira arrastrando
sus bordes, recuerda el tamaño entre sesiones, y todo lo de dentro
—filas, buscador, pestañas, carpetas y pie— se ajusta con él. El
selector de tamaño que dibujaba la maqueta 08 se retiró: unos
preajustes fijos al lado de algo que ya se adapta sobran y confunden.

| | |
|---|---|
| Tamaño inicial | 380 × 560 |
| Mínimo | 300 × 340 |
| Máximo | 720 × 1100 |
| Umbral de fila compacta | por debajo de **340** de ancho |

Las maquetas **16 y 17** ya no describen una opción: son dos ejemplos
de cómo tiene que comportarse el panel adaptable a 300 y a 470 de
ancho. A 300 la fila baja a 42px y pierde el subtítulo; "Seleccionar"
desaparece del pie y "Nuevo" se queda con el signo más.

El tamaño se mide sobre **lo que se ve**, no sobre el rectángulo de
ventana: una ventana redimensionable lleva alrededor un marco invisible
—medido en esta máquina, 7px a cada lado y 7 abajo— que `GetWindowRect`
y `AppWindow.Size` cuentan y la pantalla no enseña. Sin descontarlo, un
panel pedido de 380×560 se veía de 366×553.

`Config.Tamanos` ya no existe: los cuatro preajustes se retiraron del
núcleo. El tamaño inicial son `Config.AnchoDef` y `Config.AltoDef`, dos
medidas sueltas junto a los mínimos y máximos, que sí se quedan como
topes del redimensionado.

### Radios y marco de la ventana

| Elemento | Radio |
|---|---|
| Panel completo | **8px, y lo pone Windows** (ver abajo) |
| Diálogos | 20px |
| Filas / tarjetas | 14px |
| Botones, campos de texto | 10–12px |
| Pestañas, fichas de carpeta | 15–18px (cápsula) |
| Botones circulares de icono | círculo completo (radio = mitad del lado) |

**El panel no dibuja esquinas ni borde propios.** El `Border` raíz va
con `CornerRadius="0"` y `BorderThickness="0"` a propósito.

Tener dos redondeos —el de la ventana y el nuestro— fue un defecto real
y visible: si el nuestro es mayor, la franja entre las dos curvas se
queda sin pintar y asoma lo que haya detrás. Sobre fondo oscuro apenas
se notaba; en tema claro salían **dos cuñas oscuras en las esquinas de
arriba**. Un solo redondeo, y lo pone el sistema. El borde de 1px lo
dibuja Windows con la ventana, así que el nuestro solo lo doblaba.

La ventana va con `SetBorderAndTitleBar(true, false)`,
`ExtendsContentIntoTitleBar` y `DWMWCP_ROUND` pedido explícitamente.

**Radio medido sobre el render**, contando píxeles en la esquina
superior izquierda a 100% de escalado: 8, 5, 3, 1, 1 px de mordida en
las filas y = 1, 2, 4, 8 y 12. Es un cuarto de círculo de **radio 8px**.
Escala con el DPI: a 125% son 10px.

**No se puede redondear más, y no es una decisión nuestra.**
`DWM_WINDOW_CORNER_PREFERENCE` solo tiene cuatro valores —`DEFAULT`,
`DONOTROUND`, `ROUND` y `ROUNDSMALL`— y **ninguno acepta un radio en
píxeles**; `ROUND` es el mayor que se puede pedir y es el que se pide.
Fuente: la página de la enumeración en learn.microsoft.com.

Conseguir un radio propio exigiría ventana sin marco del sistema y
esquinas pintadas por nosotros, y eso trae de vuelta las dos cosas que
ya se arreglaron: la banda que el sistema reserva arriba para la barra
de título, y las cuñas —porque el área fuera de nuestra curva sigue sin
ser transparente—. Los 20px que pedía este documento quedan
**superados por lo que el sistema permite**.

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
(`Modelo.EsEnlace`) — no si el texto *contiene* una URL en medio de una
oración. Cuando aplica: el dominio (`Modelo.DominioDe`) en vez del
contador de caracteres, y «Abrir en el navegador» en el menú de la
fila. **El clic pega**, como en cualquier otra fila.

Hasta la 4.3.0 el clic abría el navegador. Se quitó porque por el mismo
sitio pasaban los cuatro caminos que llevan a pegar —el clic, el Enter
del buscador, y «Pegar» y «Pegar sin formato» del menú—, así que un
enlace no se podía pegar en ningún campo: el elemento que decía «Pegar»
abría Chrome. Comprobado de punta a punta contra el Bloc de notas.

**Guardados va en cinco grupos plegables: marcadores, plantillas,
correos, prompts de IA y notas.** Ninguno es otro tipo de dato: son
guardados que se separan porque no se usan igual, y mezclarlos obliga a
leer la lista entera para encontrar cualquiera de ellos.

Iconos por tipo, todos de Segoe Fluent Icons y verificados en la lista
oficial: marcador `E71B` (Link), plantilla `E943` (Code), correo `E715`
(Mail), prompt `EAB7` (ChatSparkle — la familia «sparkle» con la que
Windows marca lo de IA). La nota no lleva icono: es la mayoría, y una
columna de iconos en todas las filas sería ruido.

**El tipo lo elige el usuario** en el desplegable del diálogo de
guardar, que comparte línea con la etiqueta «Nombre». pastepad propone
el que se deduce del texto —una URL entera es marcador, unos
`[[campos]]` plantilla, el resto nota— y deja de proponer en cuanto el
usuario lo toca. Lo elegido solo se escribe en `snippets.json` si
contradice a lo deducido, así que un archivo de antes de la 4.4.0 se
vuelve a escribir sin una clave de más.

**Ni correo ni prompt de IA se deducen nunca.** No hay nada en un cuerpo
de correo que lo separe de una nota, y proponerlo por llevar una arroba
convertiría en correo cualquier texto que mencione una dirección. Con un
prompt es peor: no hay ni siquiera una arroba de la que tirar. Son los
dos tipos que existen precisamente para elegirlos a mano.

Un prompt con `[[campos]]` se propone como plantilla, y está bien que
así sea: lo que decide si pregunta antes de pegar es el texto, no el
tipo. Quien lo quiera entre sus prompts lo cambia y le sigue
preguntando los campos igual.

La cabecera de grupo mide 32px, lleva chevron, icono y contador, y
**solo aparece cuando hay más de un grupo con algo dentro**: con uno
solo no hay nada que separar.

**Carpetas: dos modos intercambiables.** `menu` (desplegable, un solo
botón) o `fichas` (una cápsula por carpeta en fila horizontal). Se
elige desde Apariencia y se recuerda entre sesiones en la preferencia
`"carpetas"`. El desplegable es el modo por defecto porque con muchas
carpetas las fichas no caben en el ancho del panel.

Ojo: la v3 en Python **había retirado el modo fichas** por no usarse
(`ventanas.py`, línea 286). Se recupera porque el usuario lo pidió
expresamente, y porque esta sección siempre lo describió.

**Idioma.** La aplicación tiene cuatro (`es`, `en`, `pt`, `fr`) con 97
cadenas traducidas en `idiomas.py`, y el diálogo de Apariencia no las
ofrecía: hueco de la especificación, no del código. Ya hay selector, y
se guarda en la preferencia `"idioma"` que el formato de datos ya
tenía.

**Cómo se agrupa Apariencia.** Tres bloques, en el orden en que se
tocan, con el formato de la Configuración de Windows 11: una tarjeta
por ajuste, **rótulo arriba y control debajo**, a todos los anchos.

1. **Color de acento** — las 18 bolitas, y debajo el fondo.
2. **Carpetas** — desplegable o fichas.
3. **Sistema** — atajo global e idioma.

Sin sección de tamaño.

**Una sola disposición, y por qué no hay dos.** Hubo un intento de
poner el rótulo a la izquierda y el control a la derecha cuando el
panel fuera ancho, y se retiró después de medirlo a siete anchos. La
fila de dos columnas no entra sin romper algo hasta un panel de unos
**455 px**, muy por encima de los 380 de fábrica:

- por debajo, el rótulo se parte en dos y tres líneas — "Cómo / se /
  enseñan" con el panel a 366;
- y si se le da prioridad al rótulo, el que pierde es el desplegable,
  que **no pone puntos suspensivos: recorta por la izquierda**. Medido:
  "l + Shift + V" en vez de "Ctrl + Shift + V", que es perder el dato
  sin avisar.

Apilar siempre evita las dos cosas en todo el rango 300–720 y quita una
clase entera de fallos que dependen del ancho.

**Márgenes del diálogo.** El contenido va centrado por construcción: el
ancho del diálogo se fija al del panel menos 16, y el relleno es el
mismo a los dos lados. Medido sobre el render:

| Ancho del panel | Diálogo | Margen izq. | Margen der. |
|---|---|---|---|
| 300 | 280 | 49 | 50 |
| 380 | 360 | 49 | 50 |
| 720 | 700 | 49 | 49 |

Constante y simétrico. En el caso más apretado, 300, a la tarjeta le
quedan **149 px por dentro** y el valor más largo del desplegable
—"Lista desplegable"— sigue entrando entero.

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
- Navegación por teclado de forma sistemática.

### El contraste, auditado y corregido

El contraste se calculó con la fórmula de WCAG 2.1 para toda la paleta,
no se miró. Salieron tres cosas.

**1. Los 18 acentos pasan** AA para texto normal. El peor es `azul` con
5.38:1. La tabla está en la sección 2.

**2. `tenue` no llegaba en ninguno de los 12 fondos, y se ha subido.**

`tenue` es el color del subtítulo de cada fila —"29 caracteres", el
nombre de la carpeta— y del mensaje de los estados vacíos: texto que
informa, no adorno. Daba entre 2.64:1 y 3.31:1 cuando AA pide 4.5:1.

Cada fondo lleva su propio valor: la corrección que arregla `salvia` no
arregla `lila`. Se obtuvo mezclando `tenue` hacia el `texto` de su
propia paleta —lo justo para llegar a 4.5:1 y ni un paso más, para
mover el aspecto lo mínimo— y midiendo contra el peor de sus dos
fondos (`fondo` y `tarjeta`; en la oscura, también contra el gris de
Mica, `#202020`, que es lo que se ve de verdad detrás del panel).

| Fondo | `tenue` antes | ratio | `tenue` ahora | ratio |
|---|---|---|---|---|
| oscura | `#6B6B75` | 3.09 | `#86868E` | **4.51** |
| clara | `#8E8E98` | 3.00 | `#707079` | **4.53** |
| medianoche | `#64708C` | 3.22 | `#7E89A1` | **4.54** |
| grafito | `#6E6E6E` | 3.04 | `#8B8B8B` | **4.56** |
| bosque | `#5E7A6B` | 3.31 | `#779082` | **4.51** |
| papel | `#96897A` | 3.03 | `#776C60` | **4.55** |
| niebla | `#8A95A6` | 2.67 | `#636E80` | **4.55** |
| arena | `#948B7E` | 3.06 | `#776F65` | **4.51** |
| lila | `#9086AE` | 3.03 | `#73698E` | **4.54** |
| salvia | `#849E90` | 2.64 | `#5D766A` | **4.51** |
| rubor | `#A5858E` | 3.01 | `#866871` | **4.52** |

Comprobado además sobre el render, no solo sobre el código: el píxel del
subtítulo mide `#86868E` en oscuro y `#707079` en claro.

**3. Al subir `tenue` se perdía el escalón con `medio`, y se ha
recuperado subiendo `medio` en seis paletas.**

Con `tenue` en 4.5:1, en cuatro fondos claros quedaba pegado a `medio`
—hasta 1.07:1 en `salvia`— y el subtítulo dejaba de leerse como
secundario: se cambiaba un problema por otro. Como `tenue` ya no puede
bajar sin romper AA, el escalón solo se recupera subiendo `medio`. Se
fijó en 1.30:1, que es el que ya tenían las paletas que no se
estropearon.

| Fondo | `medio` antes | `medio` ahora | escalón antes | escalón ahora |
|---|---|---|---|---|
| papel | `#6B6155` | `#645B4F` | 1.18 | **1.30** |
| niebla | `#5A6779` | `#505C6E` | 1.12 | **1.31** |
| arena | `#6E675C` | `#645D53` | 1.13 | **1.31** |
| lila | `#655885` | `#635682` | 1.26 | **1.30** |
| salvia | `#557264` | `#496458` | 1.07 | **1.31** |
| rubor | `#77565F` | `#76555E` | 1.29 | **1.31** |

Las otras cinco no se tocan. En `lila` y `rubor` el ajuste es del 4% y
del 1%: existe para que la regla se cumpla en las once, no porque se
vea. Los seis `medio` nuevos quedan además por encima de 5.9:1, así que
también suben de sobra el listón de AA.

**Estos valores mandan sobre `pastepad/estilo.py`**, que se queda con
los viejos. Es la única excepción a la regla de la sección 2, y es
deliberada: legibilidad por delante del aspecto heredado.

**4. La barra blanca de foco de 3px da 1.44:1 sobre `oro` y 1.89:1
sobre `menta`**, por debajo del 3:1 que pide WCAG 1.4.11 para un
elemento gráfico. Se mantiene blanca porque así lo dicen las maquetas y
porque su trabajo es ser una señal *de forma*, no de color, sobre una
fila que ya está diferenciada por el fondo de acento. Dicho con el
número delante para que se decida a sabiendas.
