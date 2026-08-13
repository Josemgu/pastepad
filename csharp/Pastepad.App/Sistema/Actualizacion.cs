using System.Net;
using System.Net.Http;
using System.Text.Json;
using Pastepad.Nucleo;

namespace Pastepad.App.Sistema;

/// <summary>
/// Preguntarle a GitHub si hay una version mas nueva.
///
/// Solo pregunta y devuelve el dato: quien decide si eso merece un aviso
/// es <see cref="Versiones"/>, en el nucleo, donde se puede probar sin
/// red. Aqui solo esta lo que no se puede probar sin salir a internet.
///
/// No descarga nada. El boton del aviso abre la pagina de la release en
/// el navegador, y eso tiene una ventaja que no es menor: al bajar el
/// instalador con el navegador vuelve la marca de la web, asi que
/// SmartScreen sigue haciendo de red de seguridad mientras el binario no
/// este firmado.
/// </summary>
internal static class Actualizacion
{
    const string Url =
        "https://api.github.com/repos/Josemgu/pastepad/releases/latest";

    /// <summary>
    /// Corto a proposito. Esto pasa de fondo y a nadie le importa: si la
    /// red va mal, se calla y se reintenta mañana.
    /// </summary>
    static readonly TimeSpan Espera = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Uno solo para todo el proceso, que es como se usa HttpClient: uno
    /// por llamada agota los puertos del sistema.
    /// </summary>
    static readonly HttpClient _cliente = Crear();

    static HttpClient Crear()
    {
        var c = new HttpClient { Timeout = Espera };

        // El User-Agent NO es cortesia: sin el, GitHub responde 403
        // "Request forbidden by administrative rules" y lo dice en el
        // cuerpo. Medido contra la API real antes de escribir esto.
        c.DefaultRequestHeaders.Add("User-Agent", $"pastepad/{Config.Version}");

        // La version de la API, para que un cambio de la de por defecto
        // no nos cambie el formato de la respuesta por sorpresa.
        c.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        c.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

        return c;
    }

    /// <summary>Lo que hay publicado, o null si no se pudo saber.</summary>
    internal sealed record Publicada(string Version, string Pagina);

    /// <summary>
    /// Pregunta y devuelve lo publicado. Nunca lanza: un fallo de red no
    /// puede tumbar la aplicacion, y menos uno de una comprobacion que
    /// el usuario no ha pedido.
    /// </summary>
    public static async Task<Publicada?> Consultar()
    {
        try
        {
            using var respuesta = await _cliente.GetAsync(Url);

            if (!respuesta.IsSuccessStatusCode)
            {
                // 403 y 429 son el limite de peticiones de GitHub. No es
                // un fallo nuestro y no hay nada que hacer salvo esperar
                // a mañana, pero se anota: si algun dia el aviso deja de
                // funcionar, esta linea es la que lo explica.
                Registro.Anotar(
                    $"actualizaciones: GitHub respondio {(int)respuesta.StatusCode} "
                    + $"{respuesta.StatusCode}"
                    + (respuesta.StatusCode is HttpStatusCode.Forbidden
                                             or HttpStatusCode.TooManyRequests
                        ? " (limite de peticiones)"
                        : ""));

                return null;
            }

            using var flujo = await respuesta.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(flujo);

            if (!json.RootElement.TryGetProperty("tag_name", out var tag)
                || tag.GetString() is not { Length: > 0 } etiqueta)
            {
                Registro.Anotar("actualizaciones: la respuesta no traia tag_name");
                return null;
            }

            string pagina =
                json.RootElement.TryGetProperty("html_url", out var url)
                && url.GetString() is { Length: > 0 } direccion
                    ? direccion
                    : "https://github.com/Josemgu/pastepad/releases/latest";

            return new Publicada(Versiones.SinLaV(etiqueta), pagina);
        }
        catch (Exception e) when (e is HttpRequestException
                                    or TaskCanceledException
                                    or OperationCanceledException
                                    or JsonException
                                    or UriFormatException)
        {
            // Sin red, con el DNS caido o con la respuesta rota: una
            // linea y a otra cosa. Al usuario no se le enseña nada.
            Registro.Anotar(
                $"actualizaciones: no se pudo consultar ({e.GetType().Name}: "
                + $"{e.Message})");

            return null;
        }
    }
}
