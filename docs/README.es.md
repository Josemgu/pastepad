<div align="center">

<img src="logo.svg" alt="" width="120">

# pastepad

Un gestor de portapapeles para Windows.

[English](../README.md) · [Español](README.es.md) · [MIT](../LICENSE)

</div>

Windows guarda 25 cosas en el portapapeles y las pierde al reiniciar.
pastepad guarda 80, más lo que archives en tus propias carpetas, que no
caducan.

Pulsa <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>V</kbd> donde estés. Escribe
dos letras, pulsa <kbd>Enter</kbd> y el texto cae en el campo donde
tenías el cursor.

<div align="center">
  <img src="capturas/es-reciente.png" alt="Pestaña Reciente" width="270">
  <img src="capturas/es-guardados.png" alt="Pestaña Guardados" width="270">
  <img src="capturas/es-apariencia.png" alt="Diálogo de apariencia" width="270">
</div>

## Qué hace

Todo lo que copias aparece en Reciente, lo mismo un texto que una
captura. Lo que uses a menudo se puede fijar, y entonces se queda arriba
y el recorte que limpia el resto no se lo lleva.

Los textos guardados van en carpetas que nombras tú. Si escribes
`[[algo]]` dentro de uno, el programa te lo pregunta antes de pegar:

```
Hola [[nombre]], te escribo sobre [[tema]] del día [[fecha]].
```

Los enlaces reciben otro trato. Cuando lo copiado es una dirección web y
nada más, la fila enseña un icono de enlace y el dominio en lugar del
contador de caracteres, y al hacer clic se abre el navegador en vez de
pegarse. Dentro de Guardados van en un grupo plegable aparte, porque
abrir un marcador y pegar una nota son dos gestos distintos: mezclados,
hay que leerse la lista entera para encontrar cualquiera de los dos.

La búsqueda cruza las dos pestañas a la vez. Las palabras pueden ir en
cualquier orden, las tildes dan igual, y lo que coincide en el título
pesa más que lo que aparece perdido en el cuerpo.

