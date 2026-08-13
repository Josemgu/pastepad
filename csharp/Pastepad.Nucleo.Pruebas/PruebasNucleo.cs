using Pastepad.Nucleo;

namespace Pastepad.Nucleo.Pruebas;

/// <summary>
/// Las 19 pruebas de prueba.py, traducidas una por una y con el mismo
/// nombre. Son la especificacion ejecutable del modelo: si estas pasan,
/// la logica esta portada.
///
/// Corren sin abrir ninguna ventana y sin el Windows App SDK. Esa es la
/// ventaja de tener el nucleo separado de la interfaz, y es lo que
/// permitio migrar de tkinter a Flet reutilizando 906 lineas.
/// </summary>
public abstract class BaseConCarpetaTemporal
{
    protected string Temporal { get; private set; } = "";
    protected Rutas Rutas { get; private set; } = null!;

    [TestInitialize]
    public void Preparar()
    {
        Temporal = Path.Combine(
            Path.GetTempPath(), "pastepad-pruebas-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Temporal);
        Rutas = Rutas.EnCarpeta(Temporal);
    }

    [TestCleanup]
    public void Limpiar()
    {
        try
        {
            if (Directory.Exists(Temporal)) Directory.Delete(Temporal, true);
        }
        catch (IOException)
        {
            // Una carpeta temporal que sobrevive no invalida la prueba.
        }
    }
}

[TestClass]
public sealed class PruebaModelo : BaseConCarpetaTemporal
{
    Almacen Nuevo() => new(Rutas);

    [TestMethod]
    public void test_crear_y_borrar_carpeta()
    {
        var a = Nuevo();

        a.CrearCarpeta("Trabajo");

        a.AnadirSnippet(new Snippet
        {
            Titulo = "uno",
            Categoria = "Trabajo",
            Runs = [Modelo.CrearFragmento("hola")],
        });

        a.AnadirSnippet(new Snippet
        {
            Titulo = "dos",
            Categoria = "Trabajo",
            Runs = [Modelo.CrearFragmento("adios")],
        });

        Assert.HasCount(2, a.ContenidoDe("Trabajo"));

        int borrados = a.BorrarCarpeta("Trabajo");

        Assert.AreEqual(2, borrados);
        CollectionAssert.DoesNotContain(a.Carpetas, "Trabajo");
        Assert.IsEmpty(a.Snippets);
    }

    [TestMethod]
    public void test_renombrar_arrastra_contenido()
    {
        var a = Nuevo();

        a.CrearCarpeta("Vieja");

        a.AnadirSnippet(new Snippet
        {
            Titulo = "x",
            Categoria = "Vieja",
            Runs = [Modelo.CrearFragmento("y")],
        });

        Assert.IsTrue(a.RenombrarCarpeta("Vieja", "Nueva"));
        Assert.AreEqual("Nueva", a.Snippets[0].Categoria);
    }

    [TestMethod]
    public void test_no_renombrar_a_uno_existente()
    {
        var a = Nuevo();

        a.CrearCarpeta("A");
        a.CrearCarpeta("B");

        Assert.IsFalse(a.RenombrarCarpeta("A", "B"));
    }

    [TestMethod]
    public void test_fijados_sobreviven_al_recorte()
    {
        var a = Nuevo();

        var fijo = new Entrada
        {
            Tipo = Entrada.Texto_,
            Texto = "importante",
            Pin = true,
        };

        a.Hist.Add(fijo);

        for (int i = 0; i < Config.MaxHist + 20; i++)
        {
            a.Anotar(new Entrada
            {
                Tipo = Entrada.Texto_,
                Texto = $"relleno {i}",
            });
        }

        CollectionAssert.Contains(a.Hist, fijo);

        int libres = a.Hist.Count(x => !x.Pin);
        Assert.IsLessThanOrEqualTo(Config.MaxHist, libres,
            $"quedaron {libres} sueltas, el tope es {Config.MaxHist}");
    }

    [TestMethod]
    public void test_vaciar_respeta_fijados()
    {
        var a = Nuevo();

        a.Hist =
        [
            new Entrada { Tipo = Entrada.Texto_, Texto = "a", Pin = true },
            new Entrada { Tipo = Entrada.Texto_, Texto = "b" },
        ];

        a.VaciarHistorial();

        Assert.HasCount(1, a.Hist);
    }

