namespace Pastepad.Nucleo;

/// <summary>
/// Limites y valores por defecto. Los colores y las medidas de la
/// interfaz NO estan aqui: viven en la especificacion de UI, que es su
/// unica fuente. Este archivo llego a tener una copia con valores
/// distintos y solo servia para confundir.
/// </summary>
public static class Config
{
    public const string App = "pastepad";

    /// <summary>
    /// Sale del ensamblado, que a su vez sale del tag de git. Nunca
    /// escrita a mano: se publico una release con binarios viejos por
    /// tenerla clavada en doce sitios.
    /// </summary>
    public static string Version { get; } =
        typeof(Config).Assembly
            .GetCustomAttributes(
                typeof(System.Reflection.AssemblyInformationalVersionAttribute),
                false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion
            .Split('+')[0]
        ?? "0.0.0";

    // --- ventana
    public static readonly IReadOnlyDictionary<string, (int Ancho, int Alto)>
        Tamanos = new Dictionary<string, (int, int)>
        {
            ["mini"] = (300, 380),
            ["chico"] = (340, 460),
            ["mediano"] = (380, 560),
            ["grande"] = (470, 700),
        };

    public const string TamanoDef = "mediano";

    public const int MinAncho = 300;
    public const int MinAlto = 340;
    public const int MaxAncho = 720;
    public const int MaxAlto = 1100;

    /// <summary>Atajos que se hacen con una sola mano.</summary>
    public static readonly IReadOnlyDictionary<string, string> Atajos =
        new Dictionary<string, string>
        {
            ["ctrl+shift+v"] = "Ctrl + Shift + V",
            ["ctrl+q"] = "Ctrl + Q",
            ["ctrl+space"] = "Ctrl + Espacio",
            ["ctrl+shift+space"] = "Ctrl + Shift + Espacio",
            ["alt+q"] = "Alt + Q",
            ["ctrl+alt+v"] = "Ctrl + Alt + V",
        };

    public const string AtajoDef = "ctrl+shift+v";

    // --- limites
    public const int MaxHist = 80;
    public const int MaxCaracteres = 200_000;

    // --- tipografia
    public const string FuenteAlt = "Segoe UI";   // Windows 10
    public const string FuenteDef = "Calibri";
    public const int TamDef = 11;
    public const string ColorDef = "#000000";
}

/// <summary>
/// Donde vive todo. Se pasa al <see cref="Almacen"/> en vez de vivir en
/// variables globales: asi las pruebas usan una carpeta temporal sin
/// tocar estado compartido, que en la version en Python obligaba a
/// reasignar el modulo de configuracion entero.
/// </summary>
public sealed record Rutas(
    string Datos,
    string Historial,
    string Preferencias,
    string Imagenes)
{
    /// <summary>
    /// %LOCALAPPDATA%\pastepad. En Archivos de programa Windows bloquea
    /// la escritura sin avisar: el programa arrancaria pero no guardaria
    /// nada.
    /// </summary>
    public static Rutas Predeterminadas()
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            Config.App);

        return EnCarpeta(baseDir);
    }

    public static Rutas EnCarpeta(string carpeta) => new(
        Path.Combine(carpeta, "snippets.json"),
        Path.Combine(carpeta, "historial.json"),
        Path.Combine(carpeta, "config.json"),
        Path.Combine(carpeta, "imagenes"));
}
