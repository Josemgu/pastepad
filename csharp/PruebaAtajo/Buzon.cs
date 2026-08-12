using System.Runtime.InteropServices;

namespace PruebaAtajo;

/// <summary>
/// Ventana solo-mensajes: no se dibuja, no sale en Alt+Tab, no tiene
/// pixeles. Solo existe para tener un HWND al que Windows entregue
/// WM_HOTKEY y WM_CLIPBOARDUPDATE.
///
/// Por que asi y no subclaseando la ventana de XAML: pastepad se oculta
/// a la bandeja, y atar el atajo a la ventana visible es el mismo
/// acoplamiento que mato a la version en Python. Ademas hay un fallo
/// abierto de ExecutionEngineException al subclasear ventanas de WinUI.
///
/// Lo que esta prueba responde: si la bomba de mensajes de WinUI 3
/// despacha los mensajes de esta ventana, que se crea en su mismo hilo.
/// No esta documentado en ninguna parte.
/// </summary>
internal sealed class Buzon : IDisposable
{
    // La version en Python llamaba a RegisterHotKey con hWnd nulo: eso
    // registra un atajo de HILO, y su WM_HOTKEY llega con hwnd nulo, asi
    // que DispatchMessage no lo entrega a ningun procedimiento de
    // ventana. La bomba de XAML lo tiraria. Por eso hay que pasar un
    // HWND real, y por eso existe esta clase.

    const int HWND_MESSAGE = -3;

    const uint WM_HOTKEY = 0x0312;
    const uint WM_CLIPBOARDUPDATE = 0x031D;

    const uint MOD_CONTROL = 0x0002;
    const uint MOD_SHIFT = 0x0004;
    const uint MOD_NOREPEAT = 0x4000;  // sin repeticion al mantener
    const uint VK_V = 0x56;

    const int ID_ATAJO = 1;            // valido de 0x0000 a 0xBFFF

    const string CLASE = "PastepadBuzonPrueba";

    /// <summary>Salta cuando llega WM_HOTKEY.</summary>
    public event Action? Atajo;

    /// <summary>Salta cuando llega WM_CLIPBOARDUPDATE.</summary>
    public event Action? Portapapeles;

    /// <summary>Ultimo error de Windows, si el montaje fallo.</summary>
    public string? Problema { get; private set; }

    public IntPtr Handle { get; private set; }

    public bool AtajoRegistrado { get; private set; }
    public bool EscuchaPortapapeles { get; private set; }

    // El delegado vive en un campo de instancia a proposito. Si solo
    // existiera dentro de la llamada a RegisterClass, el recolector
    // podria llevarselo y Windows saltaria a memoria liberada. Es la
    // sospecha habitual detras del ExecutionEngineException que se ve
    // al subclasear.
    readonly Procedimiento _procedimiento;

    bool _desechado;

    /// <summary>
    /// Crea la ventana y registra atajo y escucha. Tiene que llamarse
    /// desde el hilo de interfaz: RegisterHotKey falla si el HWND lo
    /// creo otro hilo.
    /// </summary>
    public Buzon()
    {
        _procedimiento = Despachar;

        var instancia = GetModuleHandleW(null);

        var clase = new WNDCLASSW
        {
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(
                _procedimiento),
            hInstance = instancia,
            lpszClassName = CLASE,
        };

        // Si la clase ya existe (por ejemplo tras un reinicio en
        // caliente del depurador) RegisterClassW devuelve 0 con
        // ERROR_CLASS_ALREADY_EXISTS. No es un fallo: se sigue.
        if (RegisterClassW(ref clase) == 0)
        {
            const int CLASE_YA_EXISTE = 1410;
            int error = Marshal.GetLastWin32Error();
            if (error != CLASE_YA_EXISTE)
            {
                Problema = $"RegisterClassW fallo con error {error}";
                Registro.Anotar(Problema);
                return;
            }
        }

        Handle = CreateWindowExW(
            0, CLASE, string.Empty, 0, 0, 0, 0, 0,
            HWND_MESSAGE, IntPtr.Zero, instancia, IntPtr.Zero);

        if (Handle == IntPtr.Zero)
        {
            Problema = "CreateWindowExW fallo con error " +
                       Marshal.GetLastWin32Error();
            Registro.Anotar(Problema);
            return;
        }

        Registro.Anotar($"buzon creado, hwnd 0x{Handle:X}");

        AtajoRegistrado = RegisterHotKey(
            Handle, ID_ATAJO,
            MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_V);

        if (!AtajoRegistrado)
        {
            // El caso normal es que otra aplicacion ya se quedo con el
            // atajo. Se dice cual es el error, no se calla.
            Problema = "RegisterHotKey fallo con error " +
                       Marshal.GetLastWin32Error() +
                       " (¿otra app tiene Ctrl+Shift+V?)";
            Registro.Anotar(Problema);
        }
        else
        {
            Registro.Anotar("Ctrl+Shift+V registrado");
        }

        EscuchaPortapapeles = AddClipboardFormatListener(Handle);

        if (!EscuchaPortapapeles)
        {
            var fallo = "AddClipboardFormatListener fallo con error " +
                        Marshal.GetLastWin32Error();
            Problema = Problema is null ? fallo : Problema + "; " + fallo;
            Registro.Anotar(fallo);
        }
        else
        {
            Registro.Anotar("escucha del portapapeles activa");
        }
    }

    /// <summary>
    /// El procedimiento de ventana. Ninguna excepcion puede escapar de
    /// aqui: cruzaria de vuelta a codigo nativo, que es comportamiento
    /// indefinido y se manifiesta como un cierre sin traza.
    /// </summary>
    IntPtr Despachar(IntPtr hwnd, uint mensaje, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            switch (mensaje)
            {
                case WM_HOTKEY when (int)wParam == ID_ATAJO:
                    Atajo?.Invoke();
                    return IntPtr.Zero;

                case WM_CLIPBOARDUPDATE:
                    Portapapeles?.Invoke();
                    return IntPtr.Zero;
            }
        }
        catch (Exception e)
        {
            Registro.Fallo("Buzon.Despachar", e);
        }

        return DefWindowProcW(hwnd, mensaje, wParam, lParam);
    }

    public void Dispose()
    {
        if (_desechado) return;
        _desechado = true;

        if (Handle == IntPtr.Zero) return;

        if (AtajoRegistrado) UnregisterHotKey(Handle, ID_ATAJO);
        if (EscuchaPortapapeles) RemoveClipboardFormatListener(Handle);

        DestroyWindow(Handle);
        Handle = IntPtr.Zero;

        Registro.Anotar("buzon cerrado");
    }

    // --- API de Windows ---------------------------------------------

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate IntPtr Procedimiento(
        IntPtr hwnd, uint mensaje, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct WNDCLASSW
    {
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern ushort RegisterClassW(ref WNDCLASSW clase);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr CreateWindowExW(
        uint estiloExtra, string clase, string titulo, uint estilo,
        int x, int y, int ancho, int alto,
        nint padre, IntPtr menu, IntPtr instancia, IntPtr parametro);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr DefWindowProcW(
        IntPtr hwnd, uint mensaje, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool RegisterHotKey(
        IntPtr hwnd, int id, uint modificadores, uint tecla);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool UnregisterHotKey(IntPtr hwnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr GetModuleHandleW(string? nombre);
}
