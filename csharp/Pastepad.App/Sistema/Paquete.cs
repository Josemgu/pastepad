namespace Pastepad.App.Sistema;

/// <summary>
/// Si los archivos de pastepad estan acabando en un sitio distinto del
/// que pastepad cree.
///
/// **Esto no deberia pasar nunca y por eso hay que detectarlo.** Cuando
/// a pastepad lo abre otra aplicacion empaquetada, hereda su contenedor
/// y Windows redirige sus escrituras a <c>%LOCALAPPDATA%</c> hacia
/// <c>...\Packages\&lt;paquete&gt;\LocalCache\Local</c>. pastepad calcula
/// bien su ruta, cree que la esta leyendo, y en realidad lee y escribe
/// en una copia. El usuario lo abre despues desde su sitio y **su
/// historial y sus textos han desaparecido**, aunque en el disco sigan
/// intactos.
///
/// Paso de verdad el 14 de agosto de 2026: pastepad se lanzo desde una
/// sesion que vivia dentro de otra aplicacion empaquetada, estuvo horas
/// capturando el portapapeles dentro de la copia redirigida, y cuando el
/// usuario lo abrio desde el menu Inicio se encontro la carpeta de
/// verdad sin <c>snippets.json</c>. Arranco de cero sin decir nada, que
/// es lo que convirtio un accidente del entorno en «se borro todo».
///
/// **La identidad de paquete no sirve para detectarlo, aunque lo
/// parezca.** Se probo primero con <c>GetCurrentPackageFullName</c> y
/// devuelve <c>APPMODEL_ERROR_NO_PACKAGE</c>: el proceso hijo hereda la
/// redireccion de archivos pero NO la identidad. Medido lanzando
/// pastepad desde dentro del contenedor — el aviso no salio.
///
/// Lo que si funciona es preguntar por un ARCHIVO, no por el proceso ni
/// por la carpeta. <c>GetFinalPathNameByHandle</c> devuelve la ruta real
/// detras de un handle, y ahi la redireccion se ve entera.
///
/// **Y tiene que ser un archivo que se acabe de escribir.** Medido, los
/// tres casos en el mismo sitio redirigido:
///
/// - la carpeta            -> se resuelve a si misma. No delata nada.
/// - un archivo que ya estaba -> tampoco, hasta que alguien lo escribe:
///   la redireccion es copia-al-escribir, archivo a archivo.
/// - una sonda recien creada  -> sale bajo <c>...\Packages\...</c>
///
/// Que haya que escribir para verlo no es un inconveniente: escribir es
/// justo lo que se quiere comprobar antes de dejar que el programa
/// guarde nada.
/// </summary>
internal static class Paquete
{
    /// <summary>
    /// Como se llama la sonda. Empieza por punto para que no estorbe si
    /// alguna vez sobreviviera, aunque no deberia: se crea con
    /// <c>DELETE_ON_CLOSE</c>, asi que Windows la borra al soltar el
    /// handle incluso si el proceso muere de golpe.
    /// </summary>
    const string Sonda = ".pastepad-sonda";

    /// <summary>
    /// Donde acaban de verdad los archivos de la carpeta de datos, si no
    /// es donde se pidieron. Null cuando todo esta en su sitio, que es
    /// lo normal.
    /// </summary>
    public static string? RutaDesviada(string carpeta)
    {
        try
        {
            if (!Directory.Exists(carpeta)) return null;

            nint handle = Nativo.CreateFileW(
                Path.Combine(carpeta, Sonda),
                Nativo.GENERIC_WRITE,
                0,
                0,
                Nativo.CREATE_ALWAYS,
                Nativo.FILE_FLAG_DELETE_ON_CLOSE,
                0);

            if (handle == Nativo.HANDLE_INVALIDO)
            {
                // Que no se pueda escribir la sonda es en si mismo un
                // problema, pero de otro tipo: de eso ya se encarga el
                // almacen al no poder escribir sus archivos.
                Registro.Anotar(
                    "no se pudo crear la sonda en la carpeta de datos, error "
                    + System.Runtime.InteropServices.Marshal.GetLastWin32Error());

                return null;
            }

            try
            {
                var buffer = new char[1024];
                uint largo = Nativo.GetFinalPathNameByHandleW(
                    handle, buffer, (uint)buffer.Length, 0);

                if (largo == 0 || largo > buffer.Length)
                {
                    Registro.Anotar(
                        "GetFinalPathNameByHandle no dijo la ruta real, error "
                        + System.Runtime.InteropServices.Marshal.GetLastWin32Error());

                    return null;
                }

                string real = new(buffer, 0, (int)largo);

                // Viene en forma larga: \\?\C:\... Se quita para poder
                // compararla con la que pidio el programa.
                if (real.StartsWith(@"\\?\", StringComparison.Ordinal))
                    real = real[4..];

                // Se compara la CARPETA de la sonda, no la sonda: lo que
                // importa es donde acaba lo que se escriba, no el nombre
                // del archivo con el que se averiguo.
                string donde = Path.GetDirectoryName(real) ?? real;

                return SonLaMisma(donde, carpeta) ? null : donde;
            }
            finally
            {
                Nativo.CloseHandle(handle);
            }
        }
        catch (Exception e)
        {
            // Ancho a proposito: esto es una comprobacion de diagnostico
            // en la primera linea del arranque, y no puede impedir que
            // el programa abra.
            Registro.Fallo("comprobar la ruta real de la carpeta de datos", e);
            return null;
        }
    }

    /// <summary>
    /// Sin barra final y sin distinguir mayusculas, que en Windows es el
    /// mismo nombre.
    /// </summary>
    static bool SonLaMisma(string a, string b) =>
        string.Equals(
            Path.TrimEndingDirectorySeparator(a),
            Path.TrimEndingDirectorySeparator(b),
            StringComparison.OrdinalIgnoreCase);
}
