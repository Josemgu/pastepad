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

    public static string Ruta { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Nucleo.Config.App,
        "errores.log");

    /// <summary>
    /// Donde se escribe de verdad, que puede no ser <see cref="Ruta"/>.
    /// </summary>
    public static string RutaUsada { get; private set; } = Ruta;

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
            if (Intentar(Ruta, linea)) { RutaUsada = Ruta; return; }

            if (Intentar(_reserva, linea)) { RutaUsada = _reserva; return; }

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
        catch (Exception e) when (e is IOException
                                    or UnauthorizedAccessException
                                    or NotSupportedException)
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
            $"=== arranque === pid {Environment.ProcessId}, "
            + $"usuario {yo.Name}, "
            + $"elevado {new System.Security.Principal.WindowsPrincipal(yo)
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator)}, "
            + $"exe {Environment.ProcessPath}, "
            + $"localappdata {Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData)}";

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

    public static void Fallo(string donde, Exception e)
    {
        Anotar($"FALLO en {donde}: {e.GetType().Name}: {e.Message}");
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
