using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp10
{
	class Program
	{
		static void Main(string[] args)
		{
			Actions2.CreateBasicIfc_File();
			Actions2.AddPointsToIfc();
			Actions2.AddFacesToIfc();
			Actions2.AddStyleToSurface();
			Actions2.AddSurfaceToIfcSite();

			Console.WriteLine("End");
			//Console.ReadKey();
		}
	}
}
