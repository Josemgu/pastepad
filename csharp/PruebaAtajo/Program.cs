using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace PruebaAtajo;

/// <summary>
/// Main propio, en lugar del que genera XAML. Hace falta porque la
/// decision de instancia unica tiene que tomarse antes de crear
/// ninguna ventana: dos procesos peleandose por el atajo global fue la
/// causa de que la version anterior pareciera rota durante dias.
/// </summary>
public static class Program
{
    const string CLAVE_INSTANCIA = "pastepad-prueba-atajo";

    [STAThread]
    static void Main(string[] args)
    {
        Registro.EngancharGlobales();
        Registro.Anotar("=== arranque ===");

        if (!SoyLaInstanciaBuena())
        {
            Registro.Anotar("ya habia otra instancia; esta se retira");
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

    /// <summary>
    /// Devuelve false si ya hay otra instancia, tras cederle la
    /// activacion.
    /// </summary>
    static bool SoyLaInstanciaBuena()
    {
        try
        {
            var duena = AppInstance.FindOrRegisterForKey(CLAVE_INSTANCIA);

            if (duena.IsCurrent) return true;

            var argumentos = AppInstance.GetCurrent()
                                        .GetActivatedEventArgs();
            Redirigir(duena, argumentos);
            return false;
        }
        catch (Exception e)
        {
            // Esto es lo secundario de la prueba; lo que se mide es el
            // atajo. Si AppInstance no funciona desempaquetado se anota
            // bien visible y se sigue, en vez de dejar la prueba sin
            // respuesta. El mutex con nombre es el plan B.
            Registro.Fallo("instancia unica (se continua igualmente)", e);
            return true;
        }
    }

    /// <summary>
    /// Cede la activacion a la instancia que ya manda. Va en otro hilo
    /// porque la llamada necesita que este no este bloqueado, y aqui
    /// todavia no hay bomba de mensajes.
    /// </summary>
    static void Redirigir(AppInstance duena, AppActivationArguments args)
    {
        using var esperando = new SemaphoreSlim(0, 1);

        _ = Task.Run(() =>
        {
            try
            {
                duena.RedirectActivationToAsync(args).AsTask().Wait();
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

        esperando.Wait();
    }
}
