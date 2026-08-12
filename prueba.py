# -*- coding: utf-8 -*-
"""Pruebas del modelo y la busqueda.

Corren sin abrir ninguna ventana: esa es la ventaja de tenerlos
separados de la interfaz.
"""
import os, sys, tempfile, unittest
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

tmp = tempfile.mkdtemp()
from pastepad import config as cfg
cfg.RUTA_DATOS = os.path.join(tmp, "s.json")
cfg.RUTA_HIST = os.path.join(tmp, "h.json")
cfg.RUTA_PREFS = os.path.join(tmp, "c.json")
cfg.DIR_IMG = os.path.join(tmp, "img")

from pastepad import modelo
from pastepad.busqueda import Indice, normalizar, puntuar


class PruebaModelo(unittest.TestCase):
    def setUp(self):
        for r in (cfg.RUTA_DATOS, cfg.RUTA_HIST, cfg.RUTA_PREFS):
            if os.path.exists(r): os.remove(r)
        self.a = modelo.Almacen()

    def test_crear_y_borrar_carpeta(self):
        self.a.crear_carpeta("Trabajo")
        self.a.anadir_snippet({"titulo": "uno", "categoria": "Trabajo",
                               "runs": [modelo.fragmento("hola")]})
        self.a.anadir_snippet({"titulo": "dos", "categoria": "Trabajo",
                               "runs": [modelo.fragmento("adios")]})
        self.assertEqual(len(self.a.contenido_de("Trabajo")), 2)
        borrados = self.a.borrar_carpeta("Trabajo")
        self.assertEqual(borrados, 2)
        self.assertNotIn("Trabajo", self.a.carpetas)
        self.assertEqual(len(self.a.snippets), 0)

    def test_renombrar_arrastra_contenido(self):
        self.a.crear_carpeta("Vieja")
        self.a.anadir_snippet({"titulo": "x", "categoria": "Vieja",
                               "runs": [modelo.fragmento("y")]})
        self.assertTrue(self.a.renombrar_carpeta("Vieja", "Nueva"))
        self.assertEqual(self.a.snippets[0]["categoria"], "Nueva")

    def test_no_renombrar_a_uno_existente(self):
        self.a.crear_carpeta("A"); self.a.crear_carpeta("B")
        self.assertFalse(self.a.renombrar_carpeta("A", "B"))

    def test_fijados_sobreviven_al_recorte(self):
        fijo = {"tipo": "texto", "texto": "importante", "pin": True}
        self.a.hist.append(fijo)
        for i in range(cfg.MAX_HIST + 20):
            self.a.anotar({"tipo": "texto", "texto": "relleno %d" % i})
        self.assertIn(fijo, self.a.hist)
        libres = [x for x in self.a.hist if not x.get("pin")]
        self.assertLessEqual(len(libres), cfg.MAX_HIST)

    def test_vaciar_respeta_fijados(self):
        self.a.hist = [{"tipo": "texto", "texto": "a", "pin": True},
                       {"tipo": "texto", "texto": "b"}]
        self.a.vaciar_historial()
        self.assertEqual(len(self.a.hist), 1)

    def test_no_repite_lo_recien_copiado(self):
        self.assertTrue(self.a.anotar({"tipo": "texto", "texto": "igual"}))
        self.assertFalse(self.a.anotar({"tipo": "texto", "texto": "igual"}))

    def test_escritura_atomica(self):
        self.a.crear_carpeta("Persistente")
        otro = modelo.Almacen()
        self.assertIn("Persistente", otro.carpetas)


class PruebaPlantillas(unittest.TestCase):
    def test_campos_en_orden_sin_repetir(self):
        t = "Hola [[nombre]], sobre [[tema]] y otra vez [[nombre]]."
        self.assertEqual(modelo.campos_de(t), ["nombre", "tema"])

    def test_rellenar(self):
        f = [modelo.fragmento("Hola [[nombre]]")]
        r = modelo.rellenar(f, {"nombre": "Ana"})
        self.assertEqual(modelo.texto_de(r), "Hola Ana")

    def test_una_linea_corta_bien(self):
        largo = "palabra " * 5000
        self.assertLessEqual(len(modelo.una_linea(largo, 40)), 43)


class PruebaBusqueda(unittest.TestCase):
    def test_ignora_tildes(self):
        self.assertEqual(normalizar("información"), "informacion")

    def test_palabras_en_cualquier_orden(self):
        p = puntuar(["rep", "men"], "reporte mensual", "")
        self.assertIsNotNone(p)

    def test_titulo_pesa_mas_que_cuerpo(self):
        en_titulo = puntuar(["pago"], "pago pendiente", "")
        en_cuerpo = puntuar(["pago"], "otra cosa", "hay un pago aqui")
        self.assertGreater(en_titulo, en_cuerpo)

    def test_no_coincide_devuelve_none(self):
        self.assertIsNone(puntuar(["zzz"], "hola", "mundo"))

    def test_indice_se_rehace_al_invalidar(self):
        for r in (cfg.RUTA_DATOS, cfg.RUTA_HIST):
            if os.path.exists(r): os.remove(r)
        a = modelo.Almacen()
        idx = Indice(a)
        self.assertEqual(len(idx.entradas()), 0)
        a.anadir_snippet({"titulo": "Reporte mensual", "categoria": "W",
                          "runs": [modelo.fragmento("cifras")]})
        idx.invalidar()
        self.assertEqual(len(idx.buscar("rep men")), 1)
        self.assertEqual(len(idx.buscar("inexistente")), 0)


class PruebaEnlaces(unittest.TestCase):
    def test_reconoce_direcciones(self):
        for t in ("https://github.com/x", "http://localhost:8000",
                  "www.google.com", "  https://x.com  "):
            self.assertTrue(modelo.es_enlace(t), t)

    def test_ignora_texto_con_enlace_dentro(self):
        # Un parrafo que menciona una url no debe abrirse al hacer clic.
        self.assertFalse(modelo.es_enlace("mira esto https://x.com"))
        self.assertFalse(modelo.es_enlace("texto normal"))
        self.assertFalse(modelo.es_enlace(""))

    def test_completa_el_esquema(self):
        self.assertEqual(modelo.url_de("www.google.com"),
                         "https://www.google.com")
        self.assertEqual(modelo.url_de("https://x.com"), "https://x.com")

    def test_dominio_limpio(self):
        self.assertEqual(modelo.dominio_de("https://www.github.com/a/b"),
                         "github.com")
        self.assertEqual(modelo.dominio_de("http://localhost:8000/x"),
                         "localhost:8000")


if __name__ == "__main__":
    unittest.main(verbosity=2)
