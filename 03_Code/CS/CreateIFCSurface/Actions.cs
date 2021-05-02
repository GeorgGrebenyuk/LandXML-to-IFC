using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.IO;



namespace CreateIFCSurface
{
	public static class Actions
	{
		public static void ConvertOpeation (string PathToSourceFile, string PathToFinishFile, double [] CoordTransform)
		{
			string guid = Guid.NewGuid().ToString();
			string IFC_FilePath = $@"C:\Users\GeorgKeneberg\Documents\Temp\Test-{guid}.ifc";
			//Скопировали файл шаблона для последующего заполнения
			File.Copy(@"D:\Programming\GitRepo\LandXML-to-IFC\02_Resources\ЦММ_Шаблон.ifc", IFC_FilePath);
			//Добавили информацию о сдвижке координат
			string InfoAboutLocation = $"#43= IFCCARTESIANPOINT(({CoordTransform[1]},{CoordTransform[0]},{CoordTransform[2]}));";
			//string InfoAboutLocation = $"#43= IFCCARTESIANPOINT((0.,0.,0.));";
			File.AppendAllText(IFC_FilePath, InfoAboutLocation + Environment.NewLine);

			//Добавляем блок информации о точках (группа Pnts)
			XDocument LandXML_Doc = XDocument.Load(PathToSourceFile);
			XElement Pnts = LandXML_Doc.Descendants().Where(a => a.Name.LocalName == "Pnts").First();
			long Counter1 = 1005;
			foreach (XElement OnePoint in Pnts.Elements())
			{
				string[] PointCoordinates = OnePoint.Value.Split(' ');
				string NewPointCoord = 
					(Convert.ToDouble(PointCoordinates[0]) * 1000).ToString() + "," + 
					(Convert.ToDouble(PointCoordinates[1]) * 1000).ToString() + "," + 
					(Convert.ToDouble(PointCoordinates[2]) * 1000).ToString();
				string InfoAboutPoint = $"#{Counter1}= IFCCARTESIANPOINT(({NewPointCoord}));" + Environment.NewLine;
				File.AppendAllText(IFC_FilePath, InfoAboutPoint);
				Counter1++;
			}
			//Добавляем блок информации о триангуляции (группа Faces)
			XElement Faces = LandXML_Doc.Descendants().Where(a => a.Name.LocalName == "Faces").First();
			string IFCOPENSHELL = "#675= IFCOPENSHELL((";
			foreach (XElement OneFace in Faces.Elements())
			{
				string[] PointNums = OneFace.Value.Split(' ');
				string NewPointsNums = 
					"#" + (Convert.ToInt64(PointNums[0]) + 1000).ToString() + "," +
					"#" + (Convert.ToInt64(PointNums[1]) + 1000).ToString() + "," +
					"#" + (Convert.ToInt64(PointNums[2]) + 1000).ToString();

				string IFCPOLYLOOP = $"#{Counter1}= IFCPOLYLOOP(({NewPointsNums}));" + Environment.NewLine;
				string IFCFACEOUTERBOUND = $"#{Counter1+1}= IFCFACEOUTERBOUND(#{Counter1},.T.);" + Environment.NewLine;
				string IFCFACE = $"#{Counter1 + 2}= IFCFACE((#{Counter1+1}));" + Environment.NewLine;
				if (IFCOPENSHELL.Length < 22) IFCOPENSHELL += $"#{Counter1 + 2}";
				else IFCOPENSHELL += $",#{Counter1 + 2}";

				File.AppendAllText(IFC_FilePath, IFCPOLYLOOP + IFCFACEOUTERBOUND + IFCFACE);
				//File.AppendAllText(IFC_FilePath, InfoAboutPoint);
				Counter1 +=3;
			}
			//Добавляем блок информации о IFCOPENSHELL
			File.AppendAllText(IFC_FilePath, IFCOPENSHELL + "));" + Environment.NewLine);

			//Обязательная концовка файла
			File.AppendAllText(IFC_FilePath, "ENDSEC;" + Environment.NewLine + "END-ISO-10303-21;" + Environment.NewLine);

		}
		/// <summary>
		/// Инициализируем файл LandXML и выводим его BoundingBox + высоту
		/// </summary>
		/// <param name="PathToSourceFile"></param>
		/// <returns>Массив со значениями, которыt прописываются в консоль приложения + записываются в Fields как рекомендованные паарметры трансформации</returns>
		public static void CheckFileLocation (string PathToSourceFile)
		{
			XDocument LandXML_Doc = XDocument.Load(PathToSourceFile);
			XElement Pnts = LandXML_Doc.Descendants().Where(a => a.Name.LocalName == "Pnts").First();

			//IEnumerable<XNode> PntsCollection = LandXML_Doc.Descendants().Where(a => a.Name.LocalName == "Pnts");
			//Временные переменные
			double XMin = 100000000; double YMin = 100000000; double XMax = -100000000; double YMax = -100000000; double ZMin = 100000000; double ZMax = -100000000;
			foreach (XElement OnePoint in Pnts.Elements())
			{
				string [] OnePointCoords = OnePoint.Value.Split(' ');
				double X_Coord = Convert.ToDouble(OnePointCoords[1]);
				double Y_Coord = Convert.ToDouble(OnePointCoords[0]);
				double Z_Coord = Convert.ToDouble(OnePointCoords[2]);

				if (X_Coord > XMax) XMax = X_Coord;
				else if (X_Coord < XMin) X_Coord = XMin;

				if (Y_Coord > YMax) YMax = Y_Coord;
				else if (Y_Coord < YMin) Y_Coord = YMin;

				if (Z_Coord > ZMax) ZMax = Z_Coord;
				else if (Z_Coord < ZMin) Z_Coord = ZMin;
			}

			//double[] Offset = new double[3] { -(XMax + XMin) * 500, -(YMax + YMin) * 500, -(ZMax + ZMin) * 500 };
			double[] Offset = new double[3] { -XMax*1000, -YMax * 1000, -ZMax * 1000,};
			ConvertOpeation(MainWindow.PathToLandXMLFile, MainWindow.PathToIFCSaving, Offset);

		}
	}
}
