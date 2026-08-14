using System.Runtime.InteropServices;

namespace Pastepad.App.Sistema;

/// <summary>
/// Todo lo que habla con Windows, en un solo sitio. Aparte del resto
/// para que el codigo de la interfaz no tenga que saber nada de handles
/// ni de estructuras del sistema.
///
/// Las firmas estan escritas a mano y no generadas: son pocas y asi se
/// ven. Si esta lista crece mucho, la alternativa es CsWin32.
/// </summary>
internal static partial class Nativo
{
    // --- mensajes ---------------------------------------------------

    public const uint WM_DESTROY = 0x0002;
    public const uint WM_HOTKEY = 0x0312;
    public const uint WM_CLIPBOARDUPDATE = 0x031D;

    /// <summary>Lo manda una segunda instancia para pedir el panel.</summary>
    public const uint WM_MOSTRARSE = 0x0400 + 7;   // WM_USER + 7

    /// <summary>Lo manda el icono de la bandeja al hacerle clic.</summary>
    public const uint WM_BANDEJA = 0x0400 + 8;     // WM_USER + 8

    public const uint WM_LBUTTONUP = 0x0202;
    public const uint WM_RBUTTONUP = 0x0205;
    public const uint WM_COMMAND = 0x0111;

    // --- el aviso de que nos van a cerrar ---------------------------

    public const uint WM_CLOSE = 0x0010;
    public const uint WM_QUERYENDSESSION = 0x0011;
    public const uint WM_ENDSESSION = 0x0016;

    /// <summary>
    /// Llega en el lParam de WM_QUERYENDSESSION cuando quien cierra no
    /// es Windows apagandose, sino el Restart Manager haciendole sitio a
    /// un instalador. Sirve para distinguir "me actualizan" de "se apaga
    /// el equipo", que en el registro no es lo mismo.
    /// </summary>
    public const nint ENDSESSION_CLOSEAPP = 0x1;

    // --- modificadores del atajo ------------------------------------

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    // --- teclas -----------------------------------------------------

    public const byte VK_SHIFT = 0x10;
    public const byte VK_CONTROL = 0x11;
    public const byte VK_MENU = 0x12;      // Alt
    public const byte VK_V = 0x56;

    public const uint KEYEVENTF_KEYUP = 0x0002;

    // --- portapapeles -----------------------------------------------

    public const uint CF_UNICODETEXT = 13;
    public const uint CF_DIB = 8;

    // --- varios -----------------------------------------------------

    public const nint HWND_MESSAGE = -3;

    /// <summary>
    /// Ventana emergente sin marco. Sin WS_VISIBLE: nace oculta y nunca
    /// se muestra.
    /// </summary>
    public const uint WS_POPUP = 0x80000000;

    /// <summary>Fuera de Alt+Tab y de la barra de tareas.</summary>
    public const uint WS_EX_TOOLWINDOW = 0x00000080;

    public const int SW_RESTORE = 9;
    public const uint GMEM_MOVEABLE = 0x0002;
    public const uint MONITOR_DEFAULTTONEAREST = 2;
    public const int ERROR_CLASS_ALREADY_EXISTS = 1410;

