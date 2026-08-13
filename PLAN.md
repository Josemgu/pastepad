# Plan — reescritura en C# con WinUI 3

Plan técnico verificado contra documentación oficial, 12 ago 2026.
Complementa a `TRASPASO.md`: aquel dice qué conservar y por qué se
abandonó Flet; este dice cómo se construye lo nuevo y en qué orden.

Todo lo que se afirma aquí lleva fuente. Lo que no se pudo comprobar
está marcado como tal y tiene un paso o un plan B que lo cubre.

---

## Estado

| Paso | Estado |
|---|---|
| 1 — atajo y portapapeles | Mecanismo probado. Falta el remojo largo. Ver `csharp/PruebaAtajo/RESULTADOS.md` |
| 2 — lógica y 19 pruebas | **Hecho.** 22 en verde en `csharp/Pastepad.Nucleo.Pruebas` (las 19 de `prueba.py` más 3 que vigilan el formato del archivo) |
| 3 — la cáscara | Montado y verificado por el qa: **el pegado pasa** (12/12), atajo 30/30 dos veces, instancia única, bandeja, bordes. Queda 1 fallo abierto (ver abajo) |
| 4 — la interfaz | **Hecho.** El qa lo da por listo: 24 comprobaciones, 24 pasan. Dos vueltas del ciclo diseñador → programador → planificador → qa |
| 5 — publicar y medir | **Hecho.** Instalador de 47,1 MB, verificado por el qa: 22 comprobaciones, 21 limpias |
| 6 — retirar la versión en Python | **Hecho.** Vive en el tag `ultima-version-python` |

Solución en `csharp/Pastepad.slnx`. Las pruebas corren con
`dotnet test csharp/Pastepad.Nucleo.Pruebas`, sin ventana y sin el
Windows App SDK. Son **40**.

