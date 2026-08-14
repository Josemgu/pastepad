# CLAUDE.md

Contexto para trabajar en este proyecto. Léelo antes de tocar código.

## Qué es

pastepad: gestor de portapapeles para Windows. Guarda todo lo que se
copia, más textos que el usuario organiza en carpetas, y los pega donde
estaba el cursor con un atajo global.

Reemplaza al historial de Windows (Win+V), que solo guarda 25 entradas y
las pierde al reiniciar.

## Estado

Versión 4.8.0, reescrita en C# con WinUI 3 sobre el Windows App SDK
2.3.1 y .NET 10. Desempaquetada y self-contained.

La versión anterior (3.x, Python con Flet) **ya no está en el repo**.
Vive en el tag `ultima-version-python` y en `v3.0.1`. Si necesitas ver
cómo hacía algo:

```
git show ultima-version-python:pastepad/busqueda.py
```

Por qué se reescribió, qué se conservó y qué se aprendió por el camino:
`TRASPASO.md`. Cómo se hizo, paso a paso y con las fuentes:
`PLAN.md`. Los dos siguen vigentes.

**Lo que está pedido y sin hacer va en `PENDIENTE.md`.** Míralo antes de
empezar una versión. Y desde agosto de 2026 se publica una actualización
al mes: en cada una, el planificador entrega además qué optimizar y qué
ha sacado Microsoft del Windows App SDK, esté o no en el encargo.

## Estructura

```
csharp/
  Pastepad.Nucleo/           net10.0 puro, sin Windows App SDK
    Modelo.cs                reglas de los datos
    Almacen.cs               lo unico que toca el disco
    Busqueda.cs              ranking e indice con cache
    Textos.cs                4 idiomas
    Versiones.cs             comparar versiones y decidir si toca avisar
    Argumentos.cs            la linea con la que Windows nos reabre
    Tipos.cs                 de que es cada guardado, y cuando se escribe
    Config.cs, Datos.cs, Autoarranque.cs, Rutas
  Pastepad.Nucleo.Pruebas/   99 pruebas, sin abrir ventana
  Pastepad.App/
    Sistema/                 todo lo que habla con Win32
      Buzon.cs               ventana solo-mensajes: atajo y portapapeles
      Cierre.cs              oye que nos van a cerrar y vuelca antes
      Portapapeles.cs        lectura, escritura, formatos privados, RTF
      Foco.cs                devolver el foco y pegar
      Actualizacion.cs       consulta la API de GitHub. Solo consulta
      Arranque.cs            autoarranque: clave Run Y tarea de sesion
      Paquete.cs             si nuestros archivos acaban en otro sitio
      Bandeja.cs, Pantalla.cs, Nativo.cs
    Panel.xaml(.cs)          el panel
    Formato.cs               la barra de formato sobre RichEditBox
    Dialogos.cs, Estilo.cs, Fila.cs
    Program.cs               Main propio: instancia unica
  PruebaAtajo/               el sondeo del paso 1. No se publica
instalador/pastepad.iss      Inno Setup, el unico archivo
docs/                        35 maquetas, especificacion, logos
```

`Pastepad.Nucleo` no importa nada gráfico **a propósito**. Es lo que
permite que 99 pruebas corran sin abrir ventana y sin el Windows App
SDK. No metas WinUI ahí dentro.

Ese reparto es también por qué `Versiones.cs` está en el núcleo y
`Actualizacion.cs` en la aplicación: la decisión —comparar versiones,
saber cuándo toca mirar y cuándo avisar— es lo que se puede romper en
silencio, así que vive donde se puede probar. La consulta de red no.

## Cómo trabajar aquí

```powershell
dotnet build csharp/Pastepad.slnx
dotnet test csharp/Pastepad.Nucleo.Pruebas
```

**Compila siempre por la solución, no por el proyecto.** Escriben en
carpetas distintas: la solución en `bin/x86/...`, el proyecto en
`bin/...` para x64. Medir sobre el binario viejo ya dio dos
conclusiones falsas —que `ResizeClient` funcionaba, y que un arreglo de
disposición no se había aplicado—. Comprueba la marca de tiempo del
`.exe` antes de creerte una medida.

