using System;
//using System.Drawing;
using System.Xml;
using System.Collections.Generic;

namespace Directory_info
{
	/// <summary>
	/// Summary description for ProgramSettings.
	/// </summary>
	public class ProgramSettings
	{
		// The file name used to save the program settings.
		private string _fileName;

		// An Xml document used to store and save program settings.
		private XmlDocument _document;

		public ProgramSettings(string strFileName)
		{
			//
			// TODO: Add constructor logic here
			//

			// Assign the file name.
			_fileName = strFileName;

			// If the file already exists, load it. Otherwise, create a new document.
			_document = new XmlDocument();
			try
			{
				_document.Load(_fileName);
			}
			catch (Exception)
			{
				// Create a new XML document and set the root node.
				_document = new XmlDocument();
				_document.AppendChild(_document.CreateElement("ProgramSettings"));
			}
		}

		//
		// Saves the settings XML file.
		//
		public void Save()
		{
			_document.Save(_fileName);
		}

		//
		// Reads a value from the settings file.
		//
		public string GetValue(string section, string name)
		{
			try
			{
				return _document.DocumentElement.SelectSingleNode(section + "/" + name).InnerText;
			}
			catch (Exception)
			{
				return "";
			}
		}

		//
		// Writes a value to the settings file.
		//
		public void SetValue(string section, string name, string value)
		{
			// If the section does not exist, create it.
			XmlNode sectionNode = _document.DocumentElement.SelectSingleNode(section);
			if (sectionNode == null)
				sectionNode = _document.DocumentElement.AppendChild(_document.CreateElement(section));
			// If the node does not exist, create it.
			XmlNode node = sectionNode.SelectSingleNode(name);
			if (node == null)
				node = sectionNode.AppendChild(_document.CreateElement(name));

			// Set the value.
			node.InnerText = value;
		}

        public void SaveDirList(List<List<DirInfo>> listaDirHistoria)
        {
            // Comprobar si ya existe una sección "DirInfo" y en caso afirmativo borrarla para grabar nuevos datos
            if (_document.DocumentElement.SelectSingleNode("DirInfo") != null)
                _document.DocumentElement.RemoveChild(_document.DocumentElement.SelectSingleNode("DirInfo"));
            
            // Si no hay elementos en la lista, entonces no hacer nada
            if (listaDirHistoria.Count == 0)
                return;

            // Crear una sección "DirInfo"
            XmlNode node = _document.DocumentElement.AppendChild(_document.CreateElement("DirInfo"));
            XmlNode subnode = null, subsub = null, valor = null;
            Int32 i = 0, j = 0;

            foreach (List<DirInfo> listaDir in listaDirHistoria)
            {
                subnode = node.AppendChild(_document.CreateElement("Directory" + i.ToString()));
                i++;
                j = 0;
                
                foreach (DirInfo dir in listaDir)
                {
                    subsub = subnode.AppendChild(_document.CreateElement("SubDirectory" + j.ToString()));

                    valor = subsub.AppendChild(_document.CreateElement("Name"));
                    valor.InnerText=dir.Nombre;

                    valor = subsub.AppendChild(_document.CreateElement("Path"));
                    valor.InnerText=dir.Ruta;

                    valor = subsub.AppendChild(_document.CreateElement("Folders"));
                    valor.InnerText = dir.Carpetas.ToString();

                    valor = subsub.AppendChild(_document.CreateElement("Files"));
                    valor.InnerText = dir.Archivos.ToString();

                    valor = subsub.AppendChild(_document.CreateElement("Percentage"));
                    valor.InnerText = dir.porcentaje.ToString();

                    valor = subsub.AppendChild(_document.CreateElement("Bytes"));
                    valor.InnerText = dir.bytes.ToString();

                    valor = subsub.AppendChild(_document.CreateElement("KB"));
                    valor.InnerText = dir.kilo.ToString();

                    valor = subsub.AppendChild(_document.CreateElement("MB"));
                    valor.InnerText = dir.mega.ToString();

                    valor = subsub.AppendChild(_document.CreateElement("GB"));
                    valor.InnerText = dir.giga.ToString();

                    j++;
                }
            }
        }

        public void ReadDirList(List<List<DirInfo>> listaDirHistoria)
        {
            XmlNode node = _document.DocumentElement.SelectSingleNode("DirInfo");
            XmlNode subnode = null, subsub = null;
            DirInfo dir;
            Int32 i = 0, j=0;

            if (node == null)
                return;

            subnode = node.SelectSingleNode("Directory" + i.ToString());
            while (subnode != null)
            {
                listaDirHistoria.Add(new List<DirInfo>());
                subsub = subnode.SelectSingleNode("SubDirectory" + j.ToString());
                while (subsub != null)
                {
                    dir.Nombre = subsub.SelectSingleNode("Name").InnerText;
                    dir.Ruta = subsub.SelectSingleNode("Path").InnerText;
                    dir.Carpetas = Int32.Parse(subsub.SelectSingleNode("Folders").InnerText);
                    dir.Archivos = Int32.Parse(subsub.SelectSingleNode("Files").InnerText);
                    dir.porcentaje = Double.Parse(subsub.SelectSingleNode("Percentage").InnerText);
                    dir.bytes = long.Parse(subsub.SelectSingleNode("Bytes").InnerText);
                    dir.kilo = Double.Parse(subsub.SelectSingleNode("KB").InnerText);
                    dir.mega = Double.Parse(subsub.SelectSingleNode("MB").InnerText);
                    dir.giga = Double.Parse(subsub.SelectSingleNode("GB").InnerText);

                    listaDirHistoria[i].Add(dir);

                    j++;
                    subsub = subnode.SelectSingleNode("SubDirectory" + j.ToString());
                }

                j = 0;
                i++;
                subnode = node.SelectSingleNode("Directory" + i.ToString());
                
            }

        }
	}

    public class AppSettings
    {
        public Int32 Left { get; set; }
        public Int32 Top { get; set; }
        public Int32 Width { get; set; }
        public Int32 Height { get; set; }
    }
}
