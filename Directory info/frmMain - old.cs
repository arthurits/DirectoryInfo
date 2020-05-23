using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Directory_info
{
    public partial class frmMain : Form
    {
        // Definición de variables
        private int sortColumn;
        private int indexHistory;
        private List<List<DirInfo>> listaDirHistoria;

        public frmMain()
        {
            InitializeComponent();
            
            // Inicializar las variables
            sortColumn = -1;
            indexHistory = -1;
            listaDirHistoria = new List<List<DirInfo>>();

            // Función personalizada para inicializar el control ListView y el Chart
            InitializeListView();
            InitializeChart();

            // Ajustar el tamaño
            SetSize();
        }

        #region Eventos del formulario
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
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

            // Guardar los datos de configuración
            //SaveProgramSettings();
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            // Llamar a la subrutina que ajusta los controles en el formulario
            SetSize();
        }

        /// <summary>
        /// Rutina para ajustar el tamaño y posición de los controles en el formulario
        /// </summary>
        private void SetSize()
        {
            // Leave a small margin around the outside of the control
            Size size = new Size(this.ClientSize.Width, this.ClientSize.Height - this.statusStrip1.ClientSize.Height - lblResults.ClientSize.Height - 28);
            splitHorizontal.Size = size;
            splitVertical.Size = splitHorizontal.Panel1.ClientSize;
            lbxHistory.Size = splitHorizontal.Panel2.ClientSize;
            lstLista.Size = splitVertical.Panel1.ClientSize;
            chart.Size = splitVertical.Panel2.ClientSize;
        }
        #endregion Eventos del formulario

        #region Elementos del menú principal
        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Cerrar el formulario
            this.Close();
        }

        private void selDirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Definición de variables
            DirectoryInfo dir = new DirectoryInfo(@"C:\Alf\Proyectos");         

            // Llamar a la rutina para calcular los datos del directorio
            CalcularDirectorio(dir.FullName);
        }
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

            //nFiles += dirLista[lstLista.Items.IndexOfKey("Xtras (archivos)")].Archivos;
            DirInfo dirInfo = (listaDirHistoria[indexHistory])[lista.SelectedItems[0].Index];
            //DirInfo dirInfo = dirLista[lista.SelectedItems[0].Index];

            // Llamar a la rutina para calcular los datos del directorio
            CalcularDirectorio(dirInfo.Ruta);

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
            List<DirInfo> dirLista;
            Int32 index = lbxHistory.SelectedIndex;
            
            // Si no hay elementos seleccionados, salir de la subrutina
            if (index < 0)
                return;
            
            // Obtener la lista de directorios correspondiente
            dirLista = listaDirHistoria[index];

            // Llenar el control ListView con la información recogida
            PopulateListView(dirLista);

            // Llenar el gráfico con la información recogida
            PopulateChart(dirLista);

            // Presentar los resultados
            PresentarResultados(dirLista);

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
                    indexHistory -= 1;
                    lbxHistory.SelectedIndex = index - 1;

                    if (lbxHistory.Items.Count == 0)
                    {
                        // Borrar los datos del control lstLista y del control chart
                        lstLista.Items.Clear();
                        chart.Series["Porcentaje"].Points.Clear();
                    }
                }
            }
        }
        #endregion Eventos de los controles

        #region Subrutinas internas
        /// <summary>
        /// Actualiza el control lstLista con los datos pasados
        /// </summary>
        /// <param name="dir">Lista con la datos DirInfo para presentar en el control ListView</param>
        private void PopulateListView(List<DirInfo> dir)
        {
            // Definición de variables
            ListViewItem item;

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

                item = new ListViewItem(new string[] { d.Nombre, d.mega.ToString("0.##"), d.porcentaje.ToString("#0.##"), d.Carpetas.ToString(), d.Archivos.ToString() });
                item.Name = d.Nombre;
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
            ColumnHeader encabezado = new ColumnHeader();

            // Borrar cualquier formato previo
            lstLista.Clear();

            // Añadir la columna 1
            encabezado.Text = "Directorio";
            encabezado.Width = 165;

            lstLista.Columns.Add(encabezado);

            // Añadir la columna 2
            encabezado = new ColumnHeader();
            encabezado.Text = "Tamaño (MB)";
            encabezado.Width = 60;

            lstLista.Columns.Add(encabezado);

            // Añadir la columna 3
            encabezado = new ColumnHeader();
            encabezado.Text = "Porcentaje (%)";
            encabezado.Width = 60;

            lstLista.Columns.Add(encabezado);

            // Añadir la columna 4
            encabezado = new ColumnHeader();
            encabezado.Text = "Carpetas";
            encabezado.Width = 60;

            lstLista.Columns.Add(encabezado);

            // Añadir la columna 5
            encabezado = new ColumnHeader();
            encabezado.Text = "Archivos";
            encabezado.Width = 60;

            lstLista.Columns.Add(encabezado);

        }

        private void InitializeChart()
        {
            // Crear el área, la leyenda y la serie
            ChartArea chartArea = new ChartArea();
            Legend chartLegend = new Legend();
            Series chartSeries = new Series();
            Title chartTitle = new Title();

            // Definir las propiedades del área
            chartArea.Name = "PorcentajeA";
            chartArea.Area3DStyle.Enable3D = true;
            chartArea.Area3DStyle.Perspective = 30;
            chartArea.ShadowColor = Color.SkyBlue;

            // Definir las propiedades de la leyenda
            chartLegend.Name = "PorcentajeL";
            chartLegend.Docking = Docking.Bottom;

            // Definir las propiedades de las series
            chartSeries.ChartArea = "PorcentajeA";
            chartSeries.ChartType = SeriesChartType.Pie;
            chartSeries.IsValueShownAsLabel = true;
            chartSeries.LabelFormat = "#.##";
            chartSeries.LabelAngle = 30;
            chartSeries.Legend = "PorcentajeL";
            chartSeries.Name = "Porcentaje";
            chartSeries["PieDrawingStyle"] = "SoftEdge";

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

        // http://msdn.microsoft.com/es-es/library/system.io.directory(VS.80).aspx
        // http://dotnetperls.com/Content/Recursively-Find-Files.aspx

        private long DirSize(DirectoryInfo dir, ref Int32 nCarpetas, ref Int32 nArchivos)
        {
            // Definición de variables
            long Size = 0;

            try
            {
                // Obtener lo que ocupan los archivos del directorio
                Size = FileSize(dir, ref nArchivos);

                // Iterar para cada uno de los subdirectorios encontrados en el directorio
                DirectoryInfo[] directories = dir.GetDirectories();
                foreach (DirectoryInfo directory in directories)
                {
                    Size += DirSize(directory, ref nCarpetas, ref nArchivos);
                    nCarpetas += 1;
                }

                // Finalizar
                return (Size);
            }
            catch
            {
                return (0L);
            }
        }

        private long FileSize(DirectoryInfo dir, ref Int32 nArchivos)
        {
            // Definición de variables
            long Size = 0;

            try
            {
                // Iterar para cada uno de los archivos encontrados en el directorio
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    Size += file.Length;
                    nArchivos += 1;
                }

                // Finalizar
                return (Size);
            }
            catch
            {
                return (0L);
            }
        }

        private long getDirSize(List<DirInfo> dirLista)
        {
            // Definición de variables
            long lTamañoTotal = 0;

            // Iterar para cada directorio
            foreach (DirInfo d in dirLista)
                lTamañoTotal += d.bytes;

            // Devolver el resultado
            return (lTamañoTotal);
        }

        private int getDirFiles(List<DirInfo> dirLista)
        {
            // Definición de variables
            int nFiles = 0;

            // Iterar para cada directorio
            foreach (DirInfo d in dirLista)
                nFiles += d.Archivos;

            // Sumar los archivos "sueltos" que hay en el directorio
            //nFiles += dirLista[lstLista.Items.IndexOfKey("Xtras (archivos)")].Archivos ;

            // Devolver el resultado
            return (nFiles);
        }

        private int getDirFolders(List<DirInfo> dirLista)
        {
            // Definición de variables
            int nFolders = 0;

            // Iterar para cada subdirectorio
            foreach (DirInfo d in dirLista)
                nFolders += d.Carpetas;

            // Sumar los propios directorios
            nFolders += dirLista.Count;

            // Devolver el resultado
            return (nFolders);
        }

        /// <summary>
        /// Convierte un valor de bytes a kilo, mega o giga
        /// </summary>
        /// <param name="bytes">Valor de bytes a convertir</param>
        /// <param name="option">Opción de conversión</param>
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

        /// <summary>
        /// Itera a través del directorio especificado para obtener información
        /// del tamaño que ocupa
        /// </summary>
        /// <param name="dir">Directorio que se quiere analizar</param>
        private List<DirInfo> ExplorarDirectorio(DirectoryInfo dir)
        {
            // Definición de variables
            DirectoryInfo[] directories;

            DirInfo dirData =new DirInfo();
            List<DirInfo> dirLista = new List<DirInfo>();

            long lTamañoTotal = 0;
            int nCarpetas = 0;
            int nArchivos = 0;

            // Iterar para cada uno de los subdirectorios
            directories = dir.GetDirectories();
            foreach (DirectoryInfo d in directories)
            {
                // Si se trata de un directorio real
                if (d.Attributes == FileAttributes.Directory)
                {
                    // Recuperar la información de los subdirectorios                    
                    nCarpetas = 0;
                    nArchivos = 0;
                    dirData.Nombre = d.Name;
                    dirData.Ruta = d.FullName;
                    dirData.bytes = DirSize(d, ref nCarpetas, ref nArchivos);
                    dirData.kilo = SizeConversion(dirData.bytes, ConversionOptions.BytesToKilo);
                    dirData.mega = SizeConversion(dirData.bytes, ConversionOptions.BytesToMega);
                    dirData.giga = SizeConversion(dirData.bytes, ConversionOptions.BytesToGiga);
                    dirData.Carpetas = nCarpetas;
                    dirData.Archivos = nArchivos;

                    // Añadir a la lista
                    dirLista.Add(dirData);
                    
                    // Calcular el tamaño total del directorio
                    lTamañoTotal += dirData.bytes;
                }
            }

            // Obtener la información para los archivos del directorio seleccionado
            nCarpetas = 0;
            nArchivos = 0;
            dirData.Nombre = "Xtras (archivos)";
            dirData.bytes = FileSize(dir, ref nArchivos);
            dirData.kilo = SizeConversion(dirData.bytes, ConversionOptions.BytesToKilo);
            dirData.mega = SizeConversion(dirData.bytes, ConversionOptions.BytesToMega);
            dirData.giga = SizeConversion(dirData.bytes, ConversionOptions.BytesToGiga);
            dirData.Carpetas = nCarpetas;
            dirData.Archivos = nArchivos;

            dirLista.Add(dirData);

            lTamañoTotal += dirData.bytes;

            // Ahora se puede calcular el porcentaje que ocupa cada directorio
            for (int i = 0; i < dirLista.Count; i++)
            {
                dirData = dirLista[i];
                dirData.porcentaje = 100f * dirData.bytes / ((Double)lTamañoTotal);
                dirLista[i]=dirData;
            }

            // Devolver el resultado
            return (dirLista);
        }

        private void CalcularDirectorio(String strPath)
        {
            // Definición de variables
            DirectoryInfo dir = new DirectoryInfo(strPath);
            List<DirInfo> dirLista;

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
            indexHistory += 1;

            // Presentar los resultados en el lblResults
            PresentarResultados(dirLista);
        }

        private void PresentarResultados(List <DirInfo> dirLista)
        {
            // Definición de variables
            long lTamañoTotal = 0;
            String strCadena = "";
            
            // Construcción de la cadena de información:
            lTamañoTotal = getDirSize(dirLista);
            strCadena = String.Format("La ruta '{0}' presenta las siguientes características:\n", dirLista[0].Ruta);
            strCadena += String.Format("Tamaño total: {0} MegaBytes.\n", SizeConversion(lTamañoTotal, ConversionOptions.BytesToMega).ToString("0.##"));
            strCadena += String.Format("Número de carpetas: {0}.\n", getDirFolders(dirLista).ToString());
            strCadena += String.Format("Número de archivos: {0}.", getDirFiles(dirLista).ToString());

            lblResults.Text = strCadena;

            // Mostrar un MessageBox
            //String strResultado = String.Format("El tamaño del directorio es de:\n{0} Bytes\n{1:2} KiloBytes\n{2:2} MegaBytes\n{3:2} GigaBytes",
            //lTamañoTotal.ToString(),
            //SizeConversion(lTamañoTotal, ConversionOptions.BytesToKilo).ToString("0.##"),
            //SizeConversion(lTamañoTotal, ConversionOptions.BytesToMega).ToString("0.##"),
            //SizeConversion(lTamañoTotal, ConversionOptions.BytesToGiga).ToString("0.##"));

            //MessageBox.Show(strResultado, "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        #endregion Subrutinas internas

    }
}
