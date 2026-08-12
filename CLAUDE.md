# CLAUDE.md

Contexto para trabajar en este proyecto. Léelo antes de tocar código.

## Qué es

pastepad: gestor de portapapeles para Windows. Guarda todo lo que se
copia, más textos que el usuario organiza en carpetas, y los pega donde
estaba el cursor con un atajo global.

Reemplaza al historial de Windows (Win+V), que solo guarda 25 entradas y
las pierde al reiniciar.

## Estado actual

Versión 3.0.0, migrada de tkinter a Flet. La estructura y las pruebas
están verificadas; **la interfaz sigue sin probarse en una máquina real**.

Hasta la revisión de agosto de 2026, los ocho módulos v3 estaban sueltos
en la raíz mientras `pastepad/` conservaba los cuatro módulos tkinter de
la v2. `python main.py` moría en el import, y `prueba.py` daba 19 en
verde porque probaba el paquete viejo. Ya está corregido: el paquete
resuelve a 3.0.0 y las pruebas corren contra el código real.

### Problema abierto

El atajo global abre el panel **solo la primera vez** y después deja de
responder. Ya se intentó:

- Sacar el trabajo del callback de `keyboard` a una cola (`_pulsado`
  solo hace `put_nowait` y sale)
- Mandar el Ctrl+V del pegado con `keybd_event` de la API de Windows en
  vez de con `keyboard.send`, para no ocupar la librería que escucha
- Soltar Shift y Alt antes del pegado, por si el atajo los deja pulsados

**Lo que se descartó en la revisión de agosto de 2026:**

- *El hilo de `_atender_cola` muere.* No puede: es un `while True` con un
  `except Exception` que lo envuelve todo. Ninguna excepción normal lo
  saca del bucle.
- *Los permisos de administrador.* UIPI produce un fallo correlacionado
  con qué ventana tiene el foco, no un fallo permanente tras la primera
  pulsación. El síntoma no cuadra.

**La causa probable, y lo que se hizo al respecto:** `self.visible` es una
bandera en sombra, y `alternar()` decide solo sobre ella. Si queda pegada
en `True` con la ventana cerrada, cada pulsación llama a `ocultar()` y el
panel no vuelve a salir. Se asignaba **antes** de `refrescar()` y de los
dos `update()`, que son las tres cosas que pueden lanzar. Ahora se asigna
en la última línea de `mostrar()`, así un fallo la deja en `False` y la
siguiente pulsación reintenta.

Eso corta la corrupción de estado, pero **no arregla la excepción de
fondo si la hay**. Para verla:

1. `errores.log` ya recoge los fallos de los hilos. Antes no: `sys.excepthook`
   solo cubre el hilo principal, y `page.run_thread` entrega el trabajo a
   un `ThreadPoolExecutor` que atrapa la excepción en su `Future` — no la
   ve ni `sys.excepthook` ni `threading.excepthook`. Por eso `_vigilar` y
   `_atender_cola` capturan y llaman a `registro.fallo()` a mano.
2. `cfg.TRAZA_ATAJO` (hoy en `True`) anota en cada pulsación el valor de
   `self.visible` junto al de `page.window.visible`. Si divergen, la
   bandera se desincronizó. **Ponlo en `False` cuando el fallo esté
   cerrado.**

Hipótesis que sigue viva si el log no aclara nada: `keyboard` y el bucle
de eventos de Flutter en conflicto. La alternativa sería `RegisterHotKey`
de la API de Windows, más estable para atajos globales, pero exige
atender la cola de mensajes del sistema.

**Ejecuta `python main.py`, pulsa el atajo tres o cuatro veces y lee
`errores.log`.**

## Estructura

```
main.py               arranque — llama a ft.run()
pastepad/
  config.py           colores, medidas, rutas, atajos    [sin tkinter/flet]
  registro.py         errores.log — el único que escribe [sin tkinter/flet]
  modelo.py           los datos y sus reglas             [sin tkinter/flet]
  busqueda.py         ranking con caché de normalización [sin tkinter/flet]
  windows.py          portapapeles, foco, ventana, arranque
  estilo.py           colores vivos y piezas reutilizables
  filas.py            las tarjetas de la lista
  ventanas.py         los diálogos
  app.py              coordina todo lo anterior
prueba.py             19 pruebas — corren sin abrir ventana
docs/FUNCIONES.md     las 133 funciones, tres líneas cada una
```