**Compila siempre por la solución**, no por el proyecto. Escriben en
carpetas distintas —`bin\x86\Debug\...\win-x86\` la solución,
`bin\Debug\...\win-x64\` el proyecto— y medir sobre el binario viejo ya
dio dos conclusiones falsas: al programador, que `ResizeClient`
funcionaba; y al qa, que el apilado no ocurría y que el margen del
diálogo era de 17 px. Comprobar la marca de tiempo del `.exe` antes de
creerse una medida.

### Lo que queda abierto al cerrar el paso 6

- **El arranque en frío tras reiniciar sigue sin medirse.** Es el único
  número que le falta al requisito dos. Lo da la primera línea de
  `errores.log` el día que se reinicie el equipo: busca `listo en`.
  En caliente son 420–463 ms.
- ~~**`--datos` aísla los datos pero NO el autoarranque.**~~ **Cerrado en
  la 4.2.0.** Volvió a pasar dos veces durante el trabajo de esa versión,
  así que `--datos` ya implica no tocar el registro y lo dice en el log.
- **Sin firmar.** SmartScreen avisa. El certificado de SignPath está por
  solicitar, y exige verificación en dos pasos y aprobación manual de
  cada release. Hay que firmar **dos veces**: el `.exe` antes de
  construir el instalador y el instalador después.
- **El changelog no tiene entradas de 3.0.0 ni 3.0.1**, aunque los tags
  existen. Salta de la 2.0.0 a la 4.0.0.

### Lo que quedó abierto al cerrar el paso 4

Nada de esto bloquea; queda escrito para que no se pierda.

- **Las 18 bolitas de acento exponen su identificador crudo** como
  nombre accesible: `menta_fria`, `ambar`, `durazno`. Iguales en los
  cuatro idiomas. Con lector de pantalla en inglés se oye `menta_fria`.
  Traducirlas son 18 claves nuevas.
- **`Almacen.Problema` sigue solo en español.** Es la única cadena que
  ve el usuario que no pasa por `T()`, y ahora ya se puede traducir.
- **`Registro.RutaUsada` no la lee nadie.** O se usa en el aviso de
  arranque degradado —que hoy dice qué falló pero no dónde está el
  detalle— o se borra.
- **`App.xaml` y `Estilo.cs` declaran los mismos colores dos veces.**
  Hoy coinciden. Nada impide que dejen de hacerlo, y no habría error de
  compilación que lo cazara.
- **La causa del fallo de la carpeta sigue sin encontrarse.** El daño
  está contenido y ahora deja rastro: `HResult` en el log, reintento
  con espera, y la ruta real del almacén en la línea de arranque.

### Fallo abierto: la instancia que no ve su carpeta

Encontrado por el qa el 12 ago 2026, y es el mismo síntoma que ya se
había visto dos veces sin explicación.

Una instancia arrancó **sin poder usar `%LOCALAPPDATA%\pastepad` para
nada**: no leyó `historial.json` ni `config.json`, y no escribió una
sola línea en `errores.log`. Y sin embargo el atajo iba perfecto
(60/60) y el pegado funcionaba. Desde fuera parecía sana. Al cerrarse
guardó el historial vacío encima del real y se perdió todo lo de esa
sesión.

**No se ha reproducido.** La sospecha es que arrancó mientras se
mataba a la instancia anterior, pero no está confirmado.

Lo que sí está arreglado es el daño, que era lo grave:

- Un archivo que existe y no se puede leer **queda marcado y no se
  sobrescribe** en toda la sesión. Cuatro pruebas lo fijan.
- El arranque en ese estado **abre el panel con el aviso**, en vez de
  fingir normalidad.
- El log junto al ejecutable dice dónde está el detalle, y avisa si el
  principal no se pudo escribir.

La causa raíz sigue sin encontrar. Si vuelve a pasar, ahora deja
rastro.

---

**Cuidado al probar:** la versión nueva usa `%LOCALAPPDATA%\pastepad`,
que es donde está instalada la v3 en esta máquina. Lee y escribe **los
datos de verdad**. La compatibilidad quedó comprobada en vivo, pero
cualquier prueba ensucia el historial real. Antes de tocar nada,
copiar la carpeta.

---

## El problema en una frase

Reescribir pastepad conservando la lógica ya probada (1.208 líneas y
19 pruebas) y el formato de datos, de modo que **el atajo global no
vuelva a morir** — que es lo único que la versión en Python nunca
logró.

---

## Lo que se averiguó

### Versiones y plantilla de partida

| Qué | Valor |
|---|---|
| Windows App SDK estable | 2.3.1 (16 jul 2026) |
| .NET | SDK 10; TFM `net10.0-windows10.0.26100.0` |
| IDE | Visual Studio 2026, workload *WinUI application development* |
| Sin VS | `dotnet new install Microsoft.WindowsAppSDK.WinUI.CSharp.Templates` y luego `dotnet new winui` |
| Mínimo de Windows | 10 v1809 (17763) |

Fuentes: [descargas del SDK][d1], [primera app WinUI 3][d2],
[WinUI 3][d3].

**Desempaquetado.** `<WindowsPackageType>None</WindowsPackageType>`
activa el auto-inicializador del bootstrapper. Requiere el Visual C++
Redistributable en la máquina destino. El runtime del SDK se resuelve
de dos formas: instalador aparte, o
`<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>`, que lo
mete en la carpeta de salida y elimina la dependencia externa.
Ver [despliegue desempaquetado][d4] y [distribuir desempaquetado][d5].

Sin identidad de paquete se pierden: actualizaciones por App Installer,
tareas en segundo plano por manifiesto, asociaciones de archivo por
manifiesto y personalización de mosaico. **Nada de eso lo usa
pastepad.** La misma página confirma que los mecanismos Win32
tradicionales —entradas de registro escritas por el instalador y
accesos directos— siguen funcionando, así que **el autoarranque por
`HKCU\...\Run` sobrevive tal cual**.

### El atajo global — el punto crítico

Tres piezas, cada una verificada:

1. **HWND de la ventana** — `WinRT.Interop.WindowNative.GetWindowHandle(this)`.
   Ver [obtener el HWND][d6].
2. **Registro** — `RegisterHotKey(hWnd, id, MOD_CONTROL|MOD_SHIFT|MOD_NOREPEAT, vk)`.
   La documentación dice que con `hWnd` nulo los `WM_HOTKEY` se publican
   en la cola del **hilo** llamante, y que con un HWND real el sistema
   los publica en la cola de **esa ventana**. Falla si se asocia el
   atajo a una ventana creada por otro hilo, así que hay que llamarlo
   desde el hilo de interfaz. `MOD_NOREPEAT` es `0x4000`, F12 está
   reservada y el `id` va de `0x0000` a `0xBFFF`.
   Ver [RegisterHotKey][d7] y [WM_HOTKEY][d8].
3. **Dónde se atiende** — WinUI 3 no expone `WndProc`. La vía
   documentada para interceptar los mensajes de un HWND es
   `SetWindowSubclass` con un `SUBCLASSPROC`, retirado con
   `RemoveWindowSubclass`. Ver [SetWindowSubclass][d9], la
   [discusión 8934][d10] del repo de WinUI y la [pregunta 5815][d11],
   que plantea exactamente esta duda y se cerró sin respuesta oficial.

**El fallo de fondo de la versión anterior.** El código en Python llama
a `RegisterHotKey(None, ...)` — atajo de hilo, no de ventana — y bombea
la cola en un hilo propio. En WinUI eso no vale: un mensaje de hilo
(`msg.hwnd == NULL`) no lo despacha `DispatchMessage` a ningún
procedimiento de ventana, así que la bomba de XAML lo descartaría.
**Hay que pasar un HWND real.** Ese es el cambio de fondo, no un
detalle del portado.

**Los dos mecanismos posibles:**

| | Subclasear la ventana XAML | Ventana solo-mensajes propia (`HWND_MESSAGE`) |
|---|---|---|
| Fiabilidad | Depende de que la ventana XAML nunca se destruya | Independiente del ciclo de vida de la interfaz |
| Partes móviles | 1 (el subclase) | 1 (nuestra clase de ventana), pero es código nuestro entero |
| Riesgo conocido | [issue 8669][d12]: `ExecutionEngineException` aleatorio al subclasear | Sin issue conocido |
| Verificado | Sí, todas las piezas | `RegisterHotKey` sí; que la bomba de WinUI despache ahí, **no** |
| Sirve para el portapapeles | Sí | Sí |

**Se elige la ventana solo-mensajes propia, con el subclase como plan
B.** El requisito número uno es que el atajo funcione siempre, y
pastepad se oculta a la bandeja: atar el atajo a la ventana visible
reintroduce justo el acoplamiento que mató la versión anterior. Además
el patrón ya está escrito y probado en `windows.py` (`abrir_buzon`, con
los `argtypes` de `HWND_MESSAGE` que costaron encontrar).

### El portapapeles

| | `AddClipboardFormatListener` (Win32) | `Clipboard.ContentChanged` (WinRT) |
|---|---|---|
| Aviso | `WM_CLIPBOARDUPDATE` a nuestra ventana | Evento estático |
| Leer en segundo plano | Sí | **No.** La documentación dice que solo se accede al portapapeles con la app enfocada |
| Formatos privados | `RegisterClipboardFormat`, `IsClipboardFormatAvailable`, `GetClipboardData` | Sin vía documentada |

**Gana Win32, sin discusión.** Un gestor de portapapeles vive en
segundo plano por definición: la API WinRT queda descartada por diseño,
no por gusto. Ver [AddClipboardFormatListener][d13] y
[Clipboard de WinRT][d14].

Los cuatro formatos privados se leen igual que hoy — dos por presencia
(`Clipboard Viewer Ignore`, `ExcludeClipboardContentFromMonitorProcessing`)
y dos por valor cero (`CanIncludeInClipboardHistory`,
`CanUploadToCloudClipboard`) — portando `contenido_privado()` línea por
línea.

### Ventana y bandeja

- **Sin marco** — `OverlappedPresenter.SetBorderAndTitleBar(false, false)`,
  con `IsAlwaysOnTop`, `IsResizable` y
  `PreferredMinimum/MaximumWidth/Height`, que cubre los límites de
  300×340 a 720×1100 de `config.py` sin escribir código.
  Ver [OverlappedPresenter][d15].
- **Ocultar sin destruir** — `AppWindow.Hide()` oculta la ventana de
  todas las representaciones del sistema pero mantiene el objeto vivo.
  Además `IsShownInSwitchers` para que no salga en Alt+Tab ni en la
  barra de tareas, y `MoveAndResize` para colocarla junto al cursor.
  Ver [AppWindow][d16].
- **Mica** — `<Window.SystemBackdrop><MicaBackdrop/></Window.SystemBackdrop>`,
  propiedad desde el SDK 1.3. Acrílico con `DesktopAcrylicBackdrop`.
  **Mica solo pinta en Windows 11 (22000+)**; por debajo, con
  transparencia desactivada o en ahorro de batería, cae a un color
  sólido. La paleta `#0B0B0D` de `ESPECIFICACION-UI.md` es el plan B
  natural. Ver [system backdrops][d17] y [Mica][d18].
