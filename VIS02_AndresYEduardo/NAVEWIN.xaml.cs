using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Pincel = System.Windows.Media.Brush;

namespace VIS02_AndresYEduardo;

public partial class NAVEWIN : Window
{
    private DirectoryInfo _directorioActual;

    public bool EsModoNoche { get; private set; }

    public NAVEWIN()
    {
        InitializeComponent();

        string rutaMisImagenes =
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        _directorioActual = new DirectoryInfo(rutaMisImagenes);
        CargarDirectorio(_directorioActual);
    }

    private void CargarDirectorio(DirectoryInfo directorio)
    {
        TvDirectorio.Items.Clear();
        LblRutaActual.Content = directorio.FullName;

        TreeViewItem elementoRaiz = CrearElementoDirectorio(directorio);
        TvDirectorio.Items.Add(elementoRaiz);
        elementoRaiz.IsExpanded = true;
    }

    private TreeViewItem CrearElementoDirectorio(DirectoryInfo directorio)
    {
        TreeViewItem elementoDirectorio = new()
        {
            Header = directorio.Name,
            Tag = directorio.FullName
        };

        try
        {
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

        return elementoDirectorio;
    }

    private TreeViewItem CrearElementoDirectorioConCargaDiferida(DirectoryInfo directorio)
    {
        TreeViewItem elementoDirectorio = new()
        {
            Header = directorio.Name,
            Tag = directorio.FullName
        };

        elementoDirectorio.Items.Add(null);
        elementoDirectorio.Expanded += SubElemento_Expandido;
        return elementoDirectorio;
    }

    private static TreeViewItem CrearElementoArchivo(FileInfo archivo)
    {
        return new TreeViewItem
        {
            Header = archivo.Name,
            Tag = archivo.FullName
        };
    }

    private static void AgregarMensaje(TreeViewItem elementoPadre, string mensaje)
    {
        elementoPadre.Items.Add(new TreeViewItem
        {
            Header = mensaje,
            IsEnabled = false
        });
    }

    private void SubElemento_Expandido(object remitente, RoutedEventArgs argumentos)
    {
        if (remitente is not TreeViewItem elementoDirectorio ||
            argumentos.OriginalSource != elementoDirectorio ||
            elementoDirectorio.Items.Count != 1 ||
            elementoDirectorio.Items[0] is not null)
        {
            return;
        }

        elementoDirectorio.Items.Clear();

        if (elementoDirectorio.Tag is not string rutaDirectorio)
        {
            return;
        }

        try
        {
            DirectoryInfo directorio = new(rutaDirectorio);

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

    private void SeleccionarCarpeta_Click(object remitente, RoutedEventArgs argumentos)
    {
        using System.Windows.Forms.FolderBrowserDialog dialogo = new()
        {
            Description = "Seleccione una carpeta para explorar",
            UseDescriptionForTitle = true,
            SelectedPath = _directorioActual.FullName,
            ShowNewFolderButton = false
        };

        if (dialogo.ShowDialog() != System.Windows.Forms.DialogResult.OK ||
            string.IsNullOrWhiteSpace(dialogo.SelectedPath))
        {
            return;
        }

        _directorioActual = new DirectoryInfo(dialogo.SelectedPath);
        CargarDirectorio(_directorioActual);
    }

    private void Directorio_ElementoSeleccionado(
        object remitente,
        RoutedPropertyChangedEventArgs<object> argumentos)
    {
        if (argumentos.NewValue is not TreeViewItem elementoSeleccionado ||
            elementoSeleccionado.Tag is not string rutaArchivo ||
            !File.Exists(rutaArchivo))
        {
            return;
        }

        DETAILWIN ventanaDetalle = new(rutaArchivo)
        {
            Owner = this
        };
        ventanaDetalle.Show();
    }

    private void ModoNoche_Cambio(object remitente, RoutedEventArgs argumentos)
    {
        EsModoNoche = ChkModoNoche.IsChecked == true;

        string claveRecurso = EsModoNoche
            ? "NocheBackgroundBrush"
            : "NormalBackgroundBrush";

        MainGrid.Background =
            (Pincel)System.Windows.Application.Current.FindResource(claveRecurso);
    }
}
