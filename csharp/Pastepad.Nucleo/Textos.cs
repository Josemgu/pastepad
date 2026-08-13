namespace Pastepad.Nucleo;

/// <summary>
/// Los textos de la interfaz en cuatro idiomas.
///
/// La clave es el texto en espanol, no un identificador inventado. Dos
/// razones: el codigo se sigue leyendo sin ir a buscar que significa
/// "btn.paste.plain", y si a una traduccion le falta una frase sale la
/// espaniola en vez de un hueco o el nombre de la clave.
///
/// Esto es la fuente y se edita a mano. Antes se generaba a partir de
/// un modulo de la version en Python; el dia que esa version se borre,
/// las traducciones se habrian ido con ella.
///
/// Vive en el nucleo y no en la interfaz porque no necesita nada de
/// WinUI —es un diccionario y un string.Format— y asi las pruebas
/// pueden comprobarlo sin abrir ninguna ventana.
///
/// Se descarto .resw con MRT Core: en una aplicacion desempaquetada
/// resuelve por el idioma del sistema y no por el que el usuario elige,
/// obliga a pasar makepri en cada cambio de texto, y el cambio en
/// caliente no esta documentado para el marcado.
/// </summary>
public static class Textos
{
    public const string IdiomaDef = "es";

    /// <summary>Codigo de idioma y su nombre, para el selector.</summary>
    public static readonly IReadOnlyDictionary<string, string> Nombres =
        new Dictionary<string, string>
        {
            ["es"] = "Español",
            ["en"] = "English",
            ["pt"] = "Português",
            ["fr"] = "Français",
        };

    /// <summary>El idioma en uso. Lo pone la aplicacion al arrancar.</summary>
    public static string Idioma { get; set; } = IdiomaDef;

    /// <summary>
    /// Las tres tablas, para poder compararlas entre si. Es lo unico que
    /// este diseño no detecta solo: una clave que falte en frances no
    /// rompe nada, simplemente sale en espaniol en mitad de una frase
    /// francesa, y eso no lo ve ni el compilador ni el usuario que no
    /// habla los dos idiomas.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>
        Tablas => _tablas;

    /// <summary>
    /// Traduce. Lo que no este traducido sale en espaniol, que es
    /// preferible a un hueco o al nombre de la clave.
    /// </summary>
    public static string T(string espanol) =>
        _tablas.TryGetValue(Idioma, out var tabla)
        && tabla.TryGetValue(espanol, out var traducido)
            ? traducido
            : espanol;

    /// <summary>
    /// Traduce y rellena los huecos %s y %d, numerandolos en el orden en
    /// que aparecen.
    ///
    /// Lo de numerarlos importa: la version anterior ponia {0} en todos,
    /// y "Eliminar la carpeta %s y sus %d textos?" salia como "Eliminar
    /// la carpeta 3 y sus 3 textos?" — los dos huecos recibian el mismo
    /// valor. Con un solo hueco nadie lo noto.
    /// </summary>
    public static string T(string espanol, params object[] valores)
    {
        string plantilla = T(espanol);
        var molde = new System.Text.StringBuilder(plantilla.Length + 8);
        int hueco = 0;

        for (int i = 0; i < plantilla.Length; i++)
        {
            if (plantilla[i] == '%' && i + 1 < plantilla.Length
                && plantilla[i + 1] is 's' or 'd')
            {
                molde.Append('{').Append(hueco++).Append('}');
                i++;
                continue;
            }

            // Una llave que venga en el texto es literal y hay que
            // duplicarla, o string.Format la toma por un hueco suyo.
            if (plantilla[i] is '{' or '}') molde.Append(plantilla[i]);

            molde.Append(plantilla[i]);
        }

        return string.Format(molde.ToString(), valores);
    }