- **Bandeja** — WinUI 3 no la trae y la [propuesta 2020][d19] sigue sin
  implementarse. Se elige `Shell_NotifyIcon` directo sobre la misma
  ventana solo-mensajes del atajo, no la librería `H.NotifyIcon.WinUI`:
  ese `WndProc` ya va a existir, así que el icono cuesta unas 40 líneas
  más y cero dependencias.

### Instancia única

`AppInstance.FindOrRegisterForKey`, `IsCurrent` y
`RedirectActivationToAsync`, con `DISABLE_XAML_GENERATED_MAIN` y un
`Program.cs` propio — obligatorio de todas formas, porque la
redirección tiene que decidirse antes de crear ninguna ventana. Hay
[guía oficial con código][d20] y [ejemplo para Win32 desempaquetado][d21].

Matiz: la propia guía avisa de problemas conocidos con apps recortadas
y recomienda `PublishTrimmed=false`. Si estorbara, el mutex con nombre
de `reservar_instancia()` funciona sin depender del SDK.

### Empaquetado y Native AOT

- **Native AOT sí es viable**: `PublishAot` está soportado desde el
  Windows App SDK **1.6** y exige `Microsoft.Windows.CsWinRT` 2.1.1 o
  posterior. Advertencia oficial en esas notas: las apps compiladas con
  AOT pueden colgarse tras navegar entre páginas por una condición de
  carrera en el recolector. Ver [notas de la 1.6][d22].