**Para ejecutar, siempre `--datos`.** Desde la 4.2.0 esa opción tampoco
toca la entrada de arranque del registro, que hasta entonces sí
reescribía —dejando el autoarranque del usuario apuntando a una
compilación de pruebas—. Aun así, si algo va raro, mira
`HKCU\...\Run\pastepad`.

```powershell
& ".\csharp\Pastepad.App\bin\x86\Debug\net10.0-windows10.0.26100.0\win-x86\pastepad.exe" --datos "C:\temp\prueba"
```

Sin esa opción usa `%LOCALAPPDATA%\pastepad`, que es **el historial real
del usuario**. Una sesión de pruebas ya se llevó por delante entradas
suyas.

Para publicar:

```powershell
dotnet publish csharp/Pastepad.App -c Release -p:Platform=x86 -r win-x86
```

## Decisiones medidas. No las revientes sin medir tú

Estas no son opiniones: se comprobaron ejecutando, y hay número.

**El atajo va sobre una ventana solo-mensajes**, no subclasando la
ventana de XAML. `Sistema/Buzon.cs`. Medido 100/100 en el sondeo y
30/30 dos veces por QA sobre el ejecutable. El subclase arrastra un
`ExecutionEngineException` abierto en el repositorio de WinUI, y atar el
atajo a la ventana visible —que se esconde— es el acoplamiento que mató
a la versión anterior.

**La escucha del cierre es una ventana APARTE del buzon**, y de nivel
superior. `Sistema/Cierre.cs`. Una ventana solo-mensajes «cannot be
enumerated», y lo que no se enumera no recibe `WM_QUERYENDSESSION`: el
buzón es sordo a eso por construcción. Medido con la propia API del
Restart Manager sobre pastepad corriendo: `bRestartable=False`, y el tipo
cambiaba entre `RmOtherWindow` y `RmMainWindow` **según el panel
estuviera escondido o abierto**. Después del cambio: cierre limpio en
157–201 ms, reapertura en 56–68 ms, y una copia hecha 400 ms antes
sobrevive.

**`RegisterApplicationRestart(null, ...)` no conserva los argumentos, los
borra.** «If this parameter is NULL or an empty string, the previously
registered command line is removed». Se comprobó: una instancia lanzada
con `--datos` volvió sin él y abrió el almacén real.

**El portapapeles va por Win32**, no por la clase `Clipboard` de WinRT.
Su documentación dice que solo se accede con la aplicación enfocada, y
un gestor de portapapeles vive en segundo plano por definición.

**El filtro de secuencia del portapapeles no es redundante.**
`WM_CLIPBOARDUPDATE` llega **más de una vez por copia** —PowerShell
dispara tres—. Sin `GetClipboardSequenceNumber` cada copia entra tres
veces en el historial.

**Instancia única con `AppInstance.FindOrRegisterForKey`**, no con un
mutex. Y la clave se resume con SHA-256, no con `GetHashCode`, que en
.NET Core está aleatorizado por proceso.

**`MarcoVentana.cs` se queda.** `AppWindow.ResizeClient` dice en su
documentación que calcula el área no cliente por ti; medido, da 31 px
de más y el panel crece en cada apertura.

**Los ajustes de los diálogos van siempre apilados.** El reparto en dos
columnas no cabe hasta un panel de ~455 px, y el de fábrica son 380. El
desplegable no pone puntos suspensivos: recorta por la izquierda, y
salía `l + Shift + V` en vez de `Ctrl + Shift + V`.

**El autoarranque va por dos caminos, y es a propósito.** La clave
`HKCU\...\Run` y una tarea al iniciar sesión con 30 s de retraso. No es
cinturón y tirantes por gusto: hubo un arranque en el que la clave era
correcta, el ejecutable existía, no estaba desactivada en el
Administrador de tareas, y Windows sí procesó esa clave —OneDrive, que
está en la misma, arrancó 15 s después de explorer— y pastepad no dejó
**ni una línea** en ninguno de los dos logs ni informe de fallo. O el
proceso no llegó a crearse, o murió antes de su primera instrucción, que
es donde el host de .NET falla en silencio. La causa sigue sin saberse.