    // --- estructuras ------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WNDCLASSW
    {
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public readonly int Ancho => right - left;
        public readonly int Alto => bottom - top;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    public const uint NIM_ADD = 0x00000000;
    public const uint NIM_MODIFY = 0x00000001;
    public const uint NIM_DELETE = 0x00000002;

    public const uint NIF_MESSAGE = 0x00000001;
    public const uint NIF_ICON = 0x00000002;
    public const uint NIF_TIP = 0x00000004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NOTIFYICONDATAW
    {
        public uint cbSize;
        public nint hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public nint hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public nint hBalloonIcon;
    }

    // --- ventanas ---------------------------------------------------

    // DllImport y no LibraryImport: el generador no sabe convertir
    // structs con cadenas dentro (SYSLIB1051), y WNDCLASSW las lleva.
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern ushort RegisterClassW(ref WNDCLASSW clase);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true)]
    public static partial nint CreateWindowExW(
        uint estiloExtra, string clase, string titulo, uint estilo,
        int x, int y, int ancho, int alto,
        nint padre, nint menu, nint instancia, nint parametro);

    [LibraryImport("user32.dll")]
    public static partial nint DefWindowProcW(
        nint hwnd, uint mensaje, nint wParam, nint lParam);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyWindow(nint hwnd);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true)]
    public static partial nint FindWindowExW(
        nint padre, nint tras, string? clase, string? titulo);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PostMessageW(
        nint hwnd, uint mensaje, nint wParam, nint lParam);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true)]
    public static partial nint GetModuleHandleW(string? nombre);

    // --- que nos reabran despues de actualizarnos -------------------

    /// <summary>
    /// Le dice a Windows con que linea de comandos volver a abrirnos si
    /// nos cierra. Sin esta llamada el Restart Manager NO reabre nada:
    /// "Restart Manager can only restart applications that have been
    /// registered for restart. This is the only way that the Restart
    /// Manager can determine the command-line command to use".
    ///
    /// Devuelve un HRESULT, no un booleano.
    /// </summary>
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int RegisterApplicationRestart(
        string? lineaComandos, uint banderas);

    /// <summary>No reabrirnos si lo que hubo fue un cierre inesperado.</summary>
    public const uint RESTART_NO_CRASH = 1;

    /// <summary>Ni si Windows nos da por colgados.</summary>
    public const uint RESTART_NO_HANG = 2;

    // --- atajo global -----------------------------------------------

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterHotKey(
        nint hwnd, int id, uint modificadores, uint tecla);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterHotKey(nint hwnd, int id);

    // --- portapapeles -----------------------------------------------

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AddClipboardFormatListener(nint hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RemoveClipboardFormatListener(nint hwnd);

    [LibraryImport("user32.dll")]
    public static partial uint GetClipboardSequenceNumber();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool OpenClipboard(nint duena);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EmptyClipboard();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsClipboardFormatAvailable(uint formato);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint GetClipboardData(uint formato);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint SetClipboardData(uint formato, nint datos);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true)]
    public static partial uint RegisterClipboardFormatW(string nombre);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint GlobalAlloc(uint banderas, nuint bytes);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint GlobalLock(nint memoria);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GlobalUnlock(nint memoria);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nuint GlobalSize(nint memoria);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint GlobalFree(nint memoria);

    // --- foco -------------------------------------------------------

    [LibraryImport("user32.dll")]
    public static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(nint hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint SetFocus(nint hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindow(nint hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsIconic(nint hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(nint hwnd, int orden);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint GetWindowThreadProcessId(nint hwnd, out uint pid);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AttachThreadInput(
        uint hiloPropio, uint hiloDestino, [MarshalAs(UnmanagedType.Bool)] bool enganchar);

    [LibraryImport("kernel32.dll")]
    public static partial uint GetCurrentThreadId();

    [LibraryImport("user32.dll")]
    public static partial void keybd_event(
        byte tecla, byte codigo, uint banderas, nuint extra);

    /// <summary>
    /// Cuando se PUSO en la cola el mensaje que se esta despachando, no
    /// cuando se atiende: «the elapsed time, in milliseconds, from the
    /// time the system was started to the time the message was created
    /// (that is, placed in the thread's message queue)».
    ///
    /// Restandoselo a GetTickCount sale lo que el mensaje espero a que
    /// alguien lo recogiera, que es el unico trozo del camino del atajo
    /// que hasta ahora no se medía y donde tiene que estar el retraso que
    /// el usuario nota tras diez minutos parado.
    /// </summary>
    [LibraryImport("user32.dll")]
    public static partial int GetMessageTime();

    // --- pantalla ---------------------------------------------------

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out POINT punto);

    [LibraryImport("user32.dll")]
    public static partial nint MonitorFromPoint(POINT punto, uint banderas);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetMonitorInfoW(nint monitor, ref MONITORINFO info);

    [LibraryImport("user32.dll")]
    public static partial int GetDpiForWindow(nint hwnd);

    // --- bandeja ----------------------------------------------------

    // Igual que RegisterClassW: NOTIFYICONDATAW lleva cadenas de tamaño
    // fijo, que el generador tampoco convierte.
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Shell_NotifyIconW(uint mensaje, ref NOTIFYICONDATAW datos);

    public const uint IMAGE_ICON = 1;
    public const uint LR_LOADFROMFILE = 0x00000010;
    public const uint LR_DEFAULTSIZE = 0x00000040;

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true)]
    public static partial nint LoadImageW(
        nint instancia, string nombre, uint tipo, int ancho, int alto, uint banderas);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint LoadIconW(nint instancia, nint nombre);

    public static readonly nint IDI_APPLICATION = 32512;

    // --- menu de la bandeja -----------------------------------------

    public const uint MF_STRING = 0x00000000;
    public const uint MF_SEPARATOR = 0x00000800;
    public const uint TPM_RIGHTBUTTON = 0x0002;
    public const uint TPM_RETURNCMD = 0x0100;

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint CreatePopupMenu();

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AppendMenuW(
        nint menu, uint banderas, nuint id, string? texto);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyMenu(nint menu);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial int TrackPopupMenuEx(
        nint menu, uint banderas, int x, int y, nint hwnd, nint parametros);
}
