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
