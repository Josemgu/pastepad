# Referencia de funciones

Las 133 funciones y métodos de pastepad 3.0.0, en orden de dependencia:
los módulos de abajo no saben nada de los de arriba.

| Módulo | Funciones | Depende de |
|---|---|---|
| `config.py` | 2 | — |
| `registro.py` | 3 | config |
| `modelo.py` | 30 | config |
| `busqueda.py` | 7 | modelo |
| `windows.py` | 20 | config |
| `estilo.py` | 7 | config, windows |
| `filas.py` | 7 | config, estilo, modelo |
| `ventanas.py` | 9 | config, estilo, modelo |
| `app.py` | 47 | todos |
| `main.py` | 1 | estilo, registro, windows, app, modelo |

---

## config.py — constantes y rutas

Sin lógica: solo valores y dos funciones que resuelven dónde vive todo.
No importa Flet ni tkinter, a propósito.

**`carpeta_base()`**
Devuelve el directorio donde se guardan los datos del usuario.
Si el programa está congelado en un `.exe` usa la carpeta del ejecutable;
si corre desde el código fuente, sube dos niveles desde `config.py`.

**`ruta_icono()`** — *sin usar desde la v3*
Busca el `.ico` en tres sitios: el temporal de PyInstaller (`sys._MEIPASS`),
la carpeta base y `docs/`. Devuelve el primero que exista, o `None`.
Era para la ventana de tkinter; Flet resuelve el icono por su cuenta.

---

## registro.py — el rastro de los fallos

El único módulo que escribe en `errores.log`. Sin dependencias gráficas,
así que se puede llamar desde cualquier hilo.

**`anotar(texto, origen="-")`**
Añade una entrada al log con fecha, versión y de dónde viene.
Toma un candado porque los tres hilos pueden escribir a la vez, y nunca
lanza: si lo hiciera, tumbaría al manejador de errores que la llamó.

**`fallo(origen, repetir=True)`**
Anota la excepción que se está manejando en ese momento; va dentro de un
`except`, donde antes había un `pass`.
Con `repetir=False` no vuelve a escribir un fallo idéntico consecutivo —
un bucle que falla cada 0,7 s llenaría el archivo en una tarde.

**`instalar()`**
Engancha `sys.excepthook` y `threading.excepthook`.
Son dos ganchos distintos: el primero solo cubre el hilo principal y el
segundo lo que revienta dentro de un `Thread`. **Ninguno cubre
`page.run_thread`**, que entrega el trabajo a un `ThreadPoolExecutor` y
atrapa la excepción en su `Future`; por eso `_vigilar` y `_atender_cola`
capturan y anotan por su cuenta.

---

## modelo.py — los datos y sus reglas

El único módulo que toca los JSON. Sin dependencias gráficas: por eso
las 19 pruebas corren sin abrir ventana.

### Archivos

**`_leer(ruta, defecto)`**
Carga un JSON y devuelve su contenido.
Si el archivo no existe, está corrupto o no se puede leer, devuelve
`defecto` en silencio — arrancar siempre gana sobre avisar del fallo.

**`_escribir(ruta, datos)`**
Guarda un JSON de forma atómica: escribe en `ruta.tmp` y luego hace
`os.replace`, que en Windows es una operación indivisible.
Un corte a mitad de escritura deja el archivo bueno intacto, no truncado.

### Fragmentos de texto

**`fragmento(texto, fuente, tam, negrita, cursiva, subrayado, color)`**
Crea un trozo de texto con formato, en claves de una letra (`t`, `f`, `s`,
`b`, `i`, `u`, `c`) para que el JSON no engorde.
Un snippet guardado es una lista de estos.

**`texto_de(fragmentos)`**
Concatena el campo `t` de todos los fragmentos y devuelve el texto plano.
Es la conversión que usa todo el resto del programa cuando le da igual
el formato.

**`una_linea(texto, tope=52)`**
Resume un texto a una sola línea para mostrarlo en la lista.
Recorta a `tope*4` caracteres **antes** de colapsar espacios: hacerlo al
revés sobre un texto de miles de líneas costaba casi un segundo.

