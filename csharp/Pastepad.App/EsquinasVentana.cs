using System.Runtime.InteropServices;

namespace Pastepad.App;

/// <summary>
/// El redondeo de las esquinas de la ventana.
///
/// Lo dibuja Windows, no nosotros. Se le pide el radio grande
/// explicitamente en vez de dejarlo en "que decida el sistema": asi el
/// valor esta escrito y no depende de que el criterio por defecto no
/// cambie.
///
/// **No hay radio a medida.** La enumeracion oficial solo tiene cuatro
/// valores —DEFAULT, DONOTROUND, ROUND y ROUNDSMALL— y ninguno acepta
/// un numero de pixeles. Fuente: DWM_WINDOW_CORNER_PREFERENCE en
/// learn.microsoft.com. ROUND es el mayor que se puede pedir.
///
/// Va en su propio archivo y no junto a <see cref="MarcoVentana"/>
/// porque aquel esta pendiente de sustituirse por API de AppWindow y
/// esto no tiene nada que ver con aquello.
/// </summary>
static class EsquinasVentana
{
    /// <summary>DWMWA_WINDOW_CORNER_PREFERENCE.</summary>
    const int Preferencia = 33;

    /// <summary>DWMWCP_ROUND: el radio grande, el mayor que se ofrece.</summary>
    const int Redondas = 2;

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(
        nint ventana, int atributo, ref int valor, int tamano);

    /// <summary>
    /// Pide el redondeo grande. En Windows 10 la llamada falla y no
    /// pasa nada: alli las ventanas no se redondean y el panel sale con
    /// esquinas rectas, que es lo que hace el resto del sistema.
    /// </summary>
    public static void Redondear(nint ventana)
    {
        try
        {
            int valor = Redondas;
            DwmSetWindowAttribute(ventana, Preferencia, ref valor, sizeof(int));
        }
        catch (Exception e)
        {
            Registro.Fallo("redondear las esquinas de la ventana", e);
        }
    }
}
