using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Timers;

namespace fsmon
{
	public class FswatcherChangeTimeout
	{
		/************************************************
		* PROPERTIES
		************************************************/
		private Fswatcher __caller;
		private FSEventType __type;
		private FileSystemEventArgs __fseventargs;
		private Timer __timer = new Timer();

		/************************************************
		* ACCESSORS
		************************************************/	
		public string path
		{
			get{ return __fseventargs.FullPath; }
		}

		/************************************************
		* CONSTRUCTORS
		************************************************/
		public FswatcherChangeTimeout(Fswatcher CALLER, FileSystemEventArgs ARGS, FSEventType TYPE, int TIMEOUT)
		{
			__caller = CALLER;
			__type = TYPE;
			__fseventargs = ARGS;
			__timer.AutoReset = false;
			__timer.Interval = TIMEOUT;
			__timer.Elapsed += new ElapsedEventHandler(onTimerElapsed);
		}

		/************************************************
		* METHODS
		************************************************/	
		public void onTimerElapsed(Object source, ElapsedEventArgs e)
		{
			__caller.onFileSystemChangeTimeoutElapsed(this, __fseventargs, __type);
		}

		public void ResetTimer()
		{
			__timer.Stop();
			__timer.Start();
		}
	}
}

