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

    public void Dispose()
    {
        if (!_puesto) return;

        Nativo.Shell_NotifyIconW(Nativo.NIM_DELETE, ref _datos);
        _puesto = false;
    }
}
