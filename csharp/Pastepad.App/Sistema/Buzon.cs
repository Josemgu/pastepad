using System.Runtime.InteropServices;

namespace Pastepad.App.Sistema;

/// <summary>
/// Ventana solo-mensajes: no se dibuja, no sale en Alt+Tab, no tiene
/// pixeles. Es el corazon de la aplicacion — por aqui entran el atajo
/// global, los avisos del portapapeles, el clic en la bandeja y la
/// llamada de una segunda instancia.
///
/// Vive tanto como el proceso, no como la ventana visible. Atar esto a
/// la ventana del panel, que se oculta a la bandeja, es el acoplamiento
/// que mato a la version en Python.
///
/// Comprobado el 12 ago 2026: la bomba de mensajes de WinUI 3 despacha
/// los mensajes de esta ventana siempre que se cree en su mismo hilo.
/// Ver csharp/PruebaAtajo/RESULTADOS.md.
/// </summary>
internal sealed class Buzon : IDisposable
{
    public const string Clase = "pastepad_buzon_3ff1c0de";

    const int ID_ATAJO = 1;
    const nuint MENU_ABRIR = 1;
    const nuint MENU_SALIR = 2;

    /// <summary>Se pulso el atajo global.</summary>
    public event Action? Atajo;

    /// <summary>Cambio el portapapeles.</summary>
    public event Action? Portapapeles;

    /// <summary>Otra instancia, o la bandeja, piden ver el panel.</summary>
    public event Action? Mostrarse;

    /// <summary>Se pidio cerrar desde el menu de la bandeja.</summary>
    public event Action? Salir;

    public nint Handle { get; private set; }
    public bool AtajoPuesto { get; private set; }
    public string? Problema { get; private set; }

    // El delegado se guarda en un campo a proposito: si solo viviera en
    // la llamada a RegisterClass, el recolector podria llevarselo y
    // Windows saltaria a memoria liberada.
    readonly Procedimiento _procedimiento;

    bool _desechado;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate nint Procedimiento(nint hwnd, uint mensaje, nint wParam, nint lParam);

    /// <summary>
    /// Tiene que construirse en el hilo de interfaz: RegisterHotKey
    /// falla si el HWND lo creo otro hilo.
    /// </summary>
    public Buzon()
    {
        _procedimiento = Despachar;

        var instancia = Nativo.GetModuleHandleW(null);

        var clase = new Nativo.WNDCLASSW
        {
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_procedimiento),
            hInstance = instancia,
            lpszClassName = Clase,
        };

        if (Nativo.RegisterClassW(ref clase) == 0)
        {
            int error = Marshal.GetLastWin32Error();
            if (error != Nativo.ERROR_CLASS_ALREADY_EXISTS)
            {
                Problema = $"RegisterClassW fallo con error {error}";
                Registro.Anotar(Problema);
                return;
            }
        }

        Handle = Nativo.CreateWindowExW(
            0, Clase, Clase, 0, 0, 0, 0, 0,
            Nativo.HWND_MESSAGE, 0, instancia, 0);

        if (Handle == 0)
        {
            Problema = "CreateWindowExW fallo con error " +
                       Marshal.GetLastWin32Error();
            Registro.Anotar(Problema);
            return;
        }

        Registro.Anotar($"buzon creado, hwnd 0x{Handle:X}");