La tarea se registra sin ser administrador porque va con
`InteractiveToken` y `LeastPrivilege`: «you do not need to specify a
password when registering the task if you register the task to run under
the security context of your account and you use the S4U or interactive
logon type». Con `Password` o `S4U` haría falta el privilegio de inicio
de sesión como proceso por lotes, que un usuario normal no tiene.
Comprobado registrándola y borrándola sin elevación.

**No se puede registrar lo que no pasa.** Por eso cada arranque anota
cuánto llevaba Windows encendido (`Environment.TickCount64`): unos
segundos significa que nos abrió Windows, unas horas que el autoarranque
falló y abrió el usuario. Es lo único que convierte «no arrancó» en algo
que se lee.

**Medir el atajo de punta a punta, no solo el trozo cómodo.** La medida
vieja empezaba en `AlAtajo` y acababa en `Activate()`, y decía 25 ms
mientras el usuario notaba esperas. Se dejaban fuera los dos trozos donde
puede estar el retraso: lo que el mensaje espera en la cola —que sale de
`GetTickCount - GetMessageTime()`, y es donde asoma un proceso que
Windows echó de memoria o frenó por estar en segundo plano— y lo que
tarda en dibujarse el primer fotograma, porque `AppWindow.Show()` vuelve
antes de que haya píxeles.

## Cosas que romperás si no tienes cuidado

- **`dotnet publish` no copia el XAML compilado ni el `.pri` ni los
  Assets.** Hay un `Target` en el `.csproj` que los añade, y un
  `<Error>` que rompe la compilación si falta el `.pri`. Sin eso, lo
  publicado **muere al arrancar** con `XamlParseException` — y el build
  funciona igual, así que solo se ve ejecutando lo que se distribuye.
- **`dotnet publish` tampoco vacía la carpeta de destino.** Lo que
  quede ahí viaja dentro del instalador. Ya se coló un `errores.log`.
- **`File.Exists` devuelve `false` si no hay permiso de lectura**, sin
  lanzar. Por eso `Almacen.Leer` abre el archivo en vez de preguntar: un
  archivo ilegible que pase por «no existe» acaba sobrescrito.
- **Los datos del usuario viven en `%LOCALAPPDATA%\pastepad`** y el
  programa en `%LOCALAPPDATA%\Programs\pastepad`. Separados a propósito:
  así desinstalar no puede tocar el historial.

## Probar con contenido largo. No es opcional

La 4.0.0 salió con **seis fallos que 24 comprobaciones no vieron**,
porque todas usaban textos de dos líneas. El usuario los encontró el
primer día. Dos de los seis destruían texto en silencio.

Cuando pruebes cualquier cosa que toque texto, hazlo con **treinta,
sesenta y cien líneas**. Y comprueba el resultado **en
`snippets.json`**, no en pantalla: los dos que destruían datos se veían
bien mientras no abrieras el archivo.

Tres trampas que ya costaron caras:

- **Un `TextBox` de WinUI con `AcceptsReturn` en `false` se queda con la
  primera línea** de lo que se le asigne. En un inicializador de objeto,
  `Text` tiene que ir **después** de `AcceptsReturn`, o el texto se
  trunca al entrar.
- **`TextBox.Text` devuelve `\r` a secas**, no `\r\n` ni `\n`. Partir
  por `\n` deja una sola línea con todo dentro.
- Al comparar longitudes, la caja carga **tantos caracteres menos como
  saltos de línea tenga** el archivo. Con 100 líneas, 5290 en disco son
  5191 en la caja. Si la cuenta cuadra, no hay pérdida.

**El panel se compone una vez al arrancar, fuera de pantalla.**
`Panel.Calentar()`. No es prudencia: medido, en el instante en que la
ventana aparece el contenido está a medio pintar —unos 5.500 píxeles de
los 36.500 que tiene completa— y termina en unos 150 ms. Con la máquina
cargada se estira hasta parecer que falta media interfaz. Después del
cambio, seis vueltas bajo carga: 6.562 en la cabecera y 36.518 en el
cuerpo **ya en el instante 0**, idéntico a los 300 ms.

