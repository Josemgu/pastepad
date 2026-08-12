using System.Runtime.InteropServices;
using System.Text;
using Pastepad.Nucleo;

namespace Pastepad.App.Sistema;

public enum TipoContenido
{
    Nada,

    Texto,

    Imagen,

    /// <summary>Quien copio pidio expresamente que no se guarde.</summary>
    Privado,
}

public readonly record struct Contenido(
    TipoContenido Tipo, string? Texto, byte[]? Imagen);

/// <summary>
/// Lectura y escritura del portapapeles con la API de Win32.
///
/// No se usa la clase Clipboard de WinRT a proposito: su documentacion
/// dice que solo se puede acceder al portapapeles con la aplicacion
/// enfocada, y un gestor de portapapeles vive en segundo plano por
/// definicion.
/// </summary>
internal static class Portapapeles
{
    // Formatos con los que un programa dice "no guardes esto". Los usan
    // KeePass, Bitwarden, el Administrador de credenciales de Windows y
    // el modo incognito de Chrome.
    // learn.microsoft.com/windows/win32/dataxchg/clipboard-formats
    static readonly string[] _privadosPorPresencia =
    [
        "Clipboard Viewer Ignore",
        "ExcludeClipboardContentFromMonitorProcessing",
    ];

    static readonly string[] _privadosPorCero =
    [
        "CanIncludeInClipboardHistory",
        "CanUploadToCloudClipboard",
    ];

    /// <summary>
    /// Contador que Windows sube con cada copia. Leerlo cuesta una
    /// llamada; abrir el portapapeles cuesta muchisimo mas.
    ///
    /// Ademas sirve para descartar avisos repetidos: WM_CLIPBOARDUPDATE
    /// no llega una vez por copia, sino una por cada sesion que abra el
    /// programa que copia. Medido: PowerShell dispara tres por copia.
    /// </summary>
    public static uint Secuencia() => Nativo.GetClipboardSequenceNumber();

    /// <summary>
    /// Abre el portapapeles, hace algo y lo cierra pase lo que pase.
    /// Windows solo deja abrirlo a un programa a la vez, asi que hay que
    /// reintentar.
    /// </summary>
    static T? Con<T>(Func<T> accion, int intentos = 4)
    {
        for (int i = 0; i < intentos; i++)
        {
            if (Nativo.OpenClipboard(0))
            {
                try
                {
                    return accion();
                }
                catch (Exception e)
                {
                    Registro.Fallo("Portapapeles", e);
                    return default;
                }
                finally
                {
                    Nativo.CloseClipboard();
                }
            }

            Thread.Sleep(40);
        }

        Registro.Anotar(
            $"no se pudo abrir el portapapeles en {intentos} intentos");

        return default;
    }

    /// <summary>
    /// True si quien copio pidio que no se guarde. Con el portapapeles
    /// ya abierto.
    /// </summary>
    static bool EsPrivado()
    {
        foreach (var nombre in _privadosPorPresencia)
        {
            uint f = Nativo.RegisterClipboardFormatW(nombre);
            if (f != 0 && Nativo.IsClipboardFormatAvailable(f)) return true;
        }

        foreach (var nombre in _privadosPorCero)
        {
            uint f = Nativo.RegisterClipboardFormatW(nombre);
            if (f == 0 || !Nativo.IsClipboardFormatAvailable(f)) continue;

            // El formato existe: vale cero, o no se puede leer, en los
            // dos casos significa "no".
            nint mano = Nativo.GetClipboardData(f);
            if (mano == 0) return true;

            nint datos = Nativo.GlobalLock(mano);
            if (datos == 0) return true;

            try
            {
                if ((nuint)Nativo.GlobalSize(mano) >= 4 &&
                    Marshal.ReadInt32(datos) == 0)
                    return true;
            }
            finally
            {
                Nativo.GlobalUnlock(mano);
            }
        }

        return false;
    }

