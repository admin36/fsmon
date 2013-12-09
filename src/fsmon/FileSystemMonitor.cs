using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace fsmon
{
	public class FSMonitor
	{
		public FSMonitor(fsmon.core FSMCORE, XmlNode CONFIGNODE)
		{
			//set config property
			configuration = CONFIG;

			//fsmon.MainClass.Log("Generating FileSystemMonitor Instance");
		}

		public void start()
		{

		}

		public void stop()
		{

		}

		public void restart()
		{

		}

		public void onFSChanged(object source, FileSystemEventArgs e)
		{

		}
	}
}

