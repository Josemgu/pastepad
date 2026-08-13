namespace Pastepad.Nucleo;

/// <summary>
/// Las reglas de los datos, sin nada de interfaz. Todo lo de aqui se
/// puede probar sin abrir una ventana.
/// </summary>
public static class Modelo
{
    public static Fragmento CrearFragmento(
        string texto,
        string? fuente = null,
        int? tam = null,
        int negrita = 0,
        int cursiva = 0,
        int subrayado = 0,
        string? color = null) => new()
        {
            T = texto,
            F = fuente ?? Config.FuenteDef,
            S = tam ?? Config.TamDef,
            B = negrita,
            I = cursiva,
            U = subrayado,
            C = color ?? Config.ColorDef,
        };

    public static string TextoDe(IEnumerable<Fragmento> fragmentos) =>
        string.Concat(fragmentos.Select(f => f.T));

    /// <summary>
    /// Los dos caracteres con los que puede venir partido un texto.
    ///
    /// El TextBox de WinUI devuelve el salto de linea como \r a secas
    /// —comprobado leyendo el snippets.json que escribio el programa—,
    /// mientras que lo que llega del portapapeles trae \r\n. Partir solo
    /// por \n dejaba las sesenta lineas pegadas en una.
    /// </summary>
    static readonly char[] Saltos = ['\r', '\n'];

    /// <summary>
    /// Las lineas con algo escrito, venga el texto partido como venga.
    /// Las vacias se caen: son separacion, no contenido.
    /// </summary>
    public static List<string> LineasDe(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return [];

        var salida = new List<string>();

        foreach (var linea in texto.Split(Saltos))
        {
            string limpio = linea.Trim();
            if (limpio.Length > 0) salida.Add(limpio);
        }

        return salida;
    }

    /// <summary>
    /// Deja todos los saltos en \r\n, que es como los espera Windows.
    ///
    /// Lo que sale del TextBox lleva \r a secas y asi se guardaba en
    /// snippets.json; pegado en el Bloc de notas, un texto de tres
    /// lineas salia en una sola.
    /// </summary>
    public static string NormalizarSaltos(string texto) =>
        string.IsNullOrEmpty(texto)
            ? texto
            : texto.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");

    /// <summary>
    /// La primera linea con algo escrito, con los espacios de dentro
    /// normalizados. Ni corta ni añade: lo que devuelve esta contenido
    /// tal cual en el texto del usuario.
    ///
    /// Es la que vale para GUARDAR. Para la pantalla esta
    /// <see cref="UnaLinea"/>, que marca el recorte con puntos
    /// suspensivos — y esos puntos no pueden acabar en un archivo.
    /// </summary>
    public static string PrimeraLinea(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return "";

        foreach (var linea in texto.Split(Saltos))
        {
            string limpio = string.Join(" ", linea.Split(
                (char[]?)null, StringSplitOptions.RemoveEmptyEntries));

            if (limpio.Length > 0) return limpio;
        }

        return "";
    }

    /// <summary>
    /// Un guardado nuevo a partir de lo que el usuario escribio.
    ///
    /// Vive aqui y no en cada dialogo porque el titulo es un dato que va
    /// a snippets.json y tiene que decidirse en un solo sitio. Se
    /// guardaba resumido con <see cref="UnaLinea"/>, y el resumen dejaba
    /// los puntos suspensivos DENTRO del archivo: quien leyera el titulo
    /// se encontraba un texto que el usuario nunca escribio. Acortarlo
    /// para que quepa en la fila es cosa de la interfaz, que es la unica
    /// que sabe cuanto cabe.
    ///
    /// El texto se guarda con los saltos normalizados por el mismo
    /// motivo: es el unico sitio por el que pasa lo que el usuario
    /// escribio antes de llegar al archivo.
    ///
    /// <paramref name="titulo"/> es para los marcadores. En un texto la
    /// primera linea sirve de titulo, pero en un enlace la primera linea
    /// ES la direccion, y una lista de direcciones no se puede buscar
    /// por nombre. Vacio o en blanco vuelve a la primera linea, que es
    /// lo de siempre.
    /// </summary>
    public static Snippet CrearSnippet(
        string texto, string categoria, string? titulo = null)
    {
        string limpio = NormalizarSaltos(texto);
        string nombre = (titulo ?? "").Trim();

        return new Snippet
        {
            Titulo = nombre.Length > 0 ? nombre : PrimeraLinea(limpio),
            Categoria = categoria,
            Runs = [CrearFragmento(limpio)],
        };
    }

