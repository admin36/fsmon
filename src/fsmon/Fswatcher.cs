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
	/************************************************
	* ENUM
	************************************************/	
	public enum FSEventType
	{
		File,
		Directory,
		Unknown
	}

	/************************************************
	* DELEGATES
	************************************************/	
	public delegate void FSEventCallback(FileSystemEventArgs e, FSEventType type);
	public delegate void FSRenamedCallback(RenamedEventArgs e, FSEventType type);
	public delegate void FSErrorCallback(ErrorEventArgs e);

	public class Fswatcher
	{
		/************************************************
		* PROPERTIES
		************************************************/
		private int							__changetimeout			= 250;
		private FileSystemWatcher 			__filewatcher 			= new FileSystemWatcher();
		//private FileSystemWatcher 			__dirwatcher 			= new FileSystemWatcher();
		private FSEventCallback 			__oncreatedcallbacks;
		private FSEventCallback 			__onchangedcallbacks;
		private FSEventCallback 			__ondeletedcallbacks;
		private FSRenamedCallback 			__onrenamedcallbacks;
		private FSErrorCallback 			__onerrorcallbacks;
		private List<FswatcherChangeTimeout> __fswatcherchangetimeouts = new List<FswatcherChangeTimeout>();

		/************************************************
		* CONSTRUCTORS
		************************************************/
		public Fswatcher(string WATCHDIR, string FILTER="*.*", bool RECURSE=false, bool ENABLED=false, int CHANGETIMEOUT=250)
		{
			__filewatcher.Path = WATCHDIR;
			__filewatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.Attributes;
			__filewatcher.EnableRaisingEvents = ENABLED;
			__filewatcher.IncludeSubdirectories = RECURSE;
			__filewatcher.Filter = FILTER;
			__filewatcher.Created += new FileSystemEventHandler(onFileSystemEvent);
			__filewatcher.Changed += new FileSystemEventHandler(onFileSystemEvent);
			__filewatcher.Deleted += new FileSystemEventHandler(onFileSystemEvent);
			__filewatcher.Renamed += new RenamedEventHandler(onFileSystemEvent);
			__filewatcher.Error += new ErrorEventHandler(onFileSystemEvent);

			/*__dirwatcher.Path = WATCHDIR;
			__dirwatcher.NotifyFilter = ;
			__dirwatcher.EnableRaisingEvents = ENABLED;
			__dirwatcher.IncludeSubdirectories = RECURSE;
			__dirwatcher.Filter = FILTER;
			__dirwatcher.Created += new FileSystemEventHandler(onFileSystemEvent);
			__dirwatcher.Changed += new FileSystemEventHandler(onFileSystemEvent);
			__dirwatcher.Deleted += new FileSystemEventHandler(onFileSystemEvent);
			__dirwatcher.Renamed += new RenamedEventHandler(onFileSystemEvent);
			__dirwatcher.Error += new ErrorEventHandler(onFileSystemEvent);*/

			__changetimeout = CHANGETIMEOUT;
		}

		/************************************************
		* METHODS
		************************************************/	
		private void onFileSystemEvent(object source, FileSystemEventArgs e)
		{
			//get the type
			FSEventType type = this.GetFSEventType(e);

			//call callback handlers
			if(e.ChangeType == WatcherChangeTypes.Created )
			{
				try{ __oncreatedcallbacks(e, type); } catch(Exception exception){ Console.WriteLine(exception.Message); }
			}
			else if(e.ChangeType == WatcherChangeTypes.Changed )
			{
				if(__changetimeout > 0)
				{
					//try to find a timeout watcher, reset and return if found
					foreach(FswatcherChangeTimeout timeout in __fswatcherchangetimeouts)
					{
						if(timeout.path == e.FullPath)
						{				
							timeout.ResetTimer();
							return;
						}
					}

					//otherwise just create a new timeout watcher
					FswatcherChangeTimeout newTimeout = new FswatcherChangeTimeout(this, e, type, __changetimeout);
					__fswatcherchangetimeouts.Add(newTimeout);
				}
				else
				{
					try{ __onchangedcallbacks(e, type); } catch(Exception exception){ Console.WriteLine(exception.Message); }
				}

			}
			else if(e.ChangeType == WatcherChangeTypes.Deleted )
			{
				try{ __ondeletedcallbacks(e, type); } catch(Exception exception){ Console.WriteLine(exception.Message); }
			}
		}

		private void onFileSystemEvent(object source, RenamedEventArgs e)
		{
			//get the type
			FSEventType type = this.GetFSEventType(e);

			try{ __onrenamedcallbacks(e, type); } catch(Exception exception){ Console.WriteLine(exception.Message); }
		}

		private void onFileSystemEvent(object source, ErrorEventArgs e)
		{
			try{ __onerrorcallbacks(e); } catch(Exception exception){ Console.WriteLine(exception.Message); }
		}

		private FSEventType GetFSEventType(FileSystemEventArgs e)
		{
			try
			{
				// get the file attributes for file or directory
				FileAttributes attr = File.GetAttributes(e.FullPath);

				//detect whether its a directory or file
				if((attr & FileAttributes.Directory) == FileAttributes.Directory)
					return FSEventType.Directory;
				else
					return FSEventType.File;
			}
			catch(Exception exception)
			{
				//likely the file does not exist, or was deleted just before attributes could be pulled
				return FSEventType.Unknown;
			}
		}

		private FSEventType GetFSEventType(RenamedEventArgs e)
		{
			try
			{
				// get the file attributes for file or directory
				FileAttributes attr = File.GetAttributes(e.FullPath);

				//detect whether its a directory or file
				if((attr & FileAttributes.Directory) == FileAttributes.Directory)
					return FSEventType.Directory;
				else
					return FSEventType.File;
			}
			catch(Exception exception)
			{
				//likely the file does not exist, or was deleted just before attributes could be pulled
				return FSEventType.Unknown;
			}
		}

		public void Start()
		{
			__filewatcher.EnableRaisingEvents = true;
			//__dirwatcher.EnableRaisingEvents = true;
		}

		public void Stop()
		{
			__filewatcher.EnableRaisingEvents = false;
			//__dirwatcher.EnableRaisingEvents = false;
		}

		public void Restart()
		{
			this.Stop();
			this.Start();
		}

		public void RegisterFSErrorCallback(FSErrorCallback CALLBACK)
		{
			__onerrorcallbacks += CALLBACK;
		}

		public void RegisterFSRenamedCallback(FSRenamedCallback CALLBACK)
		{
			__onrenamedcallbacks += CALLBACK;
		}

		public void RegisterFSCreatedCallback(FSEventCallback CALLBACK)
		{
			__oncreatedcallbacks += CALLBACK;
		}

		public void RegisterFSChangedCallback(FSEventCallback CALLBACK)
		{
			__onchangedcallbacks += CALLBACK;
		}

		public void RegisterFSDeletedCallback(FSEventCallback CALLBACK)
		{
			__ondeletedcallbacks += CALLBACK;
		}

		public void onFileSystemChangeTimeoutElapsed(FswatcherChangeTimeout timeoutwatcher, FileSystemEventArgs e, FSEventType type)
		{
			//call the callback
			try{ __onchangedcallbacks(e, type); } catch(Exception exception){ Console.WriteLine(exception.Message); }

			//remove the change timeout watcher from the list
			__fswatcherchangetimeouts.Remove(timeoutwatcher);
		}
	}
}