Va con `Show(false)` —«Shows the window with an option to activate it or
not»— para no robarle el primer plano a nadie al iniciar sesión, y a
`-32000,-32000`, que queda fuera de cualquier monitor: el escritorio
virtual de la máquina de pruebas iba de `X -2560..2560, Y 0..1440`. Y
**no toca `EstaVisible`**, así que el manejador que esconde el panel al
perder el foco sale por su primera línea y no se entera.

**Un atajo que no se puede registrar se avisa por la bandeja, no por el
panel.** El panel se abre con el atajo: quien tiene el problema es
exactamente quien no puede llegar al mensaje que se lo explica.

Y lo peor no es quedarse sin atajo, es **cambiarlo sin decirlo**. Si el
del usuario está cogido, pastepad cae al de fábrica, y hasta la 4.8.0 se
quedaba tan ancho porque el de fábrica sí entraba y no había `Problema`
que enseñar. El usuario sigue pulsando el suyo para siempre y la tecla
se le cuela al programa de delante — que es justo la señal de que
pastepad no la recibió. Reproducido con dos instancias peleándose:
`Windows rechazo ctrl+alt+p (error 1409)`, que es
`ERROR_HOTKEY_ALREADY_REGISTERED`.

**Los avisos de cuando el panel no está delante van por la bandeja.**
Al pegar, el panel se esconde ANTES de devolver el foco, así que el
aviso «Copiado, pero no pude volver» se pintaba en una ventana ya
invisible: el usuario se quedaba con el texto copiado, sin pegar y sin
saber por qué. Y encima el aviso sobrevivía escondido y reaparecía en la
apertura siguiente, ya sin contexto. Si escribes un `Avisar()` nuevo,
mira antes si en ese punto el panel está visible.

### El árbol de accesibilidad no dice nada de lo que se pinta

El usuario reportó que el atajo de la cabecera no se veía en la primera
apertura. **Los volcados de UI Automation lo veían las dos veces**, así
que ninguna comprobación por automatización lo iba a detectar nunca. Y
lo que la automatización reporta como «Esc» ni siquiera es una etiqueta:
no existe esa cadena en el código, es el `KeyboardAccelerator` con
`Key="Escape"`, que UIA expone como propiedad.

**Para cualquier cosa que sea «no se ve», hay que capturar píxeles.** Y
capturar la ventana ENTERA, no un recorte: con un recorte de 44 px de la
cabecera se concluyó que «solo fallaba esa fila», y al medir la ventana
completa resultó que el primer fotograma está a medio pintar en todas
partes —de ~5.500 píxeles de contenido a 0 ms a ~36.500 a los 300 ms—.
El recorte solo probaba que el borde del buscador estaba dibujado.

Cuidado además con dónde se toma el color de fondo de referencia: la
ventana tiene esquinas redondeadas, así que en `(2,2)` se cuela el
escritorio y la cuenta se dispara.

Y para capturar el panel hay un obstáculo: **se esconde solo al perder
el foco**. Sacarlo lanzando una segunda instancia, o invocar algo por
UIA, devuelve el foco a la terminal y el panel desaparece antes de la
captura. Hay que sacarlo con el atajo y sondear cada 100-150 ms.

### Y para automatizar la interfaz

Los `MenuFlyoutItem` de esta aplicación **no responden a clic sintético**
con `mouse_event`. Hay que usar `InvokePattern.Invoke()`. Para que el
botón de tres puntos de una fila asome, además hay que seleccionar la
fila por patrón. Eso dejó dos comprobaciones sin hacer en dos rondas
distintas antes de descubrirse.

**Si los archivos no acaban donde se pidieron, no se escribe nada.**
`Sistema/Paquete.cs` + `Almacen.Congelar`. Cuando otra aplicación
empaquetada abre pastepad, este hereda su contenedor y Windows redirige
`%LOCALAPPDATA%` a `…\Packages\<paquete>\LocalCache\Local`. pastepad
calcula bien su ruta, cree que la lee, y lee y escribe una copia; el
usuario lo abre después desde su sitio y **su historial y sus textos han
desaparecido**, aunque en disco sigan intactos. Pasó el 14 ago 2026.

