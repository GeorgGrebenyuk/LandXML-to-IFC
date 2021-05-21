using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;

using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.IO;
using Xbim.Ifc4.ActorResource;
using Xbim.Ifc4.DateTimeResource;
using Xbim.Ifc4.ExternalReferenceResource;
using Xbim.Ifc4.PresentationOrganizationResource;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MaterialResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.QuantityResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;


namespace ConsoleApp10
{
	public class Actions2
	{
		//Общие переменные
		private static string PathToLandXMLFile = @"D:\Programming\GitRepo\LandXML-to-IFC\01_SampleFiles\ТопоПростая.xml";
		private static string PathToSaveIFC = null;

		private static XbimEditorCredentials EditorConfigs = new XbimEditorCredentials
		{
			ApplicationDevelopersName = "xbim developer",
			ApplicationFullName = "xbim toolkit",
			ApplicationIdentifier = "xbim",
			ApplicationVersion = "4.0",
			EditorsFamilyName = "Grebenyuk",
			EditorsGivenName = "Egor",
			EditorsOrganisationName = "TBS"
		};
		public static int StringCounter = 0;
		public static int IFCGEOMETRICREPRESENTATIONCONTEXT_Model = 0;
		public static void CreateBasicIfc_File ()
		{
			PathToSaveIFC = $@"C:\Users\GeorgKeneberg\Documents\Temp\Surface-{Guid.NewGuid().ToString()}.ifc";

			using (var model = IfcStore.Create(EditorConfigs, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
			{
				using (var Trans1 = model.BeginTransaction("Hello, IFC!"))
				{
					var myProject = model.Instances.New<IfcProject>(a => a.Name = "RootElement");
					//Задаем единицы модели
					myProject.Initialize(ProjectUnits.SIUnitsUK);
					Trans1.Commit();
				}
					model.SaveAs(PathToSaveIFC);
			}
			using (var model = IfcStore.Open(PathToSaveIFC, EditorConfigs))
			{
				//Устанавливаем номер для IfcGeometricRepresentationSubContext
				IFCGEOMETRICREPRESENTATIONCONTEXT_Model = model.Instances.OfType<IfcRepresentationContext>().Where(a => a.ContextType.Value == "Model").First().EntityLabel;
			}

		}
		//Создаем список для переопределения номеров точек
		public static List<string> PointsNums = new List<string>();
		public static void AddPointsToIfc ()
		{
			using (var model = IfcStore.Open(PathToSaveIFC,EditorConfigs))
			{
				using (var Trans2 = model.BeginTransaction("Add points"))
				{
					//Добавляем блок информации о точках (группа Pnts)
					XDocument LandXML_Doc = XDocument.Load(PathToLandXMLFile);
					XElement Pnts = LandXML_Doc.Descendants().Where(a => a.Name.LocalName == "Pnts").First();
					//Добавляем блок информации о триангуляции (группа Faces)
					XElement Faces = LandXML_Doc.Descendants().Where(a => a.Name.LocalName == "Faces").First();

					//Создаем ряд геометрических представлений для точек поверхности
					List<string> PointsNumbers = new List<string>();
					void AddInfoAboutPoint(string PointNum)
					{
						char K = '"';
						PointsNumbers.Add(PointNum);
						XElement OnePoint = Pnts.Elements().Where(a => a.Attribute("id").Value.Replace(K.ToString(), string.Empty) == PointNum).First();
						string[] PointCoordinates = OnePoint.Value.Split(' ');
						var myPoint = model.Instances.New<IfcCartesianPoint>(a => a.SetXYZ(
							(Convert.ToDouble(PointCoordinates[0]) * 1000),
							(Convert.ToDouble(PointCoordinates[1]) * 1000),
							(Convert.ToDouble(PointCoordinates[2]) * 1000)));
						PointsNums.Add(PointNum + '|' + myPoint.EntityLabel);
						StringCounter = myPoint.EntityLabel;
					}
					bool IsContain(string CheckValue)
					{
						foreach (string str in PointsNumbers)
						{
							if (str == CheckValue) return true;
						}
						return false;
					}
					//Заносим информацию о точках смотря на отдельные грани в рамках набора граней
					foreach (XElement OneFace in Faces.Elements())
					{
						string[] PointNums = OneFace.Value.Split(' ');
						for (int i1 = 0; i1 < 3; i1++)
						{if (IsContain(PointNums[i1]) == false) AddInfoAboutPoint(PointNums[i1]);}
					}
					//Заканчиваем вносить изменения
					Trans2.Commit();
				}
				model.SaveAs(PathToSaveIFC);
			}
		}
		private static void DeleteEndOfFile()
		{
			var tempFile = Path.GetTempFileName();
			var linesToKeep = File.ReadLines(PathToSaveIFC).Where(l => !l.Contains("ENDSEC;") & !l.Contains("END-ISO-10303-21;"));
			File.WriteAllLines(tempFile, linesToKeep);
			File.Delete(PathToSaveIFC);
			File.Move(tempFile, PathToSaveIFC);
		}
		private static void RepairEndOdFile()
		{
			File.AppendAllText(PathToSaveIFC, "ENDSEC;" + Environment.NewLine + "END-ISO-10303-21;" + Environment.NewLine);
		}

		private static int IfcFaceBasedSurfaceModelNum = 0;
		public static void AddFacesToIfc ()
		{
			DeleteEndOfFile();
			StringCounter += 1; //Увеличиваем на +1 счетчик строк файла IFC

			string GetNewNum(string OldNum)
			{
				foreach (string str in PointsNums)
				{
					if (str.Split('|')[0] == OldNum) return str.Split('|')[1];
				}
				return null;
			}
			using (StreamWriter sw = new StreamWriter(PathToSaveIFC, true, System.Text.Encoding.UTF8))
			{
				XDocument LandXML_Doc = XDocument.Load(PathToLandXMLFile);
				XElement Faces = LandXML_Doc.Descendants().Where(a => a.Name.LocalName == "Faces").First();

				string myIfcConnectedFaceSet = null;
				foreach (XElement OneFace in Faces.Elements())
				{
					string[] PointNums = OneFace.Value.Split(' ');
					string[] NewPointsNums = new string[3];
					for (int i1 = 0; i1 < 3; i1++) { NewPointsNums[i1] = GetNewNum(PointNums[i1]); }

					string IFCPOLYLOOP = $"#{StringCounter}=IFCPOLYLOOP((#{NewPointsNums[0]},#{NewPointsNums[1]},#{NewPointsNums[2]}));";
					sw.WriteLine(IFCPOLYLOOP);
					string IFCFACEOUTERBOUND = $"#{StringCounter + 1}=IFCFACEOUTERBOUND(#{StringCounter},.T.);";
					sw.WriteLine(IFCFACEOUTERBOUND);
					string IFCFACE = $"#{StringCounter + 2}=IFCFACE((#{StringCounter + 1}));";
					sw.WriteLine(IFCFACE);

					if (myIfcConnectedFaceSet == null) myIfcConnectedFaceSet += $"#{StringCounter + 2}";
					else myIfcConnectedFaceSet += $",#{StringCounter + 2}";
					StringCounter += 3;
				}
				sw.WriteLine($"#{StringCounter}=IFCCONNECTEDFACESET(({myIfcConnectedFaceSet}));");
				sw.WriteLine($"#{StringCounter+1}=IFCFACEBASEDSURFACEMODEL((#{StringCounter}));");
				IfcFaceBasedSurfaceModelNum = StringCounter + 1;
				PointsNums.Clear();
			}
		}
		public static void AddStyleToSurface ()
		{
			StringCounter += 2; //Увеличиваем на +1 счетчик строк файла IFC
			using (StreamWriter sw = new StreamWriter(PathToSaveIFC, true, System.Text.Encoding.UTF8))
			{
				sw.WriteLine($@"#{StringCounter}=IFCCOLOURRGB($,0.380392156862745,0.294117647058824,0.243137254901961);");
				sw.WriteLine($@"#{StringCounter + 1}=IFCSURFACESTYLERENDERING(#{StringCounter},0.,$,$,$,$,IFCNORMALISEDRATIOMEASURE(0.5),IFCSPECULAREXPONENT(128.),.NOTDEFINED.);");
				sw.WriteLine($@"#{StringCounter + 2}=IFCSURFACESTYLE('\X2\04170435043C043B044F\X0\',.BOTH.,(#{StringCounter + 1}));");
				sw.WriteLine($@"#{StringCounter + 3}=IFCPRESENTATIONSTYLEASSIGNMENT((#{StringCounter + 2}));");
				sw.WriteLine($@"#{StringCounter + 4}=IFCSTYLEDITEM(#{IfcFaceBasedSurfaceModelNum},(#{StringCounter + 3}),$);");
			}
		}
		public static void AddSurfaceToIfcSite ()
		{
			StringCounter += 5;
			using (StreamWriter sw = new StreamWriter(PathToSaveIFC, true, System.Text.Encoding.UTF8))
			{
				sw.WriteLine($@"#{StringCounter}=IFCSHAPEREPRESENTATION(#{IFCGEOMETRICREPRESENTATIONCONTEXT_Model},'Body','SurfaceModel',(#{IfcFaceBasedSurfaceModelNum})); ('Body' 'SurfaceModel');");
				sw.WriteLine($@"#{StringCounter + 1}=IFCPRODUCTDEFINITIONSHAPE($,$,(#{StringCounter}));");
			}
			RepairEndOdFile();
			using (var model = IfcStore.Open(PathToSaveIFC, EditorConfigs))
			{
				using (var Trans4 = model.BeginTransaction("Add surface to IfcSite"))
				{
					var myIfcSite = model.Instances.New<IfcSite>();
					var mySurface = model.Instances.FirstOrDefault<IfcProductDefinitionShape>();
					myIfcSite.Representation = mySurface;
					//Площадка IfcLocalPlacement
					var RootPoint = model.Instances.New<IfcCartesianPoint>(a => a.SetXYZ(0d, 0d, 0d));
					var RootAxis3D = model.Instances.New<IfcAxis2Placement3D>(a => a.Location = RootPoint);
					var myLocalPlacent = model.Instances.New<IfcLocalPlacement>(a=>a.RelativePlacement = RootAxis3D);
					myIfcSite.ObjectPlacement = myLocalPlacent;
					//Заканчиваем вносить изменения
					Trans4.Commit();
				}
				model.SaveAs(PathToSaveIFC);
			}
		}
	}
}
