namespace Pastepad.Nucleo;

/// <summary>
/// Poner una fecha guardada en palabras, para la linea de debajo del
/// titulo de un apunte.
///
/// Vive en el nucleo y recibe el «ahora» como parametro por lo mismo de
/// siempre: asi se puede probar sin depender de que hora sea al correr
/// las pruebas.
/// </summary>
public static class Fechas
{
    /// <summary>
    /// «ahora mismo», «hace 5 min», «ayer 09:30», o la fecha corta si ya
    /// hace dias.
    ///
    /// Una cadena que no se pueda interpretar devuelve vacio en vez de
    /// lanzar: notas.json se puede editar a mano, y una fecha rota no
    /// puede tumbar la lista entera — como mucho, esa fila se queda sin
    /// su linea de abajo.
    /// </summary>
    public static string Legible(string? iso, DateTimeOffset ahora)
    {
        if (!DateTimeOffset.TryParse(iso, out var cuando)) return "";

        var pasado = ahora - cuando;

        // Una fecha del futuro no es un caso que valga la pena distinguir
        // —un reloj mal puesto, un archivo copiado de otra maquina—, pero
        // si hay que evitar que salga «hace -40 minutos».
        if (pasado < TimeSpan.FromMinutes(1)) return Textos.T("ahora mismo");

        if (pasado < TimeSpan.FromHours(1))
            return Textos.T("hace %d min", (int)pasado.TotalMinutes);

        var dia = cuando.ToLocalTime().Date;
        var hoy = ahora.ToLocalTime().Date;

        // Por dias del calendario y no por horas transcurridas: a las
        // 00:30, algo de las 23:00 es «ayer» aunque haga hora y media.
        if (dia == hoy) return Textos.T("hoy %s", Hora(cuando));
        if (dia == hoy.AddDays(-1)) return Textos.T("ayer %s", Hora(cuando));

        return cuando.ToLocalTime().ToString("d");
    }

    static string Hora(DateTimeOffset cuando) =>
        cuando.ToLocalTime().ToString("t");
}