    public static Contenido Leer() => Con(() =>
    {
        if (EsPrivado())
            return new Contenido(TipoContenido.Privado, null, null);

        if (Nativo.IsClipboardFormatAvailable(Nativo.CF_UNICODETEXT))
        {
            nint mano = Nativo.GetClipboardData(Nativo.CF_UNICODETEXT);
            if (mano != 0)
            {
                nint datos = Nativo.GlobalLock(mano);
                try
                {
                    if (datos != 0)
                    {
                        string? texto = Marshal.PtrToStringUni(datos);

                        if (!string.IsNullOrEmpty(texto))
                        {
                            if (texto.Length > Config.MaxCaracteres)
                                texto = texto[..Config.MaxCaracteres];

                            return new Contenido(TipoContenido.Texto, texto, null);
                        }
                    }
                }
                finally
                {
                    Nativo.GlobalUnlock(mano);
                }
            }
        }

        if (Nativo.IsClipboardFormatAvailable(Nativo.CF_DIB))
        {
            nint mano = Nativo.GetClipboardData(Nativo.CF_DIB);
            if (mano != 0)
            {
                nint datos = Nativo.GlobalLock(mano);
                try
                {
                    if (datos != 0)
                    {
                        int tam = (int)Nativo.GlobalSize(mano);
                        var dib = new byte[tam];
                        Marshal.Copy(datos, dib, 0, tam);
                        return new Contenido(TipoContenido.Imagen, null, dib);
                    }
                }
                finally
                {
                    Nativo.GlobalUnlock(mano);
                }
            }
        }

        return new Contenido(TipoContenido.Nada, null, null);
    }, 2);

    /// <summary>
    /// Deja el texto en el portapapeles en dos versiones a la vez: con
    /// formato para Word y Outlook, plana para todo lo demas.
    /// </summary>
    public static bool Copiar(
        IReadOnlyList<Fragmento> fragmentos, bool sinFormato = false)
    {
        string plano = Modelo.TextoDe(fragmentos);

        return Con(() =>
        {
            if (!Nativo.EmptyClipboard())
            {
                Registro.Anotar("EmptyClipboard fallo con error " +
                                Marshal.GetLastWin32Error());
                return false;
            }

            if (!PonerTexto(Nativo.CF_UNICODETEXT, plano)) return false;

            if (!sinFormato)
            {
                uint rtfId = Nativo.RegisterClipboardFormatW("Rich Text Format");

                if (rtfId != 0)
                {
                    // El RTF va en ASCII: los caracteres fuera de ese
                    // rango ya viajan escapados como \uNNNN?.
                    byte[] rtf = Encoding.ASCII.GetBytes(ARtf(fragmentos) + "\0");
                    PonerBytes(rtfId, rtf);
                }
            }

            return true;
        });
    }

    public static bool CopiarImagen(string ruta)
    {
        byte[] archivo;

        try
        {
            archivo = File.ReadAllBytes(ruta);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Registro.Fallo($"leer la imagen {ruta}", e);
            return false;
        }

        if (archivo.Length <= 14) return false;

        // Se le quita la cabecera de archivo BMP: al portapapeles va el
        // DIB crudo, sin esos 14 bytes.
        byte[] dib = archivo[14..];

        return Con(() =>
        {
            if (!Nativo.EmptyClipboard()) return false;
            return PonerBytes(Nativo.CF_DIB, dib);
        });
    }

    /// <summary>
    /// Le pone cabecera de archivo a los bytes crudos que da Windows.
    /// </summary>
    public static byte[] DibABmp(byte[] dib)
    {
        var salida = new byte[14 + dib.Length];

        salida[0] = (byte)'B';
        salida[1] = (byte)'M';

        BitConverter.GetBytes(14 + dib.Length).CopyTo(salida, 2);
        BitConverter.GetBytes(54).CopyTo(salida, 10);

        dib.CopyTo(salida, 14);
        return salida;
    }

