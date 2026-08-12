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

    [STAThread]
    static void Main(string[] args)
    {
        LeerArgumentos(args);

        Registro.EngancharGlobales();
        Registro.AnotarArranque();

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
                CarpetaDatos = args[i + 1];
                return;
            }
        }
    }

    /// <summary>
    /// La clave de instancia lleva la carpeta de datos dentro: si no,
    /// una instancia de pruebas cederia el control a la de verdad y no
    /// llegaria a arrancar.
    /// </summary>
    static string Clave() => CarpetaDatos is null
        ? CLAVE_INSTANCIA
        : CLAVE_INSTANCIA + "-" + CarpetaDatos.GetHashCode().ToString("X");

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
