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

    /// <summary>
    /// Con barra de formato, un guardado tiene varios fragmentos y el
    /// corte cae donde el usuario puso la negrita. Un [[campo]] partido
    /// en tres no lo veia el reemplazo de antes y se pegaba con los
    /// corchetes dentro.
    /// </summary>
    [TestMethod]
    public void test_rellenar_un_campo_partido_entre_fragmentos()
    {
        var f = new[]
        {
            Modelo.CrearFragmento("Hola [["),
            Modelo.CrearFragmento("nombre", negrita: 1),
            Modelo.CrearFragmento("]], que tal"),
        };

        var r = Modelo.Rellenar(f, new Dictionary<string, string> { ["nombre"] = "Ana" });

        Assert.AreEqual("Hola Ana, que tal", Modelo.TextoDe(r));
        Assert.DoesNotContain("[[", Modelo.TextoDe(r));
    }

    /// <summary>
    /// Y el resto del formato no se mueve: lo que estaba en negrita sigue
    /// en negrita despues de rellenar.
    /// </summary>
    [TestMethod]
    public void test_rellenar_conserva_el_formato_de_alrededor()
    {
        var f = new[]
        {
            Modelo.CrearFragmento("Estimado "),
            Modelo.CrearFragmento("[[cliente]]", negrita: 1, color: "#C00000"),
            Modelo.CrearFragmento(": adjunto el informe de [[mes]]."),
        };

        var r = Modelo.Rellenar(f, new Dictionary<string, string>
        {
            ["cliente"] = "Ana Perez",
            ["mes"] = "julio",
        });

        Assert.AreEqual(
            "Estimado Ana Perez: adjunto el informe de julio.", Modelo.TextoDe(r));

        var enNegrita = r.Single(x => x.B == 1);
        Assert.AreEqual("Ana Perez", enNegrita.T);
        Assert.AreEqual("#C00000", enNegrita.C);
    }

    /// <summary>
    /// Rellenar no puede multiplicar los fragmentos: sin unir los trozos
    /// seguidos con el mismo formato, cada pegado dejaba uno por pieza.
    /// </summary>
    [TestMethod]
    public void test_rellenar_no_multiplica_los_fragmentos()
    {
        var f = new[] { Modelo.CrearFragmento("a [[x]] b [[x]] c") };

        var r = Modelo.Rellenar(f, new Dictionary<string, string> { ["x"] = "1" });

        Assert.AreEqual("a 1 b 1 c", Modelo.TextoDe(r));
        Assert.HasCount(1, r);
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
    /// El TextBox de WinUI separa las lineas con \r a secas. Partiendo
    /// solo por \n, el titulo de un texto de tres lineas guardado desde
    /// el dialogo salia "Primera linea Segunda linea Tercera linea":
    /// las tres pegadas. Comprobado leyendo el snippets.json que escribio
    /// el programa.
    /// </summary>
    [TestMethod]
    public void test_el_titulo_con_saltos_de_windows_y_de_textbox()
    {
        Assert.AreEqual("Primera linea",
            Modelo.PrimeraLinea("Primera linea\rSegunda linea\rTercera linea"));

        Assert.AreEqual("Primera linea",
            Modelo.PrimeraLinea("Primera linea\r\nSegunda linea"));

        Assert.AreEqual("Primera linea",
            Modelo.CrearSnippet("Primera linea\rSegunda linea", "Trabajo").Titulo);
    }

    /// <summary>
    /// Desde la 4.3.0 el nombre se pide para cualquier guardado y no
    /// solo para los enlaces, asi que este reparto pasa de valer para los
    /// marcadores a valer para todo lo que se guarda.
    ///
    /// Dejarlo en blanco tiene que seguir dando la primera linea: es lo
    /// que hace que quien guarda dos frases no tenga que rellenar un
    /// campo mas.
    /// </summary>
    [TestMethod]
    public void test_el_nombre_en_blanco_deja_la_primera_linea()
    {
        const string cuerpo = "Hi team,\rI am practicing Git.";

        foreach (string? vacio in new[] { null, "", "   ", "\r\n" })
        {
            Assert.AreEqual("Hi team,",
                Modelo.CrearSnippet(cuerpo, "Cuerpos", vacio).Titulo,
                $"con el nombre en '{vacio ?? "null"}'");
        }

        // Y con nombre gana el nombre, que es el caso que pidio el
        // usuario: cinco cuerpos que empiezan igual y hay que
        // distinguirlos en la fila.
        Assert.AreEqual("Practicando Git",
            Modelo.CrearSnippet(cuerpo, "Cuerpos", "  Practicando Git  ").Titulo);
    }

    /// <summary>
    /// Y las lineas se cuentan igual vengan como vengan: el dialogo de
    /// agregar una lista contaba "1 nota" con sesenta lineas pegadas.
    /// </summary>
    [TestMethod]
    public void test_las_lineas_se_parten_venga_como_venga_el_salto()
    {
        Assert.HasCount(3, Modelo.LineasDe("una\rdos\rtres"));
        Assert.HasCount(3, Modelo.LineasDe("una\r\ndos\r\ntres"));
        Assert.HasCount(3, Modelo.LineasDe("una\ndos\ntres"));

        // Las vacias son separacion, no contenido.
        Assert.HasCount(2, Modelo.LineasDe("una\r\n\r\n  \r\ndos"));
        Assert.IsEmpty(Modelo.LineasDe(""));
    }

    /// <summary>
    /// Abrir un guardado en el editor y guardarlo **sin tocar nada**
    /// tiene que dejarlo igual. Esta es la prueba del fallo que se colo
    /// en la 4.0.1: una nota de cien lineas se quedaba en la primera —de
    /// 7990 caracteres a 77— y el titulo propio se perdia. No se ve
    /// mirando la pantalla, hay que mirar el archivo.
    ///
    /// Reproduce el viaje entero: leer del disco, sacar el texto como lo
    /// saca el dialogo, rearmarlo como lo rearma al guardar, y volver a
    /// leer.
    /// </summary>
    [TestMethod]
    public void test_abrir_y_guardar_sin_cambios_no_toca_el_guardado()
    {
        string largo = string.Join(
            "\r\n",
            Enumerable.Range(1, 100).Select(n => $"Linea {n} de la nota larga"));

        var a = new Almacen(Rutas);

        a.AnadirSnippet(new Snippet
        {
            Titulo = "TITULO DISTINTO DEL TEXTO",
            Categoria = "Trabajo",
            Runs = [Modelo.CrearFragmento(largo)],
        });

        var antes = new Almacen(Rutas).Snippets.Single();

        // Lo que el dialogo carga en la caja y devuelve al guardar.
        string cargado = Modelo.TextoDe(antes.Runs);

        Assert.AreEqual(largo.Length, cargado.Length,
            "el editor no carga el texto entero");

        var otro = new Almacen(Rutas);
        var viejo = otro.Snippets.Single();

        otro.ReemplazarSnippet(
            viejo, Modelo.CrearSnippet(cargado, viejo.Categoria, viejo.Titulo));

        var despues = new Almacen(Rutas).Snippets.Single();

        Assert.AreEqual(largo, Modelo.TextoDe(despues.Runs),
            "guardar sin cambios cambio el texto");

        Assert.AreEqual("TITULO DISTINTO DEL TEXTO", despues.Titulo,
            "guardar sin cambios se llevo el titulo por delante");
    }

    /// <summary>
    /// Un marcador puede llevar nombre propio. Sin el, el titulo es la
    /// direccion, y buscar por titulo en una lista de direcciones no
    /// sirve de nada.
    /// </summary>
    [TestMethod]
    public void test_el_marcador_puede_llevar_nombre_propio()
    {
        const string url = "https://ejemplo.com/muy/larga/y/fea?x=1";

        var conNombre = Modelo.CrearSnippet(url, "Trabajo", "Panel de la SIE");
        Assert.AreEqual("Panel de la SIE", conNombre.Titulo);
        Assert.AreEqual(url, Modelo.TextoDe(conNombre.Runs));

        // Sin nombre, o con uno en blanco, sigue valiendo la primera
        // linea: no se puede quedar un guardado sin titulo.
        Assert.AreEqual(url, Modelo.CrearSnippet(url, "Trabajo").Titulo);
        Assert.AreEqual(url, Modelo.CrearSnippet(url, "Trabajo", "   ").Titulo);
    }

    /// <summary>
    /// Y un texto de varias lineas no es un enlace aunque venga partido
    /// con \r a secas, que es como lo devuelve el TextBox.
    /// </summary>
    [TestMethod]
    public void test_un_texto_de_varias_lineas_no_es_enlace()
    {
        Assert.IsTrue(Modelo.EsEnlace("https://ejemplo.com"));
        Assert.IsFalse(Modelo.EsEnlace("https://ejemplo.com\rsegunda linea"));
        Assert.IsFalse(Modelo.EsEnlace("https://ejemplo.com\r\nsegunda linea"));
    }

    /// <summary>
    /// Lo que se guarda lleva los saltos de Windows. Con \r a secas, un
    /// texto de tres lineas pegado en el Bloc de notas salia en una.
    /// </summary>
    [TestMethod]
    public void test_lo_guardado_lleva_los_saltos_de_windows()
    {
        var s = Modelo.CrearSnippet("una\rdos\r\ntres\ncuatro", "Trabajo");

        Assert.AreEqual("una\r\ndos\r\ntres\r\ncuatro", Modelo.TextoDe(s.Runs));

        // Y no los duplica al pasar dos veces.
        Assert.AreEqual("una\r\ndos",
            Modelo.NormalizarSaltos(Modelo.NormalizarSaltos("una\r\ndos")));
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

/// <summary>
/// El editor de la carpeta entera: una caja con una nota por linea.
///
/// Lo que se juega aqui es lo mismo de siempre —que guardar no destruya
/// lo que el usuario no pidio destruir—, y con el volumen que el usuario
/// dio de ejemplo: 3000 notas de las que se borran cien.
/// </summary>
[TestClass]
public sealed class PruebaEditarCarpeta : BaseConCarpetaTemporal
{
    static Snippet Nota(
        string texto, string? titulo = null, string carpeta = "Trabajo") =>
        Modelo.CrearSnippet(texto, carpeta, titulo);

    [TestMethod]
    public void test_las_lineas_intactas_conservan_su_misma_nota()
    {
        var uno = Nota("https://ejemplo.com/x", "Panel de la SIE");
        var dos = Nota("segunda");

        // Una con formato puesto a mano, que es lo que se perderia si la
        // fusion rehiciera las notas en vez de reutilizarlas.
        var tres = new Snippet
        {
            Titulo = "tercera",
            Categoria = "Trabajo",
            Runs = [Modelo.CrearFragmento("tercera", "Arial", 20, negrita: 1)],
        };

        var antes = Modelo.PartirCarpeta([uno, dos, tres]);

        var f = Modelo.FusionarCarpeta(
            antes, "https://ejemplo.com/x\rsegunda\rtercera", "Trabajo");

        Assert.AreEqual(3, f.Conservadas);
        Assert.AreEqual(0, f.Nuevas);
        Assert.IsEmpty(f.Quitadas);

        // La misma nota, no una copia igual.
        Assert.AreSame(uno, f.Resultado[0]);
        Assert.AreSame(tres, f.Resultado[2]);

        Assert.AreEqual("Panel de la SIE", f.Resultado[0].Titulo,
            "el nombre propio del marcador se rehizo");

        Assert.AreEqual("Arial", f.Resultado[2].Runs[0].F);
        Assert.AreEqual(20, f.Resultado[2].Runs[0].S);
        Assert.AreEqual(1, f.Resultado[2].Runs[0].B);
    }

    /// <summary>
    /// El ejemplo del usuario, con su volumen: 3000 notas linea por
    /// linea y quiere quitar cien. Las 2900 que quedan no se tocan.
    /// </summary>
    [TestMethod]
    public void test_quitar_cien_de_tres_mil_no_toca_las_otras()
    {
        var carpeta = Enumerable.Range(1, 3000)
            .Select(n => Nota($"nota numero {n}"))
            .ToList();

        var antes = Modelo.PartirCarpeta(carpeta);

        string editado = string.Join("\r",
            Enumerable.Range(101, 2900).Select(n => $"nota numero {n}"));

        var f = Modelo.FusionarCarpeta(antes, editado, "Trabajo");

        Assert.HasCount(2900, f.Resultado);
        Assert.HasCount(100, f.Quitadas);
        Assert.AreEqual(2900, f.Conservadas);
        Assert.AreEqual(0, f.Nuevas, "se rehizo alguna nota que no cambio");

        Assert.AreSame(carpeta[100], f.Resultado[0]);
        Assert.AreSame(carpeta[2999], f.Resultado[2899]);

        // Y las que se van son las cien primeras, no otras cien.
        CollectionAssert.AreEqual(
            carpeta.Take(100).ToList(), f.Quitadas);
    }

    /// <summary>
    /// El viaje entero por el disco, que es donde se vieron los dos
    /// fallos que destruian texto en silencio: se guarda, se relee, y se
    /// cuenta lo que hay en snippets.json.
    /// </summary>
    [TestMethod]
    public void test_guardar_la_carpeta_editada_sobrevive_al_disco()
    {
        var a = new Almacen(Rutas);

        foreach (var n in Enumerable.Range(1, 3000))
            a.Snippets.Add(Nota($"nota numero {n}"));

        a.CrearCarpeta("Trabajo");
        a.AnadirSnippet(Nota("de otra carpeta", carpeta: "Otra"));

        var otro = new Almacen(Rutas);
        var antes = Modelo.PartirCarpeta(otro.ContenidoDe("Trabajo"));

        Assert.HasCount(3000, antes.DeUnaLinea);

        string editado = string.Join("\r",
            Enumerable.Range(101, 2900).Select(n => $"nota numero {n}"));

        var f = Modelo.FusionarCarpeta(antes, editado, "Trabajo");
        otro.ReemplazarContenido("Trabajo", f.Resultado);

        var leido = new Almacen(Rutas);

        Assert.HasCount(2900, leido.ContenidoDe("Trabajo"));
        Assert.HasCount(1, leido.ContenidoDe("Otra"),
            "editar una carpeta se llevo por delante otra");

        Assert.AreEqual("nota numero 101",
            Modelo.TextoDe(leido.ContenidoDe("Trabajo")[0].Runs));

        Assert.AreEqual("nota numero 3000",
            Modelo.TextoDe(leido.ContenidoDe("Trabajo")[2899].Runs));
    }

    /// <summary>
    /// Una nota de varias lineas no cabe en "una nota por linea": ni se
    /// ensena partida ni se pierde al guardar.
    /// </summary>
    [TestMethod]
    public void test_una_nota_de_varias_lineas_se_queda_como_estaba()
    {
        var larga = Nota(string.Join("\r\n",
            Enumerable.Range(1, 60).Select(n => $"linea {n}")));

        var antes = Modelo.PartirCarpeta([Nota("corta"), larga]);

        Assert.HasCount(1, antes.DeUnaLinea);
        Assert.HasCount(1, antes.DeVariasLineas);
        Assert.AreEqual("corta", antes.Texto,
            "la nota larga se colo en la caja partida en 60 lineas");

        // El usuario borra "corta" y guarda con la caja vacia.
        var f = Modelo.FusionarCarpeta(antes, "", "Trabajo");

        Assert.HasCount(1, f.Quitadas);
        Assert.HasCount(1, f.Resultado);
        Assert.AreSame(larga, f.Resultado[0]);

        Assert.HasCount(60, Modelo.LineasDe(Modelo.TextoDe(f.Resultado[0].Runs)),
            "la nota de 60 lineas salio partida o recortada");
    }

    /// <summary>
    /// Dos notas con el mismo texto son dos notas. Emparejadas de una en
    /// una, dejar una sola linea repetida borra una y conserva la otra.
    /// </summary>
    [TestMethod]
    public void test_dos_notas_iguales_se_emparejan_una_a_una()
    {
        var uno = Nota("repetida");
        var dos = Nota("repetida");

        var antes = Modelo.PartirCarpeta([uno, dos]);

        var dosVeces = Modelo.FusionarCarpeta(antes, "repetida\rrepetida", "Trabajo");
        Assert.AreEqual(2, dosVeces.Conservadas);
        Assert.IsEmpty(dosVeces.Quitadas);

        var unaVez = Modelo.FusionarCarpeta(antes, "repetida", "Trabajo");
        Assert.HasCount(1, unaVez.Resultado);
        Assert.HasCount(1, unaVez.Quitadas);
        Assert.AreSame(uno, unaVez.Resultado[0]);
    }

    /// <summary>
    /// Y lo que si cambio se guarda como nota nueva, con su titulo
    /// puesto por la primera linea, como cualquier otro guardado.
    /// </summary>
    [TestMethod]
    public void test_una_linea_cambiada_es_una_nota_nueva()
    {
        var antes = Modelo.PartirCarpeta([Nota("como estaba")]);

        var f = Modelo.FusionarCarpeta(antes, "como quedo", "Trabajo");

        Assert.AreEqual(0, f.Conservadas);
        Assert.AreEqual(1, f.Nuevas);
        Assert.HasCount(1, f.Quitadas);

        Assert.AreEqual("como quedo", f.Resultado[0].Titulo);
        Assert.AreEqual("Trabajo", f.Resultado[0].Categoria);
    }

    /// <summary>
    /// Reemplazar el contenido no puede mandar la carpeta al final del
    /// archivo: la pestana se ordena por como estan en snippets.json.
    /// </summary>
    [TestMethod]
    public void test_la_carpeta_editada_no_se_va_al_final()
    {
        var a = new Almacen(Rutas);

        a.AnadirSnippet(Nota("primera de trabajo"));
        a.AnadirSnippet(Nota("de otra", carpeta: "Otra"));

        a.ReemplazarContenido("Trabajo", [Nota("sigue siendo la primera")]);

        Assert.AreEqual("Trabajo", new Almacen(Rutas).Snippets[0].Categoria);
    }
}

/// <summary>
/// La barra de formato. Lo que se guarda tiene que seguir siendo texto
/// plano correcto y seguir teniendo sus [[campos]].
/// </summary>
[TestClass]
public sealed class PruebaFormato
{
    [TestMethod]
    public void test_el_texto_plano_no_depende_del_formato()
    {
        var runs = new[]
        {
            Modelo.CrearFragmento("Estimado ", "Calibri", 11),
            Modelo.CrearFragmento("Ana", "Arial", 14, negrita: 1, color: "#C00000"),
            Modelo.CrearFragmento(":\r\nadjunto lo pedido.", "Calibri", 11),
        };

        var s = Modelo.CrearSnippet(runs, "Correo");

        Assert.AreEqual("Estimado Ana:\r\nadjunto lo pedido.",
            Modelo.TextoDe(s.Runs));

        // El titulo sale del texto entero, no del primer fragmento.
        Assert.AreEqual("Estimado Ana:", s.Titulo);

        // Y los campos se siguen viendo aunque esten repartidos.
        CollectionAssert.AreEqual(
            new[] { "cliente" },
            Modelo.CamposDe(Modelo.TextoDe(new[]
            {
                Modelo.CrearFragmento("Hola [["),
                Modelo.CrearFragmento("cliente]]"),
            })));
    }

    [TestMethod]
    public void test_los_fragmentos_seguidos_con_el_mismo_formato_se_unen()
    {
        // Lo que devuelve el editor al escribir letra a letra.
        var runs = "hola".Select(c => Modelo.CrearFragmento(c.ToString())).ToList();

        var s = Modelo.CrearSnippet(runs, "Correo");

        Assert.HasCount(1, s.Runs);
        Assert.AreEqual("hola", s.Runs[0].T);
    }

    [TestMethod]
    public void test_los_saltos_del_editor_se_normalizan_al_guardar()
    {
        var s = Modelo.CrearSnippet(
            [Modelo.CrearFragmento("una\rdos"), Modelo.CrearFragmento("\rtres", negrita: 1)],
            "Correo");

        Assert.AreEqual("una\r\ndos\r\ntres", Modelo.TextoDe(s.Runs));
    }

    [TestMethod]
    public void test_vinetas_van_y_vienen()
    {
        const string bloque = "uno\rdos\rtres";

        string con = Modelo.AlternarVinetas(bloque);
        Assert.AreEqual("• uno\r• dos\r• tres", con);

        // Y el mismo boton las quita.
        Assert.AreEqual(bloque, Modelo.AlternarVinetas(con));
    }

    [TestMethod]
    public void test_numeros_van_y_vienen_y_sustituyen_a_la_vineta()
    {
        string num = Modelo.AlternarNumeros("uno\rdos\rtres");
        Assert.AreEqual("1. uno\r2. dos\r3. tres", num);

        Assert.AreEqual("uno\rdos\rtres", Modelo.AlternarNumeros(num));

        // Una linea ya numerada no se numera dos veces al pasar a viñeta.
        Assert.AreEqual("• uno\r• dos\r• tres", Modelo.AlternarVinetas(num));
    }

    [TestMethod]
    public void test_las_lineas_en_blanco_se_quedan_como_estan()
    {
        Assert.AreEqual("• uno\r\r• dos", Modelo.AlternarVinetas("uno\r\rdos"));
        Assert.AreEqual("1. uno\r\r2. dos", Modelo.AlternarNumeros("uno\r\rdos"));
    }

    [TestMethod]
    public void test_la_sangria_se_pone_y_se_quita()
    {
        string mas = Modelo.Sangrar("uno\rdos", true);
        Assert.AreEqual("\tuno\r\tdos", mas);

        Assert.AreEqual("uno\rdos", Modelo.Sangrar(mas, false));

        // Sin sangria, quitarla no muerde el texto.
        Assert.AreEqual("uno\rdos", Modelo.Sangrar("uno\rdos", false));
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
/// De que es cada guardado. Lo que se prueba aqui no es la propuesta
/// —que es la clasificacion de siempre— sino las dos reglas nuevas: que
/// lo que elige el usuario gana, y que lo que no elige no se escribe.
/// </summary>
[TestClass]
public sealed class PruebaTipos
{
    [TestMethod]
    public void test_propone_lo_que_se_deducia_antes()
    {
        Assert.AreEqual(Tipos.Marcador, Tipos.Deducir("https://x.com"));
        Assert.AreEqual(Tipos.Plantilla, Tipos.Deducir("Hola [[nombre]]"));
        Assert.AreEqual(Tipos.Nota, Tipos.Deducir("una cosa cualquiera"));
        Assert.AreEqual(Tipos.Nota, Tipos.Deducir(""));
        Assert.AreEqual(Tipos.Nota, Tipos.Deducir(null));
    }

    [TestMethod]
    public void test_un_enlace_con_campos_es_marcador()
    {
        // Entre los dos gana el que se reconoce con mas certeza.
        Assert.AreEqual(Tipos.Marcador, Tipos.Deducir("https://x.com/[[id]]"));
    }

    [TestMethod]
    public void test_correo_no_se_deduce_nunca()
    {
        // No hay nada en un cuerpo de correo que lo separe de una nota.
        // Deducirlo por una arroba convertiria en correo cualquier texto
        // que mencione una direccion.
        Assert.AreEqual(Tipos.Nota, Tipos.Deducir("escribe a hola@x.com"));
        Assert.AreEqual(Tipos.Nota, Tipos.Deducir("Estimado cliente:"));
    }

    [TestMethod]
    public void test_lo_que_elige_el_usuario_manda()
    {
        // El caso que trajo todo esto: un cuerpo de correo es texto
        // corriente, y hasta ahora no habia forma de decir que era.
        Assert.AreEqual(Tipos.Correo, Tipos.De(Tipos.Correo, "Hola equipo,"));

        // Y al reves: una direccion que el usuario quiere tratar como
        // nota deja de comportarse como marcador.
        Assert.AreEqual(Tipos.Nota, Tipos.De(Tipos.Nota, "https://x.com"));
    }

    [TestMethod]
    public void test_sin_elegir_vale_lo_deducido()
    {
        Assert.AreEqual(Tipos.Marcador, Tipos.De(null, "https://x.com"));
        Assert.AreEqual(Tipos.Nota, Tipos.De("", "texto"));
    }

    [TestMethod]
    public void test_un_tipo_desconocido_no_deja_el_guardado_fuera()
    {
        // snippets.json se edita a mano y llega de versiones que aun no
        // existen. Un tipo que no conocemos no puede dejar la fila sin
        // grupo, que en la practica es desaparecer de la lista.
        Assert.AreEqual(Tipos.Nota, Tipos.De("factura", "texto"));
        Assert.IsFalse(Tipos.Vale("factura"));
        Assert.IsFalse(Tipos.Vale(null));
    }

    [TestMethod]
    public void test_no_se_escribe_lo_que_ya_se_deducia()
    {
        // Abrir un guardado y pulsar Guardar sin tocar nada no puede
        // añadirle una clave que antes no tenia.
        Assert.IsNull(Tipos.ParaGuardar(Tipos.Marcador, "https://x.com"));
        Assert.IsNull(Tipos.ParaGuardar(Tipos.Nota, "texto normal"));
        Assert.IsNull(Tipos.ParaGuardar(null, "texto normal"));
        Assert.IsNull(Tipos.ParaGuardar("factura", "texto normal"));
    }

    [TestMethod]
    public void test_si_se_escribe_lo_que_contradice_al_texto()
    {
        Assert.AreEqual(Tipos.Correo, Tipos.ParaGuardar(Tipos.Correo, "Hola,"));
        Assert.AreEqual(Tipos.Nota, Tipos.ParaGuardar(Tipos.Nota, "https://x.com"));
    }

    [TestMethod]
    public void test_el_guardado_nuevo_lo_lleva()
    {
        var s = Modelo.CrearSnippet("Hola equipo,", "Trabajo", null, Tipos.Correo);

        Assert.AreEqual(Tipos.Correo, s.Tipo);
        Assert.AreEqual(Tipos.Correo, Tipos.De(s));
    }

    [TestMethod]
    public void test_el_guardado_de_siempre_no_gana_una_clave()
    {
        var s = Modelo.CrearSnippet("https://x.com", "Trabajo");

        Assert.IsNull(s.Tipo);
        Assert.AreEqual(Tipos.Marcador, Tipos.De(s));
    }

    [TestMethod]
    public void test_con_formato_tambien()
    {
        var runs = new List<Fragmento>
        {
            Modelo.CrearFragmento("Estimado "),
            Modelo.CrearFragmento("cliente", negrita: 1),
        };

        var s = Modelo.CrearSnippet(runs, "Trabajo", "Bienvenida", Tipos.Correo);

        Assert.AreEqual(Tipos.Correo, s.Tipo);
        Assert.AreEqual("Bienvenida", s.Titulo);
        Assert.HasCount(2, s.Runs);
    }
}

/// <summary>
/// La linea con la que Windows nos reabre despues de actualizarnos.
/// Equivocarse aqui no rompe nada a la vista: pastepad vuelve a abrirse
/// tan tranquilo sobre otra carpeta de datos.
/// </summary>
[TestClass]
public sealed class PruebaArgumentos
{
    [TestMethod]
    public void test_sin_argumentos_no_registra_linea()
    {
        // null y cadena vacia significan lo mismo para la API de
        // Windows: borrar lo registrado. Devolver null lo deja claro.
        Assert.IsNull(Argumentos.Componer([]));
    }

    [TestMethod]
    public void test_argumento_simple_no_se_entrecomilla()
    {
        Assert.AreEqual("--datos C:\\temp\\prueba",
            Argumentos.Componer(["--datos", "C:\\temp\\prueba"]));
    }

    /// <summary>
    /// La que de verdad importa. La carpeta del proyecto es
    /// "Mi pequeno Secreto\Pastepad", con dos espacios, y sin comillas
    /// Windows la partiria en tres argumentos: pastepad volveria sin
    /// carpeta valida y escribiria donde no toca.
    ///
    /// Todas las pruebas manuales se hicieron sobre la ruta corta de 8.3,
    /// que no lleva espacios, asi que esta rama no se ejecuto ni una vez.
    /// </summary>
    [TestMethod]
    public void test_ruta_con_espacios_va_entre_comillas()
    {
        Assert.AreEqual(
            "--datos \"C:\\Users\\Jose Miguel Ortiz\\Mi pequeno Secreto\"",
            Argumentos.Componer(
                ["--datos", "C:\\Users\\Jose Miguel Ortiz\\Mi pequeno Secreto"]));
    }

    [TestMethod]
    public void test_no_entrecomilla_dos_veces()
    {
        Assert.AreEqual("--datos \"C:\\con espacios\"",
            Argumentos.Componer(["--datos", "\"C:\\con espacios\""]));
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
public sealed class PruebaVersiones
{
    /// <summary>
    /// La trampa de comparar versiones: como texto, "4.0.10" sale MENOR
    /// que "4.0.9" porque compara el "1" con el "9". Con diez parches
    /// publicados el aviso dejaria de aparecer, y en silencio.
    /// </summary>
    [TestMethod]
    public void test_la_decima_es_mayor_que_la_novena()
    {
        Assert.IsTrue(Versiones.HayNovedad("4.0.9", "4.0.10"),
            "4.0.10 tiene que ser mas nueva que 4.0.9");

        Assert.IsFalse(Versiones.HayNovedad("4.0.10", "4.0.9"));

        // Y que no sea casualidad de esos dos numeros.
        Assert.IsTrue(Versiones.HayNovedad("4.9.0", "4.10.0"));
        Assert.IsTrue(Versiones.HayNovedad("9.0.0", "10.0.0"));
    }

    [TestMethod]
    public void test_la_misma_version_no_es_novedad()
    {
        Assert.IsFalse(Versiones.HayNovedad("4.0.1", "4.0.1"));

        // Con distinto numero de partes tampoco: System.Version trata
        // las que faltan como -1, y "4.0.1" le saldria menor que
        // "4.0.1.0" si no se normalizaran.
        Assert.IsFalse(Versiones.HayNovedad("4.0.1", "4.0.1.0"));
        Assert.IsFalse(Versiones.HayNovedad("4.0.1.0", "4.0.1"));
    }

    [TestMethod]
    public void test_la_v_del_tag_se_quita()
    {
        Assert.AreEqual("4.0.1", Versiones.SinLaV("v4.0.1"));
        Assert.AreEqual("4.0.1", Versiones.SinLaV("4.0.1"));
        Assert.AreEqual("", Versiones.SinLaV(null));

        // Y el tag entero tambien vale para comparar.
        Assert.IsTrue(Versiones.HayNovedad("4.0.1", "v4.1.0"));
    }

    /// <summary>
    /// Lo que no se entiende no avisa. Mejor callarse que sacarle al
    /// usuario una banda con cualquier cosa dentro.
    /// </summary>
    [TestMethod]
    public void test_lo_que_no_se_entiende_no_avisa()
    {
        Assert.IsFalse(Versiones.HayNovedad("4.0.1", "cuatro"));
        Assert.IsFalse(Versiones.HayNovedad("4.0.1", ""));
        Assert.IsFalse(Versiones.HayNovedad("4.0.1", null));
        Assert.IsFalse(Versiones.HayNovedad(null, "4.1.0"));

        // Un tag de prueba tampoco: no se interpreta y no se avisa.
        Assert.IsFalse(Versiones.HayNovedad("4.0.1", "v4.1.0-beta1"));
    }

    [TestMethod]
    public void test_se_comprueba_una_vez_al_dia()
    {
        var ahora = new DateTimeOffset(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);

        // Sin fecha guardada: primer arranque, toca.
        Assert.IsTrue(Versiones.TocaComprobar(null, ahora));

        // Ya se miro hoy, aunque se reabra el programa diez veces.
        Assert.IsFalse(Versiones.TocaComprobar("2026-08-13", ahora));

        Assert.IsTrue(Versiones.TocaComprobar("2026-08-12", ahora));

        // Una fecha que no se entiende no puede dejar el aviso apagado
        // para siempre.
        Assert.IsTrue(Versiones.TocaComprobar("ayer", ahora));

        Assert.AreEqual("2026-08-13", Versiones.Hoy(ahora));
    }

    /// <summary>
    /// De cada version se avisa una sola vez. Una banda que sale todos
    /// los dias se deja de leer.
    /// </summary>
    [TestMethod]
    public void test_no_se_repite_el_aviso_de_la_misma_version()
    {
        Assert.IsTrue(Versiones.TocaAvisar("4.0.1", "4.1.0", null));
        Assert.IsFalse(Versiones.TocaAvisar("4.0.1", "4.1.0", "4.1.0"));

        // Pero de la siguiente si.
        Assert.IsTrue(Versiones.TocaAvisar("4.0.1", "4.2.0", "4.1.0"));

        // Y si no hay novedad, no se avisa aunque no conste avisada.
        Assert.IsFalse(Versiones.TocaAvisar("4.1.0", "4.1.0", null));
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
