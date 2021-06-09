using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using Mutual.Helpers.Utilities;
using _Type = Mutual.Helpers.Utilities._Type;

namespace Mutual.Helpers.Xml
{
    public class XmlSerializerException : CustomException
    {
		public XmlSerializerException(string message) : base(message)
		{ 
		}
		public XmlSerializerException(object sender, string message) : base(sender, message)
		{
		}
    }
}