    [TestMethod]
    public void test_no_repite_lo_recien_copiado()
    {
        var a = Nuevo();

        Assert.IsTrue(a.Anotar(
            new Entrada { Tipo = Entrada.Texto_, Texto = "igual" }));

        Assert.IsFalse(a.Anotar(
            new Entrada { Tipo = Entrada.Texto_, Texto = "igual" }));
    }

    [TestMethod]
    public void test_escritura_atomica()
    {
        var a = Nuevo();
        a.CrearCarpeta("Persistente");

        var otro = Nuevo();

        CollectionAssert.Contains(otro.Carpetas, "Persistente");
    }
}

[TestClass]
public sealed class PruebaPlantillas
{
    [TestMethod]
    public void test_campos_en_orden_sin_repetir()
    {
        const string t = "Hola [[nombre]], sobre [[tema]] y otra vez [[nombre]].";

        CollectionAssert.AreEqual(
            new[] { "nombre", "tema" }, Modelo.CamposDe(t));
    }

    [TestMethod]
    public void test_rellenar()
    {
        var f = new[] { Modelo.CrearFragmento("Hola [[nombre]]") };

        var r = Modelo.Rellenar(f, new Dictionary<string, string>
        {
            ["nombre"] = "Ana",
        });

        Assert.AreEqual("Hola Ana", Modelo.TextoDe(r));
    }

    [TestMethod]
    public void test_una_linea_corta_bien()
    {
        string largo = string.Concat(Enumerable.Repeat("palabra ", 5000));

        Assert.IsLessThanOrEqualTo(43, Modelo.UnaLinea(largo, 40).Length);
    }
}

[TestClass]
public sealed class PruebaBusqueda : BaseConCarpetaTemporal
{
    [TestMethod]
    public void test_ignora_tildes()
    {
        Assert.AreEqual("informacion", Busqueda.Normalizar("información"));
    }

    [TestMethod]
    public void test_palabras_en_cualquier_orden()
    {
        var p = Busqueda.Puntuar(["rep", "men"], "reporte mensual", "");

        Assert.IsNotNull(p);
    }

    [TestMethod]
    public void test_titulo_pesa_mas_que_cuerpo()
    {
        var enTitulo = Busqueda.Puntuar(["pago"], "pago pendiente", "");
        var enCuerpo = Busqueda.Puntuar(["pago"], "otra cosa", "hay un pago aqui");

        Assert.IsNotNull(enTitulo);
        Assert.IsNotNull(enCuerpo);
        Assert.IsGreaterThan(enCuerpo.Value, enTitulo.Value,
            $"titulo {enTitulo} no supera a cuerpo {enCuerpo}");
    }

    [TestMethod]
    public void test_no_coincide_devuelve_none()
    {
        Assert.IsNull(Busqueda.Puntuar(["zzz"], "hola", "mundo"));
    }

    [TestMethod]
    public void test_indice_se_rehace_al_invalidar()
    {
        var a = new Almacen(Rutas);
        var idx = new Indice(a);

        Assert.AreEqual(0, idx.Cuantas());

        a.AnadirSnippet(new Snippet
        {
            Titulo = "Reporte mensual",
            Categoria = "W",
            Runs = [Modelo.CrearFragmento("cifras")],
        });

        idx.Invalidar();

        Assert.HasCount(1, idx.Buscar("rep men"));
        Assert.IsEmpty(idx.Buscar("inexistente"));
    }
}

/// <summary>
/// Estas no vienen de prueba.py. Guardan la promesa de que el JSON que
/// escribimos es el mismo que leia la v3.0.1: se escribio una vez
/// "EsImagen": false en cada entrada —una propiedad calculada que se
/// colo en el archivo— y la comprobacion de ida y vuelta no lo vio,
/// porque comparaba los campos ya parseados y no las claves crudas.
/// </summary>
[TestClass]
public sealed class PruebaFormatoDelArchivo : BaseConCarpetaTemporal
{
    static HashSet<string> ClavesDe(System.Text.Json.JsonElement e) =>
        [.. e.EnumerateObject().Select(p => p.Name)];

    [TestMethod]
    public void historial_solo_lleva_las_claves_de_siempre()
    {
        var a = new Almacen(Rutas);

        a.Anotar(new Entrada { Tipo = Entrada.Texto_, Texto = "suelta" });
        a.Anotar(new Entrada { Tipo = Entrada.Texto_, Texto = "fijada" });
        a.Fijar(a.Hist[0]);
        a.Volcar(forzar: true);

        using var doc = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Rutas.Historial));

