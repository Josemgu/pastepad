using System.Runtime.InteropServices;

namespace Pastepad.App.Sistema;

/// <summary>
/// Donde esta el raton y donde cabe el panel.
/// </summary>
internal static class Pantalla
{
    /// <summary>Donde esta el raton, en pixeles fisicos.</summary>
    public static (int X, int Y) Puntero()
    {
        if (Nativo.GetCursorPos(out var punto)) return (punto.x, punto.y);

        Registro.Anotar("GetCursorPos fallo con error " +
                        Marshal.GetLastWin32Error());

        return (0, 0);
    }

    /// <summary>
    /// El area del monitor donde esta el punto, sin la barra de tareas.
    /// </summary>
    public static Nativo.RECT AreaUtil(int x, int y)
    {
        nint monitor = Nativo.MonitorFromPoint(
            new Nativo.POINT { x = x, y = y }, Nativo.MONITOR_DEFAULTTONEAREST);

        var info = new Nativo.MONITORINFO
        {
            cbSize = (uint)Marshal.SizeOf<Nativo.MONITORINFO>(),
        };

        if (monitor != 0 && Nativo.GetMonitorInfoW(monitor, ref info))
            return info.rcWork;

        Registro.Anotar("GetMonitorInfo fallo; se usa 1920x1080");

        return new Nativo.RECT { left = 0, top = 0, right = 1920, bottom = 1080 };
    }

    /// <summary>
    /// Coloca un rectangulo de ese tamaño junto al puntero sin que se
    /// salga del monitor. Devuelve pixeles fisicos, que es lo que quiere
    /// AppWindow.MoveAndResize.
    /// </summary>
    public static (int X, int Y) JuntoAlPuntero(int ancho, int alto)
    {
        var (raton, y) = Puntero();
        var area = AreaUtil(raton, y);

        int x = raton + 12;
        int destinoY = y + 12;

        if (x + ancho > area.right) x = raton - ancho - 12;
        if (destinoY + alto > area.bottom) destinoY = y - alto - 12;

        // Si tampoco cabe del otro lado, se pega al borde antes que
        // quedarse medio fuera de la pantalla.
        x = Math.Clamp(x, area.left, Math.Max(area.left, area.right - ancho));
        destinoY = Math.Clamp(destinoY, area.top, Math.Max(area.top, area.bottom - alto));

        return (x, destinoY);
    }
}
