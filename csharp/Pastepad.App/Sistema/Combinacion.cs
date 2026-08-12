namespace Pastepad.App.Sistema;

/// <summary>
/// Traduce "ctrl+shift+v" a lo que entiende RegisterHotKey.
/// </summary>
internal static class Combinacion
{
    static readonly Dictionary<string, uint> _modificadores = new()
    {
        ["ctrl"] = Nativo.MOD_CONTROL,
        ["control"] = Nativo.MOD_CONTROL,
        ["alt"] = Nativo.MOD_ALT,
        ["shift"] = Nativo.MOD_SHIFT,
        ["win"] = Nativo.MOD_WIN,
    };

    static readonly Dictionary<string, uint> _teclas = new()
    {
        ["space"] = 0x20,
        ["espacio"] = 0x20,
        ["enter"] = 0x0D,
        ["return"] = 0x0D,
        ["tab"] = 0x09,
        ["esc"] = 0x1B,
        ["escape"] = 0x1B,
        ["insert"] = 0x2D,
    };

    /// <summary>
    /// "ctrl+shift+v" -> (MOD_CONTROL|MOD_SHIFT, 0x56). null si no vale.
    ///
    /// Exige al menos un modificador: Windows rechaza registrar una
    /// tecla suelta, y ademas se la quitaria al resto del sistema.
    /// </summary>
    public static (uint Mods, uint Tecla)? Descomponer(string texto)
    {
        uint mods = 0;
        uint? tecla = null;

        foreach (var parte in texto.ToLowerInvariant().Replace(" ", "").Split('+'))
        {
            if (parte.Length == 0) continue;

            if (_modificadores.TryGetValue(parte, out uint m))
                mods |= m;
            else if (_teclas.TryGetValue(parte, out uint t))
                tecla = t;
            else if (parte.Length == 1)
                tecla = char.ToUpperInvariant(parte[0]);
            else
                return null;
        }

        return tecla is not null && mods != 0 ? (mods, tecla.Value) : null;
    }
}
