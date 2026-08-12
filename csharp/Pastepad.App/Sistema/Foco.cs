namespace Pastepad.App.Sistema;

/// <summary>
/// Devolver el foco a donde estaba el cursor y pegar alli. Es la pieza
/// que hace util a pastepad: sin ella, el Ctrl+V se va al vacio.
/// </summary>
internal static class Foco
{
    static readonly uint _miPid = (uint)Environment.ProcessId;

    /// <summary>
    /// Handle de la ventana con el foco, salvo que sea nuestra. Hay que
    /// llamarlo en el instante del atajo, antes de mostrar el panel: en
    /// cuanto el panel aparece, esta informacion ya se perdio.
    /// </summary>
    public static nint VentanaActiva()
    {
        nint hwnd = Nativo.GetForegroundWindow();
        if (hwnd == 0) return 0;

        Nativo.GetWindowThreadProcessId(hwnd, out uint pid);
        return pid == _miPid ? 0 : hwnd;
    }

    /// <summary>
    /// Vuelve a poner esa ventana al frente, con su cursor donde estaba.
    ///
    /// Windows no deja que cualquier proceso robe el primer plano. El
    /// truco es engancharse al hilo de esa ventana un instante: mientras
    /// dura el enganche, SetForegroundWindow si funciona.
    /// </summary>
    public static bool Devolver(nint hwnd)
    {
        if (hwnd == 0 || !Nativo.IsWindow(hwnd)) return false;

        uint hiloDestino = Nativo.GetWindowThreadProcessId(hwnd, out _);
        uint hiloPropio = Nativo.GetCurrentThreadId();

        bool enganchado = false;

        if (hiloDestino != 0 && hiloDestino != hiloPropio)
            enganchado = Nativo.AttachThreadInput(hiloPropio, hiloDestino, true);

        try
        {
            if (Nativo.IsIconic(hwnd))
                Nativo.ShowWindow(hwnd, Nativo.SW_RESTORE);

            if (!Nativo.SetForegroundWindow(hwnd))
            {
                Registro.Anotar($"SetForegroundWindow rechazado para 0x{hwnd:X}");
                return false;
            }

            Nativo.SetFocus(hwnd);
            return true;
        }
        finally
        {
            if (enganchado)
                Nativo.AttachThreadInput(hiloPropio, hiloDestino, false);
        }
    }

    /// <summary>
    /// Trae al frente una ventana nuestra. Windows aplica la misma
    /// restriccion que con las ajenas: un proceso en segundo plano no
    /// puede ponerse delante por las buenas, y sin esto el panel sale
    /// visible pero sin foco.
    ///
    /// El enganche se hace al hilo de quien manda ahora, que es quien
    /// tiene el permiso que a nosotros nos falta.
    /// </summary>
    public static bool TraerAlFrente(nint propia)
    {
        if (propia == 0) return false;

        nint delante = Nativo.GetForegroundWindow();

        if (delante == propia) return true;

        uint hiloDelante = delante != 0
            ? Nativo.GetWindowThreadProcessId(delante, out _)
            : 0;

        uint hiloPropio = Nativo.GetCurrentThreadId();

        bool enganchado = false;

        if (hiloDelante != 0 && hiloDelante != hiloPropio)
            enganchado = Nativo.AttachThreadInput(hiloPropio, hiloDelante, true);

        try
        {
            Nativo.SetForegroundWindow(propia);
            Nativo.SetFocus(propia);

            return Nativo.GetForegroundWindow() == propia;
        }
        finally
        {
            if (enganchado)
                Nativo.AttachThreadInput(hiloPropio, hiloDelante, false);
        }
    }

    /// <summary>
    /// Manda Ctrl+V con la API de Windows.
    /// </summary>
    public static void PegarConTeclado()
    {
        // El atajo lleva Shift o Alt y pueden seguir pulsadas: si no se
        // sueltan primero, el destino recibe Ctrl+Shift+V en vez de
        // Ctrl+V, que en muchas aplicaciones es "pegar sin formato" y en
        // otras no es nada.
        Nativo.keybd_event(Nativo.VK_SHIFT, 0, Nativo.KEYEVENTF_KEYUP, 0);
        Nativo.keybd_event(Nativo.VK_MENU, 0, Nativo.KEYEVENTF_KEYUP, 0);

        Thread.Sleep(20);

        Nativo.keybd_event(Nativo.VK_CONTROL, 0, 0, 0);
        Nativo.keybd_event(Nativo.VK_V, 0, 0, 0);

        Thread.Sleep(30);

        Nativo.keybd_event(Nativo.VK_V, 0, Nativo.KEYEVENTF_KEYUP, 0);
        Nativo.keybd_event(Nativo.VK_CONTROL, 0, Nativo.KEYEVENTF_KEYUP, 0);
    }
}