        if (!Nativo.AddClipboardFormatListener(Handle))
        {
            Problema = "AddClipboardFormatListener fallo con error " +
                       Marshal.GetLastWin32Error();
            Registro.Anotar(Problema);
        }
    }

    /// <summary>
    /// Registra o cambia el atajo. False si Windows no lo acepta, que
    /// casi siempre significa que otra aplicacion ya lo tiene.
    /// </summary>
    public bool PonerAtajo(string combinacion)
    {
        if (Handle == 0) return false;

        var partes = Combinacion.Descomponer(combinacion);

        if (partes is null)
        {
            Problema = $"la combinacion '{combinacion}' no vale";
            Registro.Anotar(Problema);
            return false;
        }

        if (AtajoPuesto)
        {
            Nativo.UnregisterHotKey(Handle, ID_ATAJO);
            AtajoPuesto = false;
        }

        var (mods, vk) = partes.Value;

        // MOD_NOREPEAT: sin esto, mantener la combinacion pulsada la
        // dispara en bucle y el panel parpadea. Medido: una pulsacion
        // mantenida son 31 repeticiones del teclado y un solo aviso.
        AtajoPuesto = Nativo.RegisterHotKey(
            Handle, ID_ATAJO, mods | Nativo.MOD_NOREPEAT, vk);

        if (!AtajoPuesto)
        {
            Problema = $"Windows rechazo {combinacion} (error " +
                       $"{Marshal.GetLastWin32Error()}). " +
                       "Probablemente otra aplicacion ya lo usa.";
            Registro.Anotar(Problema);
        }
        else
        {
            Problema = null;
            Registro.Anotar($"atajo {combinacion} registrado");
        }

        return AtajoPuesto;
    }

    /// <summary>
    /// Le pide a la instancia que ya estaba abierta que saque el panel.
    /// Se la busca por el nombre de clase de su buzon.
    /// </summary>
    public static bool PedirQueSeMuestre()
    {
        nint hwnd = Nativo.FindWindowExW(Nativo.HWND_MESSAGE, 0, Clase, null);
        if (hwnd == 0) return false;

        return Nativo.PostMessageW(hwnd, Nativo.WM_MOSTRARSE, 0, 0);
    }

    /// <summary>
    /// El procedimiento de ventana. Ninguna excepcion puede escapar de
    /// aqui: cruzaria de vuelta a codigo nativo, que es comportamiento
    /// indefinido y se manifiesta como un cierre sin traza.
    /// </summary>
    nint Despachar(nint hwnd, uint mensaje, nint wParam, nint lParam)
    {
        try
        {
            switch (mensaje)
            {
                case Nativo.WM_HOTKEY when (int)wParam == ID_ATAJO:
                    Atajo?.Invoke();
                    return 0;

                case Nativo.WM_CLIPBOARDUPDATE:
                    Portapapeles?.Invoke();
                    return 0;

                case Nativo.WM_MOSTRARSE:
                    Mostrarse?.Invoke();
                    return 0;

                case Nativo.WM_BANDEJA:
                    AtenderBandeja((uint)lParam);
                    return 0;

                case Nativo.WM_COMMAND:
                    AtenderMenu((nuint)(wParam & 0xFFFF));
                    return 0;
            }
        }
        catch (Exception e)
        {
            Registro.Fallo("Buzon.Despachar", e);
        }

        return Nativo.DefWindowProcW(hwnd, mensaje, wParam, lParam);
    }

    void AtenderBandeja(uint evento)
    {
        switch (evento)
        {
            case Nativo.WM_LBUTTONUP:
                Mostrarse?.Invoke();
                break;

            case Nativo.WM_RBUTTONUP:
                AbrirMenu();
                break;
        }
    }

    /// <summary>
    /// El menu del clic derecho. Windows exige el SetForegroundWindow
    /// previo: sin el, el menu se queda abierto al pinchar fuera.
    /// </summary>
    void AbrirMenu()
    {
        nint menu = Nativo.CreatePopupMenu();
        if (menu == 0) return;

        try
        {
            Nativo.AppendMenuW(menu, Nativo.MF_STRING, MENU_ABRIR, "Abrir pastepad");
            Nativo.AppendMenuW(menu, Nativo.MF_SEPARATOR, 0, null);
            Nativo.AppendMenuW(menu, Nativo.MF_STRING, MENU_SALIR, "Salir");

            Nativo.GetCursorPos(out var punto);
            Nativo.SetForegroundWindow(Handle);

            int elegido = Nativo.TrackPopupMenuEx(
                menu,
                Nativo.TPM_RIGHTBUTTON | Nativo.TPM_RETURNCMD,
                punto.x, punto.y, Handle, 0);

            if (elegido > 0) AtenderMenu((nuint)elegido);
        }
        finally
        {
            Nativo.DestroyMenu(menu);
        }
    }

    void AtenderMenu(nuint id)
    {
        switch (id)
        {
            case MENU_ABRIR: Mostrarse?.Invoke(); break;
            case MENU_SALIR: Salir?.Invoke(); break;
        }
    }

    public void Dispose()
    {
        if (_desechado) return;
        _desechado = true;

        if (Handle == 0) return;

        if (AtajoPuesto) Nativo.UnregisterHotKey(Handle, ID_ATAJO);
        Nativo.RemoveClipboardFormatListener(Handle);
        Nativo.DestroyWindow(Handle);

        Handle = 0;
        Registro.Anotar("buzon cerrado");
    }
}