### Enlaces

**`es_enlace(texto)`**
Devuelve `True` solo si el texto entero es una dirección web.
Rechaza si hay espacios, saltos de línea o más de 2000 caracteres, y exige
que empiece por `http://`, `https://` o `www.` — así un párrafo que
menciona una URL de pasada no se abre al hacer clic.

**`url_de(texto)`**
Devuelve la dirección lista para el navegador.
Si empieza por `www.` le antepone `https://`; si ya trae esquema, la deja.

**`dominio_de(texto)`**
Extrae el dominio suelto para mostrarlo debajo del título.
Quita el esquema, quita el `www.`, corta en la primera barra y limita a
60 caracteres.

### Plantillas

**`campos_de(texto)`**
Busca los marcadores `[[campo]]` y devuelve sus nombres en orden, sin
repetir. Recorre el texto con `index` en vez de una expresión regular.
Es lo que decide si al pegar hay que preguntar algo antes.

**`rellenar(fragmentos, valores)`**
Sustituye cada `[[clave]]` por su valor en todos los fragmentos.
Copia cada fragmento antes de tocarlo, así el snippet guardado no se
modifica al pegarlo.

### Almacen — el estado completo

**`Almacen.__init__()`**
Lee los tres JSON (preferencias, historial, snippets) y los deja en memoria.
Repara los snippets viejos que no tienen `runs` construyéndoles uno desde
su campo `texto`, para que la v1 siga abriendo en la v3.

**`Almacen.pref(clave, defecto)`**
Lee una preferencia del diccionario en memoria.
No toca el disco.

**`Almacen.poner_pref(clave, valor)`**
Cambia una preferencia y la guarda en `config.json` inmediatamente.
Cada llamada es una escritura completa del archivo.

**`Almacen.guardar_datos()`**
Vuelca carpetas y snippets a `snippets.json`.
Lo llaman todas las operaciones que modifican los guardados.

**`Almacen.guardar_hist()`**
Vuelca el historial a `historial.json`.
Lo llaman todas las operaciones que modifican el historial.

**`Almacen.crear_carpeta(nombre)`**
Añade una carpeta si no existe ya y guarda.
Devuelve `True` si la creó, `False` si estaba repetida o el nombre iba vacío.

**`Almacen.renombrar_carpeta(viejo, nuevo)`**
Cambia el nombre de la carpeta y arrastra todos sus snippets con ella.
Rechaza si el nombre nuevo está ocupado o el viejo no existe.

**`Almacen.contenido_de(carpeta)`**
Devuelve la lista de snippets cuya `categoria` es esa carpeta.
Filtro simple, sin caché.

**`Almacen.borrar_carpeta(carpeta)`**
Elimina la carpeta y todos los snippets que contiene.
Devuelve cuántos se llevó por delante, que es lo que el diálogo de
confirmación enseña al usuario antes de hacerlo.

**`Almacen.anadir_snippet(snippet)`**
Guarda un texto nuevo, creando su carpeta si hacía falta.
Escribe a disco en la misma llamada.

**`Almacen.reemplazar_snippet(viejo, nuevo)`**
Sustituye un snippet por su versión editada, en el mismo sitio de la lista.
Devuelve `False` si el viejo ya no estaba.

**`Almacen.anotar(entrada)`**
Mete algo copiado al principio del historial, justo debajo de los fijados.
Antes comprueba las **4 primeras** entradas para no duplicar lo que se
acaba de copiar; a partir de la quinta, un duplicado sí entra.

**`Almacen._recortar()`**
Deja como mucho `MAX_HIST` (80) entradas sueltas, borrando las más viejas.
Las fijadas no cuentan para el límite ni se borran nunca.

**`Almacen._borrar_imagen(entrada)`**
Si la entrada es una imagen, borra su `.bmp` del disco.
Ignora cualquier fallo: que no se pueda borrar el archivo no debe impedir
que la entrada desaparezca de la lista.