    static readonly IReadOnlyDictionary<string, string> _en =
        new Dictionary<string, string>
        {
            ["%d caracteres"] =
                "%d characters",
            ["Borrar (%d)"] =
                "Delete (%d)",
            ["Renombrar %s"] =
                "Rename %s",
            ["Eliminar %s y su contenido"] =
                "Delete %s and its contents",
            ["¿Eliminar la carpeta %s?"] =
                "Delete folder %s?",
            ["¿Eliminar la carpeta %s y sus %d textos? Esto no se puede deshacer."] =
                "Delete folder %s and its %d texts? This cannot be undone.",
            ["¿Eliminar la carpeta %s y su texto? Esto no se puede deshacer."] =
                "Delete folder %s and its text? This cannot be undone.",
            ["¿Borrar %d elementos? Esto no se puede deshacer."] =
                "Delete %d items? This cannot be undone.",
            ["¿Borrar %d elemento? Esto no se puede deshacer."] =
                "Delete this item? This cannot be undone.",
            ["%d notas"] =
                "%d notes",
            ["%d nota"] =
                "%d note",
            ["Buscar en todo"] =
                "Search everything",
            ["Reciente"] =
                "Recent",
            ["Guardados"] =
                "Saved",
            ["Seleccionar"] =
                "Select",
            ["Nuevo"] =
                "New",
            ["Todos"] =
                "All",
            ["Cancelar"] =
                "Cancel",
            ["Aceptar"] =
                "OK",
            ["Guardar"] =
                "Save",
            ["Aplicar"] =
                "Apply",
            ["Agregar"] =
                "Add",
            ["Confirmar"] =
                "Confirm",
            ["Sí, borrar"] =
                "Yes, delete",
            ["En pausa"] =
                "Paused",
            ["Pausar la captura"] =
                "Pause capture",
            ["Reanudar la captura"] =
                "Resume capture",
            ["Apariencia"] =
                "Appearance",
            ["Cerrar"] =
                "Close",
            ["Copia algo y aparecerá aquí"] =
                "Copy something and it shows up here",
            ["Vacío. Usa Nuevo para guardar un texto"] =
                "Empty. Use New to save a text",
            ["Nada coincide con esa búsqueda"] =
                "Nothing matches that search",
            ["Imagen copiada"] =
                "Copied image",
            ["captura"] =
                "screenshot",
            ["Marcadores"] =
                "Bookmarks",
            ["Notas"] =
                "Notes",
            ["Abrir en el navegador"] =
                "Open in browser",
            ["Pegar"] =
                "Paste",
            ["Pegar sin formato"] =
                "Paste as plain text",
            ["Copiar"] =
                "Copy",
            ["Fijar arriba"] =
                "Pin to top",
            ["Quitar de arriba"] =
                "Unpin",
            ["Editar y guardar..."] =
                "Edit and save...",
            ["Borrar"] =
                "Delete",
            ["Todas las carpetas"] =
                "All folders",
            ["Nueva carpeta"] =
                "New folder",
            ["Nueva carpeta..."] =
                "New folder...",
            ["Nombre de la carpeta"] =
                "Folder name",
            ["Renombrar carpeta"] =
                "Rename folder",
            ["Nuevo nombre"] =
                "New name",
            ["Agregar una lista"] =
                "Add a list",
            ["Vaciar el historial"] =
                "Clear history",
            ["¿Vaciar el historial? Los fijados se quedan."] =
                "Clear the history? Pinned items stay.",
            ["Nuevo texto"] =
                "New text",
            ["Editar texto"] =
                "Edit text",
            ["Guardar en"] =
                "Save into",
            ["Escribe [[algo]] y el programa te lo preguntará antes de pegar"] =
                "Write [[anything]] and you'll be asked for it before pasting",
            ["Completar antes de pegar"] =
                "Fill in before pasting",
            ["Una nota por cada línea"] =
                "One note per line",
            ["Todo junto en una sola nota"] =
                "All together in a single note",
            ["Quitar numeración y viñetas"] =
                "Strip numbering and bullets",
            ["Agregar a %s"] =
                "Add to %s",
            ["Fondo"] =
                "Background",
            ["Color de acento"] =
                "Accent colour",
            ["Atajo para abrir"] =
                "Shortcut to open",
            ["Idioma"] =
                "Language",
            ["Según Windows"] =
                "Follow Windows",
            ["Oscuro"] =
                "Dark",
            ["Claro"] =
                "Light",
            ["Medianoche"] =
                "Midnight",
            ["Grafito"] =
                "Graphite",
            ["Bosque"] =
                "Forest",
            ["Papel"] =
                "Paper",
            ["Niebla"] =
                "Mist",
            ["Arena"] =
                "Sand",
            ["Lila"] =
                "Lilac",
            ["Salvia"] =
                "Sage",
            ["Rubor"] =
                "Blush",
            ["Carpetas"] =
                "Folders",
            ["Cómo se enseñan"] =
                "How they show",
            ["Lista desplegable"] =
                "Drop-down list",
            ["Fichas en fila"] =
                "Chips in a row",
            ["Sistema"] =
                "System",
            ["Ya hay una carpeta llamada %s."] =
                "There is already a folder called %s.",
            ["No se pudo renombrar: ya hay una carpeta %s."] =
                "Could not rename: there is already a folder %s.",
            ["%s ya lo usa otro programa. Se dejó el anterior."] =
                "%s is already taken by another program. The previous one was kept.",
            ["No se pudo copiar al portapapeles."] =
                "Could not copy to the clipboard.",
            ["No se pudo abrir el enlace."] =
                "Could not open the link.",
            ["Copiado, pero no pude volver a la ventana anterior. Pega con Ctrl+V."] =
                "Copied, but I could not get back to the previous window. Paste with Ctrl+V.",
        };