    /// <summary>
    /// Lo mismo, pero conservando el formato que trae cada fragmento.
    /// Es por donde pasa lo que sale del editor con barra de formato.
    ///
    /// El titulo se saca del texto completo, no del primer fragmento:
    /// poner en negrita las tres primeras palabras no puede cambiar como
    /// se llama el guardado.
    /// </summary>
    public static Snippet CrearSnippet(
        IReadOnlyList<Fragmento> runs, string categoria, string? titulo = null)
    {
        var limpios = new List<Fragmento>();

        foreach (var f in runs)
        {
            string t = NormalizarSaltos(f.T);
            if (t.Length == 0) continue;

            // Fragmentos seguidos con el mismo formato son uno solo: sin
            // esto, escribir carácter a carácter dejaba un fragmento por
            // pulsación en snippets.json.
            if (limpios.Count > 0 && MismoFormato(limpios[^1], f))
            {
                limpios[^1].T += t;
                continue;
            }

            limpios.Add(Copiar(f, t));
        }

        if (limpios.Count == 0) limpios.Add(CrearFragmento(""));

        string nombre = (titulo ?? "").Trim();

        return new Snippet
        {
            Titulo = nombre.Length > 0
                ? nombre
                : PrimeraLinea(TextoDe(limpios)),
            Categoria = categoria,
            Runs = limpios,
        };
    }

