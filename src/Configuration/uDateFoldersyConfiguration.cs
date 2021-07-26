using System;
using System.ComponentModel;
using System.Configuration;
using System.Xml.Serialization;

namespace uDateFoldersy.Configuration
{
	public class uDateFoldersySettingsSection : ConfigurationSection
	{
		[ConfigurationProperty("DateFolders", IsRequired = false)]
		public DateFolders DateFolders => (DateFolders)this["DateFolders"];
	}


    /// <remarks/>
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public class DateFolders : ConfigurationElement
    {
		/// <remarks/>
		[ConfigurationProperty("RootDocTypeAliases")]
		public InnerTextConfigurationElement<string> RootDocTypeAliases => (InnerTextConfigurationElement<string>)this["RootDocTypeAliases"];

		/// <remarks/>
		[ConfigurationProperty("YearFolders")]
		public FolderSettings YearFolders => (FolderSettings)this["YearFolders"];

		/// <remarks/>
		[ConfigurationProperty("MonthFolders")]
		public FolderSettings MonthFolders => (FolderSettings)this["MonthFolders"];

		[ConfigurationProperty("DayFolders")]

		public FolderSettings DayFolders => (FolderSettings)this["DayFolders"];

		[ConfigurationProperty("TargetDocTypeAliases")]
		/// <remarks/>
		public InnerTextConfigurationElement<string> TargetDocTypeAliases => (InnerTextConfigurationElement<string>)this["TargetDocTypeAliases"];

		/// <remarks/>
		[ConfigurationProperty("DatePropertyAlias")]
		public InnerTextConfigurationElement<string> DatePropertyAlias => (InnerTextConfigurationElement<string>)this["DatePropertyAlias"];

		/// <remarks/>
		[ConfigurationProperty("FolderNameFormat")]
		public FolderNameFormat FolderNameFormat => (FolderNameFormat)this["FolderNameFormat"];

		/// <remarks/>
		[ConfigurationProperty("enabled", DefaultValue = true)]
		public bool Enabled => (bool)this["enabled"];
    }

    public class FolderSettings : ConfigurationElement
    {
        /// <remarks/>
        [ConfigurationProperty("enabled")]
        public bool Enabled => (bool)this["enabled"];

        /// <remarks/>
        [ConfigurationProperty("docTypeAlias")]
        public string DoctypeAlias => (string)this["docTypeAlias"];
	}

    public class FolderNameFormat : ConfigurationElement
    {
	    /// <remarks/>
        [ConfigurationProperty("MonthFormat")]
	    public InnerTextConfigurationElement<string> MonthFormat => (InnerTextConfigurationElement<string>)this["MonthFormat"];

        /// <remarks/>
        [ConfigurationProperty("DayFormat")]
        public InnerTextConfigurationElement<string> DayFormat => (InnerTextConfigurationElement<string>)this["DayFormat"];
    }
}
