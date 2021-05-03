using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using MathNet.Numerics.LinearAlgebra;
using System.Reflection;


namespace CreateIFCSurface
{
	public static class Actions
	{
		private static void CreateFileFromTemplate (string PathToFinishFile)
		{
			var assembly = Assembly.GetExecutingAssembly();
			using (Stream stream = assembly.GetManifestResourceStream("CreateIFCSurface.Data.Template.ifc"))

			using (StreamReader reader = new StreamReader(stream))
			{
				File.AppendAllText (PathToFinishFile,reader.ReadToEnd());
			}
		}
		public static void ConvertOpeation (string PathToSourceFile, double [] CoordTransform, bool ReCalc)
		{
			string guid = Guid.NewGuid().ToString();
			string SourceDir = Path.GetDirectoryName(PathToSourceFile);
			
			string IFC_FilePath = SourceDir + $"\\Converted-{guid}.ifc";
			//Создали болванку файла IFC из ресурсов проекта
			CreateFileFromTemplate(IFC_FilePath);
			//Скопировали файл шаблона для последующего заполнения - для отладки
			//File.Copy(@"D:\Programming\GitRepo\LandXML-to-IFC\02_Resources\ЦММ_Шаблон.ifc", IFC_FilePath);
			//Добавили информацию о сдвижке координат
			string InfoAboutLocation = null;
			string InfoAboutAngle = null;
			InfoAboutLocation = $"#43= IFCCARTESIANPOINT((0.,0.,0.));";
			InfoAboutAngle = $"#45= IFCDIRECTION((1.,0.,0.));";

			//if (ReCalc == true)
			//{
			//	InfoAboutLocation = $"#43= IFCCARTESIANPOINT((0.,0.,0.));";
			//	InfoAboutAngle = $"#45= IFCDIRECTION((1.,0.,0.));";
			//}
			//else
			//{
			//	InfoAboutLocation = $"#43= IFCCARTESIANPOINT(({CoordTransform[1]},{CoordTransform[0]},{CoordTransform[2]}));";
			//	InfoAboutAngle = $"#45= IFCDIRECTION(({Math.Cos(CoordTransform[3])},{-Math.Sin(CoordTransform[3])},0.));";
			//}

			//string InfoAboutLocation = $"#43= IFCCARTESIANPOINT((0.,0.,0.));";
			File.AppendAllText(IFC_FilePath, InfoAboutLocation + Environment.NewLine);
			File.AppendAllText(IFC_FilePath, InfoAboutAngle + Environment.NewLine);
			//Добавляем блок информации о точках (группа Pnts)
			XDocument LandXML_Doc = XDocument.Load(PathToSourceFile);
			XElement Pnts = LandXML_Doc.Descendants().Where(a => a.Name.LocalName == "Pnts").First();
			//Добавляем блок информации о триангуляции (группа Faces)
			XElement Faces = LandXML_Doc.Descendants().Where(a => a.Name.LocalName == "Faces").First();
			List<string> PointsNumbers = new List<string>();
			
			
			long NumMax = 0; //Переменная для установления максимального номера точки (участвует в формировании номеров строк IFC)
			//Метод добавления в файл IFC информации о точке по её номеру (из атрибута id в файле LandXML)
			void AddInfoAboutPoint (string PointNum)
			{
				PointsNumbers.Add(PointNum);
				char K = '"'; //Символ кавычки в параметре id
				
				XElement OnePoint = Pnts.Elements().Where(a => a.Attribute("id").Value.Replace(K.ToString(), string.Empty) == PointNum).First();
				string[] PointCoordinates = OnePoint.Value.Split(' ');
				string NewPointCoord = null;
				if (ReCalc == true)
				{
					ωz = CoordTransform[3];
					//Элементы матрицы поворота Р
					double a11 = Math.Cos(ωz);
					double a12 = Math.Sin(ωz);
					double a13 = 0d;
					double a21 = -Math.Sin(ωz);
					double a22 = Math.Cos(ωz);
					double a23 = 0d;
					double a31 = 0d;
					double a32 = 0d;
					double a33 = 1d;
					//Уравнение трансформации координат
					var P = CreateMatrix.Dense(3, 3, new double[] { a11, a21, a31, a12, a22, a32, a13, a23, a33 });
					var OldCoords = CreateMatrix.Dense(3, 1, new double[] { Convert.ToDouble(PointCoordinates[1]), Convert.ToDouble(PointCoordinates[0]), Convert.ToDouble(PointCoordinates[2]) });
					var OffsetCoords = CreateMatrix.Dense(3, 1, new double[] {  CoordTransform[0]/1000d, CoordTransform[1]/1000d, CoordTransform[2]/1000d });
					var NewCoords_m = P * OldCoords + OffsetCoords;

					double[][] NewCoords = NewCoords_m.ToRowArrays();

					NewPointCoord =
					(NewCoords[0][0] * 1000).ToString() + "," +
					(NewCoords[1][0] * 1000).ToString() + "," +
					(NewCoords[2][0] * 1000).ToString();
				}
				//Пока исполтзуется только для прямого пересчета координат
				else NewPointCoord =
					(Convert.ToDouble(PointCoordinates[0]) * 1000).ToString() + "," +
					(Convert.ToDouble(PointCoordinates[1]) * 1000).ToString() + "," +
					(Convert.ToDouble(PointCoordinates[2]) * 1000).ToString();

				string InfoAboutPoint = $"#{Convert.ToInt64(PointNum)+1000}= IFCCARTESIANPOINT(({NewPointCoord}));" + Environment.NewLine;
				File.AppendAllText(IFC_FilePath, InfoAboutPoint);
				//Counter1++;
			}
			bool IsContain (string CheckValue)
			{
				foreach (string str in PointsNumbers)
				{
					if (str == CheckValue) return true;
				}
				return false;
			}
			
			//Заносим информацию о точках
			foreach (XElement OneFace in Faces.Elements())
			{
				string[] PointNums = OneFace.Value.Split(' ');
				for (int i1 = 0;i1<3;i1++)
				{
					if (IsContain(PointNums[i1]) == false) AddInfoAboutPoint(PointNums[i1]);
					if (Convert.ToInt64(PointNums[i1]) > NumMax) NumMax = Convert.ToInt64(PointNums[i1]);
				}
			}
			long Counter1 = 1000 + NumMax+1;

			//Заносим информацию о гранях
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
				if (X_Coord < XMin) XMin = X_Coord;

				if (Y_Coord > YMax) YMax = Y_Coord;
				if (Y_Coord < YMin) YMin = Y_Coord;

				if (Z_Coord > ZMax) ZMax = Z_Coord;
				if (Z_Coord < ZMin) ZMin = Z_Coord;
			}

