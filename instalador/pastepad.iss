; Instalador de pastepad. Se compila con ISCC:
;
;   ISCC /DVersion=4.0.0 /DPublish=<carpeta de publish> pastepad.iss
;
; El programa va a %LOCALAPPDATA%\Programs\pastepad y los datos siguen
; en %LOCALAPPDATA%\pastepad. Esa separacion no es cosmetica: hasta
; ahora el programa se copiaba DENTRO de la carpeta de datos, y por eso
; el desinstalador tenia que preguntar "borro tambien tus datos?".
; Separados, desinstalar no puede tocar el historial aunque quiera —no
; por acordarse de preguntar, sino porque {app} y la carpeta de datos ya
; no son la misma.

#ifndef Version
  #define Version "0.0.0"
#endif

; La carpeta que deja
;   dotnet publish csharp/Pastepad.App -c Release -p:Platform=x86 \
;       -r win-x86 -p:Version=<version> -o publicado-x86
#ifndef Publish
  #define Publish "..\publicado-x86"
#endif

[Setup]
; Fijo y para siempre: es lo que hace que reinstalar reemplace en vez de
; duplicar, y lo que identifica la entrada de Agregar o quitar programas.
; Si cambia, Windows se encuentra dos pastepad instalados.
AppId={{DF56167A-B0A2-4B5B-AF7F-91DF597A67C9}

AppName=pastepad
AppVersion={#Version}
AppVerName=pastepad {#Version}
VersionInfoVersion={#Version}
AppPublisher=Jose Miguel Ortiz
AppPublisherURL=https://github.com/josemiguelortiz/pastepad
AppSupportURL=https://github.com/josemiguelortiz/pastepad/issues
AppUpdatesURL=https://github.com/josemiguelortiz/pastepad/releases

; Sin administrador. Con esto {autopf} resuelve a %LOCALAPPDATA%\Programs
; y no a Archivos de programa, que es donde Windows bloquea la escritura
; sin avisar.
PrivilegesRequired=lowest
DefaultDirName={autopf}\pastepad
DefaultGroupName=pastepad
DisableProgramGroupPage=yes
DisableDirPage=auto

; Sin ArchitecturesInstallIn64BitMode a proposito: el instalador corre en
; modo 32 bits en las tres arquitecturas, que es lo que hace que {autopf}
; y la rama del registro apunten siempre al mismo sitio. Un ejecutable de
; 32 bits corre igual en x64 (WOW64) y en ARM64 (emulacion).

LicenseFile=..\LICENSE
SetupIconFile=..\csharp\Pastepad.App\Assets\AppIcon.ico
UninstallDisplayIcon={app}\pastepad.exe
UninstallDisplayName=pastepad

OutputDir=..\dist
OutputBaseFilename=pastepad-{#Version}-instalador

; lzma2/ultra64 con diccionario grande: son 476 archivos y casi 200 MB de
; runtime que se repiten mucho entre si. Si ISCC se queda sin memoria
; —la documentacion pide unos 742 MB para ultra64— baja a lzma2/max:
; cuesta unos MB de descarga, no el plan.
Compression=lzma2/ultra64
SolidCompression=yes

WizardStyle=modern

; Cierra pastepad antes de sobrescribirlo. Sin esto, reinstalar encima
; con el programa abierto falla por archivo bloqueado.
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "escritorio"; Description: "Crear un acceso directo en el escritorio"; Flags: unchecked

[Files]
Source: "{#Publish}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\pastepad"; Filename: "{app}\pastepad.exe"; Comment: "Gestor de portapapeles"
Name: "{autodesktop}\pastepad"; Filename: "{app}\pastepad.exe"; Tasks: escritorio

; Sin seccion [Registry], y es la decision, no un olvido: del arranque
; con Windows se encarga la aplicacion, que lo aplica en cada inicio
; segun la preferencia "autoarranque" de config.json. Con dos duenos del
; mismo valor, uno lo escribe y el otro lo borra — y desinstalar se
; llevaba por delante una preferencia que el usuario habia elegido.
;
; Efecto secundario bueno: quien desinstale y vuelva a instalar conserva
; su preferencia en config.json y la aplicacion la vuelve a aplicar sola.

[Run]
Filename: "{app}\pastepad.exe"; Description: "Abrir pastepad"; \
    Flags: nowait postinstall skipifsilent

; El registro de reserva que la aplicacion escribe junto a su ejecutable.
; No lo instala el instalador —lo crea el programa al arrancar—, asi que
; Inno no lo borra solo y la carpeta sobrevivia a la desinstalacion con un
; archivo dentro. Lo encontro el qa.
;
; Ese log existe porque hubo arranques que no dejaban ni una linea en la
; carpeta de datos; escribir tambien aqui es lo que garantiza que quede
; rastro. Al desinstalar ya no hace falta.
[UninstallDelete]
Type: files; Name: "{app}\errores.log"

; Nada de [UninstallDelete] sobre la carpeta de DATOS, y no es un olvido.
; %LOCALAPPDATA%\pastepad —historial, textos guardados e imagenes— es del
; usuario y no lo instalo yo, asi que no me toca borrarlo. Desinstalar se
; lleva {app} y nada mas: la entrada de arranque tampoco, porque la
; gestiona la aplicacion segun la preferencia del usuario.