**`Almacen.fijar(entrada)`**
Invierte el estado de fijado de una entrada del historial y guarda.
No la reordena: de eso se encarga `hist_ordenado()` al pintar.

**`Almacen.borrar(elemento)`**
Borra tanto del historial como de los guardados; distingue por si el
elemento tiene la clave `tipo`.
Si es imagen, borra también el archivo.

**`Almacen.borrar_varios(elementos)`**
Llama a `borrar` sobre cada elemento y devuelve cuántos se borraron.
Es lo que usa el modo de selección múltiple.

**`Almacen.vaciar_historial()`**
Deja el historial solo con las entradas fijadas.
Borra del disco las imágenes de todo lo que se lleva.

**`Almacen.hist_ordenado()`**
Devuelve el historial con los fijados arriba y el resto debajo.
No modifica la lista guardada: el orden es cosa de la vista.

**`Almacen.guardar_imagen(datos_dib, a_bmp)`**
Escribe la imagen del portapapeles en `imagenes/` con nombre por marca de
tiempo en milisegundos, y la anota en el historial.
Recibe el conversor `a_bmp` como parámetro para no importar `windows.py`.

---

## busqueda.py — ranking

Ordena por parecido, no por fecha. Independiente de la interfaz.

**`normalizar(texto)`**
Pasa a minúsculas y quita tildes con una tabla de traducción.
Corta a 4000 caracteres: más allá de eso el texto no aporta al ranking y
normalizarlo cuesta.

**`puntuar(palabras, titulo, cuerpo)`**
Devuelve cuánto encaja una entrada, o `None` si falta alguna palabra.
Suma 100 por aparecer en el título y 30 en el cuerpo, con extra por
empezar palabra y por estar cerca del principio; 80 más si la frase
completa aparece literal.

**`Indice.__init__(almacen)`**
Guarda la referencia al almacén y prepara dos cachés vacías.
No indexa nada hasta que alguien busca.

**`Indice.invalidar()`**
Marca la lista como caducada poniéndola a `None`.
La caché de textos normalizados **no** se tira: se reaprovecha entrada
por entrada.

**`Indice._normalizado(dato, tipo)`**
Devuelve `(titulo, cuerpo)` ya normalizados, tirando de caché.
La clave es `id(dato)` y se valida comparando con `is` el texto original,
así un `id` reciclado por el recolector da fallo de caché y no dato erróneo.

**`Indice.entradas()`**
Construye la lista completa de `(dato, tipo, titulo, cuerpo)` y la cachea.
De paso limpia de la caché las entradas cuyo objeto ya no existe.

**`Indice.buscar(consulta)`**
Puntúa todas las entradas y devuelve las que encajan, de más a menos.
Suma 40 a los guardados: algo que guardaste a propósito pesa más que algo
que copiaste de pasada.

---

## windows.py — todo lo que habla con el sistema

El único módulo con `ctypes` y `win32*`. Todas las funciones degradan a un
valor por defecto si pywin32 no está (`HAY_WIN32`).

### Portapapeles

**`_con_portapapeles(accion, intentos=4)`**
Abre el portapapeles, ejecuta `accion` y lo cierra pase lo que pase.
Windows solo deja abrirlo a un proceso a la vez, así que reintenta con
40 ms de espera entre intentos.

**`contenido_privado()`**
Detecta si quien copió pidió que no se guarde, mirando cuatro formatos
que registran KeePass, Bitwarden, el Administrador de credenciales y el
incógnito de Chrome. Requiere el portapapeles **ya abierto**.

**`secuencia()`**
Devuelve el contador que Windows incrementa con cada copia.
Es una sola llamada barata; comparar este número evita abrir el
portapapeles cuatro mil veces por hora sin necesidad.

**`leer()`**
Devuelve `('texto', str)`, `('imagen', bytes)`, `('privado', None)` o
`(None, None)`. Comprueba primero la marca de privacidad, luego texto
Unicode y luego imagen DIB. Sin pywin32 cae a `pyperclip`.

