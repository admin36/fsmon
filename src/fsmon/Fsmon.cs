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
	public class Fsmon
	{
		/************************************************
		* ENUM
		************************************************/	
		public enum Platform
		{
			Windows,
			Linux,
			Mac
		}

		/************************************************
		* PROPERTIES
		************************************************/	
		public string[] 			__args;			//set in class.ParseArgs()
		public bool 				__run 			= true;
		public bool					__debug			= true;
		public bool 				__debuglog		= true;
		private string 				__quarantinedir = "";
		private string				__logdir 		= "";
		private string				__backupdir 	= "";
		private string				__smtpserver 	= "";
		private string				__smtpusername 	= "";
		private string				__smtppassword 	= "";
		private List<FswatchRule>	__fswatchrules 	= new List<FswatchRule>();


		/************************************************
		* ACCESSORS
		************************************************/	
		public string[] args
		{
			get{ return __args; }
			set{ __args = value; }
		}

		public Platform platform
		{
			get
			{ 
				switch (Environment.OSVersion.Platform)
				{
					case PlatformID.Unix:
						// Well, there are chances MacOSX is reported as Unix instead of MacOSX.
						// Instead of platform check, we'll do a feature checks (Mac specific root folders)
						if (Directory.Exists("/Applications")
						    & Directory.Exists("/System")
						    & Directory.Exists("/Users")
						    & Directory.Exists("/Volumes"))
							return Platform.Mac;
						else
							return Platform.Linux;
					case PlatformID.MacOSX:
						return Platform.Mac;
					default:
						return Platform.Windows;
				}
			}
		}

		public string appdir
		{
			get{ return System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location ); }
		}

		public bool run
		{
			get{ return __run; }
			set{ __run = value; }
		}

		public bool debug
		{
			get{ return __debug;  }
			set{  __debug = value; }
		}

		public bool debuglog
		{
			get{ return __debuglog;  }
			set{  __debuglog = value; }
		}

		/************************************************
		* CONSTRUCTORS
		************************************************/	
		public Fsmon(string[] ARGS)
		{
			//setup some defaults
			this.__quarantinedir 	= this.appdir+Path.DirectorySeparatorChar+"quarantine";
			this.__logdir 			= this.appdir+Path.DirectorySeparatorChar+"logs";
			this.__backupdir 		= this.appdir+Path.DirectorySeparatorChar+"backups";

			//set args
			this.args = ARGS;

			//parse args
			this.ParseArgs();
		}

		/************************************************
		* METHODS
		************************************************/	
		public void ParseArgs()
		{
			bool flag_help 			= false;
			bool flag_interactive 	= false;
			bool flag_service 		= false;
			bool flag_recurse 		= false;
			bool flag_onchange 		= false;
			bool flag_oncreate 		= false;
			bool flag_ondelete 		= false;
			bool flag_onrename 		= false;
			bool flag_onerror 		= false;
			string flag_path 		= Directory.GetCurrentDirectory();
			string flag_config 		= "";
			int flag_changetimeout 	= 250;

			//if no args, print usage and stop
			if (this.args.Length == 0) 
			{
				flag_help = true;
			} 
			else
			{
				for (int a=0; a<=this.args.Length-1; a++) 
				{
					string arg = this.args[a];

					if (arg == "-h" || arg == "--help") 
					{
						flag_help = true;
					} 
					else if (arg == "-i" || arg == "--interactive") 
					{
						flag_interactive = true;
					} 
					else if (arg == "-s" || arg == "--service") 
					{
						flag_service = true;
					} 
					else if(arg == "-c" || arg == "--config")
					{
						flag_config = this.args[a+1];
					}
					else if (arg == "-r" || arg == "--recurse") 
					{
						flag_recurse = true;
					} 
					else if (arg == "-p" || arg == "--path") 
					{
						flag_path = this.args[a+1];
					}
					else if(arg == "-ocr" || arg == "--oncreate")
					{
						flag_oncreate = true;
					}
					else if(arg == "-och" || arg == "--onchange")
					{
						flag_onchange = true;
					}
					else if(arg == "-ode" || arg == "--ondelete")
					{
						flag_ondelete = true;
					}
					else if(arg == "-ore" || arg == "--onrename")
					{
						flag_onrename = true;
					}
					else if(arg == "-oer" || arg == "--onerror")
					{
						flag_onerror = true;
					}
					else if(arg == "-cto" || arg == "--changetimeout")
					{
						flag_changetimeout = Convert.ToInt32(this.args[a+1]);
					}
				}
			}

			//check to see if any flag event specified, if not enable all
			if(!flag_onchange && !flag_oncreate && !flag_ondelete && !flag_onerror && !flag_onrename )
			{
				flag_onchange = true;
				flag_oncreate = true;
				flag_ondelete = true;
				flag_onerror = true;
				flag_onrename = true;
			}

			//flag logic
			if(flag_help)
			{
				this.PrintUsage();
				this.run = false;
			}
			else if(flag_interactive && flag_service)
			{
				this.Halt(-1, "Invalid Arguments Supplied. Cannot Run As Interactive And Service.");
			}
			else if(flag_interactive)
			{
				if( Directory.Exists(flag_path) )
				{
					this.RunInteractive(flag_path, flag_recurse, flag_oncreate, flag_onchange, flag_ondelete, flag_onrename, flag_onerror, flag_changetimeout);
					return;
				}
				else
				{
					this.Halt(-1, "Invalid Arguments Supplied. Path Argument '"+flag_path.ToString()+"' Does Not Exist.");
				}

			}
			else if(flag_service)
			{
				if(flag_config != "" && File.Exists(flag_config) || flag_config == "" )
				{
					this.RunService(flag_config);
					return;
				}
				else
				{
					this.Halt(-1, "Invalid Arguments Supplied. Config Argument '"+flag_config.ToString()+"' Does Not Exist.");
				}
			}
			else
			{
				this.PrintUsage();
				this.run = false;
			}
		}

		public void ParseConfig(string CONFIGFILE)
		{
			//halt if bad config file
			if(!File.Exists(CONFIGFILE))
			{
				this.Halt(-1, "Supplied Configuration File Does Not Exist: "+CONFIGFILE.ToString());
			}

			//begin reading in configuration file
			//parsemode: 0  no parse mode
			//parsemode: 1  [config] section parse mode
			//parsemode: 2  [RULE_NAME] section parse mode
			int parsemode = 0;
			int counter = 0;
			string confline;
			StreamReader file = new System.IO.StreamReader(CONFIGFILE);
			//flag to start new rulechain
			bool startNewChainOnNextMatchItem = true;

			//loop through config file lines
			while((confline = file.ReadLine()) != null)
			{
				//determine parse mode
				if(confline.ToLower().Trim() == "[config]")
				{
					parsemode = 1;
				}
				else if( new Regex(@"\[[0-9a-zA-Z_]{1,}\]", RegexOptions.IgnoreCase).Match( confline.ToLower().Trim() ).Success )
				{
					parsemode = 2;
					string rulename = confline.Trim().TrimStart('[').TrimEnd(']');
					FswatchRule watchrule = new FswatchRule();
					watchrule.name = rulename;
					this.__fswatchrules.Add(watchrule);
				}

				if(parsemode == 1) //parse [config] section lines
				{
					//split config line based on space
					string[] lineParts = confline.Split(' ');

					if(lineParts[0].ToLower() == "config")
					{
						if(lineParts[1].ToLower() == "quarantinedir")
						{
							string value = lineParts[2].TrimStart('"').TrimEnd('"');
							if(value != "default")
								this.__quarantinedir = value;
						}
						if(lineParts[1].ToLower() == "loggingdir")
						{
							string value = lineParts[2].TrimStart('"').TrimEnd('"');
							if(value != "default")
								this.__logdir = value;
						}
						if(lineParts[1].ToLower() == "backupdir")
						{
							string value = lineParts[2].TrimStart('"').TrimEnd('"');
							if(value != "default")
								this.__backupdir = value;
						}
						if(lineParts[1].ToLower() == "smtpserver")
						{
							string value = lineParts[2].TrimStart('"').TrimEnd('"');
							this.__smtpserver = value;
						}
						if(lineParts[1].ToLower() == "smtpusername")
						{
							string value = lineParts[2].TrimStart('"').TrimEnd('"');
							this.__smtpusername = value;
						}
						if(lineParts[1].ToLower() == "smtppassword")
						{
							string value = lineParts[2].TrimStart('"').TrimEnd('"');
							this.__smtppassword = value;
						}
					}
				}
				else if(parsemode == 2) //parse [RULE_NAME] section lines
				{
					Console.WriteLine(confline);
					//get the last watchrule created
					FswatchRule watchrule = this.__fswatchrules[this.__fswatchrules.Count-1];

					//get target watchrule action/match chain
					FswatchRuleChain watchrulechain = new FswatchRuleChain();

					//split config line based on space
					string[] lineParts = confline.Split(' ');

					if(lineParts.Length == 3 && lineParts[0].ToLower() == "config") //RULE CONFIG
					{
						if(lineParts[1].ToLower() == "enabled")
						{
							bool value = Convert.ToBoolean( lineParts[2].TrimStart('"').TrimEnd('"').Trim() );
							watchrule.enabled = value;
						}
						else if(lineParts[1].ToLower() == "path")
						{
							string value = lineParts[2].TrimStart('"').TrimEnd('"').Trim();
							watchrule.path = value;
						}
						else if(lineParts[1].ToLower() == "recurse")
						{
							bool value = Convert.ToBoolean( lineParts[2].TrimStart('"').TrimEnd('"').Trim() );
							watchrule.recurse = value;
						}
						else if(lineParts[1].ToLower() == "filter")
						{
							string value = lineParts[2].TrimStart('"').TrimEnd('"').Trim();
							watchrule.filter = value;
						}
						else if(lineParts[1].ToLower() == "changetimeout")
						{
							int value = Convert.ToInt32(lineParts[2].TrimStart('"').TrimEnd('"').Trim());
							watchrule.timeout = value;
						}
						else if(lineParts[1].ToLower() == "oncreate")
						{
							bool value = Convert.ToBoolean( lineParts[2].Trim().TrimStart('"').TrimEnd('"') );
							watchrule.oncreate = value;
						}
						else if(lineParts[1].ToLower() == "onchange")
						{
							bool value = Convert.ToBoolean( lineParts[2].TrimStart('"').TrimEnd('"').Trim() );
							watchrule.onchange = value;
						}
						else if(lineParts[1].ToLower() == "ondelete")
						{
							bool value = Convert.ToBoolean( lineParts[2].TrimStart('"').TrimEnd('"').Trim() );
							watchrule.ondelete = value;
						}
						else if(lineParts[1].ToLower() == "onrename")
						{
							bool value = Convert.ToBoolean( lineParts[2].TrimStart('"').TrimEnd('"').Trim() );
							watchrule.onrename = value;
						}
						else if(lineParts[1].ToLower() == "onerror")
						{
							bool value = Convert.ToBoolean( lineParts[2].TrimStart('"').TrimEnd('"').Trim() );
							watchrule.onerror = value;
						}
					}
					else if(lineParts.Length >= 4 && lineParts[0].ToLower() == "match") //RULE MATCH ITEMS
					{
						if(startNewChainOnNextMatchItem)
						{
							watchrulechain = new FswatchRuleChain();
							watchrule.rulechains.Add(watchrulechain);
						}
						else 
						{
							watchrulechain = watchrule.rulechains[watchrule.rulechains.Count-1];
						}

						//match values
						string context = "";
						string type = "";
						string value = "";

						if(lineParts[1].ToLower() == "filename")
						{
							context = "filename";
							if(lineParts[2].ToLower() == "regex")							
								type = "regex";
							else if(lineParts[2].ToLower() == "string")							
								type = "regex";
						}
						else if(lineParts[1].ToLower() == "filehash")
						{
							context = "filehash";
							if(lineParts[2].ToLower() == "regex")							
								type = "regex";
							else if(lineParts[2].ToLower() == "string")							
								type = "regex";
						}
						else if (lineParts[1].ToLower() == "filecontent")
						{
							context = "filecontent";
							if(lineParts[2].ToLower() == "regex")							
								type = "regex";
							else if(lineParts[2].ToLower() == "string")							
								type = "regex";
						}

						//set value
						value = lineParts[3].Trim().TrimStart('"').TrimEnd('"');

						//if all match values satisfied, add to chain
						if(context != "" && type != "" && value != "")
						{
							FswatchRuleMatchItem matchitem = new FswatchRuleMatchItem(context, type, value);
							watchrulechain.matchitems.Add(matchitem);
						}

						startNewChainOnNextMatchItem = false;						
					}
					else if(lineParts.Length >= 2 && lineParts[0].ToLower() == "action") //RULE ACTION ITEMS
					{
						startNewChainOnNextMatchItem = true;

						if(lineParts[1].ToLower() == "move")
						{
							string value = lineParts[2].Trim().TrimStart('"').TrimEnd('"');
							FswatchRuleActionItemMove moveaction = new FswatchRuleActionItemMove(value);
							watchrulechain.actionitems.Add(moveaction);
						}
						else if(lineParts[1].ToLower() == "copy")
						{
							string value = lineParts[2].Trim().TrimStart('"').TrimEnd('"');
							FswatchRuleActionItemCopy copyaction = new FswatchRuleActionItemCopy(value);
							watchrulechain.actionitems.Add(copyaction);
						}
						else if(lineParts[1].ToLower() == "delete")
						{
							if(lineParts.Length == 2)
							{
								FswatchRuleActionItemDelete deleteaction = new FswatchRuleActionItemDelete();
								watchrulechain.actionitems.Add(deleteaction);
							}
							else if(lineParts.Length == 3 && lineParts[2].ToLower() == "takebackup")
							{
								FswatchRuleActionItemDelete deleteaction = new FswatchRuleActionItemDelete(true, this.__backupdir);
								watchrulechain.actionitems.Add(deleteaction);
							}
							else if(lineParts.Length == 4 && lineParts[2].ToLower() == "takebackup")
							{
								string directory = lineParts[3].Trim().TrimStart('"').TrimEnd('"');
								FswatchRuleActionItemDelete deleteaction = new FswatchRuleActionItemDelete(true, directory);
								watchrulechain.actionitems.Add(deleteaction);
							}
						}
						else if(lineParts[1].ToLower() == "quarantine")
						{

						}
						else if(lineParts[1].ToLower() == "replace")
						{

						}
						else if(lineParts[1].ToLower() == "email")
						{

						}
						else if(lineParts[1].ToLower() == "command")
						{

						}
						else if(lineParts[1].ToLower() == "log")
						{

						}
					}
				}

				//increment line counter
				counter++;
			}

			file.Close();
		}

		public void PrintUsage()
		{
			string usage = @"
Filesystem Monitor Utility
Usage: fsmon.exe [FLAGS]

Basic Usage:
    [noflag]         :Print Usage.
    -h --help        :Print Usage.
    -s --service     :Run program in service mode.
    -i --interactive :Run program in interactive mode.	    

Service Usage:
    -c --config      :load from configuration file.	

Interactive Usage:	
    -p --path             :Path to watch in -i mode.
    -r --recurse          :watch subdirectories in -i mode.
    -ocr --oncreate       :enable oncreate events in -i mode.
    -och --onchange       :enable onchange events in -i mode.
    -ode --ondelete       :enable ondelete events in -i mode.
    -ore --onrename       :enable onrename events in -i mode.
    -oer --onerror        :enable onerror events in -i mode.
    -cto --changetimeout  :timeout on change events in -i mode.

Examples:
    fsmon.exe -s	
    fsmon.exe -s -c c:\MyConfigFile.conf	
    fsmon.exe -i
    fsmon.exe -i -p C:\MyWatchPath -r 
    fsmon.exe -i -p C:\MyWatchPath -r -ode -ore	

Service Mode:
    Service mode runs the program without any stdout. Service
    mode will attempt to locate a confiuration file unless
    one is supplied by the -c flag

Interactive Mode:
    Interactive mode runs the program to stdout with basic 
    events only (no matching or actions). Interactive mode
    does not recurse by default, use -r for recursion. 
    Default is to use the current working directory for -p.
    All events are raised unless -ocr, -och, -ode, -ore are 
    specified.

Change Event Timeout:
    When a file is changed, the filesystem may raise multiple
    change events. This is not a bug but rather a behavior of
    batched buffer writes to the file by the operating system.
    The change timeout settings in interactive or service mode
    dictates how long to wait after a change event to actually
    raise the changed event. Default is to wait 250ms after a 
    change event before raising. If a change event is raised by 
    the operating system before the timeout limit, the timeout 
    timer is reset. This setting is also useful to slow down
    change events for files that get written to very often.
    Set to 0 to receive all change events as they are raised by
    the operating system.

";

			Console.Write (usage);
			this.run = false;
		}

		public void RunInteractive(string PATH="", bool RECURSE=false, bool ONCREATE=true, bool ONCHANGE=true, bool ONDELETE=true, bool ONRENAME=true, bool ONERROR=true, int CHANGETIMEOUT=250)
		{
			//create new filesystemwatcher wrapper object
			Fswatcher watcher = new Fswatcher(PATH, "*.*", RECURSE, false, CHANGETIMEOUT);

			//setup callback methods
			FSEventCallback fseventcallback = delegate(FileSystemEventArgs e, FSEventType type)
			{
				Console.WriteLine(@"{0} {1} ""{2}""", e.ChangeType, type, e.FullPath); 				
			};
			FSRenamedCallback fsrenamedcallback = delegate(RenamedEventArgs e, FSEventType type)
			{
				Console.WriteLine(@"{0} {1} ""{2}"" ""{3}""", e.ChangeType, type, e.OldFullPath, e.FullPath); 				
			};
			FSErrorCallback fserrorcallback = delegate(ErrorEventArgs e)
			{
				Console.WriteLine(@"{0} {1} ""{3}""", "ERROR", e.GetException().Message );
			};

			//setup callbacks
			if(ONCREATE)
			{
				watcher.RegisterFSCreatedCallback(fseventcallback);
			}
			if(ONCHANGE)
			{
				watcher.RegisterFSChangedCallback(fseventcallback);
			}
			if(ONDELETE)
			{
				watcher.RegisterFSDeletedCallback(fseventcallback);
			}
			if(ONRENAME)
			{
				watcher.RegisterFSRenamedCallback(fsrenamedcallback);
			}
			if(ONERROR)
			{
				watcher.RegisterFSErrorCallback(fserrorcallback);
			}

			//start the watcher
			watcher.Start();
		}

		public void RunService(string CONFIG="")
		{
			Console.WriteLine("Running Service Mode.");

			if(CONFIG != "" && File.Exists(CONFIG) )
			{
				Console.WriteLine("Loading Configuration: {0}", CONFIG);
				this.ParseConfig(CONFIG);
			}
			else
			{
				string locatedconfig = this.LocateConfigurationFile();
				if(locatedconfig != "")
				{
					Console.WriteLine("Loading Configuration: {0}", locatedconfig);
					this.ParseConfig(locatedconfig);
				}
				else
				{
					this.Halt(-1, "Unable to locate configuration file");
				}
			}


			Console.WriteLine(this.__quarantinedir);
			Console.WriteLine(this.__logdir);
			Console.WriteLine(this.__backupdir);

			foreach(FswatchRule watchrule in this.__fswatchrules)
			{
				Console.WriteLine("Starting Watchrule: "+watchrule.name);
				watchrule.Restart();
			}

			//this.run = false;
		}

		private string LocateConfigurationFile()
		{
			//try to locate a configuration file
			string binlocalconf = this.appdir+Path.DirectorySeparatorChar+"fsmon.conf";
			string binconfdir = this.appdir+Path.DirectorySeparatorChar+"conf"+Path.DirectorySeparatorChar+"fsmon.conf";
			if(File.Exists(binlocalconf))
			{
				return binlocalconf;
			}
			else if(File.Exists(binconfdir))
			{
				return binconfdir;
			}
			else
			{
				if(this.platform == Platform.Linux)
				{
					string etcconf = "/etc/fsmon.conf";
					if(File.Exists(etcconf))
					{
						return etcconf;
					}

				}
				else if(this.platform == Platform.Windows)
				{
					string sys32conf = "C:\\WINDOWS\\System32\\fsmon.conf";
					if(File.Exists(sys32conf))
					{
						return sys32conf;
					}
				}
			}

			//return nothing
			return "";
		}

		public void Log(string TEXT)
		{
			string date = System.DateTime.Now.ToString();

			if(this.debug){ Console.WriteLine(date + " " + TEXT); }

			StreamWriter w = File.AppendText (System.AppDomain.CurrentDomain.FriendlyName + ".log");
			w.WriteLine(date + "  " + TEXT);
			w.Close ();
		}

		public void Debug(string TEXT)
		{
			if (this.debug) 
			{
				string date = System.DateTime.Now.ToString();
				Console.WriteLine(date + " DEBUG: " + TEXT);
				if (this.debuglog) 
				{
					StreamWriter w = File.AppendText (System.AppDomain.CurrentDomain.FriendlyName + ".log");
					w.WriteLine(date + " DEBUG: " + TEXT);
					w.Close ();
				}
			}
		}

		public void Halt(int EXITCODE, string TEXT, bool log=false)
		{
			Console.WriteLine(TEXT);
			if(log)
			{
				Log("HALT: " + TEXT);
			}
			Environment.Exit(EXITCODE);
		}
	}
}