- **EXE único**: `PublishSingleFile` funciona solo si es desempaquetado
  *y* self-contained, con el conjunto exacto `WindowsPackageType=None`,
  `WindowsAppSDKSelfContained`, `SelfContained`, `EnableMsixTooling`,
  `IncludeAllContentForSelfExtract` y `PublishSingleFile`. Extrae
  dependencias a un temporal en el primer arranque: no es un binario
  cerrado.
- **Instalación en `%LOCALAPPDATA%`**: se copia la carpeta de salida.
  La documentación sugiere WiX o Inno Setup para envolverla.

  **Corrección al plan original**, comprobada el 12 ago 2026: decía que
  «Inno Setup ya se usa en el repo». **No es cierto.** En `instalador/`
  hay `instalar.bat`, `desinstalar.bat`, un `LEEME.txt` y la salida de
  PyInstaller. No hay ningún `.iss`. Así que el instalador del paso 5
  se elige de cero: o se traducen esos `.bat`, o se monta Inno Setup
  por primera vez.
- **P/Invoke**: [`Microsoft.Windows.CsWin32`][d23], generador de código
  de Microsoft. Se listan las funciones en `NativeMethods.txt` y genera
  las firmas correctas — evita el tipo de error que reventó el
  `HWND_MESSAGE` en Python.

### Pruebas

Las 19 pruebas cubren `modelo.py` y `busqueda.py`, que no importan nada
gráfico. Van a una **biblioteca de clases `net10.0` pura, sin TFM de
Windows y sin referencia al Windows App SDK**, y se prueban con
**MSTest** (`dotnet test`). Sin ventana, sin runtime del SDK, sin hilo
STA. Es la misma separación que permitió migrar de tkinter a Flet
reutilizando 906 líneas: se conserva porque funcionó.

---

## El plan por pasos

### Paso 1 — Prueba de fuego del atajo y el portapapeles

Antes de nada más. Un proyecto WinUI 3 desempaquetado **vacío**: una
ventana, un contador en pantalla, nada más.

- `Program.cs` propio con `DISABLE_XAML_GENERATED_MAIN` e instancia
  única.
- Crear en el hilo de interfaz una ventana solo-mensajes
  (`CreateWindowExW` con padre `HWND_MESSAGE`) con `WndProc` propio y
  el delegado guardado en un campo **estático**.
- `RegisterHotKey(hwndBuzon, 1, MOD_CONTROL|MOD_SHIFT|MOD_NOREPEAT, 0x56)`
  y `AddClipboardFormatListener(hwndBuzon)`.
- Contar `WM_HOTKEY` y `WM_CLIPBOARDUPDATE` en pantalla y en un log.

**Cómo se sabe que quedó bien:** 100 pulsaciones dan 100 incrementos,
sin ninguna pérdida; y sigue contando después de 8 horas residente,
tras bloquear y desbloquear la sesión, tras suspender el equipo, y con
la ventana oculta con `AppWindow.Hide()`. Si falla el despacho a la
ventana solo-mensajes, se repite con `SetWindowSubclass` sobre el HWND
de la ventana XAML. **No se pasa al paso 2 hasta que uno de los dos dé
100 de 100.**

### Paso 2 — La lógica y las 19 pruebas

Biblioteca `net10.0` pura con `Modelo`, `Busqueda` y `Config`.
Traducir `modelo.py` y `busqueda.py` respetando el formato JSON al pie
de la letra: claves `t/f/s/b/i/u/c`, fijados primero, escritura atómica
a `.tmp` y `File.Move` con sobrescritura, volcado diferido de 3 s con
guardado inmediato para lo que el usuario hace a propósito. Después,
las 19 pruebas de `prueba.py` a MSTest, una por una y con el mismo
nombre.

