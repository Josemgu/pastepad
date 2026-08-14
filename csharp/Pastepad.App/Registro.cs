using System.Text;

namespace Pastepad.App;

/// <summary>
/// errores.log: el unico que escribe. Ningun catch mudo — el fallo del
/// atajo en la version anterior tardo dias en diagnosticarse porque
/// cada hilo se tragaba su excepcion en silencio.
/// </summary>
internal static class Registro
{
    static readonly Lock _cerrojo = new();

    static string? _ruta;

    /// <summary>
    /// Donde deberia ir el registro: la carpeta de datos en uso, que con
    /// <c>--datos</c> no es la de siempre. Una instancia de pruebas que
    /// escribe su log en el historial de verdad del usuario no vale de
    /// nada y ademas ensucia lo que se estaba protegiendo.
    ///
    /// Se resuelve la primera vez que se pide y no en un inicializador
    /// estatico: los argumentos se leen en la primera linea de
    /// <c>Main</c>, y un campo estatico se habria fijado antes de eso.
    /// Por lo mismo, nadie debe anotar nada antes de leer los
    /// argumentos: la ruta se queda con lo primero que se resuelva.
    /// </summary>
    public static string Ruta =>
        _ruta ??= Path.Combine(Program.CarpetaDatos ?? PorDefecto(), "errores.log");