Los tres módulos marcados no importan ninguna librería gráfica. Eso es a
propósito: permitió migrar de tkinter a Flet reutilizando 906 de 2.171
líneas, y es lo que hace que las pruebas corran sin ventana.

**No metas imports de Flet en `modelo.py`, `busqueda.py` ni `config.py`.**

## Cómo trabajar aquí

```powershell
pip install -r requirements.txt
python prueba.py      # 19 pruebas, deben pasar todas
python main.py        # abre el programa
```

Después de cualquier cambio en el modelo o la búsqueda, corre las
pruebas. Si tocas la interfaz, hay que probar a mano.

## Decisiones tomadas y por qué

Estas ya se discutieron. Si vas a revertir alguna, ten un motivo.

**Flet en vez de tkinter.** Tkinter dibuja con el motor viejo de Windows
y el resultado se ve duro: sin sombras, sin suavizado decente, sin
animaciones. Flet usa Flutter y rasteriza por GPU. El coste es que el
ejecutable pesa unos 80-150 MB en vez de 30.

**El callback del atajo no hace trabajo.** La librería `keyboard`
escucha desde su propio hilo y ese hilo se bloquea mientras el callback
tarda. Todo el trabajo va a una cola.

**El pegado devuelve el foco primero.** Al abrirse el panel, el campo
donde estaba el cursor pierde el foco, y un Ctrl+V a secas se va al
vacío. Se guarda el handle de la ventana activa en el instante del
atajo, y antes de pegar se le devuelve el foco con `AttachThreadInput`,
que es el truco que Windows exige para que `SetForegroundWindow`
funcione desde otro proceso.

**Se respetan los formatos privados del portapapeles.** Windows define
cuatro formatos con los que un programa marca contenido como "no
guardar": los usan KeePass, Bitwarden, el Administrador de credenciales
y el modo incógnito de Chrome. Si alguno está presente, ni se abre el
portapapeles. Ver `windows.contenido_privado()`.

**El portapapeles solo se lee cuando cambia.** Windows tiene un contador
(`GetClipboardSequenceNumber`) que sube con cada copia; leerlo cuesta una
llamada, abrir el portapapeles cuesta muchísimo más. Sin esto, con un
texto grande copiado, se leían 200 KB cuatro mil veces por hora.

**El índice de búsqueda cachea el texto normalizado.** Normalizar 80
textos largos cuesta unos 25 ms; sin caché eso pasaba cada vez que el
usuario copiaba algo.

**Guardar es atómico.** Se escribe en un `.tmp` y se mueve con
`os.replace`. Antes, un cierre a mitad de escritura dejaba el JSON
cortado y se perdía todo.

**Un enlace se abre, no se pega.** Si la entrada es solo una dirección
web, el clic abre el navegador. Un párrafo que menciona una URL de pasada
no cuenta — ver `modelo.es_enlace()`, hay pruebas de eso.

**No hay arrastre libre de bordes en la versión tkinter.** Se intentó
cinco veces y siempre dejaba franjas del dibujo anterior: CustomTkinter
no repinta bien bajo redimensionado continuo. En Flet esto ya no aplica.

## Cosas que romperás si no tienes cuidado

- Los datos del usuario viven junto al ejecutable: `snippets.json`,
  `historial.json`, `config.json`, `imagenes/`. Están en `.gitignore`.
  **Nunca los subas ni los borres al hacer pruebas.**
- El programa se registra solo en el arranque de Windows, usando la ruta
  desde donde se lanzó. Si mueves la carpeta, hay que abrirlo una vez
  desde el sitio nuevo. Para limpiar:
  `reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v GestorSnippets /f`
- Si el proyecto vive dentro de OneDrive, la sincronización puede
  bloquear los JSON a mitad de escritura. Mejor moverlo fuera.

## Estilo del código

- Comentarios y nombres en español, sin tildes en el código
- Los comentarios explican **por qué**, no qué hace la línea
- Docstrings en las funciones que no son obvias
- Líneas de 79 caracteres
- **Ningún `except` mudo.** Un `except Exception: pass` en un hilo es
  invisible sin consola, y fue la razón de que el fallo del atajo pasara
  tres intentos sin diagnóstico. Si hay que tragarse un error, se anota
  con `registro.fallo("de donde")`.

## Historial

`CHANGELOG.md` tiene el detalle. Resumen:

- 1.x — un solo archivo, tkinter
- 2.0 — reescrito en módulos, con pruebas
- 3.0 — interfaz migrada a Flet