        var entradas = doc.RootElement.EnumerateArray().ToList();
        Assert.HasCount(2, entradas);

        var fijada = entradas.First(e => ClavesDe(e).Contains("pin"));
        var suelta = entradas.First(e => !ClavesDe(e).Contains("pin"));

        CollectionAssert.AreEquivalent(
            new[] { "tipo", "texto", "pin" }, ClavesDe(fijada).ToArray());

        // Sin pin cuando es false, igual que hacia el JSON de Python,
        // donde la clave sencillamente no estaba.
        CollectionAssert.AreEquivalent(
            new[] { "tipo", "texto" }, ClavesDe(suelta).ToArray());
    }

    [TestMethod]
    public void una_imagen_lleva_ruta_y_no_texto()
    {
        var a = new Almacen(Rutas);

        a.Anotar(new Entrada { Tipo = Entrada.Imagen, Ruta = @"C:\x\img.bmp" });
        a.Volcar(forzar: true);

        using var doc = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Rutas.Historial));

        CollectionAssert.AreEquivalent(
            new[] { "tipo", "ruta" },
            ClavesDe(doc.RootElement.EnumerateArray().First()).ToArray());
    }

    [TestMethod]
    public void un_snippet_lleva_titulo_categoria_y_runs()
    {
        var a = new Almacen(Rutas);

        a.AnadirSnippet(new Snippet
        {
            Titulo = "Plantilla",
            Categoria = "Trabajo",
            Runs = [Modelo.CrearFragmento("Hola")],
        });

        using var doc = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Rutas.Datos));

        CollectionAssert.AreEquivalent(
            new[] { "categorias", "snippets" },
            ClavesDe(doc.RootElement).ToArray());

        var snippet = doc.RootElement.GetProperty("snippets")
                                     .EnumerateArray().First();

        CollectionAssert.AreEquivalent(
            new[] { "titulo", "categoria", "runs" }, ClavesDe(snippet).ToArray());

        // Las claves de una letra son las que hacen que el archivo no
        // engorde, y las que ya escribia la version en Python.
        CollectionAssert.AreEquivalent(
            new[] { "t", "f", "s", "b", "i", "u", "c" },
            ClavesDe(snippet.GetProperty("runs").EnumerateArray().First()).ToArray());
    }
}

/// <summary>
/// Un archivo que existe y no se puede leer NO se sobrescribe.
///
/// Viene de un fallo real: una instancia arranco sin poder acceder a su
/// carpeta, enseño un historial vacio y al cerrarse guardo ese vacio
/// encima de las entradas de verdad. Sin un solo mensaje, porque el
/// registro de errores vivia en la misma carpeta rota.
/// </summary>
[TestClass]
public sealed class PruebaNoPisarLoIlegible : BaseConCarpetaTemporal
{
    const string Corrupto = "{ esto no es JSON valido";

    [TestMethod]
    public void un_historial_ilegible_no_se_sobrescribe()
    {
        File.WriteAllText(Rutas.Historial, Corrupto);

        var a = new Almacen(Rutas);

        Assert.IsTrue(a.LecturaIncompleta);
        Assert.IsNotNull(a.Problema);

        a.Anotar(new Entrada { Tipo = Entrada.Texto_, Texto = "nueva" });
        a.Volcar(forzar: true);

        Assert.AreEqual(Corrupto, File.ReadAllText(Rutas.Historial),
            "se piso un archivo que no se pudo leer");
    }

    [TestMethod]
    public void unos_snippets_ilegibles_no_se_sobrescriben()
    {
        File.WriteAllText(Rutas.Datos, Corrupto);

        var a = new Almacen(Rutas);
        a.CrearCarpeta("Trabajo");

        Assert.AreEqual(Corrupto, File.ReadAllText(Rutas.Datos));
    }

    [TestMethod]
    public void el_archivo_sano_si_se_escribe_aunque_otro_este_roto()
    {
        File.WriteAllText(Rutas.Historial, Corrupto);

        var a = new Almacen(Rutas);
        a.CrearCarpeta("Trabajo");

        // Solo se protege lo que no se pudo leer. Bloquearlo todo
        // convertiria un archivo roto en un programa inutil.
        CollectionAssert.Contains(new Almacen(Rutas).Carpetas, "Trabajo");
    }