    /// <summary>
    /// Resumen de una linea, para ensenar. Corta antes de separar en
    /// palabras: con textos de miles de lineas, hacerlo al reves cuesta
    /// casi un segundo.
    /// </summary>
    public static string UnaLinea(string texto, int tope = 52)
    {
        if (string.IsNullOrEmpty(texto)) return "";

        int corte = tope * 4;
        string crudo = texto.Length > corte ? texto[..corte] : texto;

        string limpio = string.Join(" ", crudo.Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (texto.Length > crudo.Length || limpio.Length > tope)
            return limpio[..Math.Min(tope, limpio.Length)] + "...";

        return limpio;
    }

    // ---------------------------------------------------------- enlaces

    /// <summary>
    /// True si el texto es una direccion web y nada mas. Solo si el
    /// texto entero es el enlace: un parrafo que menciona una url de
    /// pasada no cuenta, porque abrirlo no seria lo que el usuario
    /// espera al hacer clic.
    /// </summary>
    public static bool EsEnlace(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return false;

        string t = texto.Trim();

        // Los dos saltos: lo que sale del TextBox viene partido con \r a
        // secas, y un texto de varias lineas no es un enlace aunque la
        // primera lo parezca.
        if (t.Contains(' ') || t.IndexOfAny(Saltos) >= 0 || t.Length > 2000)
            return false;

        string bajo = t.ToLowerInvariant();

        return bajo.StartsWith("http://", StringComparison.Ordinal)
            || bajo.StartsWith("https://", StringComparison.Ordinal)
            || bajo.StartsWith("www.", StringComparison.Ordinal);
    }

    /// <summary>La direccion lista para abrir en el navegador.</summary>
    public static string UrlDe(string texto)
    {
        string t = texto.Trim();

        return t.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? "https://" + t
            : t;
    }

    /// <summary>El dominio suelto, para mostrarlo debajo del titulo.</summary>
    public static string DominioDe(string texto)
    {
        string t = UrlDe(texto);

        foreach (var prefijo in new[] { "https://", "http://" })
        {
            if (t.StartsWith(prefijo, StringComparison.Ordinal))
            {
                t = t[prefijo.Length..];
                break;
            }
        }

        if (t.StartsWith("www.", StringComparison.Ordinal))
            t = t[4..];

        string dominio = t.Split('/')[0];

        return dominio.Length > 60 ? dominio[..60] : dominio;
    }

    // ------------------------------------------- vinetas y sangrias

    /// <summary>
    /// La viñeta va **en el texto** y no como lista del editor, y es una
    /// decision, no un atajo.
    ///
    /// Una lista de verdad es formato de parrafo —ITextParagraphFormat
    /// .ListType—, y lo que guarda un <see cref="Fragmento"/> es formato
    /// de caracter: fuente, tamaño, negrita, cursiva, subrayado y color.
    /// Guardar listas obligaria a cambiar el formato de snippets.json y a
    /// reescribir el generador de RTF, y encima dejaria sin respuesta que
    /// texto plano produce una lista al pegar sin formato.
    ///
    /// Con la viñeta dentro del texto, lo que se ve es lo que se guarda,
    /// lo que se pega con formato y lo que se pega sin el. Y "Quitar
    /// numeración y viñetas" del dialogo de listas ya sabe deshacerlo.
    /// </summary>
    public const string Vineta = "• ";

    static string[] Lineas(string bloque) => bloque.Split('\r');

    static bool ConVineta(string linea) =>
        linea.StartsWith(Vineta, StringComparison.Ordinal);

    /// <summary>El "12. " del principio, o 0 si no lo lleva.</summary>
    static int LargoDelNumero(string linea)
    {
        int i = 0;
        while (i < linea.Length && char.IsAsciiDigit(linea[i])) i++;

        if (i == 0 || i + 1 >= linea.Length) return 0;

        return linea[i] == '.' && linea[i + 1] == ' ' ? i + 2 : 0;
    }

    static string SinMarca(string linea) =>
        ConVineta(linea) ? linea[Vineta.Length..]
                         : linea[LargoDelNumero(linea)..];

    /// <summary>
    /// Pone viñetas a las lineas del bloque, o se las quita si ya las
    /// llevan todas. Alternar y no solo poner: es lo que hace el boton de
    /// viñetas en cualquier editor.
    /// </summary>
    public static string AlternarVinetas(string bloque)
    {
        var lineas = Lineas(bloque);
        var conAlgo = lineas.Where(l => l.Trim().Length > 0).ToList();

        if (conAlgo.Count == 0) return bloque;

        bool quitar = conAlgo.All(ConVineta);

        return string.Join('\r', lineas.Select(l =>
            l.Trim().Length == 0 ? l : quitar ? SinMarca(l) : Vineta + SinMarca(l)));
    }

    /// <summary>Lo mismo numerando desde 1.</summary>
    public static string AlternarNumeros(string bloque)
    {
        var lineas = Lineas(bloque);
        var conAlgo = lineas.Where(l => l.Trim().Length > 0).ToList();

        if (conAlgo.Count == 0) return bloque;

        bool quitar = conAlgo.All(l => LargoDelNumero(l) > 0);

        int n = 0;

        return string.Join('\r', lineas.Select(l =>
        {
            if (l.Trim().Length == 0) return l;
            if (quitar) return SinMarca(l);

            n++;
            return $"{n}. " + SinMarca(l);
        }));
    }

    /// <summary>
    /// Una tabulacion al principio de cada linea, o una menos. Con
    /// tabulador y no con espacios porque el generador de RTF ya lo
    /// convierte en \tab, asi que la sangria llega igual a Outlook y al
    /// Bloc de notas.
    /// </summary>
    public static string Sangrar(string bloque, bool mas) =>
        string.Join('\r', Lineas(bloque).Select(l =>
            l.Trim().Length == 0 ? l
            : mas ? "\t" + l
            : l.StartsWith('\t') ? l[1..] : l));

    // -------------------------------------------- la carpeta en lineas

    /// <summary>
    /// Los guardados de una carpeta repartidos entre los que caben en
    /// una linea y los que no.
    ///
    /// El editor de carpeta ensena una nota por linea, asi que una nota
    /// que ya lleva saltos dentro no se puede representar ahi: partirla
    /// la convertiria en varias notas sin que nadie lo haya pedido. Se
    /// quedan fuera de la caja y fuera de peligro, y el dialogo dice
    /// cuantas son.
    /// </summary>
    public sealed record CarpetaEnLineas(
        List<Snippet> DeUnaLinea, List<Snippet> DeVariasLineas)
    {
        /// <summary>Lo que se carga en la caja: una nota por linea.</summary>
        public string Texto =>
            string.Join("\r\n", DeUnaLinea.Select(s => TextoDe(s.Runs).Trim()));
    }

    public static CarpetaEnLineas PartirCarpeta(IReadOnlyList<Snippet> dentro)
    {
        var unas = new List<Snippet>();
        var varias = new List<Snippet>();

        foreach (var s in dentro)
        {
            string t = TextoDe(s.Runs).Trim();
            (t.IndexOfAny(Saltos) >= 0 ? varias : unas).Add(s);
        }

        return new CarpetaEnLineas(unas, varias);
    }

    /// <summary>
    /// Lo que quedaria en la carpeta con el texto que hay en la caja.
    /// <paramref name="Quitadas"/> son las notas que desaparecen: es el
    /// numero que hay que ensenar antes de guardar.
    /// </summary>
    public sealed record FusionCarpeta(
        List<Snippet> Resultado,
        List<Snippet> Quitadas,
        int Conservadas,
        int Nuevas);

    /// <summary>
    /// Rearma la carpeta a partir del texto editado, una nota por linea.
    ///
    /// **Las lineas que no cambiaron reutilizan su misma nota**, el
    /// objeto tal cual y no una copia. Con 3000 lineas, rehacerlas todas
    /// seria tirar el nombre propio de cada marcador y el formato de cada
    /// una para arreglar las cien que el usuario si toco. Solo se crea
    /// nota nueva para lo que no estaba antes.
    ///
    /// El emparejamiento va por texto exacto y en cola: dos notas con el
    /// mismo texto son dos notas, y la segunda linea repetida tiene que
    /// quedarse con la segunda nota, no con la primera otra vez.
    /// </summary>
    public static FusionCarpeta FusionarCarpeta(
        CarpetaEnLineas antes, string editado, string carpeta)
    {
        var porTexto = new Dictionary<string, Queue<Snippet>>(StringComparer.Ordinal);

        foreach (var s in antes.DeUnaLinea)
        {
            string t = TextoDe(s.Runs).Trim();

            if (!porTexto.TryGetValue(t, out var cola))
                porTexto[t] = cola = new Queue<Snippet>();

            cola.Enqueue(s);
        }

        var resultado = new List<Snippet>();
        int conservadas = 0;
        int nuevas = 0;

        foreach (var linea in LineasDe(editado))
        {
            if (porTexto.TryGetValue(linea, out var cola) && cola.Count > 0)
            {
                resultado.Add(cola.Dequeue());
                conservadas++;
                continue;
            }

            resultado.Add(CrearSnippet(linea, carpeta));
            nuevas++;
        }

        var quitadas = porTexto.Values.SelectMany(c => c).ToList();

        // Las de varias lineas nunca estuvieron en la caja, asi que no
        // pueden haberse borrado ahi. Vuelven intactas y al final.
        resultado.AddRange(antes.DeVariasLineas);

        return new FusionCarpeta(resultado, quitadas, conservadas, nuevas);
    }

    // ------------------------------------------------------- plantillas

    /// <summary>
    /// Los [[campos]] de una plantilla, en orden y sin repetir.
    /// </summary>
    public static List<string> CamposDe(string texto)
    {
        var campos = new List<string>();
        string resto = texto;

        while (true)
        {
            int i = resto.IndexOf("[[", StringComparison.Ordinal);
            if (i < 0) break;

            int j = resto.IndexOf("]]", i, StringComparison.Ordinal);

            // La version en Python no comprobaba esto y reventaba con un
            // texto como "]] [[x", donde el cierre va antes que la
            // apertura. Aqui simplemente se deja de buscar.
            if (j < 0) break;

            string nombre = resto[(i + 2)..j].Trim();

            if (nombre.Length > 0 && !campos.Contains(nombre))
                campos.Add(nombre);

            resto = resto[(j + 2)..];
        }

        return campos;
    }

    /// <summary>Todo menos el texto.</summary>
    public static bool MismoFormato(Fragmento a, Fragmento b) =>
        a.F == b.F && a.S == b.S && a.B == b.B
        && a.I == b.I && a.U == b.U
        && string.Equals(a.C, b.C, StringComparison.OrdinalIgnoreCase);

    static Fragmento Copiar(Fragmento f, string texto) => new()
    {
        T = texto,
        F = f.F,
        S = f.S,
        B = f.B,
        I = f.I,
        U = f.U,
        C = f.C,
    };

    /// <summary>
    /// Sustituye los [[campos]] por sus valores conservando el formato.
    ///
    /// Trabaja sobre el texto entero y no fragmento a fragmento. Desde
    /// que hay barra de formato, un guardado puede tener varios
    /// fragmentos y el corte cae donde el usuario haya puesto negrita:
    /// poner en negrita solo "nombre" dentro de "[[nombre]]" partia el
    /// campo en tres, ningun fragmento contenia "[[nombre]]" entero, y lo
    /// que se pegaba llevaba los corchetes dentro.
    ///
    /// El valor toma el formato del fragmento donde empieza el campo.
    /// </summary>
    public static List<Fragmento> Rellenar(
        IEnumerable<Fragmento> fragmentos,
        IReadOnlyDictionary<string, string> valores)
    {
        // Los vacios estorban al recorrido y no aportan nada.
        var trozos = fragmentos.Where(f => f.T.Length > 0).ToList();
        if (trozos.Count == 0) return [];

        string todo = TextoDe(trozos);

        var campos = new List<(int Inicio, int Largo, string Valor)>();
        int busca = 0;

        while (true)
        {
            int a = todo.IndexOf("[[", busca, StringComparison.Ordinal);
            if (a < 0) break;

            int b = todo.IndexOf("]]", a, StringComparison.Ordinal);
            if (b < 0) break;

            if (valores.TryGetValue(todo[(a + 2)..b].Trim(), out var valor))
                campos.Add((a, b + 2 - a, valor));

            busca = b + 2;
        }

        if (campos.Count == 0) return [.. trozos.Select(f => Copiar(f, f.T))];

        var piezas = new List<(int Cual, string Texto)>();

        int cual = 0;    // fragmento en el que va el recorrido
        int usado = 0;   // caracteres ya consumidos de ese fragmento
        int donde = 0;   // posicion dentro de `todo`

        void Avanzar(int cuantos, bool emitir)
        {
            while (cuantos > 0)
            {
                if (usado == trozos[cual].T.Length) { cual++; usado = 0; }

                int coge = Math.Min(cuantos, trozos[cual].T.Length - usado);

                if (emitir) piezas.Add((cual, trozos[cual].T.Substring(usado, coge)));

                usado += coge;
                cuantos -= coge;
                donde += coge;
            }
        }

        foreach (var (inicio, largo, valor) in campos)
        {
            Avanzar(inicio - donde, true);

            // El formato del valor es el del sitio donde empieza el
            // campo, no el de donde acaba.
            if (usado == trozos[cual].T.Length && cual + 1 < trozos.Count)
            {
                cual++;
                usado = 0;
            }

            piezas.Add((cual, valor));
            Avanzar(largo, false);
        }

        Avanzar(todo.Length - donde, true);

        var salida = new List<Fragmento>();

        foreach (var (indice, texto) in piezas)
        {
            if (texto.Length == 0) continue;

            // Piezas seguidas con el mismo formato vuelven a ser una
            // sola: si no, cada pegado multiplicaria los fragmentos.
            if (salida.Count > 0 && MismoFormato(salida[^1], trozos[indice]))
            {
                salida[^1].T += texto;
                continue;
            }

            salida.Add(Copiar(trozos[indice], texto));
        }

        return salida;
    }
}
