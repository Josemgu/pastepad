# -*- coding: utf-8 -*-
"""Los textos de la interfaz en varios idiomas.

La clave es el texto en espaniol, no un identificador inventado. Dos
razones: el codigo se sigue leyendo sin ir a buscar que significa
"btn.paste.plain", y si a una traduccion le falta una frase sale la
espaniola en vez de un hueco o el nombre de la clave.

Anadir un idioma es anadir un diccionario aqui y su nombre en NOMBRES.
Lo que no se traduzca cae al espaniol solo.

No importa flet: se puede probar sin abrir una ventana.
"""

NOMBRES = {
    "es": "Espanol",
    "en": "English",
    "pt": "Portugues",
    "fr": "Francais",
}
IDIOMA_DEF = "es"

EN = {
    # --- textos con numero dentro
    '%d caracteres': '%d characters',
    'Borrar (%d)': 'Delete (%d)',
    'Editar %s...': 'Edit %s...',
    'Renombrar %s': 'Rename %s',
    'Eliminar %s y su contenido': 'Delete %s and its contents',
    'Eliminar la carpeta %s?': 'Delete folder %s?',
    'Eliminar la carpeta %s y sus %d textos? Esto no se puede deshacer.': 'Delete folder %s and its %d texts? This cannot be undone.',
    'Eliminar la carpeta %s y su texto? Esto no se puede deshacer.': 'Delete folder %s and its text? This cannot be undone.',
    'Borrar %d elementos? Esto no se puede deshacer.': 'Delete %d items? This cannot be undone.',
    'Borrar %d elemento? Esto no se puede deshacer.': 'Delete this item? This cannot be undone.',
    '%d notas': '%d notes',
    '%d nota': '%d note',
    # --- panel
    "Buscar en todo": "Search everything",
    "Reciente": "Recent",
    "Guardados": "Saved",
    "Seleccionar": "Select",
    "Nuevo": "New",
    "Todos": "All",
    "Cancelar": "Cancel",
    "Aceptar": "OK",
    "Guardar": "Save",
    "Aplicar": "Apply",
    "Agregar": "Add",
    "Quitar": "Remove",
    "Recuperar": "Undo",
    "Confirmar": "Confirm",
    "Si, borrar": "Yes, delete",
    "En pausa": "Paused",
    # --- cabecera
    "Pausar la captura": "Pause capture",
    "Reanudar la captura": "Resume capture",
    "Apariencia": "Appearance",
    "Cerrar": "Close",
    # --- estados vacios
    "Copia algo y aparecera aqui": "Copy something and it shows up here",
    "Vacio. Usa Nuevo para guardar un texto":
        "Empty. Use New to save a text",
    "Nada coincide con esa busqueda": "Nothing matches that search",
    "La carpeta esta vacia": "This folder is empty",
    # --- filas
    "Imagen copiada": "Copied image",
    "captura": "screenshot",
    "caracteres": "characters",
    "Marcadores": "Bookmarks",
    "Notas": "Notes",
    # --- menu de fila
    "Abrir en el navegador": "Open in browser",
    "Pegar": "Paste",
    "Pegar sin formato": "Paste as plain text",
    "Copiar": "Copy",
    "Fijar arriba": "Pin to top",
    "Quitar de arriba": "Unpin",
    "Editar y guardar...": "Edit and save...",
    "Editar...": "Edit...",
    "Borrar": "Delete",
    # --- carpetas
    "Todas las carpetas": "All folders",
    "Nueva carpeta": "New folder",
    "Nueva carpeta...": "New folder...",
    "Nombre de la carpeta": "Folder name",
    "Renombrar carpeta": "Rename folder",
    "Nuevo nombre": "New name",
    "Editar carpeta": "Edit folder",
    "Contenido": "Contents",
    "Agregar una lista": "Add a list",
    "Elige primero una carpeta.": "Pick a folder first.",
    "Vaciar el historial": "Clear history",
    "Vaciar el historial? Los fijados se quedan.":
        "Clear the history? Pinned items stay.",
    # --- dialogo de texto
    "Nuevo texto": "New text",
    "Editar texto": "Edit text",
    "Texto": "Text",
    "Enlace": "Link",
    "Guardar en": "Save into",
    "Mis textos": "My texts",
    "Escribe o pega aqui": "Write or paste here",
    "Escribe [[algo]] y el programa te lo preguntara al pegar":
        "Write [[anything]] and you'll be asked for it before pasting",
    "Direccion": "Address",
    "Titulo": "Title",
    "Como quieres llamarlo": "What to call it",
    # --- plantillas y listas
    "Completar antes de pegar": "Fill in before pasting",
    "Pega aqui tu lista": "Paste your list here",
    "Una nota por cada linea": "One note per line",
    "Todo junto en una sola nota": "All together in a single note",
    "Quitar numeracion y vinetas": "Strip numbering and bullets",
    "notas": "notes",
    "nota": "note",
    "Agregar a": "Add to",
    # --- apariencia
    "Fondo": "Background",
    "Color de acento": "Accent colour",
    "Atajo para abrir": "Shortcut to open",
    "Idioma": "Language",
    "Segun Windows": "Follow Windows",
    "Oscuro": "Dark", "Claro": "Light", "Medianoche": "Midnight",
    "Grafito": "Graphite", "Bosque": "Forest", "Papel": "Paper",
    "Niebla": "Mist", "Arena": "Sand", "Lila": "Lilac",
    "Salvia": "Sage", "Rubor": "Blush",
}