    [TestMethod]
    public void sin_archivos_no_hay_problema_que_avisar()
    {
        var a = new Almacen(Rutas);

        // Primer arranque: que no existan es lo normal, no un fallo.
        Assert.IsFalse(a.LecturaIncompleta);
        Assert.IsNull(a.Problema);
    }

    /// <summary>
    /// El agujero que dejaba File.Exists: la referencia de .NET avisa de
    /// que devuelve false cuando no hay permiso para leer, en vez de
    /// lanzar. Con la pregunta delante, un archivo denegado pasaba por
    /// "primer arranque" y no se marcaba: el programa cargaba vacio y a
    /// partir de ahi escribia encima del bueno, en silencio.
    ///
    /// Aqui la denegacion se monta con una carpeta que se llama como el
    /// archivo. No es un rodeo caprichoso: medido, File.Exists devuelve
    /// **false** sobre ella y abrirla da UnauthorizedAccessException con
    /// HResult 0x80070005, exactamente lo mismo que un permiso denegado
    /// de verdad. Y sale igual en cualquier maquina, sin tocar ACLs ni
    /// pedir elevacion.
    /// </summary>
    [TestMethod]
    public void un_historial_que_existe_y_no_se_deja_leer_se_marca()
    {
        var incidencias = new List<string>();

        Directory.CreateDirectory(Rutas.Historial);

        var a = new Almacen(Rutas, (donde, _) => incidencias.Add(donde));

        Assert.IsTrue(a.LecturaIncompleta,
            "File.Exists mintio y se tomo por primer arranque");

        Assert.IsNotNull(a.Problema);
        Assert.IsNotEmpty(incidencias, "y ademas sin dejar una linea");

        // Y con la marca puesta, no se escribe encima.
        incidencias.Clear();

        a.Anotar(new Entrada { Tipo = Entrada.Texto_, Texto = "de esta sesion" });
        a.Volcar(forzar: true);

        Assert.IsTrue(
            incidencias.Any(x => x.Contains("no se escribe")),
            "se escribio sobre lo que no se pudo leer");

        Assert.IsFalse(File.Exists(Rutas.Historial),
            "se creo un historial nuevo encima de lo que habia");
    }

    /// <summary>
    /// El otro camino: el archivo esta abierto por otro y no se puede
    /// compartir. Cubre el reintento —tres pasadas de 100 ms, por si es
    /// la instancia anterior terminando de morirse— y que, si aun asi no
    /// entra, lo del usuario se queda como estaba.
    /// </summary>
    [TestMethod]
    public void un_historial_que_no_se_puede_abrir_no_se_sobrescribe()
    {
        const string bueno = """[{"tipo":"texto","texto":"lo del usuario"}]""";

        File.WriteAllText(Rutas.Historial, bueno);

        var incidencias = new List<string>();

        using (new FileStream(Rutas.Historial, FileMode.Open,
                              FileAccess.Read, FileShare.None))
        {
            var a = new Almacen(Rutas, (donde, _) => incidencias.Add(donde));

            Assert.IsTrue(a.LecturaIncompleta,
                "un archivo bloqueado tiene que marcarse como ilegible");

            Assert.IsNotNull(a.Problema);
            Assert.IsEmpty(a.Hist);

            a.Anotar(new Entrada { Tipo = Entrada.Texto_, Texto = "de esta sesion" });
            a.Volcar(forzar: true);
        }

        Assert.AreEqual(bueno, File.ReadAllText(Rutas.Historial),
            "se piso el historial del usuario con el vacio de esta sesion");

        // Y no en silencio: el reintento y el abandono dejan linea.
        Assert.IsNotEmpty(incidencias);
    }

    /// <summary>
    /// El otro lado de lo mismo: que no exista sigue siendo primer
    /// arranque y se escribe con toda normalidad. Si el arreglo de arriba
    /// se pasara de prudente, el programa no guardaria nunca nada.
    /// </summary>
    [TestMethod]
    public void un_archivo_que_no_existe_sigue_siendo_primer_arranque()
    {
        var incidencias = new List<string>();

        var a = new Almacen(Rutas, (donde, _) => incidencias.Add(donde));

        Assert.IsFalse(a.LecturaIncompleta);
        Assert.IsEmpty(incidencias, "un primer arranque no es una incidencia");

        a.Anotar(new Entrada { Tipo = Entrada.Texto_, Texto = "la primera" });
        a.Volcar(forzar: true);

        CollectionAssert.Contains(
            new Almacen(Rutas).Hist.Select(x => x.Texto).ToList(), "la primera");
    }
}

