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
	public class FSMonitor
	{
		/************************************************
		 * PROPERTIES
		 ************************************************/	
		public FileSystemWatcher __fswatcher = new FileSystemWatcher ();
		public XmlNode __confignode;
		public fsmon.Fsmon __fsmon;
		public ArrayList __raisedFiles = new ArrayList();

		/************************************************
		 * ACCESSORS
		 ************************************************/
		public FileSystemWatcher fswatcher
		{
			get{ return __fswatcher; }
			set{ __fswatcher = value; }
		}

		public XmlNode confignode
		{
			get{ return __confignode; }
			set{ __confignode = value; }
		}

		public fsmon.Fsmon fsmon
		{
			get{ return __fsmon; }
			set{ __fsmon = value; }
		}

		public ArrayList raisedFiles
		{
			get{ return __raisedFiles; }
			set{ __raisedFiles = value;}
		}

		/************************************************
		 * METHODS
		 ************************************************/	
		public FSMonitor (fsmon.Fsmon FSMON, XmlNode CONFIGNODE)
		{
			//set class properties
			this.fsmon = FSMON;
			this.confignode = CONFIGNODE;

			fsmon.Log("Configuring Watch Rule: '"+CONFIGNODE.Attributes["name"].Value+"'");

			//set watch path
			try
			{
				fswatcher.Path = CONFIGNODE.Attributes["watchdir"].Value;
			}
			catch(Exception e) 
			{
				fsmon.Log("Error Parsing Watch Rule 'watchdir' Attribute: "+string.Format ("{0}", e));
				throw;
			}

			//set notify filters
			fswatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

			//Set FS Callbacks
			fswatcher.Created += new FileSystemEventHandler(OnFSCreatedCallback);
			fswatcher.Deleted += new FileSystemEventHandler(OnFSDeletedCallback);
			fswatcher.Changed += new FileSystemEventHandler(OnFSChangedCallback);
			fswatcher.Renamed += new RenamedEventHandler(OnFSRenamedCallback);

			//enable event raise
			bool enableraisingevents = false;
			try
			{
				enableraisingevents = Convert.ToBoolean(CONFIGNODE.Attributes["enabled"].Value);
			}
			catch(Exception e) 
			{
				fsmon.Log("Error Parsing Watch Rule 'enabled' Attribute: "+string.Format ("{0}", e));
				throw;
			}
			fswatcher.EnableRaisingEvents = enableraisingevents;

			//include subdirectories or not
			bool includesubdirectories = false;
			try
			{  
				includesubdirectories = Convert.ToBoolean(CONFIGNODE.Attributes["includesubdir"].Value); 
			}
			catch(Exception e)
			{
				fsmon.Log("Error Parsing Watch Rule 'includesubdir' Attribute: "+string.Format ("{0}", e));
				throw;
			}
			fswatcher.IncludeSubdirectories = includesubdirectories;

			//set fs filter
			fswatcher.Filter = CONFIGNODE.Attributes["filter"].Value;
		}

		public void OnFSCreatedCallback(object source, FileSystemEventArgs e)
		{
			//create fileinfo
			FileInfo fileinfo = new FileInfo (e.FullPath);

			//here we ensure that the filesystem event is not within our application directory
			if (fileinfo.Directory.FullName != this.fsmon.appdir) 
			{
				if (fileinfo.Exists) 
				{
					fsmon.Debug("File "+e.ChangeType.ToString()+": " + e.FullPath);
					this.ProcessWatchItems (fileinfo, e.ChangeType);
				} 
				else 
				{
					fsmon.Debug ("File Does Not Exist: " + fileinfo.FullName);
				}
			}
		}

		public void OnFSChangedCallback(object source, FileSystemEventArgs e)
		{
			Console.WriteLine (e.ChangeType);

			if (!this.raisedFiles.Contains (e.FullPath)) {
				this.raisedFiles.Add (e.FullPath);
			}

			//create fileinfo
			FileInfo fileinfo = new FileInfo (e.FullPath);

			//here we ensure that the filesystem event is not within our application directory
			if (fileinfo.Directory.FullName != this.fsmon.appdir) 
			{
				if (fileinfo.Exists) 
				{
					fsmon.Debug("File "+e.ChangeType.ToString()+": " + e.FullPath);
					this.ProcessWatchItems (fileinfo, e.ChangeType);
				} 
				else 
				{
					fsmon.Debug ("File Does Not Exist: " + fileinfo.FullName);
				}
			}
		}

		public void OnFSRenamedCallback(object source, RenamedEventArgs e)
		{
			Console.WriteLine (e.ChangeType);

			//create fileinfo
			FileInfo fileinfo = new FileInfo (e.FullPath);

			//here we ensure that the filesystem event is not within our application directory
			if (fileinfo.Directory.FullName != this.fsmon.appdir) 
			{
				if (fileinfo.Exists) 
				{
					fsmon.Debug("File "+e.ChangeType.ToString()+": " + e.FullPath);
					this.ProcessWatchItems (fileinfo, e.ChangeType);
				} 
				else 
				{
					fsmon.Debug ("File Does Not Exist: " + fileinfo.FullName);
				}
			}
		}

		public void OnFSDeletedCallback(object source, FileSystemEventArgs e)
		{
			Console.WriteLine (e.ChangeType);

			//create fileinfo
			FileInfo fileinfo = new FileInfo (e.FullPath);

			//here we ensure that the filesystem event is not within our application directory
			if (fileinfo.Directory.FullName != this.fsmon.appdir) 
			{
				fsmon.Debug("File "+e.ChangeType.ToString()+": " + e.FullPath);
				this.ProcessWatchItems (fileinfo, e.ChangeType);
			}
		}

		public void ProcessWatchItems(FileInfo FILEINFO, WatcherChangeTypes CHANGETYPE)
		{
			//get our list of watch items for this watchrule
			XmlNodeList watchitems = confignode.SelectNodes ("watchitem");

			//loop through watchitems so we can process the match and action rulesets
			foreach(XmlNode watchitem in watchitems)
			{
				//first we need to make sure that the change type coming thorugh is allowed
				//per the watchtiem oncreate|ondelete|onchange|onrename attributes
				bool oncreateattr = Convert.ToBoolean(watchitem.Attributes ["oncreate"].Value);
				bool ondeleteattr = Convert.ToBoolean(watchitem.Attributes ["ondelete"].Value);
				bool onchangedattr = Convert.ToBoolean(watchitem.Attributes ["onchanged"].Value);
				bool onrenameattr = Convert.ToBoolean(watchitem.Attributes ["onrename"].Value);

				if (CHANGETYPE == WatcherChangeTypes.Changed) {
					if (onchangedattr != true)
						break;
				} else if (CHANGETYPE == WatcherChangeTypes.Created) {
					if (oncreateattr != true)
						break;
				} else if (CHANGETYPE == WatcherChangeTypes.Deleted) {
					if (ondeleteattr != true)
						break;
				} else if (CHANGETYPE == WatcherChangeTypes.Renamed) {
					if (onrenameattr != true)
						break;
				}

				//select all of the <matchcondition> tags
				XmlNodeList matchconditions = watchitem.SelectNodes ("matchcondition");
				bool allconditionsmatched = true;

				//loop through match conditions and match all
				foreach (XmlNode matchcondition in matchconditions) 
				{
					bool result = MatchCondition (FILEINFO, matchcondition);
					if (!result) { allconditionsmatched = result; }
				}

				if (allconditionsmatched) 
				{
					fsmon.Log ("Matched Conditions for watchitem '"+watchitem.Attributes["name"].Value+"' on '"+FILEINFO.FullName+"'. Invoking Actions");

					//get a list of actions and invoke
					XmlNodeList actions = watchitem.SelectNodes ("action");
					foreach (XmlNode action in actions) 
					{
						string actiontype = action.Attributes ["type"].Value;
						if (actiontype == "delete") {
							this.ActionDeleteFile (FILEINFO);
						}
					}
				}
			}
		}

		public bool MatchCondition(FileInfo FILEINFO, XmlNode MATCHCONDITION)
		{
			string context = MATCHCONDITION.Attributes ["context"].Value;
			string type = MATCHCONDITION.Attributes ["type"].Value;
			string content = MATCHCONDITION.InnerText.Trim ();

			if (context == "filename") 
			{
				if (type == "regex") 
				{
					try
					{
						Regex regexexp = new Regex(string.Format (@"{0}", content));
						if (regexexp.IsMatch (FILEINFO.Name)) 
						{
							return true;
						} 
						else 
						{
							return false;
						}
					}
					catch(Exception e) 
					{
						fsmon.Log ("Regex Error in watchitem '"+MATCHCONDITION.ParentNode.Attributes["name"].Value+"'");
						fsmon.Log ("ERROR: " + string.Format ("{0}", e.Message));
						return false;
					}
					
				} 
				else if (type == "string") 
				{
					if (FILEINFO.Name == content) 
					{
						return true;
					} 
					else 
					{
						return false;
					}
				}
			} 
			else if (context == "filehash") 
			{
				if (type == "regex") 
				{

				} 
				else if (type == "string") 
				{

				}
			} 
			else if (context == "filecontext") 
			{
				if (type == "regex") 
				{

				} 
				else if (type == "string") 
				{

				}
			} 
			else 
			{
				fsmon.Log ("Match Context Failure. Context Must Be Of Value 'filename|filehash|filecontent'");
				return false;
			}
			return false;
		}

		public void ActionMoveFile()
		{

		}

		public void ActionCopyFile()
		{

		}

		public void ActionDeleteFile(FileInfo FILEINFO)
		{
			if (FILEINFO.Exists) 
			{
				File.Delete (FILEINFO.FullName);
				this.fsmon.Log ("File Deleted: '"+FILEINFO.FullName+"'");
			}
		}

		public void ActionQuarantineFile()
		{

		}

		public void ActionReplaceFile()
		{

		}

		public void ActionNotifyFile()
		{

		}

		public void ActionCommandFile()
		{

		}

		public void ActionLogFile()
		{

		}
	}
}

