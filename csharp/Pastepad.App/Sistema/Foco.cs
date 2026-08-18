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

        if (Nativo.IsIconic(hwnd))
            Nativo.ShowWindow(hwnd, Nativo.SW_RESTORE);

        // Primero por las buenas. Ver PorLasBuenas: el enganche es lo
        // que puede costar segundos, y muchas veces no hace falta.
        if (PorLasBuenas(hwnd)) return true;

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

        if (PorLasBuenas(propia)) return true;

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
    /// Poner una ventana delante **sin engancharse al hilo de nadie**.
    ///
    /// Es el camino rapido, y sobre todo el unico que no puede quedarse
    /// esperando. El enganche que usan los dos metodos de arriba hace que
    /// «keyboard and mouse events received by both threads are processed
    /// in the order they were received», o sea que nos ponemos en la cola
    /// del programa que este delante: si ese esta ocupado, esperamos.
    /// Medido con una ventana con el hilo bloqueado a proposito,
    /// <c>SetForegroundWindow</c> tardo **5.399 ms** y encima devolvio
    /// false. Y en la maquina de trabajo del usuario —saturada de agentes
    /// de seguridad— eso deja de ser un caso de laboratorio.
    ///
    /// Sin enganche puede funcionar igual: entre las condiciones que
    /// Windows acepta para ceder el primer plano esta que «the process
    /// received the last input event», y a pastepad lo acaban de
    /// despertar con un atajo global. Cuando cuela, nos ahorramos el
    /// enganche entero; cuando no, se sigue por el camino de siempre y no
    /// se pierde nada.
    ///
    /// Se comprueba el resultado leyendo quien manda de verdad y no
    /// fiandose del valor devuelto: la llamada puede decir true y no
    /// haber cambiado nada.
    /// </summary>
    static bool PorLasBuenas(nint hwnd)
    {
        Nativo.SetForegroundWindow(hwnd);

        if (Nativo.GetForegroundWindow() != hwnd) return false;

        Nativo.SetFocus(hwnd);
        return true;
    }

    /// <summary>
    /// Espera a que esa ventana este de verdad delante, y devuelve
    /// cuanto tardo. Sustituye a una espera fija de 60 ms escrita a mano.
    ///
    /// Los 60 ms eran a ciegas: en una maquina desahogada sobraban —se
    /// pagaban enteros aunque el foco ya estuviera puesto en 5— y en una
    /// cargada podian quedarse cortos, y entonces el Ctrl+V salia antes
    /// de que el destino pudiera recibirlo. Esto es mas rapido en el caso
    /// normal y mas fiable en el malo.
    /// </summary>
    public static int EsperarAlFrente(nint hwnd, int topeMs = 400)
    {
        var reloj = System.Diagnostics.Stopwatch.StartNew();

        while (reloj.ElapsedMilliseconds < topeMs)
        {
            if (Nativo.GetForegroundWindow() == hwnd)
                return (int)reloj.ElapsedMilliseconds;

            Thread.Sleep(5);
        }

        Registro.Anotar(
            $"la ventana 0x{hwnd:X} no llego a estar delante en {topeMs} ms; "
            + "se pega igual");

        return -1;
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