    static bool PonerTexto(uint formato, string texto) =>
        PonerBytes(formato, Encoding.Unicode.GetBytes(texto + "\0"));

    /// <summary>
    /// Copia los bytes a memoria global y se la entrega a Windows. Ojo:
    /// si SetClipboardData funciona, el bloque pasa a ser del sistema y
    /// no hay que liberarlo; si falla, si.
    /// </summary>
    static bool PonerBytes(uint formato, byte[] datos)
    {
        nint mano = Nativo.GlobalAlloc(Nativo.GMEM_MOVEABLE, (nuint)datos.Length);
        if (mano == 0) return false;

        nint destino = Nativo.GlobalLock(mano);

        if (destino == 0)
        {
            Nativo.GlobalFree(mano);
            return false;
        }

        try
        {
            Marshal.Copy(datos, 0, destino, datos.Length);
        }
        finally
        {
            Nativo.GlobalUnlock(mano);
        }

        if (Nativo.SetClipboardData(formato, mano) == 0)
        {
            Registro.Anotar($"SetClipboardData({formato}) fallo con error " +
                            Marshal.GetLastWin32Error());
            Nativo.GlobalFree(mano);
            return false;
        }

        return true;
    }

    // ------------------------------------------------------------ RTF

    static string EscaparRtf(string s)
    {
        var salida = new StringBuilder(s.Length);

        foreach (char ch in s)
        {
            if (ch is '\\' or '{' or '}') salida.Append('\\').Append(ch);
            else if (ch == '\n') salida.Append("\\par\n");
            else if (ch == '\t') salida.Append("\\tab ");
            else if (ch < 128) salida.Append(ch);
            else salida.Append("\\u").Append((int)ch).Append('?');
        }

        return salida.ToString();
    }

    /// <summary>
    /// Convierte los fragmentos a RTF, que es lo que entienden Word y
    /// Outlook.
    /// </summary>
    public static string ARtf(IReadOnlyList<Fragmento> fragmentos)
    {
        var fuentes = new List<string>();
        var colores = new List<string>();

        foreach (var f in fragmentos)
        {
            if (!fuentes.Contains(f.F)) fuentes.Add(f.F);

            string c = f.C.ToUpperInvariant();
            if (!colores.Contains(c)) colores.Add(c);
        }

        var tablaF = new StringBuilder();

        for (int i = 0; i < fuentes.Count; i++)
            tablaF.Append($"{{\\f{i}\\fnil {fuentes[i]};}}");

        var tablaC = new StringBuilder();

        for (int i = 0; i < colores.Count; i++)
        {
            if (i > 0) tablaC.Append(';');

            string c = colores[i];

            tablaC.Append($"\\red{Convert.ToInt32(c.Substring(1, 2), 16)}")
                  .Append($"\\green{Convert.ToInt32(c.Substring(3, 2), 16)}")
                  .Append($"\\blue{Convert.ToInt32(c.Substring(5, 2), 16)}");
        }

        var cuerpo = new StringBuilder("\\pard\\plain ");

        foreach (var f in fragmentos)
        {
            cuerpo.Append($"\\f{fuentes.IndexOf(f.F)}")
                  .Append($"\\fs{f.S * 2}")
                  .Append($"\\cf{colores.IndexOf(f.C.ToUpperInvariant()) + 1}");

            if (f.B != 0) cuerpo.Append("\\b");
            if (f.I != 0) cuerpo.Append("\\i");
            if (f.U != 0) cuerpo.Append("\\ul");

            cuerpo.Append(' ').Append(EscaparRtf(f.T));

            if (f.U != 0) cuerpo.Append("\\ulnone");
            if (f.I != 0) cuerpo.Append("\\i0");
            if (f.B != 0) cuerpo.Append("\\b0");
        }

        return $"{{\\rtf1\\ansi\\deff0{{\\fonttbl{tablaF}}}" +
               $"{{\\colortbl;{tablaC};}}{cuerpo}}}";
    }
}