**Cómo se sabe que quedó bien:** `dotnet test` da 19 en verde sin abrir
ventana, y una instalación existente de v3.0.1 abre con la versión
nueva sin perder ni un snippet ni un fijado.

### Paso 3 — La cáscara

Sobre el esqueleto validado en el paso 1: ventana sin marco,
`IsShownInSwitchers = false`, Mica con caída a `#0B0B0D`, icono de
bandeja con `Shell_NotifyIcon` sobre la misma ventana solo-mensajes,
mostrar y ocultar junto al cursor con `MoveAndResize` respetando el
área útil del monitor, y el ciclo de pegado portado de `windows.py`:
guardar el `hwnd` activo **en el instante del atajo**,
`AttachThreadInput` y `SetForegroundWindow`, soltar Shift y Alt, y
`Ctrl+V`. Registro de errores desde el primer día, con
`AppDomain.UnhandledException` y `TaskScheduler.UnobservedTaskException`
enganchados los dos. Ningún `catch` mudo.

**Cómo se sabe que quedó bien:** copiar en Word, abrir con el atajo,
pegar en el Bloc de notas y que el texto caiga donde estaba el cursor.
Copiar desde KeePass o el modo incógnito de Chrome y que **no**
aparezca en el historial.

### Paso 4 — La interfaz

Contra `docs/ESPECIFICACION-UI.md` y las maquetas 01–19, en el orden
que el propio documento recomienda: colores primero, luego 13, 14 y 15,
luego los vacíos 10, 11 y 12. Las maquetas 20 y 33 no son
especificación, y la 26 y la 27 son diálogos de Windows.

**Cómo se sabe que quedó bien:** los hex del código coinciden uno a uno
con la tabla de la sección 2 del documento.

### Paso 5 — Publicar y medir

`WindowsPackageType=None` y `WindowsAppSDKSelfContained=true`,
instalador a `%LOCALAPPDATA%\pastepad` —por decidir: traducir los
`.bat` que ya hay o montar Inno Setup—, autoarranque por
`HKCU\...\Run`. Medir memoria residente, tiempo de apertura del panel y
arranque en frío. Native AOT **al final y como experimento medido**, no
como premisa: si aporta poco o reaparece el cuelgue documentado tras
navegar, se deja fuera. La versión sale del tag, no escrita a mano.

**Cómo se sabe que quedó bien:** el `.exe` instalado —no el proyecto en
depuración— pasa las pruebas del paso 1 y del paso 3 en una máquina
limpia, movido de carpeta al menos una vez.

### Cambios que pidió el usuario durante el paso 4

No son desvíos del plan: son decisiones suyas, tomadas al ver la
aplicación corriendo. Quedan aquí porque contradicen a
`ESPECIFICACION-UI.md`, que se escribió antes.

**1. El diseño se adapta a WinUI 3, no al revés.** Usar los materiales
y el sistema de temas del framework y luego taparlos es pagar el coste
sin cobrar el beneficio. Coincide con el motivo por el que
`TRASPASO.md` eligió WinUI 3.

**2. El tema sigue a Windows y cambia en vivo.** Con el panel abierto,
si Windows pasa de oscuro a claro, el panel pasa con él. `"tema":
"auto"` de `config.json` significa eso. Claro y oscuro explícitos se
conservan como preferencia.

**3. Fuera los tamaños fijos.** `mini`, `chico`, `mediano` y `grande`
desaparecen de Apariencia: el panel es adaptable y todo su contenido se
ajusta al redimensionar. Afecta a `Config.Tamanos` y a la sección
«Tamaño» de la maqueta 08.

**4. Falta el selector de idioma.** `pastepad/idiomas.py` tiene **4
idiomas** —es, en, pt, fr— con 230 textos, y la maqueta 08 declara
Color, Tamaño, Carpeta y Atajo, pero **ningún idioma**. Es un hueco de
la especificación, no del código.

**5. Apariencia hay que reorganizarla.** Tal cual está no queda como un
programa terminado, y el objetivo es que esto salga a producción.

**6. `tenue` sube hasta cumplir contraste AA.** Medido por el diseñador:
hoy da entre 2.89:1 (`salvia`) y 3.38:1 (`lila`) sobre los 12 fondos,
cuando WCAG AA pide 4.5:1 para texto normal. Es el color de los
subtítulos de fila y de los estados vacíos, o sea texto que hay que
poder leer.

El usuario decidió el 12 ago 2026 subirlo **aceptando que cambia el
aspecto de las doce paletas**. Legibilidad por delante del aspecto
heredado de `estilo.py`.