    static readonly IReadOnlyDictionary<string, string> _pt =
        new Dictionary<string, string>
        {
            ["%d caracteres"] =
                "%d caracteres",
            ["Borrar (%d)"] =
                "Excluir (%d)",
            ["Renombrar %s"] =
                "Renomear %s",
            ["Eliminar %s y su contenido"] =
                "Excluir %s e seu conteúdo",
            ["¿Eliminar la carpeta %s?"] =
                "Excluir a pasta %s?",
            ["¿Eliminar la carpeta %s y sus %d textos? Esto no se puede deshacer."] =
                "Excluir a pasta %s e seus %d textos? Isso não pode ser desfeito.",
            ["¿Eliminar la carpeta %s y su texto? Esto no se puede deshacer."] =
                "Excluir a pasta %s e seu texto? Isso não pode ser desfeito.",
            ["¿Borrar %d elementos? Esto no se puede deshacer."] =
                "Excluir %d itens? Isso não pode ser desfeito.",
            ["¿Borrar %d elemento? Esto no se puede deshacer."] =
                "Excluir este item? Isso não pode ser desfeito.",
            ["%d notas"] =
                "%d notas",
            ["%d nota"] =
                "%d nota",
            ["Buscar en todo"] =
                "Procurar em tudo",
            ["Reciente"] =
                "Recentes",
            ["Guardados"] =
                "Salvos",
            ["Seleccionar"] =
                "Selecionar",
            ["Nuevo"] =
                "Novo",
            ["Todos"] =
                "Todos",
            ["Cancelar"] =
                "Cancelar",
            ["Aceptar"] =
                "OK",
            ["Guardar"] =
                "Salvar",
            ["Aplicar"] =
                "Aplicar",
            ["Agregar"] =
                "Adicionar",
            ["Confirmar"] =
                "Confirmar",
            ["Sí, borrar"] =
                "Sim, excluir",
            ["En pausa"] =
                "Em pausa",
            ["Pausar la captura"] =
                "Pausar a captura",
            ["Reanudar la captura"] =
                "Retomar a captura",
            ["Apariencia"] =
                "Aparência",
            ["Cerrar"] =
                "Fechar",
            ["Copia algo y aparecerá aquí"] =
                "Copie algo e aparecerá aqui",
            ["Vacío. Usa Nuevo para guardar un texto"] =
                "Vazio. Use Novo para salvar um texto",
            ["Nada coincide con esa búsqueda"] =
                "Nada corresponde a essa busca",
            ["Imagen copiada"] =
                "Imagem copiada",
            ["captura"] =
                "captura",
            ["Marcadores"] =
                "Favoritos",
            ["Notas"] =
                "Notas",
            ["Abrir en el navegador"] =
                "Abrir no navegador",
            ["Pegar"] =
                "Colar",
            ["Pegar sin formato"] =
                "Colar sem formatação",
            ["Copiar"] =
                "Copiar",
            ["Fijar arriba"] =
                "Fixar no topo",
            ["Quitar de arriba"] =
                "Desafixar",
            ["Editar y guardar..."] =
                "Editar e salvar...",
            ["Borrar"] =
                "Excluir",
            ["Todas las carpetas"] =
                "Todas as pastas",
            ["Nueva carpeta"] =
                "Nova pasta",
            ["Nueva carpeta..."] =
                "Nova pasta...",
            ["Nombre de la carpeta"] =
                "Nome da pasta",
            ["Renombrar carpeta"] =
                "Renomear pasta",
            ["Nuevo nombre"] =
                "Novo nome",
            ["Agregar una lista"] =
                "Adicionar uma lista",
            ["Vaciar el historial"] =
                "Limpar o histórico",
            ["¿Vaciar el historial? Los fijados se quedan."] =
                "Limpar o histórico? Os fixados permanecem.",
            ["Nuevo texto"] =
                "Novo texto",
            ["Editar texto"] =
                "Editar texto",
            ["Guardar en"] =
                "Salvar em",
            ["Escribe [[algo]] y el programa te lo preguntará antes de pegar"] =
                "Escreva [[algo]] e será perguntado antes de colar",
            ["Completar antes de pegar"] =
                "Preencher antes de colar",
            ["Una nota por cada línea"] =
                "Uma nota por linha",
            ["Todo junto en una sola nota"] =
                "Tudo junto em uma única nota",
            ["Quitar numeración y viñetas"] =
                "Remover numeração e marcadores",
            ["Agregar a %s"] =
                "Adicionar a %s",
            ["Fondo"] =
                "Fundo",
            ["Color de acento"] =
                "Cor de destaque",
            ["Atajo para abrir"] =
                "Atalho para abrir",
            ["Idioma"] =
                "Idioma",
            ["Según Windows"] =
                "Conforme o Windows",
            ["Oscuro"] =
                "Escuro",
            ["Claro"] =
                "Claro",
            ["Medianoche"] =
                "Meia-noite",
            ["Grafito"] =
                "Grafite",
            ["Bosque"] =
                "Floresta",
            ["Papel"] =
                "Papel",
            ["Niebla"] =
                "Neblina",
            ["Arena"] =
                "Areia",
            ["Lila"] =
                "Lilás",
            ["Salvia"] =
                "Sálvia",
            ["Rubor"] =
                "Rubor",
            ["Carpetas"] =
                "Pastas",
            ["Cómo se enseñan"] =
                "Como aparecem",
            ["Lista desplegable"] =
                "Lista suspensa",
            ["Fichas en fila"] =
                "Fichas em linha",
            ["Sistema"] =
                "Sistema",
            ["Ya hay una carpeta llamada %s."] =
                "Já existe uma pasta chamada %s.",
            ["No se pudo renombrar: ya hay una carpeta %s."] =
                "Não foi possível renomear: já existe uma pasta %s.",
            ["%s ya lo usa otro programa. Se dejó el anterior."] =
                "%s já é usado por outro programa. O anterior foi mantido.",
            ["No se pudo copiar al portapapeles."] =
                "Não foi possível copiar para a área de transferência.",
            ["No se pudo abrir el enlace."] =
                "Não foi possível abrir o link.",
            ["Copiado, pero no pude volver a la ventana anterior. Pega con Ctrl+V."] =
                "Copiado, mas não consegui voltar à janela anterior. Cole com Ctrl+V.",
        };

