using System.Diagnostics;
using System.Text;
using Microsoft.Win32;

namespace Pastepad.App.Sistema;

/// <summary>
/// Arrancar con Windows, por dos caminos a la vez.
///
/// El primero es una entrada de registro en HKCU, que sigue funcionando
/// en una aplicacion desempaquetada: la documentacion de despliegue sin
/// identidad de paquete lo dice expresamente.
///
/// El segundo es una tarea programada al iniciar sesion, y existe
/// porque el primero fallo sin dejar rastro. Comprobado sobre la maquina
/// del usuario tras un arranque de Windows: la entrada del registro
/// estaba puesta, apuntaba a un ejecutable que existe, no estaba
/// desactivada en el Administrador de tareas, y Windows si proceso esa
/// clave en ese arranque —OneDrive, que es otra entrada de la misma
/// clave, se lanzo 15 segundos despues de explorer—. pastepad no dejo ni
/// una linea, ni en la carpeta de datos ni en el log de reserva de al
/// lado del ejecutable, y no hubo informe de fallo. O el proceso no
/// llego a crearse, o murio antes de su primera instruccion, que es
/// donde el host de .NET falla en silencio.
///
/// No se sustituye una cosa por la otra: se ponen las dos. Si las dos
/// funcionan, la segunda instancia cede a la primera y se va —eso ya
/// estaba resuelto y deja su linea en el log—. Si falla el registro,
/// arranca la tarea. La tarea espera medio minuto a proposito, para que
/// en el caso normal gane siempre el registro y el log diga cual de los
/// dos nos trajo.
/// </summary>
internal static class Arranque
{
    const string Clave = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string Nombre = "pastepad";

    /// <summary>
    /// Como se llama la tarea. Sin barras: se crea en la raiz, que es
    /// donde un usuario sin privilegios puede crearla.
    /// </summary>
    const string Tarea = "pastepad";

    /// <summary>
    /// La ruta desde donde se lanzo. Si se mueve la carpeta hay que
    /// abrirlo una vez desde el sitio nuevo.
    /// </summary>
    static string RutaPropia() =>
        Environment.ProcessPath ?? AppContext.BaseDirectory;