Detectarlo costó tres medidas, y dos de ellas parecían buenas y no lo
eran:

- **La identidad de paquete no vale.** `GetCurrentPackageFullName`
  devuelve `APPMODEL_ERROR_NO_PACKAGE`: el proceso hijo hereda la
  redirección de archivos pero **no** la identidad.
- **Un handle de directorio tampoco.** La carpeta se resuelve a sí
  misma; solo un archivo delata la redirección.
- **Y el archivo tiene que estar recién escrito.** Es copia-al-escribir,
  archivo a archivo: uno que ya estuviera resuelve a la ruta real hasta
  que alguien lo escribe.

Por eso la comprobación es una sonda que se escribe al arrancar, con
`DELETE_ON_CLOSE` para que no deje rastro.

**Y ojo al probar con `--datos`: entrecomilla la ruta.** `Start-Process
-ArgumentList` no lo hace, y con un perfil que lleva espacios el
programa recibe la ruta partida y usa `C:\Users\Jose` como carpeta de
datos. Eso hizo que el guardián "no saltara" durante varias rondas: lo
que fallaba era la prueba. Todas las rutas del scratchpad usan
`JOSEMI~1`, que no tiene espacios, y por eso el fallo no se veía ahí.

## El fallo que estuvo abierto, y cómo se cerró

Durante cinco versiones hubo esto anotado: *«una instancia arrancó sin
poder leer ni escribir en su carpeta, con el atajo funcionando
perfectamente, y al cerrarse guardó un historial vacío encima del real.
No se ha reproducido.»*

**Se reprodujo el 14 ago 2026 y era la redirección de contenedor**
descrita más arriba. El usuario abrió pastepad y se encontró sus notas,
sus estilos y su atajo borrados; en el disco estaban intactos, y lo que
él veía era una copia dentro de
`…\Packages\<paquete>\LocalCache\Local`. Se recuperó copiando desde esa
copia, con el programa cerrado y desde fuera del contenedor.

Lo que lo convirtió en «se borró todo» no fue la redirección sino el
silencio: pastepad arrancó vacío y siguió como si eso fuera normal. Por
eso el arreglo no es solo detectarlo, es **congelar la sesión**:
`Almacen.Congelar` corta todas las escrituras en `Escribir<T>`, que es
el único sitio por el que pasan, y el panel se abre con el motivo.

Dos avisos para quien investigue algo parecido:

- **Desde una sesión que viva dentro de un contenedor, lo que leas de
  `%LOCALAPPDATA%` no es lo que hay en el disco.** Pasó aquí: durante
  horas se diagnosticó sobre una vista redirigida. Para ver el disco de
  verdad hay que salir del contenedor — una tarea programada sirve, la
  lanza el Programador y no hereda nada.
- **Comprueba tu prueba antes de creerte el resultado.** El guardián
  «no saltaba» en tres rondas seguidas y funcionaba: lo que fallaba era
  la ruta sin comillas del `--datos`.

## Estilo del código

- Comentarios y nombres en español, sin tildes **en el código**. Esto
  es para identificadores y comentarios, **no para el texto que ve el
  usuario**: ahí van las tildes y los signos de apertura. Se publicó una
  versión con «Como» por «Cómo» y «Si, borrar» por «Sí, borrar» por
  confundir las dos cosas.
- Los comentarios explican **por qué**, no qué hace la línea.
- Nada de código muerto ni de opciones «por si acaso».
- La prueba se escribe junto con lo que implementa, no después.
- **Ningún `catch` mudo.** Todo lo que se captura se registra con dónde
  y por qué. Y compila sin avisos: tres `CS8601` que parecían
  cosméticos eran las cuatro tablas de traducción en `null`, o sea la
  aplicación reventando al elegir cualquier idioma.

## Historial

`CHANGELOG.md` tiene el detalle. Resumen:

- 1.x — un solo archivo, tkinter
- 2.0 — reescrito en módulos, con pruebas
- 3.0 — interfaz migrada a Flet
- 4.0 — reescrito en C# con WinUI 3, por el atajo global
