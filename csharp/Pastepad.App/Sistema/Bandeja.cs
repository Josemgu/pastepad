using System.Runtime.InteropServices;

namespace Pastepad.App.Sistema;

/// <summary>
/// El icono junto al reloj. WinUI 3 no trae ninguno —la propuesta lleva
/// años abierta en su repositorio— asi que se llama a Shell_NotifyIcon
/// directamente, sobre la misma ventana solo-mensajes que ya atiende el
/// atajo. Cuesta unas cuarenta lineas y cero dependencias.
/// </summary>
internal sealed class Bandeja : IDisposable
{
    const uint ID = 1;

    Nativo.NOTIFYICONDATAW _datos;
    bool _puesto;

    public Bandeja(nint hwnd, string consejo)
    {
        _datos = new Nativo.NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<Nativo.NOTIFYICONDATAW>(),
            hWnd = hwnd,
            uID = ID,
            uFlags = Nativo.NIF_ICON | Nativo.NIF_MESSAGE | Nativo.NIF_TIP,
            uCallbackMessage = Nativo.WM_BANDEJA,
            hIcon = CargarIcono(),
            szTip = consejo,
            szInfo = "",
            szInfoTitle = "",
        };

        _puesto = Nativo.Shell_NotifyIconW(Nativo.NIM_ADD, ref _datos);

        if (!_puesto)
            Registro.Anotar("Shell_NotifyIcon fallo con error " +
                            Marshal.GetLastWin32Error());
    }

    static nint CargarIcono()
    {
        string ruta = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");

        if (File.Exists(ruta))
        {
            nint icono = Nativo.LoadImageW(
                0, ruta, Nativo.IMAGE_ICON, 0, 0,
                Nativo.LR_LOADFROMFILE | Nativo.LR_DEFAULTSIZE);

            if (icono != 0) return icono;

            Registro.Anotar("no se pudo cargar AppIcon.ico, error " +
                            Marshal.GetLastWin32Error());
        }
        else
        {
            Registro.Anotar($"no existe {ruta}");
        }

        // Sin icono no hay entrada en la bandeja, y sin ella no hay forma
        // de recuperar la aplicacion. El generico de Windows es feo pero
        // deja el programa usable.
        return Nativo.LoadIconW(0, Nativo.IDI_APPLICATION);
    }

    /// <summary>
    /// Un globo junto al reloj. Es el unico sitio donde se le puede
    /// decir algo al usuario cuando el panel no esta delante.
    ///
    /// Existe por un fallo silencioso: al pegar, el panel se esconde
    /// ANTES de devolver el foco, asi que cuando ese paso fallaba el
    /// aviso «Copiado, pero no pude volver a la ventana anterior» se
    /// pintaba en una ventana ya escondida. El usuario se quedaba con el
    /// texto copiado, sin pegar y sin enterarse de por que. Lo encontro
    /// el qa, y le paso unas quince veces seguidas.
    ///
    /// No lleva NIIF_RESPECT_QUIET_TIME a proposito. La documentacion
    /// recomienda ponerlo «on all appropriate notifications», pero
    /// tambien dice que durante ese rato «certain notifications should
    /// still be sent because they are expected by the user as feedback
    /// in response to a user action». Esto es exactamente eso: el
    /// usuario acaba de pulsar para pegar y lo que recibe es la
    /// respuesta. Tragarselo dejaria el fallo igual de mudo que antes.
    /// </summary>
    public void Avisar(string titulo, string texto)
    {
        if (!_puesto) return;

        // El titulo no es adorno: sin el, Windows no dibuja el icono del
        // globo —«if the szInfoTitle member is zero-length, the icon is
        // not shown»— y el aviso pierde la mitad de su fuerza.
        _datos.uFlags = Nativo.NIF_ICON | Nativo.NIF_MESSAGE
                      | Nativo.NIF_TIP | Nativo.NIF_INFO;

        // Los topes son los del struct, contando el nulo final. Un texto
        // mas largo no se recorta solo: la llamada entera falla.
        _datos.szInfoTitle = Recortar(titulo, 63);
        _datos.szInfo = Recortar(texto, 255);
        _datos.dwInfoFlags = Nativo.NIIF_WARNING;

        if (!Nativo.Shell_NotifyIconW(Nativo.NIM_MODIFY, ref _datos))
            Registro.Anotar("no se pudo enseñar el globo de la bandeja, error "
                            + Marshal.GetLastWin32Error());

        // Se deja el struct como estaba. Si no, cualquier NIM_MODIFY
        // posterior —cambiar el consejo, por ejemplo— volveria a sacar el
        // mismo globo, porque NIF_INFO seguiria puesto con su texto.
        _datos.uFlags = Nativo.NIF_ICON | Nativo.NIF_MESSAGE | Nativo.NIF_TIP;
        _datos.szInfo = "";
        _datos.szInfoTitle = "";
        _datos.dwInfoFlags = 0;
    }

    static string Recortar(string texto, int tope) =>
        texto.Length <= tope ? texto : texto[..tope];

    public void Dispose()
    {
        if (!_puesto) return;

        Nativo.Shell_NotifyIconW(Nativo.NIM_DELETE, ref _datos);
        _puesto = false;
    }
}
