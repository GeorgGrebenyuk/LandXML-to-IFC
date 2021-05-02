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
			RB_3.IsEnabled = false;
			TB_Point1.IsEnabled = false;
			TB_Point2.IsEnabled = false;
			TB_Point3.IsEnabled = false;
		}
		public static string PathToLandXMLFile = null;
		public static string PathToIFCSaving = null;

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
			//if (RB_1.IsChecked == false && RB_2.IsChecked == false && RB_1.IsChecked == false) MessageBox.Show("Не выбрана опция обработки файла");
			//if (!File.Exists(PathToLandXMLFile)) { MessageBox.Show("Файл LandXML не был выбран или путь недействительный"); PathToLandXMLFile = null; }
			PathToLandXMLFile = @"D:\Programming\GitRepo\LandXML-to-IFC\02_Resources\K13-395_Surface.xml";
			if (RB_1.IsChecked == true) Actions.ConvertOpeation(PathToLandXMLFile, PathToIFCSaving, new double[3] { 0d, 0d, 0d });
			else if (RB_2.IsChecked == true) Actions.CheckFileLocation(PathToLandXMLFile);
			ConsoleApp.Text = "End!";
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

		private void Button_Click_2(object sender, RoutedEventArgs e) //Сохранение файла IFC
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			if (saveFileDialog.ShowDialog() == true) PathToIFCSaving = saveFileDialog.FileName;
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
	}
}
