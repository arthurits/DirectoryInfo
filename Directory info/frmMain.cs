using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace Directory_info
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public partial class frmMain : Form
    {
        // Definición de variables
        private AppSettings _settings = new();
        private static readonly string _settingsFileName = "Configuration.json";
        private const String strArchivosSueltos = "Xtras (archivos)";
        private const int WM_USER = 0x0400;
        //private Boolean _shouldStop;
        private Int32 sortColumn;
        private Int32 indexActual;
        private Int32 indexTotal;
        private List<List<DirInfo>> listaDirHistoria;
        private HiloDir hilo;
        private Thread thread;

        // For loading and saving program settings.
        //private Settings _settings = new Settings();
        private ProgramSettings _programSettings;
        private static readonly string _programSettingsFileName = "DirectoryInfo.xml";

        public frmMain()
        {
            // Set form icon
            string _path = Path.GetDirectoryName(Environment.ProcessPath);
            if (File.Exists(_path + @"\images\logo.ico")) this.Icon = new Icon(_path + @"\images\logo.ico");

            InitializeComponent();
            // Custom initialization for ListView and Plot
            InitializeListView();
            //InitializeChart();
            InitializeChart2();

            // Suscribirse al evento de la clase HiloDir
            hilo = new HiloDir();
            hilo.SetHwnd(this.Handle);
            //hilo.AlFinalizarHiloProc += new HiloDir.HiloProcFinalizado(hilo_AlFinalizarHiloProc);

            // Inicializar las variables
            //_shouldStop = false;
            sortColumn = -1;
            indexActual = -1;
            indexTotal = -1;
            listaDirHistoria = new List<List<DirInfo>>();

            // Cargar los valores guardados
            _programSettings = new ProgramSettings(_programSettingsFileName);
            //LoadProgramSettings();
            LoadProgramSettingsJSON();
            if (listaDirHistoria.Count > 0)
            {
                foreach (List<DirInfo> listaDir in listaDirHistoria)
                {
                    lbxHistory.Items.Add(listaDir[0].Ruta[..listaDir[0].Ruta.LastIndexOf("\\")]);
                    indexTotal += 1;
                }

                lbxHistory.SelectedIndex = indexTotal; // This also updates the Plot control since it's equivalent to the user clicking the last item in the ListBox
                PopulateListView(listaDirHistoria[indexActual]);
                PresentarResultados(listaDirHistoria[indexActual]);
            }
        }

        /// <summary>
        /// Escuchar los mensajes recibidos
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        { 
            if (m.Msg == WM_USER)
                hilo_AlFinalizarHiloProc(hilo.GetDirLista(), hilo.GetDirRuta());

            base.WndProc(ref m);
        }


        #region Eventos del formulario
        
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            using (new CenterWinDialog(this))
            {
                if (DialogResult.No == MessageBox.Show(
                                      "¿Está seguro que desea salir\nde la aplicación?",
                                      "¿Salir?",
                                      MessageBoxButtons.YesNo,
                                      MessageBoxIcon.Question,
                                      MessageBoxDefaultButton.Button2))
                {
                    // Cancelar el cierre de la ventana
                    e.Cancel = true;
                }
            }

            // Si hay algún hilo abierto, entonces hay que cerrarlo antes de salir de la aplicación
            HiloAbortar();

            // Guardar los datos de configuración
            //SaveProgramSettings();
            SaveProgramSettingsJSON();
        }

        private void frmMain_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == (char)Keys.Escape)
                HiloAbortar();

            return;
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            // Signal the native process (that launched us) to close the splash screen
            using var closeSplashEvent = new EventWaitHandle(false, EventResetMode.ManualReset, "CloseSplashScreenEvent");
            closeSplashEvent.Set();
        }

        /// <summary>
        /// Rutina para ajustar el tamaño y posición de los controles en el formulario
        /// Deprecated. Controls are already docked to the form
        /// </summary>
        private void SetSize()
        {
            // Leave a small margin around the outside of the control
            //Size size = new Size(this.ClientSize.Width, this.ClientSize.Height - this.statusStrip1.ClientSize.Height - lblResults.ClientSize.Height - 28);
            //splitHorizontal.Size = size;
            //splitVertical.Size = splitHorizontal.Panel1.ClientSize;
            //lbxHistory.Size = splitHorizontal.Panel2.ClientSize;
            //lstLista.Size = splitVertical.Panel1.ClientSize;
            //lstLista.Height -= 4;
            //chart.Size = splitVertical.Panel2.ClientSize;
            //chart.Height -= 4;
        }
        
        #endregion Eventos del formulario

        #region Elementos del menú principal

        #region Menú Archivo
        private void nuevoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Si no existen datos
            if (indexActual == -1)
                return;

            // Preguntar si que quiere borrar todo
            if (MessageBox.Show("¿Está seguro que desea borrar\ntodos los datos?",
                "Nuevo", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;

            // Resetear todo
            ResetApplication();
        }

        private void configurarPáginaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //chart.Printing.PageSetup();
        }

        private void vistaPreviaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //chart.Printing.PrintPreview();
        }

        private void imprimirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //chart.Printing.Print(true);
        }

        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Cerrar el formulario
            this.Close();
        }
        #endregion Menú Archivo

        #region Menú Edición
        private void selDirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowser = new();
            
            if (folderBrowser.ShowDialog() != DialogResult.OK)
                return;
            
            // Definición de variables
            DirectoryInfo dir = new(folderBrowser.SelectedPath);

            // Llamar a la rutina para calcular los datos del directorio
            //CalcularDirectorio(dir.FullName);
            
            // Iniciar un hilo para calcular el tamaño del directorio

                // Pasar las variables iniciales al hilo
                hilo.SetDirRuta(dir.FullName);

                // Iniciar el hilo
                // Lo más recomendable actualmente sería
                // return await Task.Run(() =>
                // {
                // });
                // https://www.pluralsight.com/guides/using-task-run-async-await
                thread = new Thread(new ThreadStart(hilo.HiloProc));
                thread.Start();
            
            // Cambiar el texto de la barra de estado de la aplicación
                HiloOcultarUI();
                this.statuslblInfo.Text = String.Concat("Calculando tamaño de '", dir.FullName,"' (presione ESC para cancelar)...");
        }

        private void copiarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Si no existen datos almacenados en listaDirHistoria (no hay gráfico)
            if (indexActual == -1)
            {
                MessageBox.Show("No existe ningún gráfico\ncuya imagen pueda ser copiada.",
                    "Exportar gráfico",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Definición de variables
            using System.IO.MemoryStream stream = new();

            // Cambiar el cursor
            this.Cursor = Cursors.WaitCursor;

            try
            {
                // create a memory stream to save the chart image    
                //stream = new System.IO.MemoryStream();

                // save the chart image to the stream    
                //chart.SaveImage(stream, System.Drawing.Imaging.ImageFormat.Bmp);

                // create a bitmap using the stream    
                //using Bitmap bmp = new Bitmap(stream);

                // save the bitmap to the clipboard    
                Clipboard.SetImage(formsPlot1.Plot.GetBitmap());
                //Clipboard.SetDataObject(bmp);

                // Mostrar un mensaje para indicar que la operación se ha realizado correctamente
                MessageBox.Show("El gráfico ha sido copiado\ncorrectamente al portapapeles.",
                    "Copiar gráfico",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // Mostrar un mensaje de error
                MessageBox.Show(ex.Message, "Error al copiar el gráfico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Cerrar el Stream para liberar recursos
                stream.Close();

                // Cambiar el cursor
                this.Cursor = Cursors.Default;

            }
        }

        private void exportarImagenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Create a new save file dialog
            SaveFileDialog saveFileDialog = new();

            // Si no existen datos almacenados en listaDirHistoria (no hay gráfico)
            if (indexActual == -1)
            {
                MessageBox.Show("No existe ningún gráfico\ncuya imagen pueda ser exportada.",
                    "Exportar gráfico",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Sets the current file name filter string, which determines 
            // the choices that appear in the "Save as file type" or 
            // "Files of type" box in the dialog box.
            saveFileDialog.FileName = "Gráfico";
            saveFileDialog.Filter = "Bitmap (*.bmp)|*.bmp|EMF (*.emf)|*.emf|EMF-Dual (*.emf)|*.emf|EMF-Plus (*.emf)|*.emf|GIF (*.gif)|*.gif|JPEG (*.jpg)|*.jpg|PNG (*.png)|*.png|TIFF (*.tif)|*.tif";
            saveFileDialog.FilterIndex = 7;
            saveFileDialog.Title = "Exportar gráfico";
            saveFileDialog.RestoreDirectory = true;

            try
            {

                // Set image file format
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    this.Cursor = Cursors.WaitCursor;

                    // Save image
                    //chart.SaveImage(saveFileDialog.FileName, format);
                    formsPlot1.Plot.SaveFig(saveFileDialog.FileName);

                    // Mostrar un mensaje para indicar que la operación se ha realizado correctamente
                    MessageBox.Show("El gráfico ha sido exportado\ncorrectamente.",
                        "Exportar gráfico",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                // Mostrar un mensaje de error
                MessageBox.Show(ex.Message, "Error al exportar el gráfico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally 
            {
                // Restaurar el cursor
                this.Cursor = Cursors.Default;
            }
        }

        private void exportarDatosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Create a new save file dialog
            SaveFileDialog saveFileDialog = new();

            // Si no existen datos almacenados en listaDirHistoria              
            if (indexActual == -1)
            {
                MessageBox.Show("No existen datos calculados\npara ser exportados.",
                    "Exportar datos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Mostrar un cuadro de diálogo para guardar el archivo
            saveFileDialog.FileName = "Directory info data";
            saveFileDialog.Filter = "Texto (*.txt)|*.txt";
            saveFileDialog.Title = "Exportar datos";
            if (saveFileDialog.ShowDialog() == DialogResult.Cancel)
                return;

            // Cambiar el cursor
            this.Cursor = Cursors.WaitCursor;

            // Definición de variables. Es necesario inicializar la variable para
            //  que luego pueda ser utilizada en el bloque "finally"
            System.IO.StreamWriter ioArchivo = null;

            try
            {
                // Definición de variables
                String strCadena;
                ioArchivo = new System.IO.StreamWriter(@saveFileDialog.FileName, false, Encoding.UTF8);

                ioArchivo.WriteLine(lblResults.Text);
                strCadena = String.Format("Nombre:\tTamaño (MB):\tPorcentaje (%)\tArchivos\tCarpetas\r\n");

                foreach (DirInfo d in listaDirHistoria[indexActual])
                {
                    strCadena += String.Format("{0}\t{1}\t{2}\t{3}\t{4}\r\n", d.Nombre, d.mega.ToString("0.000"), d.porcentaje.ToString("0.00"), d.Archivos.ToString("0"), d.Carpetas.ToString("0"));
                }

                ioArchivo.WriteLine(strCadena);

                // Mostrar un mensaje para indicar que la operación se ha realizado correctamente
                MessageBox.Show("Los datos se han exportado\ncorrectamente.",
                    "Exportar datos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // Mostrar un mensaje de error
                MessageBox.Show(ex.Message,"Error al exportar los datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Cerrar siempre el archivo
                ioArchivo.Close();

                // Restaurar el cursor
                this.Cursor = Cursors.Default;
            }

        }
        #endregion Menú Edición

        #region Menú ?
        private void acercaDeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form frmAbout = new frmAbout();
            frmAbout.ShowDialog();

            /*
            MessageBox.Show(
                "Todavía no se ha implementado\nesta opción.\n\nUn poco de paciencia por favor.",
                "En breve",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
                */
        }
        #endregion Menú ?

        #endregion Elementos del menú principal

        #region Eventos de los controles

        private void splitVertical_SplitterMoved(object sender, SplitterEventArgs e)
        {
            SetSize();
        }

        private void splitHorizontal_SplitterMoved(object sender, SplitterEventArgs e)
        {
            SetSize();
        }


        /// <summary>
        /// Cada vez que se hace "doble click" sobre un elemento se calcula el tamaño del directorio
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lstLista_ItemActivate(object sender, EventArgs e)
        {
            // Definición de variables
            ListView lista = (ListView)sender;
            String strNombre = lista.SelectedItems[0].SubItems[0].Text;
            Int32 nSelection;
            Int32 nBusqueda = -1;

            // Si se ha hecho doble click sobre los archivos del directorio
            if (strNombre == strArchivosSueltos)
                return;

            nSelection = Convert.ToInt32(lista.SelectedItems[0].Name);  // devuelve el índice del elemento seleccionado
            DirInfo dirInfo = (listaDirHistoria[indexActual])[nSelection]; // devuelve el objeto DirInfo del elemento seleccionado

            // Comprobar si el elemento seleccionado ya está calculado
            nBusqueda = lbxHistory.Items.IndexOf(dirInfo.Ruta); // Devuelve -1 cuando no se encuentra nada

            if (nBusqueda > -1)
                lbxHistory.SelectedIndex = nBusqueda;
            else
            {
                // Llamar a la rutina para calcular los datos del directorio
                //CalcularDirectorio(dirInfo.Ruta);

                // Iniciar un hilo para calcular el tamaño del directorio

                // Pasar las variables iniciales al hilo
                hilo.SetDirRuta(dirInfo.Ruta);

                // Iniciar el hilo
                thread = new Thread(new ThreadStart(hilo.HiloProc));
                thread.Start();

                // Cambiar el texto de la barra de estado de la aplicación
                HiloOcultarUI();
                this.statuslblInfo.Text = String.Concat("Calculando tamaño de '", dirInfo.Ruta, "' (presione ESC para cancelar)...");
            }

            // Finalizar
            return;
        }

        /// <summary>
        /// Ordenar según la columna seleccionada por el usuario
        /// http://msdn.microsoft.com/en-us/library/ms996467.aspx
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lstLista_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine whether the column is the same as the last column clicked.
            if (e.Column != sortColumn)
            {
                // Set the sort column to the new column.
                sortColumn = e.Column;
                // Set the sort order to ascending by default.
                lstLista.Sorting = SortOrder.Ascending;
            }
            else
            {
                // Determine what the last sort order was and change it.
                if (lstLista.Sorting == SortOrder.Ascending)
                    lstLista.Sorting = SortOrder.Descending;
                else
                    lstLista.Sorting = SortOrder.Ascending;
            }

            // Call the sort method to manually sort.
            lstLista.Sort();
            // Set the ListViewItemSorter property to a new ListViewItemComparer object.
            this.lstLista.ListViewItemSorter = new ListViewItemComparer(e.Column, lstLista.Sorting);
        }

        private void lbxHistory_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Cuando ambos índices coinciden quiere decir que no hay que calcular nada porque
            // es lo que se está mostrando en la pantalla. Las causa de esto pueden ser:
            // - El usuario ha seleccionado del ListBox el mismo elemento que ya se está mostrando en pantalla
            // - Mediante código se ha cambiado el elemento seleccionado del ListBox para que coincida con el elemento mostrado en pantalla
            //   Este código se encuentra en la rutina privada "hilo_AlFinalizarHiloProc"
            if (lbxHistory.SelectedIndex == indexActual)
                return;

            List<DirInfo> dirLista;
            indexActual = lbxHistory.SelectedIndex;

            // Si no hay elementos seleccionados, salir de la subrutina
            if (indexActual < 0)
                return;

            this.Cursor = Cursors.WaitCursor;

            // Obtener la lista de directorios correspondiente
            dirLista = listaDirHistoria[indexActual];

            // Llenar el control ListView con la información recogida
            PopulateListView(dirLista);

            // Llenar el gráfico con la información recogida
            //PopulateChart(dirLista);
            PopulateChart2(dirLista);

            // Presentar los resultados
            PresentarResultados(dirLista);

            this.Cursor = Cursors.Default;

            // Finalizar
            return;
            
        }

        private void lbxHistory_KeyUp(object sender, KeyEventArgs e)
        {
            Int32 index = lbxHistory.SelectedIndex;
            if (e.KeyCode == Keys.Delete)
            {
                if (index >= 0)
                {
                    lbxHistory.Items.RemoveAt(index);
                    listaDirHistoria.RemoveAt(index);
                    indexTotal--;
                    //indexActual = indexTotal;
                    if (index == 0)
                        lbxHistory.SelectedIndex = indexTotal > -1 ? 0 : -1;
                    else
                        lbxHistory.SelectedIndex = index - 1;   // En este evento ya se actualiza el valor de indexActual

                    // Si se han eliminado todos los elementos del ListBox, entonces se resetea todo
                    if (lbxHistory.SelectedIndex < 0)
                        ResetApplication();
                }                
            }
        }
        #endregion Eventos de los controles

        #region Subrutinas internas
        /// <summary>
        /// Rutina suscrita al evento AlFinalizarHilo
        /// </summary>
        /// <param name="dirLista">Lista de elementos DirInfo</param>
        /// <param name="strDirName">Ruta completa del directorio</param>
        private void hilo_AlFinalizarHiloProc(List<DirInfo> dirLista, String strDirName)
        {

            // Añadir el resultado a la lista global
            listaDirHistoria.Add(dirLista);

            // Llenar el control ListView con la información recogida
            PopulateListView(dirLista);

            // Llenar el gráfico con la información recogida
            //PopulateChart(dirLista);
            PopulateChart2(dirLista);

            // Mensaje en la barra de estado y cursor
            statuslblInfo.Text = "";
            this.Cursor = Cursors.Default;

            // Añadir el directorio al ListBox
                lbxHistory.Items.Add(strDirName);
                
                // Actualizar los contadores internos
                indexTotal += 1;
                indexActual = indexTotal;

                // Seleccionar el elmento racién añadido al ListBox
                lbxHistory.SelectedIndex = indexTotal;

            // Presentar los resultados en el lblResults
            PresentarResultados(dirLista);

            // Cambiar la interfaz
            HiloMostrarUI();
        }
        
        /// <summary>
        /// Actualiza el control lstLista con los datos pasados
        /// </summary>
        /// <param name="dir">Lista con la datos DirInfo para presentar en el control ListView</param>
        private void PopulateListView(List<DirInfo> dir)
        {
            // Definición de variables
            ListViewItem item;
            Int32 i = 0;

            // Borrar culaquier elemento previo
            lstLista.Items.Clear();

            // No actualizar el control hasta que se hayan pasado todos los datos
            lstLista.BeginUpdate();

            foreach (DirInfo d in dir)
            {
                //item = new ListViewItem(new string []{"1", "2"});
                //item.Text = d.Nombre;
                //item.SubItems[0].Text = d.Nombre;
                //item.SubItems[1].Text = d.mega.ToString("0.00");
                //item.SubItems[2].Text = d.porcentaje.ToString("#0.00");
                //item.SubItems[3].Text = d.Carpetas.ToString();
                //item.SubItems[4].Text = d.Archivos.ToString();

                item = new(new string[] { d.Nombre, d.mega.ToString("0.00"), d.porcentaje.ToString("#0.00"), d.Carpetas.ToString("0"), d.Archivos.ToString("0") });
                item.Name = i++.ToString();    // Se utiliza en lstLista_ItemActivate para saber el índice del elemento seleccionado
                lstLista.Items.Add(item);
            }

            // Actualizar el control
            lstLista.EndUpdate();
        }

        /// <summary>
        /// Crear las columnas del control lstLista
        /// </summary>
        private void InitializeListView()
        {
            // Definición de variables
            ColumnHeader encabezado = new();

            // Borrar cualquier formato previo
            lstLista.Clear();

            // Añadir la columna 1
            encabezado.Text = "Directorio";
            encabezado.Width = 125;
            encabezado.TextAlign = HorizontalAlignment.Left;
            
            lstLista.Columns.Add(encabezado);

            // Añadir la columna 2
            encabezado = new()
            {
                Text = "Tamaño (MB)",
                Width = 95,
                TextAlign = HorizontalAlignment.Right
            };

            lstLista.Columns.Add(encabezado);

            // Añadir la columna 3
            encabezado = new()
            {
                Text = "Porcentaje (%)",
                Width = 110,
                TextAlign = HorizontalAlignment.Right
            };

            lstLista.Columns.Add(encabezado);

            // Añadir la columna 4
            encabezado = new()
            {
                Text = "Carpetas",
                Width = 80,
                TextAlign = HorizontalAlignment.Right
            };

            lstLista.Columns.Add(encabezado);

            // Añadir la columna 5
            encabezado = new()
            {
                Text = "Archivos",
                Width = 80,
                TextAlign = HorizontalAlignment.Right
            };

            lstLista.Columns.Add(encabezado);

        }

        /// <summary>
        /// Initializes the chart objets
        /// </summary>
        private void InitializeChart2()
        {
            formsPlot1.Plot.Clear();
            formsPlot1.Plot.Title("Folder percentage",size: 16, bold: false);
            formsPlot1.Plot.Palette = ScottPlot.Drawing.Palette.Nord;
            formsPlot1.Plot.Grid(false);
            formsPlot1.Plot.Frameless();
            //formsPlot1.Plot.Ticks(false, false);
            formsPlot1.Plot.Legend(enable: true, location: ScottPlot.Alignment.LowerRight);
            //formsPlot1.Plot.TightenLayout(padding: 0);
            formsPlot1.Plot.Style(figureBackground: Color.White, dataBackground: Color.White);
            //formsPlot1.Plot.AntiAlias(figure: true, data: true, legend: true);
            var plot = formsPlot1.Plot.AddPie(new double [ 1 ],hideGridAndFrame: true);
            plot.ShowPercentages = true;
            plot.ShowValues = false;
            plot.ShowLabels = false;
            formsPlot1.Refresh();
        }

        private void PopulateChart2(List<DirInfo> dir)
        {
            // Data
            Int32 nCollected = 0;
            Int32 nPoints = dir.Count;

            List<DirInfo> OrderedDir = dir.OrderByDescending(o => o.porcentaje).ToList();

            // Optimal option
            //dir.Sort((x, y) => y.porcentaje.CompareTo(x.porcentaje));

            foreach (DirInfo d in OrderedDir)
            {
                if (d.porcentaje >= 3) nCollected++;
                else break;
            }


            // Plot test
            double[] values = new double[nCollected+1];
            string[] labels = new string[nCollected+1];
            
            for (int i=0; i<nPoints; i++)
            {
                if (i < nCollected)
                {
                    //values[i] = Math.Round(OrderedDir[i].porcentaje, 1);
                    values[i] = OrderedDir[i].bytes;
                    labels[i] = OrderedDir[i].Nombre;
                }
                else
                {
                    //values[nCollected] += OrderedDir[i].porcentaje;
                    values[nCollected] += OrderedDir[i].bytes;
                }
            }

            //values[nCollected] = Math.Round(values[nCollected], 1);
            labels[nCollected] = "Other < 3%";

            labels = Enumerable
                .Range(0, values.Length)
                .Select(i => $"{labels[i]}")
                .ToArray();

            formsPlot1.Plot.Clear();
            //formsPlot1.plt.AddPie(values, labels, showPercentages: true, showValues: false, showLabels: false);
            var plot = formsPlot1.Plot.AddPie(values);
            plot.SliceLabels = labels;
            plot.ShowPercentages = true;
            plot.ShowValues = false;
            plot.ShowLabels = false;
            formsPlot1.Refresh();
        }

        /*
        private void InitializeChart()
        {
            // Crear el área, la leyenda y la serie
            ChartArea chartArea = new ChartArea();
            Legend chartLegend = new Legend();
            Series chartSeries = new Series();
            Title chartTitle = new Title();

            // Definir las propiedades del área
            chartArea.Name = "PorcentajeA";
            chartArea.Area3DStyle.Enable3D = false;
            chartArea.Area3DStyle.Perspective = 3;
            chartArea.ShadowColor = Color.SkyBlue;
            chartArea.BackColor = Color.Transparent;
            
            // Definir las propiedades de la leyenda
            chartLegend.Name = "PorcentajeL";
            chartLegend.Docking = Docking.Bottom;
            chartLegend.BackColor = Color.Transparent;

            // Definir las propiedades de las series
            chartSeries.ChartArea = "PorcentajeA";
            chartSeries.ChartType = SeriesChartType.Pie;
            chartSeries.IsValueShownAsLabel = true;
            chartSeries.LabelFormat = "#.##";
            chartSeries.LabelAngle = 0;
            chartSeries.Legend = "PorcentajeL";
            chartSeries.Name = "Porcentaje";
            chartSeries["PieDrawingStyle"] = "SoftEdge";
            chartSeries.CustomProperties = "PieStartAngle=30";            
            
            // Definir las propiedades del título del gráfico
            chartTitle.Text = "Porcentaje que ocupa cada carpeta";
            chartTitle.Font = new Font("Times New Roman", 16, FontStyle.Bold);
            chartTitle.Docking = Docking.Top;

            // Añadir el área, la leyenda y las series al gráfico
            this.chart.ChartAreas.Clear();
            this.chart.ChartAreas.Add(chartArea);
            this.chart.Legends.Clear();
            this.chart.Legends.Add(chartLegend);
            this.chart.Series.Clear();
            this.chart.Series.Add(chartSeries);
            this.chart.Titles.Clear();
            this.chart.Titles.Add(chartTitle);

            this.chart.AntiAliasing = AntiAliasingStyles.All;
            this.chart.TextAntiAliasingQuality = TextAntiAliasingQuality.High;

            this.chart.BorderlineColor = Color.Black;
            this.chart.BorderlineDashStyle = ChartDashStyle.Solid;
            this.chart.BorderlineWidth = 1;
            //this.chart.BorderSkin.BackColor = Color.LightSkyBlue;
            //this.chart.BorderSkin.PageColor = Color.Transparent;
            //this.chart.BorderSkin.SkinStyle = BorderSkinStyle.FrameThin6;
        }

        private void PopulateChart(List<DirInfo> dir)
        {
            // Populate series data
            //double[] yValues = { 65.62, 75.54, 60.45, 34.73, 85.42 };
            //string[] xValues = { "France", "Canada", "Germany", "USA", "Italy" };
            //chart1.Series["Default"].Points.DataBindXY(xValues, yValues);

            // Borrar los datos que haya en el gráfico
            chart.Series["Porcentaje"].Points.Clear();

            // Pasar los datos
            DataPoint dataPoint;
            foreach (DirInfo d in dir)
            {
                dataPoint = new DataPoint();
                dataPoint.SetValueXY(d.Nombre, d.porcentaje);
                dataPoint.ToolTip = d.Nombre;

                //chart.Series["Porcentaje"].Points.AddXY(d.Nombre, d.porcentaje);
                chart.Series["Porcentaje"].Points.Add(dataPoint);                
            }

            // Agrupar en un trozo todos aquellos que son menores del 5%
            Series series1 = chart.Series["Porcentaje"];

            // Set the threshold under which all points will be collected
            series1["CollectedThreshold"] = "3";
    
            // Set the threshold type to be a percentage of the pie
            // When set to false, this property uses the actual value to determine the collected threshold
            series1["CollectedThresholdUsePercent"] = "true";
    
            // Set the label of the collected pie slice
            series1["CollectedLabel"] = "< 3%";
    
            // Set the legend text of the collected pie slice
            series1["CollectedLegendText"] = "Otros";

            // Set the collected pie slice to be exploded
            series1["CollectedSliceExploded"]= "false";

            // Set the color of the collected pie slice
            series1["CollectedColor"] = "Green";
    
            // Set the tooltip of the collected pie slice
            series1["CollectedToolTip"] = "Otros";
        }
        */
 
        private long GetDirSize(List<DirInfo> dirLista)
        {
            // Definición de variables
            long lTamañoTotal = 0;

            // Iterar para cada directorio
            foreach (DirInfo d in dirLista)
                lTamañoTotal += d.bytes;

            // Devolver el resultado
            return (lTamañoTotal);
        }

        private int GetDirFiles(List<DirInfo> dirLista)
        {
            // Definición de variables
            int nFiles = 0;

            // Iterar para cada directorio
            foreach (DirInfo d in dirLista)
                nFiles += d.Archivos;

            // Los archivos "sueltos" que hay en el directorio
            // ya están incluidos en la entrada "Xtras (archivos)"

            // Devolver el resultado
            return (nFiles);
        }

        private int GetDirFolders(List<DirInfo> dirLista)
        {
            // Definición de variables
            int nFolders = 0;

            // Iterar para cada subdirectorio
            foreach (DirInfo d in dirLista)
                nFolders += d.Carpetas;

            // Sumar los propios directorios
            // Hay que restar la carpeta "Xtras (archivos)"
            nFolders += dirLista.Count - 1;

            // Devolver el resultado
            return (nFolders);
        }

        /// <summary>
        /// Convierte un valor de bytes a kilo, mega o giga
        /// </summary>
        /// <param name="bytes">Valor de bytes a convertir</param>
        /// <param name="option">Opción de conversión de tipo ConversionOptions</param>
        /// <returns>Resultado de la conversión</returns>
        private Double SizeConversion(long bytes, ConversionOptions option)
        {
            // Definición de variables
            Double dResult = 0.0;

            // Según la opción
            switch (option)
            {
                case ConversionOptions.BytesToKilo:
                    dResult = bytes / 1024f;
                    break;
                case ConversionOptions.BytesToMega:
                    dResult = (bytes / 1024f) / 1024f;
                    break;
                case ConversionOptions.BytesToGiga:
                    dResult = ((bytes / 1024f) / 1024f) / 1024f;
                    break;
                default:
                    break;
            }

            // Finalizar
            return dResult;
        }

        //Rutina antigua que no se utiliza pero sirve para ver la lógica del cálculo
        /*public void CalcularDirectorio(String strPath)
        {
            // Definición de variables
            DirectoryInfo dir = new DirectoryInfo(strPath);
            //List<DirInfo> dirLista;

            // Mensaje en la barra de estado
            statuslblInfo.Text = "Calculando el tamaño del directorio...";
            this.Cursor = Cursors.WaitCursor;

            // Obtener los datos del directorio seleccionado
            dirLista = ExplorarDirectorio(dir);

            // Añadir el resultado a la lista global
            listaDirHistoria.Add(dirLista);

            // Llenar el control ListView con la información recogida
            PopulateListView(dirLista);

            // Llenar el gráfico con la información recogida
            PopulateChart(dirLista);

            // Mensaje en la barra de estado
            statuslblInfo.Text = "";
            this.Cursor = Cursors.Default;

            // Añadir el directorio al ListBox
            lbxHistory.Items.Add(dir.FullName);
            indexTotal += 1;
            indexActual = indexTotal;

            // Presentar los resultados en el lblResults
            //PresentarResultados(dirLista);
        }*/

        private void PresentarResultados(List <DirInfo> dirLista)
        {
            // Definición de variables
            long lTamañoTotal;
            String strRuta = dirLista[0].Ruta;
            String strTexto;
            
            // Quitar el nombre al final de la cadena para quedarse con el directorio raíz
            strRuta = strRuta[..(strRuta.IndexOf(dirLista[0].Nombre) - 1)];
            
            // Construcción de la cadena de información:
            lTamañoTotal = GetDirSize(dirLista);
            //strTexto = String.Format("La ruta '{0}' presenta las siguientes características:\n", strRuta);
            //strTexto += String.Format("Tamaño total: {0} MegaBytes.\n", SizeConversion(lTamañoTotal, ConversionOptions.BytesToMega).ToString("0.##"));
            //strTexto += String.Format("Número de carpetas: {0}.\n", getDirFolders(dirLista).ToString());
            //strTexto += String.Format("Número de archivos: {0}.", getDirFiles(dirLista).ToString());
            strTexto = String.Format("La ruta '{0}' presenta las siguientes características:\n", strRuta);
            strTexto += String.Format("Total size: {0} MegaBytes.\n", SizeConversion(lTamañoTotal, ConversionOptions.BytesToMega).ToString("0.##"));
            strTexto += String.Format("Number of folders: {0}.\n", GetDirFolders(dirLista).ToString());
            strTexto += String.Format("Number of files: {0}.", GetDirFiles(dirLista).ToString());

            lblResults.Text = strTexto;

        }

        private void ResetApplication()
        {
            // Borrar los controles
            lstLista.Items.Clear();
            //chart.Series["Porcentaje"].Points.Clear();
            lbxHistory.Items.Clear();
            lblResults.Text = "";
            InitializeChart2();

            // Inicializar las variables
            listaDirHistoria.Clear();
            indexTotal = -1;
            indexActual = -1;
        }

        /// <summary>
        /// Rutina que establece la interfaz gráfica cuando se inicia el hilo de cálculo
        /// </summary>
        private void HiloOcultarUI()
        {

            this.archivoToolStripMenuItem.Enabled = false;
            this.ediciónToolStripMenuItem.Enabled = false;

            return;
        }
        

        /// <summary>
        /// Rutina que establece la interfaz gráfica cuando finaliza el hilo de cálculo
        /// </summary>
        private void HiloMostrarUI()
        {

            this.archivoToolStripMenuItem.Enabled = true;
            this.ediciónToolStripMenuItem.Enabled = true;

            return;
        }

        /// <summary>
        /// Rutina que aborta el hilo si se está ejecutando
        /// </summary>
        private void HiloAbortar()
        {
            if (thread != null && thread.IsAlive == true)
            {
                this.statuslblInfo.Text = "Cancelando el cálculo...";
                
                // Abortar y esperar a que finalice el hilo
                thread.Interrupt();
                thread.Join();
                thread = null;

                // Borrar el texto de la barra de tareas
                HiloMostrarUI();
                this.statuslblInfo.Text = "";
            }

            return;
        }

        #endregion Subrutinas internas

        #region Program settings

        /// <summary>
        /// Loads any saved program settings.
        /// </summary>
        private void LoadProgramSettings()
        {
            // Load the saved window settings and resize the window.
            try
            {
                // Load the saved window settings.
                Int32 left = System.Int32.Parse(_programSettings.GetValue("Window", "Left"));
                Int32 top = System.Int32.Parse(_programSettings.GetValue("Window", "Top"));
                Int32 width = System.Int32.Parse(_programSettings.GetValue("Window", "Width"));
                Int32 height = System.Int32.Parse(_programSettings.GetValue("Window", "Height"));

                // Reposition and resize the window.
                this.StartPosition = FormStartPosition.Manual;
                this.DesktopLocation = new Point(left, top);
                this.Size = new Size(width, height);

                // Cargar los datos de búsquedas de la sesión anterior
                _programSettings.ReadDirList(listaDirHistoria);
            }
            catch (Exception)
            {
                //MessageBox.Show(ex.Message, "Error loading the initial configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Saves the current program settings.
        /// </summary>
        private void SaveProgramSettings()
        {
            // Save window settings.      
            _programSettings.SetValue("Window", "Left", this.DesktopLocation.X.ToString());
            _programSettings.SetValue("Window", "Top", this.DesktopLocation.Y.ToString());
            _programSettings.SetValue("Window", "Width", this.Size.Width.ToString());
            _programSettings.SetValue("Window", "Height", this.Size.Height.ToString());

            // Guardar los datos de las búsquedas realizadas
            _programSettings.SaveDirList(listaDirHistoria);

            // Save the program settings.
            _programSettings.Save();
        }

        private void LoadProgramSettingsJSON()
        {
            listaDirHistoria = new List<List<DirInfo>>();
            try
            {
                string jsonString = File.ReadAllText(_settingsFileName);
                var options = new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

                using JsonDocument document = JsonDocument.Parse(jsonString, options);
                
                JsonElement element = document.RootElement.GetProperty("GUI");

                this.StartPosition = FormStartPosition.Manual;
                this.DesktopLocation = new Point(element.GetProperty("Left").GetInt32(), element.GetProperty("Top").GetInt32());
                this.ClientSize = new Size(element.GetProperty("Width").GetInt32(), element.GetProperty("Height").GetInt32());

                element = document.RootElement.GetProperty("DirInfo");

                DirInfo dir;
                int i = 0;
                foreach (JsonProperty Folder in element.EnumerateObject())
                {
                    listaDirHistoria.Add(new List<DirInfo>());
                    foreach (JsonElement SubFolder in Folder.Value.EnumerateArray())
                    {
                        JsonElement SubF = SubFolder.EnumerateObject().ElementAt(0).Value;
                        
                        dir.Nombre = SubF.GetProperty("Name").GetString();
                        dir.Ruta = SubF.GetProperty("Path").GetString();
                        dir.Carpetas = SubF.GetProperty("Folders").GetInt32();
                        dir.Archivos = SubF.GetProperty("Files").GetInt32();
                        dir.porcentaje = SubF.GetProperty("Percentage").GetDouble();
                        dir.bytes = SubF.GetProperty("Bytes").GetInt64();
                        dir.kilo = SubF.GetProperty("KB").GetDouble();
                        dir.mega = SubF.GetProperty("MB").GetDouble();
                        dir.giga = SubF.GetProperty("GB").GetDouble();
                        listaDirHistoria[0].Add(dir);
                    }
                    i++;
                }


            }
            catch (FileNotFoundException)
            {
            }
            catch (Exception ex)
            {
                using (new CenterWinDialog(this))
                {
                    MessageBox.Show(this,
                        "Error loading settings file\n\n" + ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Saves into a JSON text file in _settingsFileName
        /// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-apis/
        /// </summary>
        private void SaveProgramSettingsJSON()
        {
            //_settings.Left = DesktopLocation.X;
            //_settings.Top = DesktopLocation.Y;
            //_settings.Width = ClientSize.Width;
            //_settings.Height = ClientSize.Height;

            //var jsonString = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            //File.WriteAllText(_settingsFileName, jsonString);

            //using FileStream createStream = File.Create(_settingsFileName);
            //using var writer = new Utf8JsonWriter(createStream, options: writerOptions);

            Int32 i = 0, j;
            //using var stream = new MemoryStream();
            //using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            using FileStream createStream = File.Create(_settingsFileName);
            using var writer = new Utf8JsonWriter(createStream, new JsonWriterOptions { Indented = true });
            writer.WriteStartObject();

            writer.WritePropertyName("GUI");
            writer.WriteStartObject();
            writer.WriteNumber("Left", DesktopLocation.X);
            writer.WriteNumber("Top", DesktopLocation.Y);
            writer.WriteNumber("Width", ClientSize.Width);
            writer.WriteNumber("Height", ClientSize.Height);
            writer.WriteEndObject();

            writer.WritePropertyName("DirInfo");
            writer.WriteStartObject();
            foreach (var listaDir in listaDirHistoria)
            {
                writer.WritePropertyName("Folder " + i.ToString());
                writer.WriteStartArray();
                //writer.WriteStartObject();
                j = 0;
                foreach (DirInfo dir in listaDir)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("SubFolder " + j.ToString());
                    
                    writer.WriteStartObject();
                    writer.WriteString("Name", dir.Nombre);
                    writer.WriteString("Path", dir.Ruta);
                    writer.WriteNumber("Folders", dir.Carpetas);
                    writer.WriteNumber("Files", dir.Archivos);
                    writer.WriteNumber("Percentage", dir.porcentaje);
                    writer.WriteNumber("Bytes", dir.bytes);
                    writer.WriteNumber("KB", dir.kilo);
                    writer.WriteNumber("MB", dir.mega);
                    writer.WriteNumber("GB", dir.giga);
                    writer.WriteEndObject();

                    writer.WriteEndObject();
                    j++;
                }
                writer.WriteEndArray();
                //writer.WriteEndObject();
                i++;
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.Flush();
            //File.WriteAllText(_settingsFileName, Encoding.UTF8.GetString(stream.ToArray()));
        }

        #endregion Program settings

    }

}
