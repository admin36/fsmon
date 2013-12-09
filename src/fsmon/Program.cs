using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace fsmon
{
	class MainClass
	{
		public static int Main (string[] args)
		{
			try
			{
				//new app object
				Fsmon fsm = new Fsmon(args);

				//continue to run until fsm.run is false
				while (fsm.run) 
				{ 
					//do nothing here. Console must stay running for fswatchers to work 
					//Console.ReadLine() provides a CPU usage freindly way to wait without eating up cpu cycles.
					Console.ReadLine();
				}
			}
			catch(Exception globalexception)
			{
				//as an austerity measure, lets catch any unhandled exceptions and dump them to the console.
				//future: Submit a bug report automatically, and try to dump application data if available
				//to a dump file somewhere near the binary and/or temp/log locations
				Console.WriteLine("GLOBAL FATAL UNHANDLED EXCEPTION. THE APPLICATION HAS CRASHED");
				Console.WriteLine("Oh Snap! This is embarrising, please submit a bug report.");
				Console.WriteLine("Its not safe to debug alone, take this:");
				Console.WriteLine(" ");
				Console.WriteLine("===EXCEPTION===");
				Console.WriteLine(globalexception);
			}

			return 1;
		}
	}
}