    public static bool Activo()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Clave);
            return k?.GetValue(Nombre) is not null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                    or System.Security.SecurityException)
        {
            Registro.Fallo("leer el autoarranque", e);
            return false;
        }
    }

    /// <summary>
    /// Lo que hay hoy en la clave, para poder decir que se cambio y de
    /// que a que. Devuelve el valor tal cual esta guardado —con las
    /// comillas incluidas—, no la ruta limpia: lo que interesa anotar es
    /// exactamente lo que habia.
    /// </summary>
    public static string? ValorActual()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Clave);
            return k?.GetValue(Nombre) as string;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                    or System.Security.SecurityException)
        {
            Registro.Fallo("leer el valor del autoarranque", e);
            return null;
        }
    }

    public static void Poner(bool activar)
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Clave, writable: true);

            if (k is null)
            {
                Registro.Anotar($"no se pudo abrir HKCU\\{Clave}");
                return;
            }

            if (activar)
                k.SetValue(Nombre, $"\"{RutaPropia()}\"", RegistryValueKind.String);
            else if (k.GetValue(Nombre) is not null)
                k.DeleteValue(Nombre);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                    or System.Security.SecurityException)
        {
            Registro.Fallo("escribir el autoarranque", e);
        }
    }

    // ------------------------------------------------ la tarea programada

    /// <summary>
    /// Deja la tarea como tiene que estar, y devuelve si hizo falta
    /// tocarla.
    ///
    /// **No se llama desde el hilo de interfaz.** Lanza schtasks una o
    /// dos veces, y el requisito numero dos del proyecto es que abrir el
    /// panel sea instantaneo: el arranque esta medido en ~420 ms y no se
    /// gasta ni uno en esto. Se comprueba en cada arranque en vez de
    /// apuntar en config.json que ya se hizo, porque una tarea que el
    /// usuario borre a mano tiene que volver sola; en un hilo de fondo,
    /// preguntar no cuesta nada.
    /// </summary>
    public static void AsegurarTarea(bool activar)
    {
        try
        {
            bool existe = TareaPuesta();

            if (!activar)
            {
                if (!existe) return;

                Correr("/Delete", "/TN", Tarea, "/F");
                Registro.Anotar("tarea de inicio de sesion: quitada");
                return;
            }

            // Se vuelve a escribir exista o no, por lo mismo que la
            // entrada del registro: es la forma de corregir una ruta
            // vieja despues de reinstalar en otra carpeta.
            string xml = Path.Combine(Path.GetTempPath(), "pastepad-tarea.xml");

            // UTF-16, que es lo que schtasks lee. En UTF-8 responde que
            // el XML no es valido sin decir por que.
            File.WriteAllText(xml, DefinicionXml(), Encoding.Unicode);

            try
            {
                var (codigo, salida) = Correr(
                    "/Create", "/TN", Tarea, "/XML", xml, "/F");

                Registro.Anotar(codigo == 0
                    ? $"tarea de inicio de sesion: {(existe ? "actualizada" : "creada")} "
                      + $"para {RutaPropia()}"
                    : $"tarea de inicio de sesion: schtasks devolvio {codigo}: {salida}");
            }
            finally
            {
                // Lleva dentro el nombre de usuario. No se deja tirado en
                // la carpeta temporal.
                try { File.Delete(xml); }
                catch (Exception e) when (e is IOException
                                            or UnauthorizedAccessException)
                {
                    Registro.Fallo("borrar el XML de la tarea", e);
                }
            }
        }
        catch (Exception e)
        {
            // Ancho a proposito: esto corre en un hilo de fondo, y una
            // excepcion que se escape de aqui se lleva el proceso entero
            // por delante. El autoarranque por registro sigue puesto.
            Registro.Fallo("asegurar la tarea de inicio de sesion", e);
        }
    }

    static bool TareaPuesta() => Correr("/Query", "/TN", Tarea).Codigo == 0;

    /// <summary>
    /// La tarea, tal cual la quiere schtasks.
    ///
    /// Dos decisiones que no son de adorno:
    ///
    /// <c>InteractiveToken</c> con nivel <c>LeastPrivilege</c> es lo que
    /// permite registrarla sin ser administrador y sin pedir contraseña
    /// —«you do not need to specify a password when registering the task
    /// if you register the task to run under the security context of your
    /// account and you use the S4U or interactive logon type»—. Con
    /// <c>Password</c> o <c>S4U</c> haria falta el privilegio de inicio
    /// de sesion como proceso por lotes, que un usuario normal no tiene.
    ///
    /// Y sin ninguna de las condiciones de fabrica: la tarea trae de
    /// serie «no empezar si va con bateria» y «parar si pasa a bateria»,
    /// que en un portatil desenchufado es exactamente el arranque que no
    /// ocurre.
    /// </summary>
    static string DefinicionXml()
    {
        string usuario = System.Security.Principal.WindowsIdentity
            .GetCurrent().Name;

        return $"""
        <?xml version="1.0" encoding="UTF-16"?>
        <Task version="1.4"
              xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
          <RegistrationInfo>
            <Description>Abre pastepad al iniciar sesion. La pone el propio
        programa; quitar "Arrancar con Windows" en sus ajustes la borra.</Description>
            <URI>\{Tarea}</URI>
          </RegistrationInfo>
          <Triggers>
            <LogonTrigger>
              <Enabled>true</Enabled>
              <UserId>{Escapar(usuario)}</UserId>
              <Delay>PT30S</Delay>
            </LogonTrigger>
          </Triggers>
          <Principals>
            <Principal id="Author">
              <UserId>{Escapar(usuario)}</UserId>
              <LogonType>InteractiveToken</LogonType>
              <RunLevel>LeastPrivilege</RunLevel>
            </Principal>
          </Principals>
          <Settings>
            <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
            <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
            <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
            <AllowHardTerminate>false</AllowHardTerminate>
            <StartWhenAvailable>false</StartWhenAvailable>
            <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
            <IdleSettings>
              <StopOnIdleEnd>false</StopOnIdleEnd>
              <RestartOnIdle>false</RestartOnIdle>
            </IdleSettings>
            <AllowStartOnDemand>true</AllowStartOnDemand>
            <Enabled>true</Enabled>
            <Hidden>false</Hidden>
            <RunOnlyIfIdle>false</RunOnlyIfIdle>
            <DisallowStartOnRemoteAppSession>false</DisallowStartOnRemoteAppSession>
            <UseUnifiedSchedulingEngine>true</UseUnifiedSchedulingEngine>
            <WakeToRun>false</WakeToRun>
            <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
            <Priority>7</Priority>
          </Settings>
          <Actions Context="Author">
            <Exec>
              <Command>{Escapar(RutaPropia())}</Command>
            </Exec>
          </Actions>
        </Task>
        """;
    }

    /// <summary>
    /// Un nombre de usuario o una ruta pueden llevar &amp; o comillas
    /// —«Jose &amp; Co», una carpeta con un apostrofo—, y eso dentro del
    /// XML lo deja invalido. schtasks solo diria que no lo entiende.
    /// </summary>
    static string Escapar(string texto) => texto
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;")
        .Replace("'", "&apos;");

    /// <summary>
    /// schtasks, sin ventana y con lo que diga recogido. Los argumentos
    /// van por <c>ArgumentList</c> y no en una cadena: la ruta del
    /// ejecutable lleva espacios en cuanto el usuario se llama como este.
    /// </summary>
    static (int Codigo, string Salida) Correr(params string[] argumentos)
    {
        var arranque = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var a in argumentos) arranque.ArgumentList.Add(a);

        using var proceso = Process.Start(arranque);

        if (proceso is null) return (-1, "no se pudo lanzar schtasks");

        string salida = proceso.StandardOutput.ReadToEnd()
                      + proceso.StandardError.ReadToEnd();

        // Con tope: si schtasks se quedara colgado, este hilo se queda
        // con el, y con el proceso no se puede cerrar pastepad.
        if (!proceso.WaitForExit(15_000))
        {
            Registro.Anotar("schtasks no termino en 15 s; se le deja");
            return (-1, "sin respuesta");
        }

        return (proceso.ExitCode, salida.Trim().Replace("\r\n", " "));
    }
}