PT = {
    # --- textos con numero dentro
    '%d caracteres': '%d caracteres',
    'Borrar (%d)': 'Excluir (%d)',
    'Editar %s...': 'Editar %s...',
    'Renombrar %s': 'Renomear %s',
    'Eliminar %s y su contenido': 'Excluir %s e seu conteudo',
    'Eliminar la carpeta %s?': 'Excluir a pasta %s?',
    'Eliminar la carpeta %s y sus %d textos? Esto no se puede deshacer.': 'Excluir a pasta %s e seus %d textos? Isso nao pode ser desfeito.',
    'Eliminar la carpeta %s y su texto? Esto no se puede deshacer.': 'Excluir a pasta %s e seu texto? Isso nao pode ser desfeito.',
    'Borrar %d elementos? Esto no se puede deshacer.': 'Excluir %d itens? Isso nao pode ser desfeito.',
    'Borrar %d elemento? Esto no se puede deshacer.': 'Excluir este item? Isso nao pode ser desfeito.',
    '%d notas': '%d notas',
    '%d nota': '%d nota',
    "Buscar en todo": "Procurar em tudo",
    "Reciente": "Recentes", "Guardados": "Salvos",
    "Seleccionar": "Selecionar", "Nuevo": "Novo", "Todos": "Todos",
    "Cancelar": "Cancelar", "Aceptar": "OK", "Guardar": "Salvar",
    "Aplicar": "Aplicar", "Agregar": "Adicionar", "Quitar": "Remover",
    "Recuperar": "Desfazer", "Confirmar": "Confirmar",
    "Si, borrar": "Sim, excluir", "En pausa": "Em pausa",
    "Pausar la captura": "Pausar a captura",
    "Reanudar la captura": "Retomar a captura",
    "Apariencia": "Aparencia", "Cerrar": "Fechar",
    "Copia algo y aparecera aqui": "Copie algo e aparecera aqui",
    "Vacio. Usa Nuevo para guardar un texto":
        "Vazio. Use Novo para salvar um texto",
    "Nada coincide con esa busqueda": "Nada corresponde a essa busca",
    "La carpeta esta vacia": "A pasta esta vazia",
    "Imagen copiada": "Imagem copiada", "captura": "captura",
    "caracteres": "caracteres",
    "Marcadores": "Favoritos", "Notas": "Notas",
    "Abrir en el navegador": "Abrir no navegador",
    "Pegar": "Colar", "Pegar sin formato": "Colar sem formatacao",
    "Copiar": "Copiar", "Fijar arriba": "Fixar no topo",
    "Quitar de arriba": "Desafixar",
    "Editar y guardar...": "Editar e salvar...", "Editar...": "Editar...",
    "Borrar": "Excluir",
    "Todas las carpetas": "Todas as pastas",
    "Nueva carpeta": "Nova pasta", "Nueva carpeta...": "Nova pasta...",
    "Nombre de la carpeta": "Nome da pasta",
    "Renombrar carpeta": "Renomear pasta", "Nuevo nombre": "Novo nome",
    "Editar carpeta": "Editar pasta", "Contenido": "Conteudo",
    "Agregar una lista": "Adicionar uma lista",
    "Elige primero una carpeta.": "Escolha uma pasta primeiro.",
    "Vaciar el historial": "Limpar o historico",
    "Vaciar el historial? Los fijados se quedan.":
        "Limpar o historico? Os fixados permanecem.",
    "Nuevo texto": "Novo texto", "Editar texto": "Editar texto",
    "Texto": "Texto", "Enlace": "Link", "Guardar en": "Salvar em",
    "Mis textos": "Meus textos",
    "Escribe o pega aqui": "Escreva ou cole aqui",
    "Escribe [[algo]] y el programa te lo preguntara al pegar":
        "Escreva [[algo]] e sera perguntado antes de colar",
    "Direccion": "Endereco", "Titulo": "Titulo",
    "Como quieres llamarlo": "Como quer chamar",
    "Completar antes de pegar": "Preencher antes de colar",
    "Pega aqui tu lista": "Cole sua lista aqui",
    "Una nota por cada linea": "Uma nota por linha",
    "Todo junto en una sola nota": "Tudo junto em uma unica nota",
    "Quitar numeracion y vinetas": "Remover numeracao e marcadores",
    "notas": "notas", "nota": "nota", "Agregar a": "Adicionar a",
    "Fondo": "Fundo", "Color de acento": "Cor de destaque",
    "Atajo para abrir": "Atalho para abrir", "Idioma": "Idioma",
    "Segun Windows": "Conforme o Windows",
    "Oscuro": "Escuro", "Claro": "Claro", "Medianoche": "Meia-noite",
    "Grafito": "Grafite", "Bosque": "Floresta", "Papel": "Papel",
    "Niebla": "Neblina", "Arena": "Areia", "Lila": "Lilas",
    "Salvia": "Salvia", "Rubor": "Rubor",
}