Se recalcula por paleta, no un valor único: cada fondo necesita el suyo
para llegar a 4.5:1. Y se comprueba **calculando el contraste**, no
mirándolo.

**Y arrastró un ajuste que no estaba pedido: `medio` sube en seis
paletas.** Al subir `tenue` hasta AA, en cuatro fondos claros quedaba
pegado a `medio` —en `salvia`, 1.07:1 entre uno y otro— y el subtítulo
dejaba de leerse como secundario. Como `tenue` ya no puede bajar sin
romper AA, el escalón solo se recupera subiendo `medio`. Se fijó en
1.30:1, el que ya tenían las paletas sanas.

El diseñador lo marcó como fuera de lo autorizado y el usuario lo
aprobó después, el 12 ago 2026: **se queda**. No se revierte sin una
razón nueva.

**7. El radio de esquina lo pone Windows, y es el máximo posible.** El
usuario pidió más redondeo. No se puede: `DWM_WINDOW_CORNER_PREFERENCE`
tiene cuatro valores —por defecto, no redondear, redondeado y
redondeado pequeño— y **ninguno acepta un número de píxeles**. Ya se
pide el grande, que mide 8 px a escala 100% y sube con el DPI.

Para más habría que dibujar las esquinas nosotros con una ventana sin
marco del sistema, y eso **reabre los dos defectos que ya se
arreglaron**: vuelve la banda de la barra de título y vuelven las cuñas
en las esquinas. Decidido el 12 ago 2026: **se deja como está.**

Los 20 px que pedía la especificación quedan superados por lo que la
plataforma permite. El panel ya no dibuja esquinas ni borde propios:
los ponía encima de los del sistema y esa duplicación era la causa de
las cuñas.

**8. Queda sin decidir: la barra blanca de foco sobre el fondo `oro`**
da 1.44:1, por debajo del 3:1 que pide WCAG 1.4.11 para elementos no
textuales. Se mantiene blanca porque su trabajo es ser señal de forma,
no de color, y la maqueta manda. Anotado con el número por si algún día
se revisa.

### Después de publicar: que la actualización llegue

Decidido el 13 ago 2026, con el plan del planificador delante.

**El requisito**, con las palabras del usuario: «es importante que
cualquier persona que tenga mi programa también se le actualice». Hoy
quien instaló la 4.0.0 se queda ahí para siempre — y arrastra dos
fallos que destruían texto en silencio.

**Se descarta Velopack**, aunque sea la herramienta hecha para esto, por
tres motivos medidos:

1. Instala en `%LOCALAPPDATA%\{nombre}`, que con nuestro nombre **es la
   carpeta de datos**. Pondría su actualizador junto a `historial.json`.
2. `Arranque.cs` escribiría la ruta del ejecutable interno y no la del
   lanzador que Velopack usa para sobrevivir a las actualizaciones, y su
   documentación **no cubre el arranque con Windows**. El modo de fallo
   sería «el atajo no arranca», que es el requisito número uno.
3. Las 4.0.x ya están instaladas con Inno: migrar dejaría **dos
   instalaciones**, y la que pierda el atajo queda como ventana muda.

**Se elige lo aburrido**: aviso dentro del panel usando la API de
GitHub. Nada de canal nuevo, nada de modelo de instalación nuevo. El
instalador que hay ya reemplaza sin duplicar, admite modo silencioso y
no toca los datos.

Dato comprobado en vivo: la API devuelve el **`digest`** del asset en la
misma respuesta que la URL de descarga. Mejor ancla que el `SHA256.txt`,
que es un archivo suelto dentro de la propia release.

**Avisa, no actualiza solo.** Decisión del usuario: «que pregunte a las
personas». Y hay un motivo técnico que la respalda: el volcado del
historial es diferido y **solo se fuerza al salir desde la bandeja**, así
que un cierre impuesto puede perder hasta 3 segundos de copias. Quien
tiene que cerrar pastepad es pastepad, volcando primero.

**Partido en dos, a petición del usuario:**

- **4.1.0** — comprobar, avisar en el panel, y el botón **abre la página
  de la release en el navegador**. Una sola pieza nueva, cero riesgo
  sobre el historial. Efecto secundario bueno: descargando con el
  navegador vuelve la marca de la web, así que SmartScreen sigue
  protegiendo mientras el binario no esté firmado.
