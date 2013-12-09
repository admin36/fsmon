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
	public class FswatchRuleMatchItem
	{
		/************************************************
		* PROPERTIES
		************************************************/
		private string __context;
		private string __type;
		private string __value;

		/************************************************
		* ACCESSORS
		************************************************/

		/************************************************
		* CONSTRUCTORS
		************************************************/
		public FswatchRuleMatchItem (string CONTEXT, string TYPE, string VALUE)
		{
			this.__context = CONTEXT;
			this.__type = TYPE;
			this.__value = VALUE;
		}

		/************************************************
		* METHODS
		************************************************/	
		public bool MatchCondition(Object e, FSEventType type)
		{



			if(this.__context == "filename")
			{
				if(this.__type == "string")
				{
					if(e.Name == this.__value)
						return true;
					else
						return false;
				}
				else if(this.__type == "regex")
				{
					Regex regex = new Regex(this.__value);
					if(regex.Match(e.Name).Success)
						return true;
					else
						return false;
				}
			}
			else if(this.__context == "filehash")
			{
				if(this.__type == "string")
				{
					return false;
				}
				else if(this.__type == "regex")
				{
					return false;
				}
			}
			else if(this.__context == "filecontent")
			{
				if(this.__type == "string")
				{
					return false;
				}
				else if(this.__type == "regex")
				{
					return false;
				}
			}
			else
			{
				return false;
			}
			return false;
		}
	}
}

