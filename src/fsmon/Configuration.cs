using System;

namespace fsmon
{
	public class Configuration
	{
		public string property_configfilepath;

		public string configurationFile
		{
			get{ return property_configfilepath; }
			set{  property_configfilepath = value; }
		}

		public Configuration ()
		{
		}
	}
}

