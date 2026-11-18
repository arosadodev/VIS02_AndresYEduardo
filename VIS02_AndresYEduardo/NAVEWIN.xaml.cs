// =============================================================================
// NAVEWIN.xaml.cs
// -----------------------------------------------------------------------------
// "Code-behind" de la ventana principal (NAVEWIN).
//
// Responsabilidades:
//   1. Construir un arbol de carpetas y archivos a partir de una carpeta raiz.
//   2. Cargar el contenido de cada carpeta SOLO cuando el usuario la expande
//      (carga diferida o "lazy loading"), para no bloquear la aplicacion.
//   3. Abrir la ventana DETAILWIN al hacer clic en un archivo.
//   4. Permitir elegir otra carpeta raiz y cambiar entre modo normal y noche.
// =============================================================================

using System;                       // Tipos basicos: Environment, Uri, etc.
using System.IO;                    // DirectoryInfo, FileInfo, File, Path...
using System.Windows;               // Window, RoutedEventArgs, Application...
using System.Windows.Controls;      // TreeView, TreeViewItem, CheckBox, Label...
using Application = System.Windows.Application; // Desambigua con System.Windows.Forms.Application
using Pincel = System.Windows.Media.Brush; // Alias: "Pincel" es el tipo Brush de WPF.
                                           // Se usa un alias para que el codigo se lea en espanol.

namespace VIS02_AndresYEduardo;

/// <summary>
/// Ventana principal de la aplicacion.
/// Hereda de Window (una ventana de WPF) y es "partial" porque su parte visual
/// esta en NAVEWIN.xaml y se une a esta clase al compilar.
/// </summary>
public partial class NAVEWIN : Window
{
    // Carpeta que se esta mostrando actualmente en el arbol.
    // DirectoryInfo es la clase de .NET que representa una carpeta del disco.
    private DirectoryInfo _directorioActual;

    // Indica si el modo noche esta activo. Es publica (solo lectura desde fuera)
    // para que DETAILWIN pueda consultarla y aplicar el mismo tema.
    public bool EsModoNoche { get; private set; }

    /// <summary>
    /// Constructor: se ejecuta una sola vez, al crear la ventana
    /// (WPF la crea automaticamente gracias a StartupUri="NAVEWIN.xaml").
    /// </summary>
    public NAVEWIN()
    {
        // Carga los controles definidos en el XAML (crea TvDirectorio, LblRutaActual, etc.).
        InitializeComponent();

        // Aplica el tema inicial (Modo Normal) a todos los controles y recursos
        AplicarTema(false);

        // Carpeta inicial: la carpeta "Mis documentos" del usuario actual.
        // SpecialFolder.MyDocuments se traduce, por ejemplo, a C:\Users\Nombre\Documents.
        string rutaMisDocumentos =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // Se guarda como DirectoryInfo y se pinta el arbol por primera vez.
        _directorioActual = new DirectoryInfo(rutaMisDocumentos);
        CargarDirectorio(_directorioActual);
    }

    /// <summary>
    /// Limpia el arbol y lo vuelve a construir tomando <paramref name="directorio"/>
    /// como raiz. Se usa al iniciar y al elegir otra carpeta.
    /// </summary>
    private void CargarDirectorio(DirectoryInfo directorio)
    {
        // Borra todos los nodos anteriores para no duplicar contenido.
        TvDirectorio.Items.Clear();

        // Muestra la ruta completa en la etiqueta superior.
        LblRutaActual.Content = directorio.FullName;

        // Crea el nodo raiz (leyendo su contenido completo) y lo agrega al arbol.
        TreeViewItem elementoRaiz = CrearElementoDirectorio(directorio);
        TvDirectorio.Items.Add(elementoRaiz);

        // Deja la carpeta raiz abierta para que el usuario vea su contenido de inmediato.
        elementoRaiz.IsExpanded = true;
    }

    /// <summary>
    /// Crea el nodo de una carpeta LEYENDO su contenido en ese momento.
    /// Se usa para la carpeta raiz; sus subcarpetas se crean con carga diferida.
    /// </summary>
    private TreeViewItem CrearElementoDirectorio(DirectoryInfo directorio)
    {
        // Header = texto visible (el nombre de la carpeta).
        // Tag    = dato oculto asociado al nodo; aqui se guarda la RUTA COMPLETA,
        //          que es lo que necesitaremos despues para abrir o leer cosas.
        TreeViewItem elementoDirectorio = new()
        {
            Header = directorio.Name,
            Tag = directorio.FullName
        };

        try
        {
            // Primero se agregan las subcarpetas (cada una con carga diferida)...
            foreach (DirectoryInfo subdirectorio in directorio.GetDirectories())
            {
                elementoDirectorio.Items.Add(
                    CrearElementoDirectorioConCargaDiferida(subdirectorio));
            }

            // ...y despues los archivos que hay directamente en esta carpeta.
            foreach (FileInfo archivo in directorio.GetFiles())
            {
                elementoDirectorio.Items.Add(CrearElementoArchivo(archivo));
            }
        }
        catch (UnauthorizedAccessException)
        {
            // La carpeta existe pero Windows no permite leerla (por ejemplo,
            // carpetas del sistema). En lugar de fallar, se muestra un aviso.
            AgregarMensaje(elementoDirectorio, "Acceso denegado");
        }
        catch (IOException)
        {
            // Error de entrada/salida: disco desconectado, ruta no disponible, etc.
            AgregarMensaje(elementoDirectorio, "No se puede leer el directorio");
        }

        return elementoDirectorio;
    }

