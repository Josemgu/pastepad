using Microsoft.Win32;

namespace Pastepad.App.Sistema;

/// <summary>
/// Arrancar con Windows. Es una entrada de registro en HKCU, que sigue
/// funcionando en una aplicacion desempaquetada: la documentacion de
/// despliegue sin identidad de paquete lo dice expresamente.
/// </summary>
internal static class Arranque
{
    const string Clave = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string Nombre = "pastepad";

    /// <summary>
    /// La ruta desde donde se lanzo. Si se mueve la carpeta hay que
    /// abrirlo una vez desde el sitio nuevo.
    /// </summary>
    static string RutaPropia() =>
        Environment.ProcessPath ?? AppContext.BaseDirectory;

    public static bool Activo()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Clave);
            return k?.GetValue(Nombre) is not null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                    or System.Security.SecurityException)
        {
            Registro.Fallo("leer el autoarranque", e);
            return false;
        }
    }

    /// <summary>
    /// Lo que hay hoy en la clave, para poder decir que se cambio y de
    /// que a que. Devuelve el valor tal cual esta guardado —con las
    /// comillas incluidas—, no la ruta limpia: lo que interesa anotar es
    /// exactamente lo que habia.
    /// </summary>
    public static string? ValorActual()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Clave);
            return k?.GetValue(Nombre) as string;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                    or System.Security.SecurityException)
        {
            Registro.Fallo("leer el valor del autoarranque", e);
            return null;
        }
    }

    public static void Poner(bool activar)
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Clave, writable: true);

            if (k is null)
            {
                Registro.Anotar($"no se pudo abrir HKCU\\{Clave}");
                return;
            }

            if (activar)
                k.SetValue(Nombre, $"\"{RutaPropia()}\"", RegistryValueKind.String);
            else if (k.GetValue(Nombre) is not null)
                k.DeleteValue(Nombre);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                    or System.Security.SecurityException)
        {
            Registro.Fallo("escribir el autoarranque", e);
        }
    }
}