    static readonly IReadOnlyDictionary<string, string> _fr =
        new Dictionary<string, string>
        {
            ["%d caracteres"] =
                "%d caractères",
            ["Borrar (%d)"] =
                "Supprimer (%d)",
            ["Renombrar %s"] =
                "Renommer %s",
            ["Eliminar %s y su contenido"] =
                "Supprimer %s et son contenu",
            ["¿Eliminar la carpeta %s?"] =
                "Supprimer le dossier %s ?",
            ["¿Eliminar la carpeta %s y sus %d textos? Esto no se puede deshacer."] =
                "Supprimer le dossier %s et ses %d textes ? Action irréversible.",
            ["¿Eliminar la carpeta %s y su texto? Esto no se puede deshacer."] =
                "Supprimer le dossier %s et son texte ? Action irréversible.",
            ["¿Borrar %d elementos? Esto no se puede deshacer."] =
                "Supprimer %d éléments ? Action irréversible.",
            ["¿Borrar %d elemento? Esto no se puede deshacer."] =
                "Supprimer cet élément ? Action irréversible.",
            ["%d notas"] =
                "%d notes",
            ["%d nota"] =
                "%d note",
            ["Buscar en todo"] =
                "Rechercher partout",
            ["Reciente"] =
                "Récent",
            ["Guardados"] =
                "Enregistrés",
            ["Seleccionar"] =
                "Sélectionner",
            ["Nuevo"] =
                "Nouveau",
            ["Todos"] =
                "Tout",
            ["Cancelar"] =
                "Annuler",
            ["Aceptar"] =
                "OK",
            ["Guardar"] =
                "Enregistrer",
            ["Aplicar"] =
                "Appliquer",
            ["Agregar"] =
                "Ajouter",
            ["Confirmar"] =
                "Confirmer",
            ["Sí, borrar"] =
                "Oui, supprimer",
            ["En pausa"] =
                "En pause",
            ["Pausar la captura"] =
                "Suspendre la capture",
            ["Reanudar la captura"] =
                "Reprendre la capture",
            ["Apariencia"] =
                "Apparence",
            ["Cerrar"] =
                "Fermer",
            ["Copia algo y aparecerá aquí"] =
                "Copiez quelque chose, il apparaîtra ici",
            ["Vacío. Usa Nuevo para guardar un texto"] =
                "Vide. Utilisez Nouveau pour enregistrer un texte",
            ["Nada coincide con esa búsqueda"] =
                "Aucun résultat pour cette recherche",
            ["Imagen copiada"] =
                "Image copiée",
            ["captura"] =
                "capture",
            ["Marcadores"] =
                "Favoris",
            ["Notas"] =
                "Notes",
            ["Abrir en el navegador"] =
                "Ouvrir dans le navigateur",
            ["Pegar"] =
                "Coller",
            ["Pegar sin formato"] =
                "Coller sans mise en forme",
            ["Copiar"] =
                "Copier",
            ["Fijar arriba"] =
                "Épingler en haut",
            ["Quitar de arriba"] =
                "Détacher",
            ["Editar y guardar..."] =
                "Modifier et enregistrer...",
            ["Borrar"] =
                "Supprimer",
            ["Todas las carpetas"] =
                "Tous les dossiers",
            ["Nueva carpeta"] =
                "Nouveau dossier",
            ["Nueva carpeta..."] =
                "Nouveau dossier...",
            ["Nombre de la carpeta"] =
                "Nom du dossier",
            ["Renombrar carpeta"] =
                "Renommer le dossier",
            ["Nuevo nombre"] =
                "Nouveau nom",
            ["Agregar una lista"] =
                "Ajouter une liste",
            ["Vaciar el historial"] =
                "Vider l'historique",
            ["¿Vaciar el historial? Los fijados se quedan."] =
                "Vider l'historique ? Les épinglés restent.",
            ["Nuevo texto"] =
                "Nouveau texte",
            ["Editar texto"] =
                "Modifier le texte",
            ["Guardar en"] =
                "Enregistrer dans",
            ["Escribe [[algo]] y el programa te lo preguntará antes de pegar"] =
                "Écrivez [[quelque chose]] et on vous le demandera avant de coller",
            ["Completar antes de pegar"] =
                "Compléter avant de coller",
            ["Una nota por cada línea"] =
                "Une note par ligne",
            ["Todo junto en una sola nota"] =
                "Tout dans une seule note",
            ["Quitar numeración y viñetas"] =
                "Retirer numérotation et puces",
            ["Agregar a %s"] =
                "Ajouter à %s",
            ["Fondo"] =
                "Fond",
            ["Color de acento"] =
                "Couleur d'accent",
            ["Atajo para abrir"] =
                "Raccourci d'ouverture",
            ["Idioma"] =
                "Langue",
            ["Según Windows"] =
                "Selon Windows",
            ["Oscuro"] =
                "Sombre",
            ["Claro"] =
                "Clair",
            ["Medianoche"] =
                "Minuit",
            ["Grafito"] =
                "Graphite",
            ["Bosque"] =
                "Forêt",
            ["Papel"] =
                "Papier",
            ["Niebla"] =
                "Brume",
            ["Arena"] =
                "Sable",
            ["Lila"] =
                "Lilas",
            ["Salvia"] =
                "Sauge",
            ["Rubor"] =
                "Rougeur",
            ["Carpetas"] =
                "Dossiers",
            ["Cómo se enseñan"] =
                "Comment les afficher",
            ["Lista desplegable"] =
                "Liste déroulante",
            ["Fichas en fila"] =
                "Puces en ligne",
            ["Sistema"] =
                "Système",
            ["Ya hay una carpeta llamada %s."] =
                "Il existe déjà un dossier nommé %s.",
            ["No se pudo renombrar: ya hay una carpeta %s."] =
                "Renommage impossible : il existe déjà un dossier %s.",
            ["%s ya lo usa otro programa. Se dejó el anterior."] =
                "%s est déjà utilisé par un autre programme. Le précédent est conservé.",
            ["No se pudo copiar al portapapeles."] =
                "Impossible de copier dans le presse-papiers.",
            ["No se pudo abrir el enlace."] =
                "Impossible d'ouvrir le lien.",
            ["Copiado, pero no pude volver a la ventana anterior. Pega con Ctrl+V."] =
                "Copié, mais impossible de revenir à la fenêtre précédente. Collez avec Ctrl+V.",
        };

    /// <summary>
    /// Va detras de las tres tablas a proposito: los inicializadores
    /// estaticos corren en orden textual y por delante las recogia a
    /// todas a null, que dejaba muertas las traducciones enteras.
    /// </summary>
    static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>
        _tablas = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["en"] = _en,
            ["pt"] = _pt,
            ["fr"] = _fr,
        };
}