    /// <summary>
    /// Crea el nodo de una subcarpeta SIN leer su contenido todavia.
    /// Es la clave de la carga diferida: solo se lee del disco cuando el usuario
    /// expande el nodo.
    /// </summary>
    private TreeViewItem CrearElementoDirectorioConCargaDiferida(DirectoryInfo directorio)
    {
        TreeViewItem elementoDirectorio = new()
        {
            Header = directorio.Name,
            Tag = directorio.FullName
        };

        // Se agrega un "hijo falso" (null). Sirve para que WPF dibuje la flechita
        // de expansion, aunque en realidad todavia no se conoce el contenido.
        elementoDirectorio.Items.Add(null);

        // Se suscribe al evento Expanded: cuando el usuario abra este nodo,
        // se ejecutara SubElemento_Expandido y se cargara el contenido real.
        elementoDirectorio.Expanded += SubElemento_Expandido;
        return elementoDirectorio;
    }

    /// <summary>
    /// Crea el nodo de un archivo. Es estatico porque no necesita acceder a
    /// ningun dato de la ventana: solo usa los datos del archivo recibido.
    /// </summary>
    private static TreeViewItem CrearElementoArchivo(FileInfo archivo)
    {
        return new TreeViewItem
        {
            Header = archivo.Name,      // Nombre visible del archivo
            Tag = archivo.FullName      // Ruta completa, usada luego por DETAILWIN
        };
    }

    /// <summary>
    /// Anade al nodo un hijo deshabilitado con un mensaje de error,
    /// para informar al usuario sin romper la aplicacion.
    /// </summary>
    private static void AgregarMensaje(TreeViewItem elementoPadre, string mensaje)
    {
        elementoPadre.Items.Add(new TreeViewItem
        {
            Header = mensaje,
            IsEnabled = false // El nodo se ve en gris y no se puede seleccionar.
        });
    }

    /// <summary>
    /// Se ejecuta cuando el usuario expande una subcarpeta.
    /// Sustituye el "hijo falso" (null) por el contenido real de la carpeta.
    /// </summary>
    private void SubElemento_Expandido(object remitente, RoutedEventArgs argumentos)
    {
        // Comprobaciones de seguridad:
        // 1. El remitente debe ser un TreeViewItem (un nodo del arbol).
        // 2. OriginalSource debe ser ese mismo nodo: el evento Expanded "burbujea"
        //    desde los hijos hacia los padres, y solo queremos atender la expansion
        //    del nodo concreto que se abrio.
        // 3. Debe tener exactamente 1 hijo.
        // 4. Ese unico hijo debe ser null (el marcador de "aun no cargado").
        // Si algo no se cumple, se sale del metodo sin hacer nada.
        if (remitente is not TreeViewItem elementoDirectorio ||
            argumentos.OriginalSource != elementoDirectorio ||
            elementoDirectorio.Items.Count != 1 ||
            elementoDirectorio.Items[0] is not null)
        {
            return;
        }

        // Se elimina el marcador null antes de cargar el contenido real.
        elementoDirectorio.Items.Clear();

        // El Tag guarda la ruta completa de la carpeta. Si por algun motivo
        // no fuera un string, no se puede continuar.
        if (elementoDirectorio.Tag is not string rutaDirectorio)
        {
            return;
        }

        try
        {
            DirectoryInfo directorio = new(rutaDirectorio);

            // Mismas operaciones que en CrearElementoDirectorio: primero carpetas
            // (de nuevo con carga diferida) y luego archivos.
            foreach (DirectoryInfo subdirectorio in directorio.GetDirectories())
            {
                elementoDirectorio.Items.Add(
                    CrearElementoDirectorioConCargaDiferida(subdirectorio));
            }

            foreach (FileInfo archivo in directorio.GetFiles())
            {
                elementoDirectorio.Items.Add(CrearElementoArchivo(archivo));
            }
        }
        catch (UnauthorizedAccessException)
        {
            AgregarMensaje(elementoDirectorio, "Acceso denegado");
        }
        catch (IOException)
        {
            AgregarMensaje(elementoDirectorio, "No se puede leer el directorio");
        }
    }

