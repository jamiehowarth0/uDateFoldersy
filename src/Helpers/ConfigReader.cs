using System.Configuration;
using System.Web;
using uDateFoldersy.Configuration;

namespace uDateFoldersy.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Web.Hosting;
    using System.Xml.Linq;

    public class ConfigReader
    {
        private const string m_ConfigPath = "~/config/uDateFoldersy.config";

        #region Singleton

        uDateFoldersySettingsSection _config;
        protected static volatile ConfigReader m_Instance = new ConfigReader();
        protected static object syncRoot = new Object();

        protected ConfigReader()
        {
	        var config = ConfigurationManager.OpenExeConfiguration(HostingEnvironment.MapPath(m_ConfigPath));
	        _config = (uDateFoldersySettingsSection)config.GetSection("uDateFoldersySettings");
        }


        public static ConfigReader Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    lock (syncRoot)
                    {
                        if (m_Instance == null)
                        {
                            m_Instance = new ConfigReader();
                        }
                    }
                }

                return m_Instance;
            }
        }

        #endregion



        /// <summary>
        /// Gets valid doctype aliases.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetTargetDocTypeAliases()
        {
            return this._config.DateFolders
						.TargetDocTypeAliases.Value
                        .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }



        /// <summary>
        /// Gets root doctype aliases.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public IEnumerable<string> GetRootDocTypeAliases()
        {
	        return this._config.DateFolders
		        .RootDocTypeAliases.Value.Trim()
		        .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }



        /// <summary>
        /// Gets the alias of the date property to get.
        /// </summary>
        /// <returns></returns>
        public string GetDatePropertyAlias()
        {
	        return this._config.DateFolders.DatePropertyAlias.Value.Trim();
        }



        /// <summary>
        /// Gets the alias of the day node.
        /// </summary>
        /// <returns></returns>
        public string GetDayFolderDocTypeAlias()
        {
	        return this._config.DateFolders.DayFolders.DoctypeAlias;
        }



        /// <summary>
        /// Gets the alias of the Month node.
        /// </summary>
        /// <returns></returns>
        public string GetMonthFolderDocTypeAlias()
        {
	        return this._config.DateFolders.MonthFolders.DoctypeAlias;
        }



        /// <summary>
        /// Gets the alias of the year node.
        /// </summary>
        /// <returns></returns>
        public string GetYearFolderDocTypeAlias()
        {
	        return this._config.DateFolders.YearFolders.DoctypeAlias;
        }
        
        /// <summary>
        /// Returns true if auto sorting is selected.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public bool UseAutoDateFolders()
        {
	        return this._config.DateFolders.Enabled;
        }




        /// <summary>
        /// Returns true if days is enabled.
        /// </summary>
        /// <returns></returns>
        public bool UseDays()
        {
	        return this._config.DateFolders.DayFolders.Enabled;
        }


        /// <summary>
        /// Returns true if months is enabled.
        /// </summary>
        /// <returns></returns>
        public bool UseMonths()
        {
	        return this._config.DateFolders.MonthFolders.Enabled;
        }



        /// <summary>
        /// Returns true if months is enabled.
        /// </summary>
        /// <returns></returns>
        public bool UseYears()
        {
	        return this._config.DateFolders.YearFolders.Enabled;
        }


        /// <summary>
        /// Gets month format
        /// </summary>
        /// <returns></returns>
        public string GetMonthFormat()
        {
	        return this._config.DateFolders.FolderNameFormat.MonthFormat.Value;
        }



        /// <summary>
        /// Gets day format
        /// </summary>
        /// <returns></returns>
        public string GetDayFormat()
        {
	        return this._config.DateFolders.FolderNameFormat.DayFormat.Value;
        }
    }
}
