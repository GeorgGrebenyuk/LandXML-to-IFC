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
	public class Actions
	{
		public static void Act1 ()
		{
			string MagiCadAD_File = @"C:\Users\GeorgKeneberg\Downloads\MagiCAD_for_AutoCAD_4.ifc";
			string guid = Guid.NewGuid().ToString();
			string PathToSave = $@"C:\Users\GeorgKeneberg\Documents\Temp\File-{guid}.ifc";
			string PathToSourceFile = @"C:\Users\GeorgKeneberg\Documents\Temp\Красная1.xml";

			var editor = new XbimEditorCredentials
			{
				
				ApplicationDevelopersName = "xbim developer",
				ApplicationFullName = "xbim toolkit",
				ApplicationIdentifier = "xbim",
				ApplicationVersion = "4.0",
				EditorsFamilyName = "Grebenyuk",
				EditorsGivenName = "Egor",
				EditorsOrganisationName = "TBS"
			};
			//Сохраняем во временные переменные ряд значений
			int IFCGEOMETRICREPRESENTATIONCONTEXT_Model = 0;
			int IFCBUILDINGSTOREY_El = 0;
			int IFCAXIS2PLACEMENT2D_El = 0;

			//Создаем список для переопределения номеров точек
			List<string> PointsNums = new List<string>();
			int LastNum = 0;
			using (var model = IfcStore.Create(editor,XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
			{
				//Для создания/изменения элементов необходимы транзакции. Для чтения не обязательно
				//Создаем болванку
				using (var txn = model.BeginTransaction("Hello, IFC!"))
				{
					//В рамках транзакции делаем действия
					//Создаем корневой элемент - IfcProject
					var myProject = model.Instances.New<IfcProject>(a => a.Name = "TestProject");
					//Задаем единицы модели
					myProject.Initialize(ProjectUnits.SIUnitsUK);
					//Задаем площадку
					var mySite = model.Instances.New<IfcSite>(a => a.Name = "TestSite");
					//IFCBUILDINGSTOREY
					var myStorey = model.Instances.New<IfcBuildingStorey>();
					IFCBUILDINGSTOREY_El = myStorey.EntityLabel;
					//Заканчиваем вносить изменения
					txn.Commit();
				}
				//Создаем точки
				using (var txn = model.BeginTransaction("Add points"))
				{
					//Добавляем блок информации о точках (группа Pnts)
					XDocument LandXML_Doc = XDocument.Load(PathToSourceFile);
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
						LastNum = myPoint.EntityLabel;
					}

					bool IsContain(string CheckValue)
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
						for (int i1 = 0; i1 < 3; i1++)
						{
							if (IsContain(PointNums[i1]) == false) AddInfoAboutPoint(PointNums[i1]);
						}
					}
					//Заканчиваем вносить изменения
					txn.Commit();
				}
				model.SaveAs(PathToSave);
			}
			
			void DeleteEndOfFile ()
			{
				var tempFile = Path.GetTempFileName();
				var linesToKeep = File.ReadLines(PathToSave).Where(l => !l.Contains ("ENDSEC;") & !l.Contains("END-ISO-10303-21;"));

				File.WriteAllLines(tempFile, linesToKeep);

				File.Delete(PathToSave);
				File.Move(tempFile, PathToSave);
			}
			void RepairEndOdFile ()
			{
				File.AppendAllText(PathToSave, "ENDSEC;" + Environment.NewLine + "END-ISO-10303-21;" + Environment.NewLine);
			}
			

			using (var model = IfcStore.Open(PathToSave))
			{
				IFCGEOMETRICREPRESENTATIONCONTEXT_Model = model.Instances.OfType<IfcRepresentationContext>().Where(a=>a.ContextType.Value == "Model").First().EntityLabel;
				IFCAXIS2PLACEMENT2D_El = model.Instances.OfType<IfcAxis2Placement2D>().First().EntityLabel; //Check it
			}

			DeleteEndOfFile();
			//Создаем грани
			int Counter = 1;
			using (StreamWriter sw = new StreamWriter(PathToSave, true, System.Text.Encoding.UTF8))
			{
				string GetNewNum(string OldNum)
				{
					foreach (string str in PointsNums)
					{
						if (str.Split('|')[0] == OldNum) return str.Split('|')[1];
					}
					return null;
				}
				

				//Добавляем блок информации о точках (группа Pnts)
				XDocument LandXML_Doc = XDocument.Load(PathToSourceFile);
				XElement Pnts = LandXML_Doc.Descendants().Where(a => a.Name.LocalName == "Pnts").First();
				//Добавляем блок информации о триангуляции (группа Faces)
				XElement Faces = LandXML_Doc.Descendants().Where(a => a.Name.LocalName == "Faces").First();

				string IFCOPENSHELL = null;
				foreach (XElement OneFace in Faces.Elements())
				{
					
					string[] PointNums = OneFace.Value.Split(' ');
					string[] NewPointsNums = new string[3];
					for (int i1 = 0; i1<3 ; i1++)
					{
						NewPointsNums[i1] = GetNewNum(PointNums[i1]);
					}

					
					string IFCPOLYLOOP = $"#{LastNum + Counter}= IFCPOLYLOOP((#{NewPointsNums[0]},#{NewPointsNums[1]},#{NewPointsNums[2]}));";
					sw.WriteLine(IFCPOLYLOOP);
					string IFCFACEOUTERBOUND = $"#{LastNum + Counter + 1}= IFCFACEOUTERBOUND(#{LastNum + Counter},.T.);";
					sw.WriteLine(IFCFACEOUTERBOUND);
					string IFCFACE = $"#{LastNum + Counter + 2}= IFCFACE((#{LastNum + Counter + 1}));";
					sw.WriteLine(IFCFACE);

					if (IFCOPENSHELL == null) IFCOPENSHELL += $"#{LastNum + Counter + 2}";
					else IFCOPENSHELL += $",#{LastNum + Counter + 2}";
					Counter += 3;
				}
				//Добавляем блок информации о IFCOPENSHELL
				sw.WriteLine($"#{LastNum + Counter}=IFCOPENSHELL((" + IFCOPENSHELL + "));");
				//Добавление информации об IFCSHELLBASEDSURFACEMODEL
				sw.WriteLine($"#{LastNum + Counter+1}=IFCSHELLBASEDSURFACEMODEL((#{LastNum + Counter}));");
				//Добавление информации об IFCSHAPEREPRESENTATION
				sw.WriteLine($"#{LastNum + Counter + 2}=IFCSHAPEREPRESENTATION(#{IFCGEOMETRICREPRESENTATIONCONTEXT_Model},'Body','SurfaceModel',(#{LastNum + Counter + 1}));");
				//Добавление вспомогательной геометрической информации о включении поверхности в структуру файла
				sw.WriteLine($"#{LastNum + Counter + 3}=IFCCARTESIANPOINT((0.,0.,0.));");
				sw.WriteLine($"#{LastNum + Counter + 4}=IFCDIRECTION((0.,0.,1.));");
				sw.WriteLine($"#{LastNum + Counter + 5}=IFCDIRECTION((1.,0.,0.));");
				sw.WriteLine($"#{LastNum + Counter + 6}=IFCAXIS2PLACEMENT3D(#{LastNum + Counter + 3},#{LastNum + Counter + 4},#{LastNum + Counter + 5});");
				sw.WriteLine($"#{LastNum + Counter + 5}=IFCREPRESENTATIONMAP(#{LastNum + Counter + 6},#{LastNum + Counter + 2});");
				sw.WriteLine($"#{LastNum + Counter + 6}=IFCDIRECTION((1.,0.,0.));");
				sw.WriteLine($"#{LastNum + Counter + 7}=IFCDIRECTION((0.,1.,0.));");
				sw.WriteLine($"#{LastNum + Counter + 8}=IFCDIRECTION((0.,0.,1.));");
				sw.WriteLine($"#{LastNum + Counter + 9}=IFCCARTESIANPOINT((0.,0.,0.));");
				sw.WriteLine($"#{LastNum + Counter + 10}=IFCCARTESIANTRANSFORMATIONOPERATOR3D(#{LastNum + Counter + 6},#{LastNum + Counter + 7},#{LastNum + Counter + 9},1.,#{LastNum + Counter + 8});");
				sw.WriteLine($"#{LastNum + Counter + 11}=IFCMAPPEDITEM(#{LastNum + Counter + 5},#{LastNum + Counter + 10});");
				sw.WriteLine($"#{LastNum + Counter + 12}=IFCSHAPEREPRESENTATION(#{IFCGEOMETRICREPRESENTATIONCONTEXT_Model},'Body','MappedRepresentation',(#{LastNum + Counter + 11}));");
				sw.WriteLine($"#{LastNum + Counter + 13}=IFCPRODUCTDEFINITIONSHAPE($,$,(#{LastNum + Counter + 12}));");
				//Создаем элемент IFCBUILDINGELEMENTPROXY
				sw.WriteLine($@"#{LastNum + Counter + 14}=IFCBUILDINGELEMENTPROXY('32DgpYqEj17A3g$ElKwo36',$,'\X2\0413043E044004380437043E043D04420430043B0438\X0\ \X2\043F0440043E0435043A0442043D044B0435\X0\',$,$,#{IFCAXIS2PLACEMENT2D_El},#{LastNum + Counter + 13},'',$);");

				sw.WriteLine($"#{LastNum + Counter + 15}=IFCRELCONTAINEDINSPATIALSTRUCTURE('3NBoCFC416zx3WXlQhsqTY',$,$,$,(#{LastNum + Counter + 14}),#{IFCBUILDINGSTOREY_El});");
				sw.WriteLine($"#{LastNum + Counter + 16}=IFCPROPERTYSINGLEVALUE('Reference',$,IFCLABEL(''),$);");
				sw.WriteLine($"#{LastNum + Counter + 17}=IFCPROPERTYSET('1Ov8Qgtf90ZwR0NhQwYDw1',$,'Pset_BuildingStoreyCommon',$,(#{LastNum + Counter + 16}));");
				sw.WriteLine($"#{LastNum + Counter + 18}=IFCRELDEFINESBYPROPERTIES('1M1KCMUeD0heKkSUlWoJbX',$,$,$,(#{IFCBUILDINGSTOREY_El}),#{LastNum + Counter + 17});");
				sw.WriteLine($"#{LastNum + Counter + 19}=IFCPRESENTATIONLAYERASSIGNMENT('A-M-BuildingElement',$,(#{LastNum + Counter + 12}),$);");
				sw.WriteLine($"#{LastNum + Counter + 20}=IFCQUANTITYVOLUME('NetVolume',$,$,0.,$);");
				sw.WriteLine($"#{LastNum + Counter + 21}=IFCELEMENTQUANTITY('1w6MGiW2X3Vge0KRfBDZre',$,'Qto_BuildingElementProxyQuantities',$,$,(#{LastNum + Counter + 20}));");
				sw.WriteLine($"#{LastNum + Counter + 22}=IFCRELDEFINESBYPROPERTIES('0HrteIjzj7w9P6WKPlcCI8',$,$,$,(#{LastNum + Counter + 14}),#{LastNum + Counter + 21});");
				//sw.WriteLine($"#{LastNum + Counter + 23}=IFCCARTESIANPOINT((0.,0.,0.));");
				//sw.WriteLine($"#{LastNum + Counter + 24}=IFCDIRECTION((1.,0.,0.));");

			} 
			RepairEndOdFile();


		}
	}
}
