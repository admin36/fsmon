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
	public interface FswatchRuleActionItem
	{
		void InvokeAction(FileSystemEventArgs e, FSEventType type);
		void InvokeAction(RenamedEventArgs e, FSEventType type);
	}

	public class FswatchRuleActionItemMove : FswatchRuleActionItem
	{
		private string __destination;

		public FswatchRuleActionItemMove(string DESTINATION)
		{
			this.__destination = DESTINATION;
		}

		public void InvokeAction(FileSystemEventArgs e, FSEventType type)
		{
			//invoke move action
		}

		public void InvokeAction(RenamedEventArgs e, FSEventType type)
		{
			//invoke move action
		}
	}

	public class FswatchRuleActionItemCopy : FswatchRuleActionItem
	{
		private string __destination;

		public FswatchRuleActionItemCopy(string DESTINATION)
		{
			this.__destination = DESTINATION;
		}

		public void InvokeAction(FileSystemEventArgs e, FSEventType type)
		{
			//invoke move action
		}

		public void InvokeAction(RenamedEventArgs e, FSEventType type)
		{
			//invoke move action
		}
	}

	public class FswatchRuleActionItemDelete : FswatchRuleActionItem
	{
		private bool __takebackup = false;
		private string __destination = null;

		public FswatchRuleActionItemDelete()
		{
			//do nothing
		}

		public FswatchRuleActionItemDelete(bool TAKEBACKUP, string DESTINATION)
		{
			this.__takebackup = TAKEBACKUP;
			this.__destination = DESTINATION;
		}

		public void InvokeAction(FileSystemEventArgs e, FSEventType type)
		{
			if(File.Exists(e.FullPath))
			{
				if(this.__takebackup)
				{

				}
			}
		}

		public void InvokeAction(RenamedEventArgs e, FSEventType type)
		{
			//invoke move action
		}
	}

}