**`_escapar_rtf(s)`**
Escapa un texto para meterlo en un documento RTF.
Protege `\`, `{`, `}`, convierte saltos en `\par` y tabuladores en `\tab`,
y todo lo que pase de ASCII lo emite como `\uNNNN?`.

**`a_rtf(fragmentos)`**
Genera el documento RTF completo con su tabla de fuentes y de colores.
Es el formato que entienden Word y Outlook, y lo que hace que el pegado
conserve negritas y color.

**`copiar(fragmentos, texto_de, sin_formato=False)`**
Deja el contenido en el portapapeles en dos formatos a la vez: RTF para
quien lo entienda, texto plano para el resto.
Si falla el camino con formato, cae a `pyperclip` con el texto plano.

**`dib_a_bmp(dib)`**
Le pone cabecera de archivo BMP a los bytes crudos que da Windows.
Asume que los datos empiezan en el byte 54, que es lo normal pero no
siempre cierto.

**`copiar_imagen(ruta)`**
Lee un `.bmp` del disco, le quita los 14 bytes de cabecera y lo deja en
el portapapeles como DIB.
Devuelve `False` si el archivo no existe o el portapapeles no se abre.

### Foco

**`ventana_activa()`**
Devuelve el handle de la ventana en primer plano.
Devuelve `None` si esa ventana es del propio pastepad, comparando el PID:
así el panel nunca se apunta a sí mismo como destino del pegado.

**`devolver_foco(hwnd)`**
Vuelve a poner en primer plano la ventana donde estaba el cursor.
Windows no deja que un proceso robe el foco, así que se engancha al hilo
de esa ventana con `AttachThreadInput` un instante, que es cuando
`SetForegroundWindow` sí funciona.

### Ventana — *las tres sin usar desde la v3*

**`hwnd_real(widget)`**
Sube desde el handle de un widget de Tk hasta la ventana real con
`GetAncestor(GA_ROOT)`.
Es API de tkinter (`winfo_id`); bajo Flet no hay nada que la llame.

**`redondear(hwnd, ancho, alto, radio)`**
Recorta la ventana con esquinas curvas usando una región GDI.
Flet lo hace con `border_radius`, así que ya no hace falta.

**`marco_hueco(hwnd, ancho, alto, radio, grosor)`**
Recorta la ventana dejando solo el borde, con el centro transparente.
Era el contorno que marcaba el tamaño futuro al redimensionar en tkinter.

### Teclado y pantalla

**`pegar_con_teclado()`**
Manda Ctrl+V con `keybd_event` de la API de Windows.
Antes suelta Shift y Alt, que pueden seguir pulsados del atajo; sin eso
el destino recibe Ctrl+Shift+V. No usa la librería `keyboard` porque esa
está ocupada escuchando.

**`puntero()`**
Devuelve la posición del ratón en píxeles de pantalla.
Cae a `(0, 0)` si no hay pywin32.

**`pantalla()`**
Devuelve ancho y alto del escritorio con `GetSystemMetrics`.
Cae a `(1920, 1080)` si no hay pywin32.

**`area_util(x, y, ancho_pantalla, alto_pantalla)`**
Devuelve el rectángulo aprovechable del monitor donde está ese punto,
sin la barra de tareas.
Es lo que permite que el panel se coloque bien en multi-monitor.

### Sistema

**`tema_claro()`**
Lee `AppsUseLightTheme` del registro y devuelve si Windows está en claro.
Ante cualquier fallo asume oscuro.

**`autoarranque(activar=True)`**
Añade o quita pastepad de `HKCU\...\CurrentVersion\Run`.
Registra la ruta desde donde se lanzó, así que mover la carpeta obliga a
abrirlo una vez desde el sitio nuevo.

---

## estilo.py — colores y piezas reutilizables

Mantiene el diccionario global `C` con los colores vivos y fabrica los
controles que se repiten.

**`aplicar(acento=None)`**
Recalcula toda la paleta según el tema de Windows y el acento elegido.
Modifica `C` en el sitio, así que quien ya tenga una referencia la ve
actualizada; devuelve si quedó en claro.

**`sombra(intensidad=1.0)`**
Devuelve un `BoxShadow` suave, escalado por intensidad.
Es lo que despega visualmente una tarjeta del fondo sin necesidad de borde.

**`texto(valor, tam, color, peso, lineas)`**
Fabrica un `ft.Text` con la fuente, el tamaño y el recorte por defecto.
Corta con puntos suspensivos al pasarse de `lineas`.

**`icono(nombre, al_pulsar, tam, color, tip)`**
Fabrica un botón de icono de 36×36 con esquinas de radio 10.
Unifica el aspecto de todos los iconos de la cabecera y el pie.

**`pildora(etiqueta, al_pulsar, activa, expandir)`**
Fabrica una pastilla redondeada que cambia de color y de peso al estar
activa. Se usa para las pestañas, las fichas de carpeta y las medidas.

**`boton(etiqueta, al_pulsar, tipo, icono_nombre)`**
Fabrica un botón con uno de tres estilos: `normal`, `acento` o `peligro`.
El color del texto sale del estilo, así que el icono opcional siempre
combina.

**`campo(marcador, al_cambiar, al_enviar, valor, lineas)`**
Fabrica un `ft.TextField` con el relleno, el radio y los colores del tema.
Pasa a multilínea solo si `lineas > 1`.

---

## filas.py — las tarjetas de la lista

**`_item(etiqueta, icono_nombre, al_pulsar)`**
Fabrica una opción de menú con su icono a la izquierda.
En esta versión de Flet el contenido va en `content`, no en un parámetro
`text`.

**`_resumen(dato, es_hist)`**
Devuelve `(titulo, detalle, es_enlace)` según el tipo de entrada.
Distingue cuatro casos: imagen, enlace del historial, texto del historial
(con su cuenta de caracteres) y snippet guardado.

**`Fila.__init__(dato, tipo, activa, marcando, marcada, acciones)`**
Construye la tarjeta entera: icono izquierdo, título y detalle al centro,
chincheta y menú a la derecha.
El icono izquierdo depende del modo: casilla si se está marcando, enlace
o imagen si no.

**`Fila._menu(es_hist, enlace, fijada, activa)`**
Arma el menú de tres puntos con las acciones que apliquen a esa entrada.
"Abrir en el navegador" solo sale en enlaces; "Fijar" solo en el historial;
"Editar" cambia de texto según sea historial o guardado.

**`Fila._pulsar(e)`**
Avisa al panel de que se eligió esta fila.
Toda la lógica está en `App._accion`; la fila solo reporta.

**`Fila._sobrevolar(e)`**
Cambia el color de fondo al pasar el ratón, y lo devuelve al salir.
Solo repinta el contenedor: no reconstruye nada. La fila activa se ignora
porque ya tiene el color de acento.

**`vacio(mensaje)`**
Devuelve el bloque centrado con icono y texto para cuando no hay nada.
El mensaje cambia según la pestaña y si hay búsqueda activa.

---

## ventanas.py — los diálogos

Son diálogos de la propia página de Flet, no ventanas del sistema: heredan
el tema y las animaciones sin trabajo extra.

**`_marco(titulo, cuerpo, acciones, ancho=460)`**
Envuelve cualquier contenido en un `AlertDialog` modal con el título, el
radio y los colores del tema.
Es la base de los seis diálogos siguientes.

**`abrir(page, dialogo)`**
Muestra un diálogo con `page.show_dialog`, que los apila.
Envoltorio de una línea para aislar el cambio de API de Flet.

**`cerrar(page, dialogo=None)`**
Cierra el diálogo de más arriba con `page.pop_dialog`.
El segundo parámetro se ignora; queda por compatibilidad con las llamadas.

**`texto_nuevo(page, carpetas, al_guardar, snippet, inicial)`**
Diálogo de crear o editar un texto guardado, con selector de carpeta y
caja de 10 líneas. Si el contenido queda vacío no guarda nada, pero
igual cierra.

**`una_linea(page, titulo, etiqueta, al_aceptar, valor)`**
Diálogo de un solo campo, para nombres de carpeta.
Acepta también con Enter. Solo llama al callback si el valor no está vacío.

**`campos(page, nombres, al_aceptar)`**
Pide los valores de los `[[campos]]` de una plantilla antes de pegar.
Genera una caja por campo y devuelve un diccionario nombre → valor.

**`lista_masiva(page, carpeta, al_aceptar)`**
Importa varias notas de golpe, con opción de una por línea o todo junto.
La casilla de limpiar quita viñetas (`-`, `*`, `•`, `–`) y numeración
(`1.`, `2)`) de cada línea.

**`apariencia(page, acento, tamano, atajo, carpetas, al_aplicar)`**
Diálogo de ajustes: color, tamaño del panel, estilo de carpetas y atajo.
Mantiene la selección en un diccionario local y solo la aplica al pulsar
Aplicar, así Cancelar no deja nada a medias.

**`confirmar(page, mensaje, al_confirmar, peligro=True)`**
Diálogo de sí/no. Con `peligro` el botón sale rojo y dice "Sí, borrar";
sin él, sale con el acento y dice "Aceptar".
También se usa como aviso de una sola opción pasando un callback vacío.

---

## app.py — el coordinador

La única clase que conoce a todas las demás. No toca el disco ni llama a
Windows directamente.

### Arranque y ventana

**`App.__init__(page, almacen)`**
Monta todo: estado inicial, preferencias validadas contra `config`,
ventana, interfaz, primera pintada y registro del atajo.
Termina lanzando los dos hilos de fondo con `page.run_thread`.

**`App._preparar_ventana()`**
Configura la ventana: sin marco, siempre encima, fuera de la barra de
tareas, fondo transparente y con sus límites de tamaño.
Engancha aquí los eventos de ventana y de teclado.

**`App._al_evento_ventana(e)`**
Esconde el panel cuando la ventana pierde el foco, igual que hace Win+V.
Solo actúa si `self.visible` sigue en `True`, que es lo que evita que
`ocultar()` se llame a sí mismo en bucle.

**`App._al_teclado(e)`**
Atiende Escape (cierra) y las flechas arriba y abajo (mueven la selección).
Enter no está aquí: lo recoge el `on_submit` del buscador.

**`App.mostrar()`**
Coloca la ventana junto al puntero, la hace visible, limpia el buscador,
repinta y le da el foco.
`self.visible` se asigna en la **última** línea: si algo de arriba lanza,
la bandera queda en `False` y la siguiente pulsación reintenta, en vez de
quedarse pegada en `True` con la ventana cerrada.

**`App._sitio()`**
Calcula dónde poner el panel: 14 px a la derecha y 18 abajo del ratón, o
al otro lado si no cabe. Después lo encierra dentro del área útil del
monitor, así nunca queda medio fuera ni tapado por la barra de tareas.

**`App.ocultar()`**
Esconde la ventana y olvida la ventana de destino.
Pone `self.visible` en `False` **antes** de tocar la ventana, para que el
evento de blur que llega después no vuelva a entrar.

**`App.alternar()`**
Muestra el panel si está oculto y lo esconde si está visible.
Decide únicamente por la bandera `self.visible`.

### Interfaz

**`App._construir()`**
Crea el árbol completo de controles: cabecera arrastrable, buscador,
pestañas, barra de carpetas, lista y pie.
Se vuelve a llamar entero al cambiar de tema, porque los colores están
incrustados en cada control.

**`App._pintar_pie()`**
Rellena la barra inferior según el modo: acciones de selección múltiple,
de guardados o de historial.
Es lo que hace que "Nueva carpeta" solo aparezca donde tiene sentido.

**`App._pintar_carpetas()`**
Muestra u oculta la barra de carpetas, que solo existe en Guardados.
Elige entre desplegable y fichas según la preferencia; la altura animada
de 0 a 46 da la transición.

**`App._carpetas_menu()`**
Construye el desplegable de carpetas, con crear, renombrar y eliminar
dentro del mismo menú.
Renombrar y eliminar solo salen si hay una carpeta seleccionada.

**`App._carpetas_fichas()`**
Construye la fila de pastillas de carpeta, con "Todas" al principio.
Scroll horizontal oculto para cuando no caben.

**`App.cambiar(cual)`**
Cambia entre Reciente y Guardados, repintando el color de las dos pestañas.
Sale del modo de selección múltiple si estaba activo.

**`App.elegir_carpeta(nombre)`**
Fija la carpeta que filtra la lista, o `None` para todas.
Repinta.

**`App._al_buscar(e)`**
Repinta la lista en cada tecla del buscador.
Sin retardo: la búsqueda se apoya en el índice cacheado.

**`App._al_enviar(e)`**
Pega la entrada seleccionada al pulsar Enter en el buscador.
Es el camino rápido: escribir dos letras y Enter.

**`App.refrescar()`**
El corazón de la vista: decide qué mostrar (búsqueda, guardados o
historial), guarda las listas paralelas `visibles` y `tipos`, y reconstruye
todas las filas. Termina repintando carpetas y pie.

**`App._mover(paso)`**
Mueve la selección arriba o abajo, sin salirse de los extremos.
Repinta la lista entera para actualizar el resaltado.

**`App.actual()`**
Devuelve `(dato, tipo)` de la entrada seleccionada, o `(None, None)`.
Protege contra un índice que se quedó fuera de rango.

**`App._accion(que, dato)`**
Despachador único de todo lo que puede pedir una fila: elegir, abrir,
pegar, pegar plano, copiar, fijar, editar y borrar.
Concentrar aquí las ocho acciones es lo que deja a `Fila` sin lógica.

### Pegado

**`App._texto_de(dato, tipo)`**
Devuelve el texto plano de una entrada, venga del historial o de los
guardados. Unifica las dos formas de guardar texto (`texto` contra `runs`).

**`App.abrir_enlace(dato)`**
Esconde el panel y abre la dirección en el navegador.
Cualquier fallo se ignora: no hay nada útil que decirle al usuario.

**`App.copiar(dato)`**
Deja el contenido en el portapapeles sin pegarlo, y cierra el panel.
Actualiza `ultimo_texto` y la secuencia para que el vigilante no lo vuelva
a anotar como copia nueva.

**`App.pegar(sin_formato=False)`**
El camino principal. Si la entrada es un enlace lo abre; si es una
plantilla con campos, los pide antes; si no, deja el contenido en el
portapapeles, esconde el panel y lanza el hilo que devuelve el foco y pega.

**`App._enviar(destino)`**
Corre en su propio hilo: espera 100 ms, devuelve el foco a la ventana de
antes, espera 160 ms más y manda Ctrl+V.
Las dos esperas son lo que le da tiempo a Windows a completar el cambio
de foco antes de que llegue la pulsación.

### Vigilancia del portapapeles

**`App.alternar_pausa()`**
Activa o desactiva la captura y guarda la preferencia.
Al reanudar sincroniza la secuencia, para no anotar lo que se copió
durante la pausa.

**`App._construir_cabecera_pausa()`**
Debería repintar el icono de pausa; hoy solo llama a `page.update()`.
El icono se fijó al construir la cabecera, así que no cambia.

**`App._vigilar()`**
Hilo de fondo: cada 0,7 s compara el contador del portapapeles y solo si
cambió lo abre de verdad. Descarta el contenido marcado como privado,
recorta el texto a 200 000 caracteres y anota texto o imagen.

**`App._tras_anotar()`**
Invalida el índice de búsqueda y repinta, pero solo si el panel está
visible en Reciente y sin búsqueda escrita.
Evita repintar por debajo mientras el usuario está haciendo otra cosa.

### Atajo global

**`App.registrar_atajo(combinacion=None)`**
Quita el atajo anterior si lo había y registra el nuevo con `keyboard`.
Devuelve `False` si falla, que es lo que impide guardar como preferencia
una combinación que el sistema no aceptó.

**`App._pulsado()`**
Corre dentro del hilo de la librería `keyboard` y no hace nada más que
encolar el handle de la ventana activa.
Si este callback tardara, la librería se atasca y el atajo deja de
responder tras la primera vez.

**`App._atender_cola()`**
Hilo de fondo que saca los avisos de la cola y alterna el panel, ya fuera
del hilo del teclado.
Guarda el handle como destino solo si el panel estaba oculto.

### Acciones del usuario

**`App.nuevo()`**
Abre el diálogo de texto nuevo con la lista de carpetas actual.
El resultado vuelve por `_guardar_nuevo`.

**`App._guardar_nuevo(snippet)`**
Guarda el snippet, invalida el índice y salta a la pestaña Guardados.
El salto de pestaña es lo que le confirma al usuario que se guardó.

**`App.editar(dato)`**
Si es del historial, abre el editor precargado y lo guarda como snippet
nuevo. Si ya es un guardado, lo reemplaza en su sitio.
Una entrada del historial nunca se edita en el propio historial.

**`App.pedir_campos(dato, fragmentos, campos, sin_formato)`**
Abre el diálogo de plantilla y, con los valores, rellena, copia y pega.
Repite el final de `pegar` porque el flujo se parte en dos por el diálogo.

**`App.vaciar()`**
Pide confirmación y vacía el historial dejando los fijados.
El aviso lo dice explícitamente para que nadie pierda algo por error.

**`App.nueva_carpeta()`**
Pide el nombre, crea la carpeta, la selecciona y encadena directamente el
diálogo de importar lista.
Crear una carpeta vacía casi nunca es el objetivo real.

**`App.renombrar_carpeta()`**
Pide el nombre nuevo con el viejo precargado y renombra.
No hace nada si no hay carpeta seleccionada.

**`App.borrar_carpeta()`**
Pide confirmación diciendo cuántos textos se van a perder, y borra.
El mensaje se ajusta en singular o plural, y cambia si la carpeta va vacía.

**`App.agregar_lista(carpeta=None)`**
Abre el diálogo de importación masiva sobre la carpeta indicada.
Si no hay ninguna, avisa en vez de abrirlo.
Cada línea se guarda como un snippet con su título recortado a 48.

### Selección múltiple

**`App.alternar_marcado()`**
Entra o sale del modo de selección, limpiando siempre lo marcado.
Repinta para que aparezcan o desaparezcan las casillas.

**`App.marcar_todos()`**
Marca todo lo visible, o lo desmarca si ya estaba todo marcado.
Trabaja con `id()` de cada dato, no con su posición.

**`App.borrar_marcados()`**
Pide confirmación con la cuenta exacta y borra todo lo seleccionado.
Sale del modo selección al terminar.

### Apariencia

**`App.abrir_apariencia()`**
Abre el diálogo de ajustes pasándole los valores actuales.
El resultado vuelve por `_aplicar_apariencia`.

**`App._aplicar_apariencia(acento, tamano, atajo, carpetas)`**
Guarda cada preferencia que haya cambiado, recalcula la paleta y
redimensiona la ventana.
Termina destruyendo y reconstruyendo el árbol de controles: los colores
viven dentro de cada control, así que no hay forma de refrescarlos en
el sitio.

---

## main.py — arranque

**`arrancar(page)`**
Crea el almacén, aplica la paleta, registra el autoarranque si toca y
monta `App`. Es la función que recibe `ft.run()`.
Va envuelta en un `try`: un fallo aquí dejaba la ventana en blanco sin
decir nada, y ahora queda anotado antes de dejarlo subir.

El registro de errores vivía aquí hasta la v3; está en
[`registro.py`](#registropy--el-rastro-de-los-fallos) porque los hilos de
`app.py` también lo necesitan.