[TestClass]
public sealed class PruebaTituloGuardado : BaseConCarpetaTemporal
{
    const string Largo =
        "Hola [[nombre]], tu cita es el [[dia]] a las [[hora]]. "
        + "Gracias por confiar en nosotros y hasta pronto.";

    /// <summary>
    /// Lo que se guarda tiene que estar contenido en lo que el usuario
    /// escribio. Se guardaba el resumen de pantalla, y los puntos
    /// suspensivos acababan dentro de snippets.json: "Hola [[nombre]],
    /// tu cita es el [[dia]] a las [[hora]]. Graci...". Un titulo que el
    /// usuario nunca escribio, en su archivo.
    /// </summary>
    [TestMethod]
    public void test_el_titulo_guardado_no_lleva_puntos_suspensivos()
    {
        var s = Modelo.CrearSnippet(Largo, "Trabajo");

        Assert.DoesNotContain("...", s.Titulo);

        Assert.Contains(s.Titulo, Largo,
            "el titulo guardado no es un trozo literal del texto");
    }

    /// <summary>Y sobrevive intacto al viaje por el disco.</summary>
    [TestMethod]
    public void test_el_titulo_sobrevive_al_guardado_y_la_relectura()
    {
        var a = new Almacen(Rutas);
        a.AnadirSnippet(Modelo.CrearSnippet(Largo, "Trabajo"));

        var leido = new Almacen(Rutas).Snippets.Single();

        Assert.AreEqual(Largo, leido.Titulo);
        Assert.AreEqual(Largo, Modelo.TextoDe(leido.Runs));

        Assert.DoesNotContain("...", File.ReadAllText(Rutas.Datos));
    }

    /// <summary>
    /// Solo la primera linea, y sin los saltos de linea dentro: un
    /// titulo de varias lineas rompe la fila.
    /// </summary>
    [TestMethod]
    public void test_solo_la_primera_linea_con_algo_escrito()
    {
        Assert.AreEqual("la primera",
            Modelo.PrimeraLinea("\n  \n  la   primera  \nla segunda\n"));

        Assert.AreEqual("", Modelo.PrimeraLinea(""));
        Assert.AreEqual("", Modelo.PrimeraLinea("   \n\t\n"));
    }

    /// <summary>
    /// Y el resumen de pantalla sigue marcando el corte: quitarle los
    /// puntos suspensivos al guardado no puede quitarselos a la fila.
    /// </summary>
    [TestMethod]
    public void test_el_resumen_de_pantalla_si_marca_el_corte()
    {
        Assert.EndsWith("...", Modelo.UnaLinea(Largo, 40));
    }
}

[TestClass]
public sealed class PruebaEnlaces
{
    [TestMethod]
    public void test_reconoce_direcciones()
    {
        foreach (var t in new[]
        {
            "https://github.com/x",
            "http://localhost:8000",
            "www.google.com",
            "  https://x.com  ",
        })
        {
            Assert.IsTrue(Modelo.EsEnlace(t), t);
        }
    }

    [TestMethod]
    public void test_ignora_texto_con_enlace_dentro()
    {
        // Un parrafo que menciona una url no debe abrirse al hacer clic.
        Assert.IsFalse(Modelo.EsEnlace("mira esto https://x.com"));
        Assert.IsFalse(Modelo.EsEnlace("texto normal"));
        Assert.IsFalse(Modelo.EsEnlace(""));
    }

    [TestMethod]
    public void test_completa_el_esquema()
    {
        Assert.AreEqual("https://www.google.com", Modelo.UrlDe("www.google.com"));
        Assert.AreEqual("https://x.com", Modelo.UrlDe("https://x.com"));
    }

    [TestMethod]
    public void test_dominio_limpio()
    {
        Assert.AreEqual("github.com",
            Modelo.DominioDe("https://www.github.com/a/b"));

        Assert.AreEqual("localhost:8000",
            Modelo.DominioDe("http://localhost:8000/x"));
    }
}

