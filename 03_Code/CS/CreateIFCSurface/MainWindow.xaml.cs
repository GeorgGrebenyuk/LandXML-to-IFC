using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace CreateIFCSurface
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			RB_1.IsEnabled = false;

			////Сугубо для отладки/тестирования
			//C_Point1.Text = "2216582.1221,530008.5171,136";
			//C_Point2.Text = "2216565.5541,530052.8739,136";
			//C_Point3.Text = "2216547.802,530046.2432,136";
			//F_Point1.Text = "-8.282,2.125,0";
			//F_Point2.Text = "39.068,2.125,0";
			//F_Point3.Text = "39.068,21.075,0";
		}
		public static string PathToLandXMLFile = null;

		public static StringBuilder Log = new StringBuilder();

		private void Button_Click(object sender, RoutedEventArgs e) //Выбор файла LandXML
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			if (openFileDialog.ShowDialog() == true) PathToLandXMLFile = openFileDialog.FileName;
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e) //Консоль приложения
		{

		}

		private void Button_Click_1(object sender, RoutedEventArgs e) //Кнопка запуска процедуры конвертации
		{


			//Проверка файловых путей и опции выбора параметров преобразования
			if (RB_1.IsChecked == false && RB_2.IsChecked == false && RB_1.IsChecked == false) MessageBox.Show("Не выбрана опция обработки файла");
			if (!File.Exists(PathToLandXMLFile)) { MessageBox.Show("Файл LandXML не был выбран или путь недействительный"); PathToLandXMLFile = null; }
			PathToLandXMLFile = @"D:\Programming\GitRepo\LandXML-to-IFC\02_Resources\L15_500_Surface.xml";
			if (RB_1.IsChecked == true) Actions.ConvertOpeation(PathToLandXMLFile, new double[4] { 0d, 0d, 0d, 0d },false);
			else if (RB_2.IsChecked == true) Actions.CheckFileLocation(PathToLandXMLFile);
			else if (RB_3.IsChecked == true)
			{


				string[] Data = new string[6];
				Data[0] = C_Point1.Text;
				Data[1] = C_Point2.Text;
				Data[2] = C_Point3.Text;
				Data[3] = F_Point1.Text;
				Data[4] = F_Point2.Text;
				Data[5] = F_Point3.Text;

				Actions.FindParameters(Data);
			}
			Log.Append("End!");
			ConsoleApp.Text = Log.ToString();
		}

		private void TB_XField_TextChanged(object sender, TextChangedEventArgs e) //Поле координаты X
		{

		}

		private void TB_YField_TextChanged(object sender, TextChangedEventArgs e) //Поле координаты Y
		{

		}

		private void TB_ZField_TextChanged(object sender, TextChangedEventArgs e) //Поле координаты Z
		{

		}


		private void RadioButton_Checked(object sender, RoutedEventArgs e) //Как есть
		{

		}

		private void RadioButton_Checked_1(object sender, RoutedEventArgs e) //Трансфомация координат
		{

		}

		private void RadioButton_Checked_2(object sender, RoutedEventArgs e) //Задавать сдвижку по рассчитанным значениям
		{

		}

		private void F_Point1_TextChanged(object sender, TextChangedEventArgs e) //F_Point1
		{

		}

		private void F_Point2_TextChanged(object sender, TextChangedEventArgs e) //F_Point2
		{

		}

		private void F_Point3_TextChanged(object sender, TextChangedEventArgs e) //F_Point3
		{

		}
	}
}
