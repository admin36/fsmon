using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Threading;

namespace fsmon
{
	public class FswatchRule
	{
		/************************************************
		* PROPERTIES
		************************************************/
		private string __name = "DefaultWatchRuleName";
		private string __path;
		private string __filter = "*.*";
		private bool __enabled = false;
		private bool __recurse = false;
		private bool __oncreate = true;
		private bool __onchange = true;
		private bool __ondelete = true;
		private bool __onrename = true;
		private bool __onerror = true;
		private int __timeout = 250;
		private List<FswatchRuleChain> __rulechains = new List<FswatchRuleChain>();
		private Fswatcher __watcher;


		/************************************************
		* ACCESSORS
		************************************************/	
		public string name
		{
			get{ return __name; }
			set{ __name = value; }
		}

		public string path
		{
			get{ return __path; }
			set{ __path = value; }
		}

		public string filter
		{
			get{ return __filter; }
			set{ __filter = value; }
		}

		public bool enabled
		{
			get{ return __enabled; }
			set{ __enabled = value; }
		}

		public bool recurse
		{
			get{ return __recurse; }
			set{ __recurse = value; }
		}

		public bool oncreate
		{
			get{ return __oncreate; }
			set{ __oncreate = value; }
		}

		public bool onchange
		{
			get{ return __onchange; }
			set{ __onchange = value; }
		}

		public bool ondelete
		{
			get{ return __ondelete; }
			set{ __ondelete = value; }
		}

		public bool onrename
		{
			get{ return __onrename; }
			set{ __onrename = value; }
		}

		public bool onerror
		{
			get{ return __onerror; }
			set{ __onerror = value; }
		}

		public int timeout
		{
			get{ return __timeout; }
			set{ __timeout = value; }
		}

		public List<FswatchRuleChain> rulechains
		{
			get{ return __rulechains; }
		}

		/************************************************
		* CONSTRUCTORS
		************************************************/	
		public FswatchRule()
		{
			//do nothing
		}

		public FswatchRule(string NAME, string PATH, string FILTER, bool RECURSE, bool ENABLED, int TIMEOUT)
		{
			//set vars
			this.__name = NAME;
			this.__path = PATH;
			this.__filter = FILTER;
			this.__recurse = RECURSE;
			this.__enabled = ENABLED;
			this.__timeout = TIMEOUT;

			//create our filesystem watcher
			this.__watcher = new Fswatcher(this.__path, this.__filter, this.__recurse, this.__enabled, this.__timeout);

			//register its callbacks for events
			this.__watcher.RegisterFSCreatedCallback(onFileSystemEvent);
			this.__watcher.RegisterFSChangedCallback(onFileSystemEvent);
			this.__watcher.RegisterFSDeletedCallback(onFileSystemEvent);
			this.__watcher.RegisterFSRenamedCallback(onFileSystemRenamedEvent);
			this.__watcher.RegisterFSErrorCallback(onFileSystemErrorEvent);
		}

		/************************************************
		* METHODS
		************************************************/	
		public bool Start()
		{
			//ensure the __path exists before trying tostart
			if(!Directory.Exists(this.__path))
			{
				Console.WriteLine("WatchRule {0} Will Not Be Started: {1} does not exist.", this.__name, this.__path);
				return false;
			}

			//recreate our filesystem watcher
			this.__watcher = new Fswatcher(this.__path, this.__filter, this.__recurse, this.__enabled, this.__timeout);

			//register its callbacks for events
			this.__watcher.RegisterFSCreatedCallback(onFileSystemEvent);
			this.__watcher.RegisterFSChangedCallback(onFileSystemEvent);
			this.__watcher.RegisterFSDeletedCallback(onFileSystemEvent);
			this.__watcher.RegisterFSRenamedCallback(onFileSystemRenamedEvent);
			this.__watcher.RegisterFSErrorCallback(onFileSystemErrorEvent);

			this.__watcher.Start();

			Console.WriteLine("{0} Watch Rule Chain Count: {1}", this.__name, this.__rulechains.Count);

			return true;
		}

		public void Stop()
		{
			//stop the watcher if it exists
			if(this.__watcher != null)
				this.__watcher.Stop();

			//null out watcher for GC
			this.__watcher = null;
		}

		public void Restart()
		{
			//restart the watcher
			this.Stop();
			this.Start();
		}

		private void onFileSystemEvent(FileSystemEventArgs e, FSEventType type)
		{
			Console.WriteLine(@"{0} {1} ""{2}""", e.ChangeType, type, e.FullPath); 

			for(int a=0;a<=this.__rulechains.Count-1;a++)				
			{
				bool result = this.__rulechains[a].ProcessMatchItems(e, type);
				if(result)
					this.__rulechains[a].ProcessActionItems(e, type);
			}
		}

		private void onFileSystemRenamedEvent(RenamedEventArgs e, FSEventType type)
		{
			Console.WriteLine(@"{0} {1} ""{2}"" ""{3}""", e.ChangeType, type, e.OldFullPath, e.FullPath); 	

			for(int a=0;a<=this.__rulechains.Count-1;a++)				
			{
				bool result = this.__rulechains[a].ProcessMatchItems(e, type);
				if(result)
					this.__rulechains[a].ProcessActionItems(e, type);
			}
		}

		private void onFileSystemErrorEvent(ErrorEventArgs e)
		{
			Console.WriteLine(@"{0} {1} ""{3}""", "ERROR", e.GetException().Message );
		}
	}
}