    /// <summary>
    /// Evento del boton "Seleccionar Carpeta".
    /// Abre el dialogo nativo de Windows para elegir una carpeta y recarga el arbol.
    /// </summary>
    private void SeleccionarCarpeta_Click(object remitente, RoutedEventArgs argumentos)
    {
        // FolderBrowserDialog pertenece a Windows Forms (no a WPF); por eso el
        // proyecto activa UseWindowsForms en el .csproj.
        // "using" garantiza que el dialogo se libere al terminar el metodo.
        using System.Windows.Forms.FolderBrowserDialog dialogo = new()
        {
            Description = "Seleccione una carpeta para explorar",
            UseDescriptionForTitle = true,                    // Usa Description como titulo del dialogo
            SelectedPath = _directorioActual.FullName,        // Empieza en la carpeta actual
            ShowNewFolderButton = false                       // Oculta el boton de crear carpeta nueva
        };

        // Si el usuario cancela o no elige nada, se termina sin cambios.
        if (dialogo.ShowDialog() != System.Windows.Forms.DialogResult.OK ||
            string.IsNullOrWhiteSpace(dialogo.SelectedPath))
        {
            return;
        }

        // Se guarda la carpeta elegida y se reconstruye el arbol desde ella.
        _directorioActual = new DirectoryInfo(dialogo.SelectedPath);
        CargarDirectorio(_directorioActual);
    }

    /// <summary>
    /// Evento del TreeView: se dispara cada vez que cambia el nodo seleccionado.
    /// Si el nodo seleccionado es un ARCHIVO, abre la ventana de detalle.
    /// </summary>
    private void Directorio_ElementoSeleccionado(
        object remitente,
        RoutedPropertyChangedEventArgs<object> argumentos)
    {
        // NewValue es el nuevo elemento seleccionado.
        // - Si no es un TreeViewItem, se ignora.
        // - Si su Tag no es una ruta (string), se ignora.
        // - Si la ruta NO corresponde a un archivo (es una carpeta o un mensaje
        //   de error), tambien se ignora.
        if (argumentos.NewValue is not TreeViewItem elementoSeleccionado ||
            elementoSeleccionado.Tag is not string rutaArchivo ||
            !File.Exists(rutaArchivo))
        {
            return;
        }

        // Se crea la ventana de detalle pasandole la ruta del archivo.
        // Owner = this hace que NAVEWIN sea la ventana "duena" de DETAILWIN.
        DETAILWIN ventanaDetalle = new(rutaArchivo)
        {
            Owner = this
        };
        ventanaDetalle.ShowDialog();
        elementoSeleccionado.IsSelected = false;
    }

    /// <summary>
    /// Evento de la casilla "Modo Noche" (se ejecuta al marcarla y al desmarcarla).
    /// Cambia el fondo de la ventana principal y avisa a las ventanas hijas abiertas.
    /// </summary>
    private void ModoNoche_Cambio(object remitente, RoutedEventArgs argumentos)
    {
        // IsChecked es bool? (puede ser true, false o null si la casilla es de
        // tres estados). Se considera modo noche solo si vale exactamente true.
        EsModoNoche = ChkModoNoche.IsChecked == true;

        // Aplica el tema completo a todos los controles (arbol, textos, fondo, etc.)
        AplicarTema(EsModoNoche);

        // Recorre las ventanas hijas (las que tienen Owner = this) y pide a las
        // que sean DETAILWIN que sincronicen su tema con el de esta ventana.
        foreach (Window ventana in OwnedWindows)
        {
            if (ventana is DETAILWIN detalle)
            {
                detalle.SincronizarTemaConOwner();
            }
        }
    }

    /// <summary>
    /// Aplica el tema (Normal o Noche) a los recursos globales de la aplicacion
    /// y directamente a cada control de NAVEWIN (fondo, arbol, textos, bordes).
    /// La cabecera azul conserva siempre su color fijo y no se modifica.
    /// </summary>
    public void AplicarTema(bool noche)
    {
        var res = Application.Current.Resources;
        string prefix = noche ? "Noche" : "Normal";

        // 1. Actualiza los recursos globales dinamicos para que cualquier control
        //    que use DynamicResource se actualice solo.
        res["CurrentBackgroundBrush"] = res[$"{prefix}BackgroundBrush"];
        res["CurrentForegroundBrush"] = res[$"{prefix}ForegroundBrush"];
        res["CurrentControlBgBrush"] = res[$"{prefix}ControlBgBrush"];
        res["CurrentBorderBrush"] = res[$"{prefix}BorderBrush"];

        // 2. Asigna directamente a los controles de NAVEWIN para asegurar
        //    que el fondo, arbol, bordes y etiquetas cambien al instante.
        Pincel fondo = (Pincel)res[$"{prefix}BackgroundBrush"];
        Pincel primerPlano = (Pincel)res[$"{prefix}ForegroundBrush"];
        Pincel fondoControl = (Pincel)res[$"{prefix}ControlBgBrush"];
        Pincel borde = (Pincel)res[$"{prefix}BorderBrush"];

        this.Background = fondo;
        MainGrid.Background = fondo;
        ChkModoNoche.Foreground = primerPlano;
        LblRutaActual.Foreground = primerPlano;

        MainGrid.Background =
            (Pincel)System.Windows.Application.Current.FindResource(claveRecurso);

        foreach (Window ventana in OwnedWindows)
        {
            if (ventana is DETAILWIN detalle)
            {
                detalle.SincronizarTemaConOwner();
            }
        }
    }
}
