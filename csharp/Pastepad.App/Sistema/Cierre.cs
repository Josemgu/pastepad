using System.Runtime.InteropServices;
using Pastepad.Nucleo;

namespace Pastepad.App.Sistema;

/// <summary>
/// La ventana que escucha cuando nos van a cerrar sin pedirnoslo: un
/// instalador que se actualiza encima, o Windows apagandose.
///
/// Es una ventana aparte del buzon y no es un capricho. El buzon es una
/// ventana solo-mensajes, y de esas dice la documentacion de Windows que
/// "is not visible, has no z-order, cannot be enumerated, and does not
/// receive broadcast messages". Lo que no se puede enumerar no recibe
/// WM_QUERYENDSESSION: el buzon es sordo a esto por construccion, y no
/// se toca porque es lo que hace que el atajo global funcione.
///
/// Tampoco se subclasa la ventana de XAML —eso arrastra el
/// ExecutionEngineException abierto en WinUI—. Asi que esta: una ventana
/// emergente de nivel superior, sin marco, sin WS_VISIBLE y con
/// WS_EX_TOOLWINDOW. Nunca se dibuja ni sale en Alt+Tab. Solo escucha.
///
/// Medido antes de escribirla, con la misma API que usa Inno Setup: el
/// Restart Manager veia a pastepad como RmOtherWindow con el panel en la
/// bandeja y como RmMainWindow con el panel abierto, y bRestartable en
/// False en los dos casos. Es decir, que lo que le pasaba a los datos al
/// actualizar dependia de si el panel estaba abierto.
/// </summary>
internal sealed class Cierre : IDisposable
{
    const string Clase = "pastepad_cierre_3ff1c0de";

    /// <summary>
    /// Guarda ya, que puede no haber otra oportunidad. Puede llegar mas
    /// de una vez y tiene que aguantarlo.
    /// </summary>
    public event Action? Volcar;

    /// <summary>Se acabo: cerrar del todo.</summary>
    public event Action? Terminar;

    public nint Handle { get; private set; }

    // Mismo motivo que en Buzon: si el delegado solo viviera en la
    // llamada, el recolector podria llevarselo y Windows saltaria a
    // memoria liberada.
    readonly Procedimiento _procedimiento;

    bool _desechado;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate nint Procedimiento(nint hwnd, uint mensaje, nint wParam, nint lParam);

    /// <summary>
    /// Se construye en el hilo de interfaz, como el buzon: los mensajes
    /// los despacha la bomba de WinUI, y el volcado toca el almacen, que
    /// se toca desde ese hilo en todos los demas sitios.
    /// </summary>
    public Cierre()
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
                Registro.Anotar($"cierre: RegisterClassW fallo con error {error}");
                return;
            }
        }

        // Padre 0, no HWND_MESSAGE: tiene que ser de nivel superior para
        // que se la pueda enumerar. Esa es toda la diferencia con el
        // buzon y toda la razon de que exista.
        Handle = Nativo.CreateWindowExW(
            Nativo.WS_EX_TOOLWINDOW, Clase, Clase, Nativo.WS_POPUP,
            0, 0, 0, 0, 0, 0, instancia, 0);

        if (Handle == 0)
        {
            Registro.Anotar("cierre: CreateWindowExW fallo con error " +
                            Marshal.GetLastWin32Error());
            return;
        }

        Registro.Anotar($"escucha de cierre creada, hwnd 0x{Handle:X}");
    }

    /// <summary>
    /// Le da a Windows la linea con la que reabrirnos. Sin esto el
    /// Restart Manager cierra pastepad para actualizarlo y no lo vuelve
    /// a abrir: el usuario se queda sin atajo y sin capturar copias, y
    /// sin nada en pantalla que se lo diga.
    ///
    /// Se excluyen el cierre inesperado y el cuelgue a proposito. Que
    /// Windows reabra un programa que acaba de reventar es como se monta
    /// un bucle de arranques.
    /// </summary>
    public static void PedirQueNosReabran()
    {
        // Los argumentos hay que darlos: pasar null NO los conserva, los
        // borra —"if this parameter is NULL or an empty string, the
        // previously registered command line is removed"—. Medido: una
        // instancia lanzada con --datos volvio sin el y abrio el almacen
        // de siempre. En produccion pastepad no lleva argumentos, pero
        // una sesion de pruebas si, y ese es justo el caso en que
        // equivocarse de carpeta cuesta datos del usuario.
        //
        // El nombre del ejecutable se omite adrede: "Do not include the
        // name of the executable in the command line; this function adds
        // it for you".
        string? linea = Argumentos.Componer(
            Environment.GetCommandLineArgs().Skip(1));

        int hr = Nativo.RegisterApplicationRestart(
            linea, Nativo.RESTART_NO_CRASH | Nativo.RESTART_NO_HANG);

        // E_INVALIDARG aqui significa linea demasiado larga. No es fatal
        // —pastepad sigue funcionando—, pero deja de reabrirse solo, que
        // es de las cosas que fallan sin que nadie se entere.
        Registro.Anotar(hr == 0
            ? "registrados para que nos reabran tras actualizar" +
              (linea is null ? "" : $", con {linea}")
            : $"RegisterApplicationRestart fallo con HRESULT 0x{hr:X8}");
    }

    /// <summary>
    /// Ninguna excepcion puede escapar de aqui: cruzaria de vuelta a
    /// codigo nativo, que es comportamiento indefinido.
    /// </summary>
    nint Despachar(nint hwnd, uint mensaje, nint wParam, nint lParam)
    {
        try
        {
            switch (mensaje)
            {
                // La documentacion es explicita en que aqui NO se cierra
                // uno —"another application may not be ready to shut
                // down"— y en que hay que contestar TRUE para no cancelar
                // el apagado de todos. Pero volcar si: son unos 8 ms, la
                // sesion todavia puede cancelarse sin que pase nada, y es
                // el ultimo momento en que sabemos seguro que estamos
                // vivos. Si luego el cierre se fuerza, los datos ya estan.
                case Nativo.WM_QUERYENDSESSION:
                    Registro.Anotar(
                        (lParam & Nativo.ENDSESSION_CLOSEAPP) != 0
                            ? "nos cierran para actualizarnos; volcando"
                            : "Windows se apaga; volcando");
                    Volcar?.Invoke();
                    return 1;

                // wParam en 0 significa que alguien cancelo la sesion y
                // seguimos vivos. Cerrarse ahi seria cerrarse de mas.
                case Nativo.WM_ENDSESSION:
                    if (wParam == 0)
                    {
                        Registro.Anotar("el cierre se cancelo; seguimos");
                        return 0;
                    }

                    Volcar?.Invoke();
                    Terminar?.Invoke();
                    return 0;

                // Lo manda el Restart Manager a quien no se cerro con el
                // WM_ENDSESSION. Que llegue aqui significa que algo salio
                // mal antes, asi que queda anotado.
                case Nativo.WM_CLOSE:
                    Registro.Anotar("llego WM_CLOSE tras el aviso de cierre");
                    Volcar?.Invoke();
                    Terminar?.Invoke();
                    return 0;
            }
        }
        catch (Exception e)
        {
            Registro.Fallo("Cierre.Despachar", e);
        }

        return Nativo.DefWindowProcW(hwnd, mensaje, wParam, lParam);
    }

    public void Dispose()
    {
        if (_desechado) return;
        _desechado = true;

        if (Handle == 0) return;

        Nativo.DestroyWindow(Handle);
        Handle = 0;
    }
}