- **4.2.0 — hecho, y resultó ser otra cosa.** La pregunta era si el
  Restart Manager alcanza a la ventana oculta. Medido con su propia API
  contra pastepad corriendo: **no**, y por dos motivos a la vez.

  Una ventana solo-mensajes «cannot be enumerated», así que el buzón no
  recibe `WM_QUERYENDSESSION` — no es un fallo, es la definición. Y
  `bRestartable` era `False` porque nadie había llamado a
  `RegisterApplicationRestart`, que es la condición que pone la propia
  documentación de Inno para que `RestartApplications=yes` sirva de algo.

  Lo peor no era ninguno de los dos: **el tipo que veía el Restart
  Manager cambiaba según el panel estuviera abierto o escondido**
  (`RmMainWindow` frente a `RmOtherWindow`). El mismo programa se
  comportaba de dos maneras al actualizarse según dónde lo hubieras
  dejado.

  Arreglado con una ventana de nivel superior aparte que solo escucha
  (`Sistema/Cierre.cs`), el registro para reinicio, y
  `RestartApplications=yes`. El `skipifsilent` se queda como está: con el
  reinicio funcionando ya sobra, y quitarlo abriría pastepad a quien
  instale en silencio sin tenerlo abierto.

- **El botón que descarga y lanza el instalador** sigue pendiente, y
  ahora se apoya en algo verificado. Antes de escribirlo hay que decidir
  si pastepad se lanza a sí mismo el instalador o si se limita a abrir la
  release, que es lo que hace hoy.

Con una preferencia para apagar el aviso, visible desde el primer día.

### Paso 6 — Retirar la versión en Python

**No antes de tiempo.** Mientras la interfaz se esté migrando, esos
archivos son la fuente: el diseñador lee `estilo.py` para la paleta,
`filas.py` para el grupo de marcadores y `ventanas.py` para los
diálogos. Borrarlos ahora sería quedarse sin especificación a mitad de
camino.

**Cuándo:** cuando el paso 4 esté cerrado y el qa confirme paridad de
funciones. Antes de publicar la release del paso 5, no después.

**Qué se va:**

```
main.py  prueba.py  requirements.txt  build.bat
pastepad.spec  pastepad-portable.spec
pastepad/*.py        los diez módulos y el __init__
build/               salida de PyInstaller
instalador/dist/     el ejecutable viejo y su _internal
.venv/
```

**Qué se queda, y por qué:**

| | |
|---|---|
| `docs/mockups/` y `docs/ESPECIFICACION-UI.md` | El diseño no cambia con el lenguaje |
| `TRASPASO.md` y `PLAN.md` | La memoria de por qué está hecho así |
| `CHANGELOG.md`, `LICENSE`, `SECURITY.md`, `.github/` | No dependen de la implementación |
| `README.md` y `docs/README.es.md` | Hay que **actualizarlos**, no borrarlos: hablan de Flet y de 80–150 MB |
| `instalador/*.bat` y `LEEME.txt` | Se decide en el paso 5 si se traducen o se sustituyen |

**Qué hacer con `docs/FUNCIONES.md`:** documenta las 133 funciones de
la versión en Python. Mientras dure la migración es la lista de
comprobación de qué falta por portar. Se retira con el resto, no antes.

**Cómo se sabe que quedó bien:** `dotnet test` sigue en verde, la
aplicación arranca desde una copia limpia del repo, y `grep -r "\.py"`
no devuelve referencias vivas en documentación ni en scripts.

**Antes de borrar, un tag.** La versión en Python fue tres años de
trabajo y es la única referencia de comportamiento si algo se nos pasó.
Que quede recuperable con un nombre, no solo en el historial.

---

## Los riesgos

| Riesgo | Señal que lo delata | Qué hacer |
|---|---|---|
| La bomba de WinUI no despacha a la ventana solo-mensajes | El contador del paso 1 no sube nunca | Plan B: `SetWindowSubclass` sobre el HWND de la ventana XAML |
| `ExecutionEngineException` por subclase (issue 8669) | Cierre del proceso sin excepción manejada, aleatorio | Delegado en campo estático y `RemoveWindowSubclass` al destruir. Si persiste, volver a la ventana propia |
| Otra app se queda con `Ctrl+Shift+V` | `RegisterHotKey` devuelve 0 | Avisar **en la interfaz** con el error real, no en silencio, y ofrecer los otros cinco atajos de `config.ATAJOS` |
| Mica no aparece | Fondo plano | Es el comportamiento documentado. La paleta `#0B0B0D` ya cubre el caso; verificar que se ve bien, no forzar Mica |
| Native AOT rompe algo | Cuelgue tras navegar, o fallo solo en Release | Está aislado en el paso 5. Se descarta sin coste |
| Falta el runtime del SDK en la máquina del usuario | La app no arranca, sin ventana ni mensaje | `WindowsAppSDKSelfContained=true` lo elimina de raíz. El VC++ Redistributable sigue siendo requisito: comprobarlo en el instalador |
| Las cifras de memoria de `TRASPASO.md` no se cumplen | Medición del paso 5 muy por encima de 60 MB | No es motivo para abandonar: contra los 207 MB de Flet hay margen. Se anota la cifra real y se corrige el documento |

