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
	public class FswatchRuleChain
	{
		/************************************************
		* PROPERTIES
		************************************************/	
		private List<FswatchRuleMatchItem> __matchitems = new List<FswatchRuleMatchItem>();
		private List<FswatchRuleActionItem> __actionitems = new List<FswatchRuleActionItem>();

		/************************************************
		* ACCESSORS
		************************************************/
		public List<FswatchRuleMatchItem> matchitems
		{
			get{ return __matchitems; }
		}

		public List<FswatchRuleActionItem> actionitems
		{
			get{ return __actionitems; }
		}

		/************************************************
		* CONSTRUCTORS
		************************************************/	

		/************************************************
		* METHODS
		************************************************/
		public bool ProcessMatchItems(FileSystemEventArgs e, FSEventType type)
		{
			foreach(FswatchRuleMatchItem matchitem in this.__matchitems)
			{
				bool result = matchitem.MatchCondition(e, type);
				if(!result)
					return false;
			}
			return true;
		}

		public bool ProcessMatchItems(RenamedEventArgs e, FSEventType type)
		{
			foreach(FswatchRuleMatchItem matchitem in this.__matchitems)
			{
				bool result = matchitem.MatchCondition(e, type);
				if(!result)
					return false;
			}
			return true;
		}

		public void ProcessActionItems(FileSystemEventArgs e, FSEventType type)
		{
			foreach(FswatchRuleActionItem actionitem in this.__actionitems)
			{
				actionitem.InvokeAction(e, type);
			}
		}

		public void ProcessActionItems(RenamedEventArgs e, FSEventType type)
		{
			foreach(FswatchRuleActionItem actionitem in this.__actionitems)
			{
				actionitem.InvokeAction(e, type);
			}
		}
	}
}

