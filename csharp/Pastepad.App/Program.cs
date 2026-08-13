using System.Security.Cryptography;
using System.Text;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Pastepad.App.Sistema;

namespace Pastepad.App;

/// <summary>
/// Main propio, en lugar del que genera XAML. La instancia unica tiene
/// que decidirse antes de crear ninguna ventana: dos procesos
/// peleandose por el atajo global fue la causa de que el programa
/// pareciera roto durante dias — Windows solo se lo da a uno, y el que
/// pierde queda como una ventana muda.
/// </summary>
public static class Program
{
    const string CLAVE_INSTANCIA = "pastepad-3ff1c0de";

    /// <summary>
    /// Carpeta de datos alternativa, de `--datos &lt;ruta&gt;`. Existe para
    /// poder probar sin tocar el historial de verdad del usuario: en
    /// esta maquina la carpeta de siempre es la de la version instalada,
    /// y una sesion de pruebas ya se llevo por delante entradas suyas.
    /// </summary>
    internal static string? CarpetaDatos { get; private set; }

    /// <summary>
    /// Lo que fallo al resolver la ruta de <c>--datos</c>, si fallo. No
    /// se anota en el momento: registrar antes de saber la carpeta de
    /// datos clavaria el log en la de siempre, que es justo lo que
    /// <c>--datos</c> quiere evitar.
    /// </summary>
    static Exception? _rutaMala;

    [STAThread]
    static void Main(string[] args)
    {
        // Lo primero de todo: el registro y la instancia unica dependen
        // de la carpeta de datos.
        LeerArgumentos(args);

        Registro.EngancharGlobales();
        Registro.AnotarArranque();

        if (_rutaMala is not null)
            Registro.Fallo("--datos: la ruta no se pudo resolver", _rutaMala);

        if (CarpetaDatos is not null)
            Registro.Anotar($"datos en carpeta alternativa: {CarpetaDatos}");

        if (!SoyLaInstanciaBuena())
        {
            // Que el usuario vuelva a lanzar el programa se entiende como
            // "quiero verlo", no como "abre otro".
            bool avisado = Buzon.PedirQueSeMuestre();

            Registro.Anotar(avisado
                ? "ya habia otra instancia; se le pidio que se muestre"
                : "ya habia otra instancia, pero no respondio a la peticion");

            return;
        }

        WinRT.ComWrappersSupport.InitializeComWrappers();

        Application.Start(p =>
        {
            var cola = DispatcherQueue.GetForCurrentThread();
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherQueueSynchronizationContext(cola));
            _ = new App();
        });
    }

    static void LeerArgumentos(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("--datos", StringComparison.OrdinalIgnoreCase))
            {
                CarpetaDatos = Normalizar(args[i + 1]);
                return;
            }
        }
    }

    /// <summary>
    /// A ruta absoluta y sin barra final. Dos lanzamientos que escriben
    /// en la misma carpeta tienen que dar la misma clave de instancia, y
    /// <c>datos</c>, <c>.\datos\</c> y <c>C:\...\datos</c> son la misma
    /// carpeta escrita de tres maneras.
    /// </summary>
    static string Normalizar(string ruta)
    {
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(ruta));
        }
        catch (Exception e) when (e is ArgumentException
                                    or NotSupportedException
                                    or PathTooLongException)
        {
            _rutaMala = e;

            // Se sigue con lo que vino: el almacen dira lo que pase al
            // intentar escribir alli, y eso el usuario si lo ve.
            return ruta;
        }
    }

    /// <summary>
    /// La clave de instancia lleva la carpeta de datos dentro: si no,
    /// una instancia de pruebas cederia el control a la de verdad y no
    /// llegaria a arrancar.
    /// </summary>
    static string Clave() => CarpetaDatos is null
        ? CLAVE_INSTANCIA
        : CLAVE_INSTANCIA + "-" + Resumen(CarpetaDatos);

    /// <summary>
    /// Resumen estable de la ruta: 16 hexadecimales de SHA-256 sobre
    /// ella en minusculas, que en Windows es el mismo nombre.
    ///
    /// No es String.GetHashCode y no puede serlo: en .NET Core esta
    /// aleatorizado por proceso, asi que cada lanzamiento producia una
    /// clave distinta y con <c>--datos</c> la instancia unica no
    /// funcionaba — dos procesos peleandose por el atajo global, que es
    /// exactamente el fallo que hizo que el programa pareciera roto
    /// durante dias. Aqui no se le pide nada criptografico, solo que sea
    /// el mismo numero en todos los procesos y en todos los arranques.
    /// </summary>
    static string Resumen(string ruta) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(ruta.ToLowerInvariant())))[..16];

    static bool SoyLaInstanciaBuena()
    {
        try
        {
            var duena = AppInstance.FindOrRegisterForKey(Clave());
            if (duena.IsCurrent) return true;

            // Si la cesion no prospera, la que estaba registrada ya no
            // existe: arrancar es mejor que quedarse a medias.
            return !Redirigir(duena, AppInstance.GetCurrent().GetActivatedEventArgs());
        }
        catch (Exception e)
        {
            Registro.Fallo("instancia unica", e);

            // Sin este mecanismo es preferible arrancar que no arrancar:
            // el buzon de abajo sigue detectando al que ya estuviera.
            return !Buzon.PedirQueSeMuestre();
        }
    }

    /// <summary>
    /// Cede la activacion a la instancia que ya manda. Va en otro hilo
    /// porque la llamada necesita que este no este bloqueado, y aqui
    /// todavia no hay bomba de mensajes.
    /// </summary>
    static bool Redirigir(AppInstance duena, AppActivationArguments args)
    {
        using var esperando = new SemaphoreSlim(0, 1);
        bool cedido = false;

        _ = Task.Run(() =>
        {
            try
            {
                duena.RedirectActivationToAsync(args).AsTask().Wait();
                cedido = true;
            }
            catch (Exception e)
            {
                Registro.Fallo("RedirectActivationToAsync", e);
            }
            finally
            {
                esperando.Release();
            }
        });

        // Con espera acotada: si la instancia registrada murio de golpe
        // —matada con Stop-Process, por ejemplo— la cesion no responde
        // nunca, y sin este tope el programa se quedaba colgado sin
        // ventana y sin decir nada. Desde fuera parece que "no arranca".
        if (!esperando.Wait(TimeSpan.FromSeconds(3)))
        {
            Registro.Anotar(
                "la instancia registrada no respondio en 3 s; "
                + "se sigue como instancia principal");
            return false;
        }

        return cedido;
    }
}