---

## Lo que queda fuera y por qué

- **Volver a discutir el framework.** No hay en documentación oficial
  ningún bloqueo duro de WinUI 3 para los requisitos innegociables. El
  único punto donde estorba —no expone `WndProc`— tiene solución
  documentada, y en WPF ese código Win32 sería idéntico salvo por el
  `HwndSource`.
- **La interfaz en el paso 1.** El paso 1 es deliberadamente feo: si
  mezclara la interfaz, un fallo del atajo volvería a tener veinte
  sospechosos, que es exactamente cómo se perdieron días la vez
  anterior.
- **MSIX y la Tienda.** Requiere identidad de paquete y contradice el
  requisito de instalar en `%LOCALAPPDATA%` con instalador propio.
- **`PublishSingleFile`.** Extrae a un temporal en el primer arranque,
  así que no da lo que promete el nombre y añade una variable al
  arranque en frío, que ya es lo más lento. Un instalador sobre la
  carpeta hace el mismo trabajo con menos magia.
- **Traducir `idiomas.py` y `estilo.py` ahora.** Son datos, no lógica;
  van con el paso 4.
- **Pruebas de interfaz automatizadas.** Con 19 pruebas de modelo y una
  lista de comprobación manual basta para una herramienta de un solo
  usuario. WinAppDriver añadiría más mantenimiento que señal.

---

## Lo que NO se pudo verificar

Dicho con esas palabras, y cada uno cubierto por un paso o un plan B:

1. **Las cifras de memoria de `TRASPASO.md`** (40–60 MB, 15–20% menos
   que WPF). No hay cifras oficiales de Microsoft. Se miden en el paso 5.
2. **La causa raíz del `ExecutionEngineException`** del issue 8669: el
   servidor cortó la conexión al leer el hilo. La sospecha habitual en
   .NET es que el recolector se lleve el delegado, y la mitigación sería
   mantenerlo en un campo estático — pero es sospecha, no comprobación.
3. ~~**Que la bomba de mensajes de WinUI 3 despache a ventanas ajenas
   al XAML** creadas en el mismo hilo.~~ **Comprobado el 12 ago 2026 en
   `csharp/PruebaAtajo`: sí despacha.** 100 pulsaciones sintéticas, 100
   `WM_HOTKEY` recibidos, todos en el hilo de interfaz. Detalle en
   `csharp/PruebaAtajo/RESULTADOS.md`.

---

[d1]: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads
[d2]: https://learn.microsoft.com/en-us/windows/apps/get-started/start-here
[d3]: https://learn.microsoft.com/en-us/windows/apps/winui/winui3/
[d4]: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/deploy-unpackaged-apps
[d5]: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/unpackage-winui-app
[d6]: https://learn.microsoft.com/en-us/windows/apps/develop/ui-input/retrieve-hwnd
[d7]: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey
[d8]: https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-hotkey
[d9]: https://learn.microsoft.com/en-us/windows/win32/api/commctrl/nf-commctrl-setwindowsubclass
[d10]: https://github.com/microsoft/microsoft-ui-xaml/discussions/8934
[d11]: https://github.com/microsoft/microsoft-ui-xaml/issues/5815
[d12]: https://github.com/microsoft/microsoft-ui-xaml/issues/8669
[d13]: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-addclipboardformatlistener
[d14]: https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.datatransfer.clipboard
[d15]: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.overlappedpresenter
[d16]: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindow
[d17]: https://learn.microsoft.com/en-us/windows/apps/develop/ui/system-backdrops
[d18]: https://learn.microsoft.com/en-us/windows/apps/design/style/mica
[d19]: https://github.com/microsoft/microsoft-ui-xaml/issues/2020
[d20]: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/applifecycle/applifecycle-single-instance
[d21]: https://github.com/microsoft/WindowsAppSDK-Samples/blob/main/Samples/AppLifecycle/Instancing/cpp-win32-unpackaged/CppWinMainInstancing/CppWinMainInstancing.cpp
[d22]: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-1-6
[d23]: https://www.nuget.org/packages/Microsoft.Windows.CsWin32