/// <summary>
/// Fuera del paralelismo del resto: <see cref="Textos.Idioma"/> es
/// estatico —lo pone la aplicacion una vez al arrancar— y dos pruebas
/// cambiandolo a la vez se pisan. Costo dos fallos que no se repetian.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PruebaTextos
{
    [TestCleanup]
    public void Limpiar() => Textos.Idioma = Textos.IdiomaDef;

    /// <summary>
    /// Las tres tablas, clave por clave. Es lo unico que este diseño no
    /// detecta solo: una clave que falte en frances no rompe nada ni
    /// avisa de nada, sale la frase en espaniol en mitad de una francesa.
    /// Y una que sobre es peso muerto que nadie vuelve a mirar.
    /// </summary>
    [TestMethod]
    public void test_los_tres_idiomas_tienen_las_mismas_claves()
    {
        var en = Textos.Tablas["en"].Keys.ToHashSet();

        foreach (var idioma in (string[])["pt", "fr"])
        {
            var otro = Textos.Tablas[idioma].Keys.ToHashSet();

            Assert.IsEmpty(en.Except(otro),
                $"a _{idioma} le faltan claves que si tiene _en");

            Assert.IsEmpty(otro.Except(en),
                $"_{idioma} tiene claves que _en no tiene");
        }
    }

    /// <summary>
    /// Ninguna traduccion puede pedir mas huecos que su clave: string
    /// .Format lanza FormatException si le faltan valores, y seria en
    /// marcha y solo en ese idioma.
    /// </summary>
    [TestMethod]
    public void test_ninguna_traduccion_pide_huecos_de_mas()
    {
        static int Huecos(string s) =>
            s.Split("%s").Length - 1 + s.Split("%d").Length - 1;

        foreach (var (idioma, tabla) in Textos.Tablas)
        {
            foreach (var (clave, valor) in tabla)
            {
                Assert.IsLessThanOrEqualTo(Huecos(clave), Huecos(valor),
                    $"_{idioma}['{clave}'] = '{valor}'");
            }
        }
    }

    /// <summary>
    /// Los huecos se numeran en el orden en que aparecen. Antes todos
    /// eran {0} y esta frase salia como "la carpeta 3 y sus 3 textos".
    /// </summary>
    [TestMethod]
    public void test_dos_huecos_reciben_valores_distintos()
    {
        Assert.AreEqual(
            "¿Eliminar la carpeta Trabajo y sus 3 textos? "
            + "Esto no se puede deshacer.",
            Textos.T("¿Eliminar la carpeta %s y sus %d textos? "
                     + "Esto no se puede deshacer.", "Trabajo", 3));

        Textos.Idioma = "en";

        Assert.AreEqual("Delete folder Trabajo and its 3 texts? "
                        + "This cannot be undone.",
            Textos.T("¿Eliminar la carpeta %s y sus %d textos? "
                     + "Esto no se puede deshacer.", "Trabajo", 3));
    }

    /// <summary>
    /// Una llave en el texto es literal. Las carpetas las nombra el
    /// usuario y nada le impide llamar a una "{plantillas}".
    /// </summary>
    [TestMethod]
    public void test_una_llave_en_el_texto_no_revienta()
    {
        Assert.AreEqual("¿Eliminar la carpeta {raro}?",
            Textos.T("¿Eliminar la carpeta %s?", "{raro}"));
    }

    /// <summary>
    /// Las tildes y los signos de apertura, que se habian perdido en los
    /// cuatro idiomas. Venia heredado de la version en Python, donde los
    /// nombres de idioma ya estaban sin acentuar; la regla de "sin tildes
    /// en el codigo" es para identificadores y comentarios, no para lo
    /// que lee el usuario.
    /// </summary>
    [TestMethod]
    public void test_los_textos_llevan_sus_tildes()
    {
        Assert.AreEqual("Español", Textos.Nombres["es"]);
        Assert.AreEqual("Português", Textos.Nombres["pt"]);
        Assert.AreEqual("Français", Textos.Nombres["fr"]);

        // Espaniol: la clave ES el texto, asi que se comprueba la clave.
        foreach (var esperado in (string[])[
            "Sí, borrar",
            "Cómo se enseñan",
            "Según Windows",
            "¿Eliminar la carpeta %s?",
            "¿Vaciar el historial? Los fijados se quedan.",
            "Nada coincide con esa búsqueda",
            "Quitar numeración y viñetas"])
        {
            Assert.Contains(esperado, Textos.Tablas["en"].Keys,
                $"la clave espaniola '{esperado}' no esta como se escribe");
        }

        Textos.Idioma = "fr";
        Assert.AreEqual("Récent", Textos.T("Reciente"));
        Assert.AreEqual("Enregistrés", Textos.T("Guardados"));
        Assert.AreEqual("Sélectionner", Textos.T("Seleccionar"));
        Assert.AreEqual("Système", Textos.T("Sistema"));
        Assert.AreEqual("Liste déroulante", Textos.T("Lista desplegable"));
        Assert.AreEqual("Forêt", Textos.T("Bosque"));

        Textos.Idioma = "pt";
        Assert.AreEqual("Aparência", Textos.T("Apariencia"));
        Assert.AreEqual("Limpar o histórico", Textos.T("Vaciar el historial"));
        Assert.AreEqual("Não foi possível abrir o link.",
            Textos.T("No se pudo abrir el enlace."));
    }

    /// <summary>Sin traduccion sale el espaniol, no un hueco.</summary>
    [TestMethod]
    public void test_lo_no_traducido_cae_en_espaniol()
    {
        Textos.Idioma = "fr";
        Assert.AreEqual("una frase que no existe",
            Textos.T("una frase que no existe"));
    }
}