			double[] Offset = new double[4] { -(XMax + XMin) * 500, -(YMax + YMin) * 500, -(ZMax + ZMin) * 500, 0d };
			//double[] Offset = new double[3] { XMax*1000 + ((XMax - XMin) * 500), YMax * 1000 + ((YMax - YMin) * 500), ZMax * 1000 + ((ZMax - ZMin) * 500) };
			//MainWindow.Log.Append("Параметры сдвижки были приняты:" + Environment.NewLine + XMax + "," + XMin + "," + YMax + "," + YMin + "," + ZMax + "," + ZMin);
			//double[] Offset = new double[3] { XMax*1000, YMax * 1000, ZMax * 1000,};
			ConvertOpeation(MainWindow.PathToLandXMLFile, Offset, true);

		}
		//Элементы трансформации и вспомогательные переменные
		public static double ΔX = 0d;
		public static double ΔY = 0d;
		public static double ΔZ = 0d;
		//double ωx = 0d;
		//double ωy = 0d;
		public static double ωz = 0d;
		//double M = 1.0d;
		public static double error = 0d;


		public static void  FindParameters(string[] Data) //Получение на вход массива строк (1-6) с основной формы
		{
			string OldCSPoint1 = Data[0];
			string[] OldCSPoint1_str = OldCSPoint1.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			double XC1 = Convert.ToDouble(OldCSPoint1_str[0]);
			double YC1 = Convert.ToDouble(OldCSPoint1_str[1]);
			double ZC1 = Convert.ToDouble(OldCSPoint1_str[2]);
			//Создание матрицы mC1 3х1 из исходных данных
			var mC1 = CreateMatrix.Dense(3, 1, new double[] { XC1, YC1, ZC1 });

			string OldCSPoint2 = Data[1];
			string[] OldCSPoint2_str = OldCSPoint2.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			double XC2 = Convert.ToDouble(OldCSPoint2_str[0]);
			double YC2 = Convert.ToDouble(OldCSPoint2_str[1]);
			double ZC2 = Convert.ToDouble(OldCSPoint2_str[2]);
			//Создание матрицы mC2 3х1 из исходных данных
			var mC2 = CreateMatrix.Dense(3, 1, new double[] { XC2, YC2, ZC2 });

			string OldCSPoint3 = Data[2];
			string[] OldCSPoint3_str = OldCSPoint3.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			double XC3 = Convert.ToDouble(OldCSPoint3_str[0]);
			double YC3 = Convert.ToDouble(OldCSPoint3_str[1]);
			double ZC3 = Convert.ToDouble(OldCSPoint3_str[2]);
			//Создание матрицы mC3 3х1 из исходных данных
			var mC3 = CreateMatrix.Dense(3, 1, new double[] { XC3, YC3, ZC3 });

			string NewCSPoint1 = Data[3];
			string[] NewCSPoint1_str = NewCSPoint1.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			double XN1 = Convert.ToDouble(NewCSPoint1_str[0]);
			double YN1 = Convert.ToDouble(NewCSPoint1_str[1]);
			double ZN1 = Convert.ToDouble(NewCSPoint1_str[2]);

			string NewCSPoint2 = Data[4];
			string[] NewCSPoint2_str = NewCSPoint2.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			double XN2 = Convert.ToDouble(NewCSPoint2_str[0]);
			double YN2 = Convert.ToDouble(NewCSPoint2_str[1]);
			double ZN2 = Convert.ToDouble(NewCSPoint2_str[2]);

			string NewCSPoint3 = Data[5];
			string[] NewCSPoint3_str = NewCSPoint3.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			double XN3 = Convert.ToDouble(NewCSPoint3_str[0]);
			double YN3 = Convert.ToDouble(NewCSPoint3_str[1]);
			double ZN3 = Convert.ToDouble(NewCSPoint3_str[2]);

			//Разности координат
			double dXC1 = XC2 - XC1;
			double dYC1 = YC2 - YC1;
			double dZC1 = ZC2 - ZC1;
			double dXC2 = XC3 - XC2;
			double dYC2 = YC3 - YC2;
			double dZC2 = ZC3 - ZC2;
			double dXC3 = XC3 - XC1;
			double dYC3 = YC3 - YC1;
			double dZC3 = ZC3 - ZC1;

			double dXN1 = XN2 - XN1;
			double dYN1 = YN2 - YN1;
			double dZN1 = ZN2 - ZN1;
			double dXN2 = XN3 - XN2;
			double dYN2 = YN3 - YN2;
			double dZN2 = ZN3 - ZN2;
			double dXN3 = XN3 - XN1;
			double dYN3 = YN3 - YN1;
			double dZN3 = ZN3 - ZN1;
			//Элементы ортогональной матрицы вращения
			double a11 = 0d;
			double a12 = 0d;
			double a13 = 0d;
			double a21 = 0d;
			double a22 = 0d;
			double a23 = 0d;
			double a31 = 0d;
			double a32 = 0d;
			double a33 = 0d;
			double ωz_opt = 0d;

			//Параметр оптимизации
			double v_min = 1000000000000d; //Начальное заведомо-большее значение
			while (ωz < 2 * Math.PI) //МНК для поиска значения ωz
			{
				//Коэффициенты матрицы поврота
				a11 = Math.Cos(ωz);
				a12 = Math.Sin(ωz);
				a21 = -Math.Sin(ωz);
				a22 = Math.Cos(ωz);
				//Запись системы уравнений оптимизации

				double vx1 = a11 * dXC1 + a12 * dYC1 + a13 * dZC1 - dXN1;
				double vx2 = a11 * dXC2 + a12 * dYC2 + a13 * dZC2 - dXN2;
				double vx3 = a11 * dXC3 + a12 * dYC3 + a13 * dZC3 - dXN3;
				double vy1 = a21 * dXC1 + a22 * dYC1 + a23 * dZC1 - dYN1;
				double vy2 = a21 * dXC2 + a22 * dYC2 + a23 * dZC2 - dYN2;
				double vy3 = a21 * dXC3 + a22 * dYC3 + a23 * dZC3 - dYN3;
				//Параметр оптимизации
				double v = Math.Pow(vx1, 2) + Math.Pow(vx2, 2) + Math.Pow(vx3, 2) + Math.Pow(vy1, 2) + Math.Pow(vy2, 2) + Math.Pow(vy3, 2);
				if (v < v_min)
				{   //Запись в память потенциального решения и перезадание параметра v_max
					ωz_opt = ωz;
					v_min = v;
				}
				ωz = ωz + 0.000001; //Прибавление значений, радиан
			}
			//Возврат найденного оптимального значения
			ωz = ωz_opt;
			error = v_min;
			//Пересчет матрицы
			a11 = Math.Cos(ωz);
			a12 = Math.Sin(ωz);
			a13 = 0d;
			a21 = -Math.Sin(ωz);
			a22 = Math.Cos(ωz);
			a23 = 0d;
			a31 = 0d;
			a32 = 0d;
			a33 = 1d;
			//var P = CreateMatrix.Dense(3, 3, new double[] { a11, a12, a13, a21, a22, a23, a31, a32, a33 });
			//Члены для матрицы вбивать по колонкам !!!!!!!!!!! 
			var P = CreateMatrix.Dense(3, 3, new double[] { a11, a21, a31, a12, a22, a32, a31, a23, a33 });

			//Перемножаем матрицы для вычисления вспомогательных (без сдвигов) координат
			var mHelp1 = P * mC1;
			double[][] mHelp1_result = mHelp1.ToRowArrays();
			double x1 = mHelp1_result[0][0];
			double y1 = mHelp1_result[1][0];
			double z1 = mHelp1_result[2][0];

			var mHelp2 = P * mC2;
			double[][] mHelp2_result = mHelp2.ToRowArrays();
			double x2 = mHelp2_result[0][0];
			double y2 = mHelp2_result[1][0];
			double z2 = mHelp2_result[2][0];

			var mHelp3 = P * mC3;
			double[][] mHelp3_result = mHelp3.ToRowArrays();
			double x3 = mHelp3_result[0][0];
			double y3 = mHelp3_result[1][0];
			double z3 = mHelp3_result[2][0];

			var H = CreateMatrix.Dense(9, 3, new double[] { 1d, 0d, 0d, 1d, 0d, 0d, 1d, 0d, 0d, 0d, 1d, 0d, 0d, 1d, 0d, 0d, 1d, 0d, 0d, 0d, 1d, 0d, 0d, 1d, 0d, 0d, 1d });
			var dL = CreateMatrix.Dense(9, 1, new double[] { XN1 - x1, YN1 - y1, ZN1 - z1, XN2 - x2, YN2 - y2, ZN2 - z2, XN3 - x3, YN3 - y3, ZN3 - z3 });

			var dXYZ_m = ((H.Transpose() * H).Inverse()) * H.Transpose() * dL;
			double[][] dXYZ = dXYZ_m.ToRowArrays();

			//Округляем значения чтобы не тащить кучу хвостов из точности расчета
			ΔX = Math.Round(dXYZ[0][0], 6);
			ΔY = Math.Round(dXYZ[1][0], 6);
			ΔZ = Math.Round(dXYZ[2][0], 6);
			error = Math.Round(error, 9);

			double[] Offset = new double[4] { ΔX * 1000, ΔY * 1000, ΔZ * 1000, ωz};
			MainWindow.Log.Append("Линейная ошибка" + error + "метров");
			ConvertOpeation(MainWindow.PathToLandXMLFile, Offset,true);

		}
	}
}
