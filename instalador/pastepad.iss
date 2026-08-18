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

; Donde Inno apunta lo que hay instalado. Es la misma GUID del AppId de
; abajo con el sufijo que le pone Inno, y va en HKCU y no en HKLM porque
; el instalador es PrivilegesRequired=lowest. Comprobado leyendo el
; registro de una maquina con pastepad puesto: DisplayVersion=4.3.0 en
; HKCU\...\Uninstall\{DF56...}_is1.
;
; Se escribe aparte y no se saca del AppId porque el AppId lleva la llave
; doblada ({{) para escapar la constante, y esa forma no vale como ruta.
#define ClaveInstalado \
  "Software\Microsoft\Windows\CurrentVersion\Uninstall\{DF56167A-B0A2-4B5B-AF7F-91DF597A67C9}_is1"

[Setup]
; Fijo y para siempre: es lo que hace que reinstalar reemplace en vez de
; duplicar, y lo que identifica la entrada de Agregar o quitar programas.
; Si cambia, Windows se encuentra dos pastepad instalados.
AppId={{DF56167A-B0A2-4B5B-AF7F-91DF597A67C9}

AppName=pastepad
AppVersion={#Version}
AppVerName=pastepad {#Version}
VersionInfoVersion={#Version}
; El usuario de GitHub, que ya es publico, y no el nombre real. Este
; campo es el que Windows enseña como «Editor» en Agregar o quitar
; programas y en el aviso de SmartScreen, asi que se ve.
AppPublisher=Josemgu
AppPublisherURL=https://github.com/Josemgu/pastepad
AppSupportURL=https://github.com/Josemgu/pastepad/issues
AppUpdatesURL=https://github.com/Josemgu/pastepad/releases

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
; con el programa abierto falla por archivo bloqueado. Inno usa para eso
; el Restart Manager de Windows.
CloseApplications=yes

; Y lo vuelve a abrir despues. Esto estuvo en "no" hasta la 4.1.0 porque
; en "yes" no servia de nada: la condicion que pone la documentacion de
; Inno es que "the application needs to be using the Windows
; RegisterApplicationRestart API function", y pastepad no la llamaba.
; Medido con la propia API del Restart Manager: bRestartable=False.
;
; Desde la 4.2.0 la llama —Sistema/Cierre.cs—, asi que actualizar encima
; deja pastepad abierto y con su atajo puesto, en vez de instalado y
; cerrado. Eso importa mas en la actualizacion silenciosa, que es la que
; nadie esta mirando cuando ocurre.
RestartApplications=yes

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
; Si lo cerramos nosotros para poder actualizar, se vuelve a abrir sin
; preguntar y tambien en instalacion silenciosa: el usuario no lo cerro,
; y lo que pidio fue actualizar. Esto sustituye a RestartApplications
; para este caso, porque al haberlo cerrado antes el Restart Manager ya
; no lo ve en uso y no tiene a quien reabrir.
Filename: "{app}\pastepad.exe"; Flags: nowait; Check: HayQueReabrir

; Y el de siempre —con su casilla— solo cuando NO lo estabamos usando:
; ofrecer "abrir pastepad" a quien lo tenia abierto hace diez segundos
; no tiene sentido.
Filename: "{app}\pastepad.exe"; Description: "Abrir pastepad"; \
    Flags: nowait postinstall skipifsilent; Check: NoSeCerroSolo

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

; ---------------------------------------------------------------------
; Reconocer lo que ya hay puesto antes de instalar nada
; ---------------------------------------------------------------------
;
; Inno ya reemplazaba la instalacion anterior —para eso esta el AppId
; fijo—, pero no lo decia: el instalador de la 4.4.0 sobre una 4.3.0 se
; veia exactamente igual que una instalacion nueva. Quien lo ejecuta no
; sabe si esta actualizando o duplicando, ni si sus textos guardados
; sobreviven, y la respuesta a lo segundo —que si— es justo la que hay
; que dar antes y no despues.
;
; **[Code] va la ultima de todas, y eso no es estetica.** A partir de
; aqui el archivo es Pascal, donde ";" no abre un comentario sino que
; cierra una sentencia. Un bloque de comentarios de los de arriba
; colocado detras de esta linea se compila como codigo: "'BEGIN'
; expected. Compile aborted." Paso exactamente eso.
[Code]

const
  { Renglon en blanco dentro del aviso. #13#10 y no #13 a secas: el
    cuadro de mensaje es de Windows. }
  Salto = #13#10#13#10;

  { La ventana de escucha de cierre de pastepad. Es de nivel superior a
    proposito —una ventana solo-mensajes «cannot be enumerated»— y por
    eso se la puede encontrar desde aqui. Al recibir WM_CLOSE vuelca los
    datos y termina, que es la misma puerta que usa Windows al apagar. }
  ClaseCierre = 'pastepad_cierre_3ff1c0de';
  WM_CLOSE = $0010;

var
  Instalada: string;
  EsActualizacion: Boolean;
  EstabaCorriendo: Boolean;

{ Compara dos versiones. No se usa el signo que devuelve
  ComparePackedVersion porque su documentacion dice que devuelve un
  Integer sin decir en que orden; los Int64 empaquetados si se pueden
  comparar directamente, que es para lo que se empaquetan.

  Devuelve False si alguna de las dos no se puede leer, y entonces quien
  llama tiene que conformarse con saber que hay algo instalado. }
function Comparar(const a, b: string; var salida: Integer): Boolean;
var
  va, vb: Int64;
begin
  Result := StrToVersion(a, va) and StrToVersion(b, vb);
  if not Result then exit;

  if va = vb then salida := 0
  else if va < vb then salida := -1
  else salida := 1;
end;

function InitializeSetup(): Boolean;
var
  cmp: Integer;
begin
  Result := True;
  EsActualizacion := False;

  if not RegQueryStringValue(HKEY_CURRENT_USER, '{#ClaveInstalado}',
                             'DisplayVersion', Instalada) then
    exit;

  EsActualizacion := True;

  { En instalacion silenciosa no se pregunta nada. Un MsgBox aqui se
    queda esperando a alguien que no esta mirando, y el instalador nunca
    devuelve el control — que es un fallo que este proyecto ya se
    encontro por otro camino. }
  if WizardSilent() then exit;

  { Los literales se unen con + y no poniendolos uno detras de otro:
    Pascal lo admite, pero Pascal Script no es Delphi y no hace falta
    averiguarlo por las malas en la compilacion de una release. }
  if not Comparar(Instalada, '{#Version}', cmp) then
  begin
    Result := MsgBox(
      'Ya hay una versión de pastepad instalada.' + Salto +
      'Se reemplazará por la {#Version}. Tu historial y tus textos ' +
      'guardados no se tocan: viven en otra carpeta.' + Salto +
      '¿Continuar?', mbConfirmation, MB_YESNO) = IDYES;
    exit;
  end;

  if cmp = 0 then
  begin
    Result := MsgBox(
      'Ya tienes pastepad {#Version}, que es justo la de este ' +
      'instalador.' + Salto +
      'Puedes volver a instalarla encima si algo va mal. Tu historial y ' +
      'tus textos guardados no se tocan.' + Salto +
      '¿Reinstalar?', mbConfirmation, MB_YESNO) = IDYES;
    exit;
  end;

  if cmp < 0 then
  begin
    Result := MsgBox(
      'Tienes pastepad ' + Instalada + ' y este instalador trae la ' +
      '{#Version}.' + Salto +
      'Se va a ACTUALIZAR: se reemplaza el programa y se conserva todo ' +
      'lo tuyo —historial, textos guardados, atajo y preferencias—, que ' +
      'vive en otra carpeta. Si pastepad está abierto, se cierra y se ' +
      'vuelve a abrir solo.' + Salto +
      '¿Actualizar ahora?', mbConfirmation, MB_YESNO) = IDYES;
    exit;
  end;

  { Instalar una version mas vieja encima de una mas nueva no se prohibe
    —a veces es justo lo que se quiere cuando la nueva sale mal— pero no
    puede pasar sin decirlo: desde el instalador las dos se ven igual. }
  Result := MsgBox(
    'Tienes pastepad ' + Instalada + ', que es MÁS NUEVA que la ' +
    '{#Version} de este instalador.' + Salto +
    'Si continúas, te quedarás con una versión anterior.' + Salto +
    '¿Seguir de todas formas?', mbConfirmation, MB_YESNO) = IDYES;
end;

{ Y que lo diga tambien la pagina de confirmacion, no solo el aviso del
  principio: quien llegue hasta ahi tiene que seguir viendo que esto
  reemplaza y no duplica. }
procedure CurPageChanged(IdPagina: Integer);
begin
  if (IdPagina = wpReady) and EsActualizacion then
    WizardForm.NextButton.Caption := '&Actualizar';
end;

{ Cierra la copia que este corriendo, y espera a que se vaya de verdad.
  Devuelve True solo si estaba corriendo Y se cerro. }
function CerrarLaQueCorre(): Boolean;
var
  v: HWND;
  i: Integer;
  enviado: Boolean;
begin
  Result := False;

  v := FindWindowByClassName(ClaseCierre);
  if v = 0 then exit;

  { WM_CLOSE y no matar el proceso: asi vuelca lo que tenga sin guardar
    —el historial se escribe cada pocos segundos, no en cada copia—,
    suelta el atajo global y se quita de la bandeja. Matarlo se llevaria
    por delante hasta tres segundos de copias y dejaria el icono muerto
    junto al reloj hasta pasarle el raton por encima. }
  { El resultado se recoge en una variable en vez de descartarlo: Pascal
    Script no es Delphi y no hace falta averiguar por las malas si
    admite llamar a una funcion como si fuera un procedimiento. }
  enviado := PostMessage(v, WM_CLOSE, 0, 0);
  if not enviado then exit;

  { Hasta 10 s. Se espera a que la VENTANA desaparezca, que es la señal
    de que termino de verdad y solto los archivos; si se siguiera sin
    esperar, el instalador se encontraria el ejecutable en uso y
    volveria a salir la pagina que esto quiere evitar. }
  for i := 1 to 100 do
  begin
    Sleep(100);
    if FindWindowByClassName(ClaseCierre) = 0 then
    begin
      Result := True;
      exit;
    end;
  end;
end;

{ Aqui, y no en PrepareToInstall, porque esto tiene que pasar ANTES de
  que Setup mire que archivos estan en uso. Si para entonces pastepad ya
  se cerro, no hay nada en uso y la pagina «Preparandose para instalar»
  —esa que pregunta si puede cerrar las aplicaciones— no llega a salir.

  Que salga esa pagina no era solo feo: dejaba en manos del usuario algo
  que tiene una sola respuesta correcta, y si elegia «No cerrar las
  aplicaciones» los archivos se reemplazaban con la copia vieja aun
  viva. Instalar encima quedaba distinto que instalar limpio, que es lo
  que se noto usandolo. }
function NextButtonClick(IdPagina: Integer): Boolean;
begin
  Result := True;

  if IdPagina = wpReady then
    EstabaCorriendo := CerrarLaQueCorre();
end;

{ Para el [Run] de abajo: si lo cerramos nosotros, lo volvemos a abrir
  nosotros y sin preguntar. El usuario no lo cerro, y lo que pidio fue
  actualizar, no quedarse sin el programa. }
function HayQueReabrir(): Boolean;
begin
  Result := EstabaCorriendo;
end;

function NoSeCerroSolo(): Boolean;
begin
  Result := not EstabaCorriendo;
end;