FR = {
    # --- textos con numero dentro
    '%d caracteres': '%d caracteres',
    'Borrar (%d)': 'Supprimer (%d)',
    'Editar %s...': 'Modifier %s...',
    'Renombrar %s': 'Renommer %s',
    'Eliminar %s y su contenido': 'Supprimer %s et son contenu',
    'Eliminar la carpeta %s?': 'Supprimer le dossier %s ?',
    'Eliminar la carpeta %s y sus %d textos? Esto no se puede deshacer.': 'Supprimer le dossier %s et ses %d textes ? Action irreversible.',
    'Eliminar la carpeta %s y su texto? Esto no se puede deshacer.': 'Supprimer le dossier %s et son texte ? Action irreversible.',
    'Borrar %d elementos? Esto no se puede deshacer.': 'Supprimer %d elements ? Action irreversible.',
    'Borrar %d elemento? Esto no se puede deshacer.': 'Supprimer cet element ? Action irreversible.',
    '%d notas': '%d notes',
    '%d nota': '%d note',
    "Buscar en todo": "Rechercher partout",
    "Reciente": "Recent", "Guardados": "Enregistres",
    "Seleccionar": "Selectionner", "Nuevo": "Nouveau", "Todos": "Tout",
    "Cancelar": "Annuler", "Aceptar": "OK", "Guardar": "Enregistrer",
    "Aplicar": "Appliquer", "Agregar": "Ajouter", "Quitar": "Retirer",
    "Recuperar": "Annuler", "Confirmar": "Confirmer",
    "Si, borrar": "Oui, supprimer", "En pausa": "En pause",
    "Pausar la captura": "Suspendre la capture",
    "Reanudar la captura": "Reprendre la capture",
    "Apariencia": "Apparence", "Cerrar": "Fermer",
    "Copia algo y aparecera aqui": "Copiez quelque chose, il apparaitra ici",
    "Vacio. Usa Nuevo para guardar un texto":
        "Vide. Utilisez Nouveau pour enregistrer un texte",
    "Nada coincide con esa busqueda": "Aucun resultat pour cette recherche",
    "La carpeta esta vacia": "Ce dossier est vide",
    "Imagen copiada": "Image copiee", "captura": "capture",
    "caracteres": "caracteres",
    "Marcadores": "Favoris", "Notas": "Notes",
    "Abrir en el navegador": "Ouvrir dans le navigateur",
    "Pegar": "Coller", "Pegar sin formato": "Coller sans mise en forme",
    "Copiar": "Copier", "Fijar arriba": "Epingler en haut",
    "Quitar de arriba": "Detacher",
    "Editar y guardar...": "Modifier et enregistrer...",
    "Editar...": "Modifier...", "Borrar": "Supprimer",
    "Todas las carpetas": "Tous les dossiers",
    "Nueva carpeta": "Nouveau dossier",
    "Nueva carpeta...": "Nouveau dossier...",
    "Nombre de la carpeta": "Nom du dossier",
    "Renombrar carpeta": "Renommer le dossier",
    "Nuevo nombre": "Nouveau nom",
    "Editar carpeta": "Modifier le dossier", "Contenido": "Contenu",
    "Agregar una lista": "Ajouter une liste",
    "Elige primero una carpeta.": "Choisissez d'abord un dossier.",
    "Vaciar el historial": "Vider l'historique",
    "Vaciar el historial? Los fijados se quedan.":
        "Vider l'historique ? Les epingles restent.",
    "Nuevo texto": "Nouveau texte", "Editar texto": "Modifier le texte",
    "Texto": "Texte", "Enlace": "Lien", "Guardar en": "Enregistrer dans",
    "Mis textos": "Mes textes",
    "Escribe o pega aqui": "Ecrivez ou collez ici",
    "Escribe [[algo]] y el programa te lo preguntara al pegar":
        "Ecrivez [[quelque chose]] et on vous le demandera avant de coller",
    "Direccion": "Adresse", "Titulo": "Titre",
    "Como quieres llamarlo": "Comment l'appeler",
    "Completar antes de pegar": "Completer avant de coller",
    "Pega aqui tu lista": "Collez votre liste ici",
    "Una nota por cada linea": "Une note par ligne",
    "Todo junto en una sola nota": "Tout dans une seule note",
    "Quitar numeracion y vinetas": "Retirer numerotation et puces",
    "notas": "notes", "nota": "note", "Agregar a": "Ajouter a",
    "Fondo": "Fond", "Color de acento": "Couleur d'accent",
    "Atajo para abrir": "Raccourci d'ouverture", "Idioma": "Langue",
    "Segun Windows": "Selon Windows",
    "Oscuro": "Sombre", "Claro": "Clair", "Medianoche": "Minuit",
    "Grafito": "Graphite", "Bosque": "Foret", "Papel": "Papier",
    "Niebla": "Brume", "Arena": "Sable", "Lila": "Lilas",
    "Salvia": "Sauge", "Rubor": "Rougeur",
}

TRADUCCIONES = {"es": {}, "en": EN, "pt": PT, "fr": FR}

_actual = IDIOMA_DEF


def poner(codigo):
    """Cambia el idioma. Devuelve el que quedo puesto."""
    global _actual
    _actual = codigo if codigo in TRADUCCIONES else IDIOMA_DEF
    return _actual



def t(texto):
    """El texto en el idioma puesto, o el espaniol si no esta traducido."""
    return TRADUCCIONES.get(_actual, {}).get(texto, texto)