    static string PorDefecto() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Nucleo.Config.App);

    static string? _rutaUsada;

    /// <summary>
    /// Donde se escribe de verdad, que puede no ser <see cref="Ruta"/>.
    /// </summary>
    public static string RutaUsada => _rutaUsada ?? Ruta;

    /// <summary>
    /// El segundo intento, junto al ejecutable. Existe porque hubo un
    /// arranque que no dejo ni una linea: si el sitio de siempre no
    /// admite escritura, escribir en otro es mejor que no escribir, y
    /// desde luego mejor que un Debug.WriteLine que nadie ve.
    /// </summary>
    static readonly string _reserva =
        Path.Combine(AppContext.BaseDirectory, "errores.log");

    public static void Anotar(string texto)
    {
        var linea = string.Format(
            "{0:yyyy-MM-dd HH:mm:ss.fff}  [hilo {1,3}]  {2}{3}",
            DateTime.Now, Environment.CurrentManagedThreadId, texto,
            Environment.NewLine);

        lock (_cerrojo)
        {
            if (Intentar(Ruta, linea)) { _rutaUsada = Ruta; return; }

            if (Intentar(_reserva, linea)) { _rutaUsada = _reserva; return; }

            // Sin sitio donde escribir no queda a quien avisar. Al menos
            // el depurador lo recoge.
            System.Diagnostics.Debug.WriteLine("sin log: " + linea);
        }
    }

    static bool Intentar(string ruta, string linea)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
            File.AppendAllText(ruta, linea, Encoding.UTF8);
            return true;
        }
        // ArgumentException entra aqui a proposito: con --datos la ruta
        // la escribe una persona, y una ruta imposible tiene que acabar
        // en la reserva de abajo, no en una excepcion desde dentro del
        // que se supone que registra las excepciones.
        catch (Exception e) when (e is IOException
                                    or UnauthorizedAccessException
                                    or NotSupportedException
                                    or ArgumentException)
        {
            System.Diagnostics.Debug.WriteLine(
                $"no se pudo escribir en {ruta}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// La primera linea de todas, escrita en los DOS sitios a la vez y
    /// con el entorno delante.
    ///
    /// Existe por un fallo abierto: hay arranques que no dejan ni una
    /// linea en %LOCALAPPDATA%, mientras que otros del mismo binario y
    /// el mismo usuario si. Hasta saber por que, el arranque se anota
    /// tambien junto al ejecutable, que siempre es escribible.
    /// </summary>
    public static void AnotarArranque()
    {
        var yo = System.Security.Principal.WindowsIdentity.GetCurrent();

        var entorno =
            $"=== arranque === pastepad {Nucleo.Config.Version}, "
            + $"pid {Environment.ProcessId}, "
            + $"usuario {yo.Name}, "
            + $"elevado {new System.Security.Principal.WindowsPrincipal(yo)
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator)}, "
            + $"exe {Environment.ProcessPath}, "
            + $"localappdata {Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData)}, "

            // La variable de entorno y la carpeta que el almacen usa de
            // verdad, las dos delante. Si divergen —perfil temporal,
            // redireccion, un --datos que no es el que se creia— se ve de
            // un vistazo, y era lo que faltaba para entender por que unos
            // arranques dejaban lineas y otros del mismo binario no.
            + $"LOCALAPPDATA={Environment.GetEnvironmentVariable("LOCALAPPDATA")}, "
            + $"datos {Path.GetDirectoryName(Ruta)}, "

            // Cuanto llevaba Windows encendido cuando arrancamos. Es la
            // unica forma de ver un autoarranque que NO ocurrio: mientras
            // pastepad solo sepa anotar los arranques que si pasan, un
            // fallo del autoarranque no deja nada que leer, y eso es
            // exactamente lo que se encontro al investigarlo. Unos pocos
            // segundos aqui significan que nos abrio Windows; unas horas,
            // que el autoarranque fallo y lo abrio el usuario a mano.
            //
            // GetTickCount64 —que es de donde sale TickCount64— cuenta
            // «milliseconds that have elapsed since the system was
            // started», suspension incluida: la documentacion remite a
            // QueryUnbiasedInterruptTime para el tiempo en activo. Aqui
            // interesa el del reloj de pared, no el de trabajo.
            + $"windows encendido hace {DesdeElArranque()}";

        var linea = string.Format(
            "{0:yyyy-MM-dd HH:mm:ss.fff}  [hilo {1,3}]  {2}{3}",
            DateTime.Now, Environment.CurrentManagedThreadId, entorno,
            Environment.NewLine);

        lock (_cerrojo)
        {
            bool a = Intentar(Ruta, linea);
            bool b = Intentar(_reserva, linea);

            // Sin esta pista, quien diagnostique mirando el log de al
            // lado del ejecutable ve una lista de arranques y ninguna
            // señal de actividad, y concluye que el programa no hace
            // nada. Todo lo demas se escribe en el otro.
            if (a && b)
            {
                Intentar(_reserva,
                    $"    (el detalle de esta sesion va a {Ruta}){Environment.NewLine}");
            }

            if (!a && !b)
                System.Diagnostics.Debug.WriteLine("sin log: " + linea);

            if (!a && b)
            {
                Intentar(_reserva,
                    "    ATENCION: no se pudo escribir en " + Ruta
                    + "; esta sesion se registra solo aqui."
                    + Environment.NewLine);
            }
        }
    }

    /// <summary>
    /// Legible de un vistazo, que es para lo que se lee: «4 s» dice
    /// «nos abrio Windows» y «9 h 12 min» dice «lo abrio el usuario».
    /// </summary>
    static string DesdeElArranque()
    {
        var t = TimeSpan.FromMilliseconds(Environment.TickCount64);

        if (t.TotalMinutes < 1) return $"{t.TotalSeconds:F0} s";

        if (t.TotalHours < 1) return $"{t.Minutes} min {t.Seconds} s";

        return $"{(int)t.TotalHours} h {t.Minutes} min";
    }

    /// <summary>
    /// Con el HResult en hexadecimal. Es lo que separa una violacion de
    /// uso compartido (0x80070020, alguien tiene el archivo abierto) de
    /// una denegacion de acceso (0x80070005, no tenemos permiso): las dos
    /// llegan como IOException o como un mensaje parecido, y sin este
    /// numero el diagnostico se queda en "no se pudo leer el archivo".
    /// </summary>
    public static void Fallo(string donde, Exception e)
    {
        Anotar($"FALLO en {donde}: {e.GetType().Name} "
             + $"(0x{e.HResult:X8}): {e.Message}");

        Anotar(e.StackTrace ?? "(sin traza)");
    }

    /// <summary>
    /// Los tres manejadores globales. Uno solo no basta: cada uno cubre
    /// un camino distinto por el que una excepcion puede escaparse.
    /// </summary>
    public static void EngancharGlobales()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, a) =>
        {
            if (a.ExceptionObject is Exception e)
                Fallo("AppDomain.UnhandledException", e);
            else
                Anotar($"FALLO no tipado: {a.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (_, a) =>
        {
            Fallo("TaskScheduler.UnobservedTaskException", a.Exception);
            a.SetObserved();
        };
    }
}
