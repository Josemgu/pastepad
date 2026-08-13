# CLAUDE.md

Contexto para trabajar en este proyecto. Léelo antes de tocar código.

## Qué es

pastepad: gestor de portapapeles para Windows. Guarda todo lo que se
copia, más textos que el usuario organiza en carpetas, y los pega donde
estaba el cursor con un atajo global.

Reemplaza al historial de Windows (Win+V), que solo guarda 25 entradas y
las pierde al reiniciar.

## Estado

Versión 4.2.0, reescrita en C# con WinUI 3 sobre el Windows App SDK
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
    Config.cs, Datos.cs, Autoarranque.cs, Rutas
  Pastepad.Nucleo.Pruebas/   78 pruebas, sin abrir ventana
  Pastepad.App/
    Sistema/                 todo lo que habla con Win32
      Buzon.cs               ventana solo-mensajes: atajo y portapapeles
      Cierre.cs              oye que nos van a cerrar y vuelca antes
      Portapapeles.cs        lectura, escritura, formatos privados, RTF
      Foco.cs                devolver el foco y pegar
      Actualizacion.cs       consulta la API de GitHub. Solo consulta
      Bandeja.cs, Pantalla.cs, Arranque.cs, Nativo.cs
    Panel.xaml(.cs)          el panel
    Formato.cs               la barra de formato sobre RichEditBox
    Dialogos.cs, Estilo.cs, Fila.cs
    Program.cs               Main propio: instancia unica
  PruebaAtajo/               el sondeo del paso 1. No se publica
instalador/pastepad.iss      Inno Setup, el unico archivo
docs/                        35 maquetas, especificacion, logos
```

`Pastepad.Nucleo` no importa nada gráfico **a propósito**. Es lo que
permite que 74 pruebas corran sin abrir ventana y sin el Windows App
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

### Y para automatizar la interfaz

Los `MenuFlyoutItem` de esta aplicación **no responden a clic sintético**
con `mouse_event`. Hay que usar `InvokePattern.Invoke()`. Para que el
botón de tres puntos de una fila asome, además hay que seleccionar la
fila por patrón. Eso dejó dos comprobaciones sin hacer en dos rondas
distintas antes de descubrirse.

## Fallo abierto

Una instancia arrancó sin poder leer ni escribir en su carpeta, con el
atajo funcionando perfectamente, y al cerrarse guardó un historial vacío
encima del real. **No se ha reproducido.** El daño está contenido —un
archivo ilegible queda marcado y no se sobrescribe, con cuatro pruebas
que lo fijan— y ahora deja rastro: `HResult` en el log, reintento con
espera, y la ruta real del almacén en la línea de arranque.

Detalle en `PLAN.md`.

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