Hay cosas que no se anotan nunca. Windows define cuatro
[formatos de portapapeles](https://learn.microsoft.com/es-es/windows/win32/dataxchg/clipboard-formats)
con los que un programa avisa de que eso no se guarde, y los ponen
KeePass, Bitwarden, el Administrador de credenciales y las ventanas de
incógnito de Chrome. pastepad respeta los cuatro y descarta ese
contenido.

## Cómo se usa

| Tecla | Qué hace |
|:--|:--|
| <kbd>Ctrl</kbd> <kbd>Shift</kbd> <kbd>V</kbd> | Abre el panel junto al cursor |
| Escribir | Filtra según escribes |
| <kbd>↑</kbd> <kbd>↓</kbd> | Recorre los resultados |
| <kbd>Enter</kbd> | Pega |
| <kbd>Esc</kbd> | Cierra |

Haz clic en el campo que quieres rellenar antes de abrir el panel.
pastepad se guarda qué ventana tenía el foco y se lo devuelve antes de
pegar.

La X solo esconde el panel. Para cerrarlo del todo, `taskkill /IM
pastepad.exe /F`, o Salir desde el icono de la bandeja.

## Instalación

Descarga `pastepad-4.0.0-instalador.exe` de la
[última versión](https://github.com/Josemgu/pastepad/releases/latest) y
ejecútalo. Ocupa 47,1 MB.

```
programa      %LOCALAPPDATA%\Programs\pastepad
datos         %LOCALAPPDATA%\pastepad
desinstalar   Configuración → Aplicaciones → Aplicaciones instaladas
```

No pide permiso de administrador: se instala solo para tu usuario. Al
desinstalar se va el programa y tus datos se quedan donde están, que es
justamente por lo que viven en carpetas separadas. Y el arranque con
Windows lo gobierna el propio programa, así que si reinstalas conserva
lo que hubieras elegido.

Los datos van a `%LOCALAPPDATA%` y no a *Archivos de programa* a
propósito. Ahí Windows bloquea la escritura sin avisar, y pastepad
arrancaría para luego no guardar nada.

## El aviso de Windows

La primera vez sale *«Windows protegió su PC»*. Hay que pulsar **Más
información** y luego **Ejecutar de todas formas**.

Ni la licencia MIT ni tener el código publicado evitan ese aviso.
SmartScreen no mira ninguna de las dos cosas. Mira si el binario va
firmado y cuánta gente lo ha descargado sin incidentes, y un ejecutable
nuevo y sin firmar no tiene ni lo uno ni lo otro.

Se está solicitando para el proyecto el certificado gratuito de
[SignPath](https://signpath.org/), que es lo que hace desaparecer el
aviso de verdad. A partir de ahí la reputación se acumula entre
versiones en vez de empezar de cero con cada una. Mientras tanto, estas
son las opciones:

| Opción | Efecto | Coste |
|---|---|---|
| Aceptar el aviso | Un clic, una vez | 0 |
| Comprobar el SHA256 publicado | Descarga verificable, el aviso sigue | 0 |
| Certificado gratuito para código abierto ([SignPath](https://signpath.org/), [OSSign](https://ossign.org/)) | Firma de verdad, la reputación se acumula | 0 |
| [Azure Trusted Signing](https://learn.microsoft.com/es-es/windows/apps/package-and-deploy/code-signing-options) | Lo mismo, gestionado por Microsoft | ~10 $ al mes |
| Microsoft Store | Quita el aviso por completo | Cuenta de desarrollador |

Firmarlo uno mismo no sirve: Windows no se fía de un certificado que no
venga de una autoridad reconocida. Y los certificados de validación
extendida ya no dan reputación instantánea; eso dejó de ser cierto hace
años.

Cada versión publica un `SHA256.txt` para poder comprobar que lo
descargado es exactamente lo que salió de la compilación.

## Cómo está hecho

C# sobre .NET 10 con WinUI 3, sin identidad de paquete. El portapapeles,
el atajo global, el icono de la bandeja y la devolución del foco son
llamadas a Win32; lo demás es XAML. La capa de datos tiene 45 pruebas
que corren sin abrir ninguna ventana.

Hasta la 3.0.1 el programa estaba escrito en Python con Flet. Se
abandonó porque el atajo global dejaba de responder a las pocas
pulsaciones y la causa era de fondo: el atajo y la ventana acababan en
hilos distintos. Aquel código sigue vivo en el tag
`ultima-version-python`. En [PLAN.md](../PLAN.md) y
[TRASPASO.md](../TRASPASO.md) está el razonamiento y lo que se conservó.

## Tus datos

Están en `%LOCALAPPDATA%\pastepad`, como archivos normales que puedes
copiar o respaldar:

```
snippets.json     textos guardados y carpetas
historial.json    el historial automático
config.json       idioma, tema, color, atajo, tamaño
imagenes\         las capturas copiadas
```

El historial se guarda sin cifrar. Lo que venga de un gestor de
contraseñas se descarta solo, pero cualquier otra cosa delicada que
copies sí queda escrita. La escoba del pie vacía el historial y el botón
de pausa detiene la captura. Ver [SECURITY.md](../SECURITY.md).

Mejor no dejar la carpeta dentro de OneDrive: la sincronización puede
bloquear los JSON a mitad de escritura.

## Para dejarlo a tu gusto

Cuatro idiomas: español, inglés, portugués y francés. Se elige en
Apariencia y se recuerda entre sesiones.

Doce fondos y dieciocho colores de acento. «Según Windows» sigue al tema
del sistema y cambia en caliente cuando lo cambias tú. Los dieciocho
acentos cumplen el contraste AA de WCAG contra el texto que se dibuja
encima, comprobado calculándolo y no a ojo.

El panel se estira arrastrando sus bordes, entre 300×340 y 720×1100, y
recuerda dónde lo dejaste.

## Diseño

La interfaz se dibujó antes de construirse. Hay 35 maquetas SVG en
[docs/mockups](mockups), y
[ESPECIFICACION-UI.md](ESPECIFICACION-UI.md) recoge la paleta, las
medidas y el comportamiento con los valores exactos.

Ese documento avisa de dos cosas: las maquetas 20 y 33 nunca se
comprobaron contra el programa en marcha, y la 26 y la 27 dibujan
diálogos de SmartScreen, que son de Windows y no de pastepad.

## Por qué otro más

[Ditto](https://github.com/sabrogden/Ditto) y
[CopyQ](https://github.com/hluk/CopyQ) son buenos programas con años de
trabajo detrás. Este existe porque mi trabajo diario pedía dos cosas que
ninguno de los dos hace sin montárselo: plantillas con huecos para notas
que reescribo a todas horas, y pegado con formato que sobreviva al
llegar a Outlook.

---

<div align="center">
<sub>por <a href="https://github.com/Josemgu">Jose Miguel Ortiz</a></sub>
</div>
