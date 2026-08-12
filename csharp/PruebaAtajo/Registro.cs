using System.Text;

namespace PruebaAtajo;

/// <summary>
/// El unico que escribe en disco. Todo lo que se captura se anota aqui:
/// el fallo del atajo en la version anterior tardo dias en
/// diagnosticarse porque cada hilo se tragaba su excepcion en silencio.
/// </summary>
internal static class Registro
{
    static readonly string _ruta = Path.Combine(
        AppContext.BaseDirectory, "prueba-atajo.log");

    static readonly Lock _cerrojo = new();

    /// <summary>Ruta del log, para poder enseñarla en la interfaz.</summary>
    public static string Ruta => _ruta;

    /// <summary>
    /// Una linea con marca de tiempo y el hilo que la escribe. El hilo
    /// importa: la pregunta que responde esta prueba es si los mensajes
    /// llegan al hilo de interfaz o a otro.
    /// </summary>
    public static void Anotar(string texto)
    {
        var linea = string.Format(
            "{0:yyyy-MM-dd HH:mm:ss.fff}  [hilo {1,3}]  {2}{3}",
            DateTime.Now,
            Environment.CurrentManagedThreadId,
            texto,
            Environment.NewLine);

        try
        {
            lock (_cerrojo)
            {
                File.AppendAllText(_ruta, linea, Encoding.UTF8);
            }
        }
        catch (IOException)
        {
            // Si no se puede escribir el log no queda a quien avisar.
            // Se deja constancia en el depurador y se sigue: perder una
            // linea del log no justifica tumbar la prueba.
            System.Diagnostics.Debug.WriteLine("no se pudo escribir el log");
        }
    }

    public static void Fallo(string donde, Exception e)
    {
        Anotar($"FALLO en {donde}: {e.GetType().Name}: {e.Message}");
        Anotar(e.StackTrace ?? "(sin traza)");
    }

    /// <summary>
    /// Engancha los dos manejadores globales. En .NET, como en Python,
    /// uno solo no basta: el de excepciones no observadas de tareas va
    /// por su cuenta.
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
