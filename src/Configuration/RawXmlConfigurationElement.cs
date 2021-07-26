using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace uDateFoldersy.Configuration
{
	public abstract class RawXmlConfigurationElement : ConfigurationElement
	{
		protected RawXmlConfigurationElement()
		{

		}

		protected RawXmlConfigurationElement(XElement rawXml)
		{
			RawXml = rawXml;
		}

		protected override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
		{
			RawXml = (XElement)XNode.ReadFrom(reader);
		}

		protected XElement RawXml { get; private set; }
	}
}
