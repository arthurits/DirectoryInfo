using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace Directory_info
{

    public struct DirInfo
    {
        public String Nombre;
        public String Ruta;
        public int Carpetas;
        public int Archivos;
        public double porcentaje;
        public long bytes;
        public double kilo;
        public double mega;
        public double giga;
    }

    /// <summary>
    /// Opciones de conversión para la subrutina SizeConversion
    /// </summary>
    public enum ConversionOptions
    {
        BytesToKilo = 1,
        BytesToMega = 2,
        BytesToGiga = 3,
    }

    // Implements the manual sorting of items by column.
    // http://msdn.microsoft.com/en-us/library/ms996467.aspx
    class ListViewItemComparer : System.Collections.IComparer
    {
        private int col;
        private System.Windows.Forms.SortOrder order;
        public ListViewItemComparer()
        {
            col = 0;
            order = System.Windows.Forms.SortOrder.Ascending;
        }
        public ListViewItemComparer(int column, System.Windows.Forms.SortOrder order)
        {
            col = column;
            this.order = order;
        }
        public int Compare(object x, object y) 
        {
            int returnVal= -1;
            String strX = ((System.Windows.Forms.ListViewItem)x).SubItems[col].Text;
            String strY = ((System.Windows.Forms.ListViewItem)y).SubItems[col].Text;
            Double _x = 0.0;
            Double _y = 0.0;
            
            if (col == 0)
                returnVal = String.Compare(strX, strY);
            else
            {
                _x = Convert.ToDouble(strX);
                _y = Convert.ToDouble(strY);
                if (_x > _y)
                    returnVal = 1;
                if (_x < _y)
                    returnVal = -1;
                if (_x == _y)
                    returnVal = 0;
            }

            // Determine whether the sort order is descending.
            if (order == System.Windows.Forms.SortOrder.Descending)
                // Invert the value returned by String.Compare.
                returnVal *= -1;
            return returnVal;
        }
    }

    public class HiloDir
    {
        // Variables privadas para almacenar la información inicial
        private String strRuta;
        private IntPtr hWnd;
        private List<DirInfo> dirLista;
        private const String strArchivosSueltos = "Xtras (archivos)";
        private const int WM_USER = 0x0400;

        // Definir un evento y su correspondiente delegado
        //public delegate void HiloProcFinalizado(List<DirInfo> dirLista, String strDirName);
        //public event HiloProcFinalizado AlFinalizarHiloProc;

        // Definir la llamada a la API para pasar mensajes
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        #region Constructores
        // Constructor por defecto de la clase
        public HiloDir()
        {
        }

        // Constructor de la clase que recibe las variables iniciales
        public HiloDir(String str) 
        {
            strRuta = str;
        }
        #endregion Constructores


        // Métodos para las propiedades. Es la clásica interfaz pública de la clase de C++
        #region Propiedades
        public void SetDirRuta(String str)
        {
            strRuta = str;
            return;
        }

        public void SetHwnd(IntPtr handle)
        {
            hWnd = handle;
        }

        public String GetDirRuta()
        {
            return strRuta;
        }

        public List<DirInfo> GetDirLista()
        {
            return dirLista;
        }
        #endregion Propiedades

        // El procedimiento propiamente dicho que debe realizar el hilo
        public void HiloProc() 
        {
            //Console.WriteLine(boilerplate, value); 

            // Definición de variables
            DirectoryInfo dir = new DirectoryInfo(strRuta);           

            // Obtener los datos del directorio seleccionado
            dirLista = ExplorarDirectorio(dir);

            // Generar el evento de que el procedimiento del hilo ha finalizado
            //AlFinalizarHiloProc(dirLista, strRuta);

            // Llamar a la API de PostMessage            
            PostMessage(hWnd, WM_USER, IntPtr.Zero, IntPtr.Zero);



            // Finalizar
            return;
        }

        #region Rutinas internas
        /// <summary>
        /// Itera a través del directorio especificado para obtener información
        /// del tamaño que ocupa
        /// </summary>
        /// <param name="dir">Directorio que se quiere analizar</param>
        private List<DirInfo> ExplorarDirectorio(DirectoryInfo dir)
        {
            // Definición de variables
            DirectoryInfo[] directories;

            DirInfo dirData = new DirInfo();
            List<DirInfo> dirLista = new List<DirInfo>();

            long lTamañoTotal = 0;
            int nCarpetas = 0;
            int nArchivos = 0;

            // Iterar para cada uno de los subdirectorios
            directories = dir.GetDirectories();
            foreach (DirectoryInfo d in directories)
            {
                // Si se trata de un directorio real
                if ((d.Attributes & FileAttributes.Directory) != 0)
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
            dirData.Nombre = strArchivosSueltos;    // Constante definida como miembro de la clase
            dirData.Ruta = String.Format("{0}\\{1}", dir.FullName, strArchivosSueltos);
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
                if (dirData.bytes == 0)
                    dirData.porcentaje = 0;
                else
                    dirData.porcentaje = 100f * dirData.bytes / ((Double)lTamañoTotal);
                dirLista[i] = dirData;
            }

            // Devolver el resultado
            return (dirLista);
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

        #endregion Rutinas internas

    }

}