[TestClass]
public sealed class PruebaAutoarranque : BaseConCarpetaTemporal
{
    /// <summary>
    /// Los tres casos de la preferencia. Se prueban aqui, sobre la
    /// decision pura, y no sobre el registro de Windows: HKCU es global
    /// y es del usuario, no de la prueba — una prueba que escriba ahi le
    /// cambia el arranque de sesion a quien la ejecute.
    /// </summary>
    [TestMethod]
    public void test_sin_preferencia_se_arranca_con_windows()
    {
        // Primer arranque: no hay config.json todavia.
        var a = new Almacen(Rutas);

        Assert.IsTrue(Autoarranque.Quiere(
            a.Pref(Autoarranque.Clave, Autoarranque.PorDefecto)));
    }

    [TestMethod]
    public void test_con_si_se_arranca_con_windows()
    {
        var a = new Almacen(Rutas);
        a.PonerPref(Autoarranque.Clave, "si");

        Assert.IsTrue(Autoarranque.Quiere(
            new Almacen(Rutas).Pref(Autoarranque.Clave, Autoarranque.PorDefecto)));
    }

    [TestMethod]
    public void test_con_no_no_se_arranca_con_windows()
    {
        var a = new Almacen(Rutas);
        a.PonerPref(Autoarranque.Clave, "no");

        Assert.IsFalse(Autoarranque.Quiere(
            new Almacen(Rutas).Pref(Autoarranque.Clave, Autoarranque.PorDefecto)));
    }

    /// <summary>
    /// Un config.json se edita a mano. "Si " con mayuscula o con un
    /// espacio de sobra sigue siendo que si.
    /// </summary>
    [TestMethod]
    public void test_el_si_se_lee_con_mano_ancha()
    {
        Assert.IsTrue(Autoarranque.Quiere("Si"));
        Assert.IsTrue(Autoarranque.Quiere(" si "));
        Assert.IsTrue(Autoarranque.Quiere("SI"));
    }

    /// <summary>
    /// Y lo que no es un si, no lo es — el mismo criterio que la version
    /// anterior, que comparaba con "si" tal cual.
    /// </summary>
    [TestMethod]
    public void test_lo_que_no_es_si_no_arranca()
    {
        Assert.IsFalse(Autoarranque.Quiere("no"));
        Assert.IsFalse(Autoarranque.Quiere(""));
        Assert.IsFalse(Autoarranque.Quiere("cualquier cosa"));
    }
}

[TestClass]
public sealed class PruebaMedidasDelPanel
{
    /// <summary>
    /// El tamaño del primer arranque tiene que caber entre los topes del
    /// redimensionado. Si no, el panel nace fuera de sus propios limites
    /// y el presentador lo encoge nada mas abrirse — un arranque que se
    /// ve mal solo la primera vez es de los que nadie reproduce.
    /// </summary>
    [TestMethod]
    public void test_tamano_inicial_dentro_de_los_limites()
    {
        Assert.IsGreaterThanOrEqualTo(Config.MinAncho, Config.AnchoDef);
        Assert.IsLessThanOrEqualTo(Config.MaxAncho, Config.AnchoDef);

        Assert.IsGreaterThanOrEqualTo(Config.MinAlto, Config.AltoDef);
        Assert.IsLessThanOrEqualTo(Config.MaxAlto, Config.AltoDef);
    }
}
