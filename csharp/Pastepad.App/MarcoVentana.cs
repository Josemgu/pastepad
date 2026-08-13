using System.Runtime.InteropServices;

namespace Pastepad.App;

/// <summary>
/// El hueco entre lo que Windows llama "la ventana" y lo que se ve.
///
/// Una ventana redimensionable lleva alrededor una franja invisible por
/// la que se agarra para estirarla. <c>GetWindowRect</c> —y con el
/// <c>AppWindow.Size</c>— la cuentan; la pantalla no la enseña. Medido
/// en esta maquina al 100%: 7 px a cada lado y 7 abajo, 0 arriba.
///
/// Sin descontarla, un panel pedido de 380x560 se veia de 366x553, y
/// todas las medidas de la especificacion salian cortas.
/// </summary>
static class MarcoVentana
{
    /// <summary>DWMWA_EXTENDED_FRAME_BOUNDS: lo que la ventana pinta.</summary>
    const int BordesReales = 9;

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public int Izq, Arr, Der, Aba;
    }

    [DllImport("dwmapi.dll")]
    static extern int DwmGetWindowAttribute(
        nint ventana, int atributo, out RECT valor, int tamano);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetWindowRect(nint ventana, out RECT valor);

    /// <summary>
    /// Cuanto mide de mas el rectangulo de ventana respecto de lo que se
    /// ve. Se mide una vez con la ventana ya creada; devuelve (0,0) si
    /// DWM no contesta, que es el caso en el que no hay nada que
    /// descontar.
    /// </summary>
    public static (int Ancho, int Alto) Holgura(nint ventana)
    {
        try
        {
            if (!GetWindowRect(ventana, out var fuera)) return (0, 0);

            if (DwmGetWindowAttribute(
                    ventana, BordesReales, out var dentro,
                    Marshal.SizeOf<RECT>()) != 0)
            {
                return (0, 0);
            }

            int ancho = (fuera.Der - fuera.Izq) - (dentro.Der - dentro.Izq);
            int alto = (fuera.Aba - fuera.Arr) - (dentro.Aba - dentro.Arr);

            // Un valor absurdo significa que la medida no vale; mejor no
            // descontar nada que descolocar la ventana.
            if (ancho is < 0 or > 64 || alto is < 0 or > 64) return (0, 0);

            return (ancho, alto);
        }
        catch (Exception e)
        {
            Registro.Fallo("medir el marco de la ventana", e);
            return (0, 0);
        }
    }
}
