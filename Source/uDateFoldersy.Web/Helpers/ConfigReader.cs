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

        protected XDocument ConfigXDocument;
        protected static volatile ConfigReader m_Instance = new ConfigReader();
        protected static object syncRoot = new Object();

        protected ConfigReader()
        {
            this.ConfigXDocument = XDocument.Parse(File.ReadAllText(HostingEnvironment.MapPath(m_ConfigPath)));
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
            return this.ConfigXDocument
                        .Descendants("TargetDocTypeAliases")
                        .Single()
                        .Value.Trim()
                        .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }



        /// <summary>
        /// Gets root doctype aliases.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public IEnumerable<string> GetRootDocTypeAliases()
        {
            var list = this.ConfigXDocument
                        .Descendants("RootDocTypeAliases")
                        .Single()
                        .Value.Trim()
                        .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
        
            //list.Add(this.GetDayFolderDocTypeAlias());
            //list.Add(this.GetMonthFolderDocTypeAlias());
            //list.Add(this.GetYearFolderDocTypeAlias());
            return list;
        }



        /// <summary>
        /// Gets the alias of the date property to get.
        /// </summary>
        /// <returns></returns>
        public string GetDatePropertyAlias()
        {
            return this.ConfigXDocument
                        .Descendants("DatePropertyAlias")
                        .Single()
                        .Value.Trim();
        }



        /// <summary>
        /// Gets the alias of the day node.
        /// </summary>
        /// <returns></returns>
        public string GetDayFolderDocTypeAlias()
        {
            return this.GetDocTypeAlias("DayFolders");
        }



        /// <summary>
        /// Gets the alias of the Month node.
        /// </summary>
        /// <returns></returns>
        public string GetMonthFolderDocTypeAlias()
        {
            return GetDocTypeAlias("MonthFolders");
        }



        /// <summary>
        /// Gets the alias of the year node.
        /// </summary>
        /// <returns></returns>
        public string GetYearFolderDocTypeAlias()
        {
            return GetDocTypeAlias("YearFolders");
        }


        /// <summary>
        /// Gets alias with name == elemName.
        /// </summary>
        /// <param name="elemName"></param>
        /// <returns></returns>
        private string GetDocTypeAlias(string elemName)
        {
            return this.ConfigXDocument
                        .Descendants(elemName)
                        .Single()
                        .Attribute("docTypeAlias")
                        .Value.Trim();
        }




        /// <summary>
        /// Returns true if auto sorting is selected.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public bool UseAutoDateFolders()
        {
            var dateFolders = Enumerable.Single(this.ConfigXDocument.Descendants("DateFolders"));

            var enabled = dateFolders.Attribute("enabled").Value;

            return enabled == "true";
        }




        /// <summary>
        /// Returns true if days is enabled.
        /// </summary>
        /// <returns></returns>
        public bool UseDays()
        {
            var dateFolders = this.ConfigXDocument.Descendants("DayFolders").Single();

            var enabled = dateFolders.Attribute("enabled").Value;

            return enabled.ToLower() == "true";
        }


        /// <summary>
        /// Returns true if months is enabled.
        /// </summary>
        /// <returns></returns>
        public bool UseMonths()
        {
            var dateFolders = this.ConfigXDocument.Descendants("MonthFolders").Single();

            var enabled = dateFolders.Attribute("enabled").Value;

            return enabled.ToLower() == "true";
        }



        /// <summary>
        /// Returns true if months is enabled.
        /// </summary>
        /// <returns></returns>
        public bool UseYears()
        {
            var dateFolders = this.ConfigXDocument.Descendants("YearFolders").Single();

            var enabled = dateFolders.Attribute("enabled").Value;

            return enabled.ToLower() == "true";
        }


        /// <summary>
        /// Gets month format
        /// </summary>
        /// <returns></returns>
        public string GetMonthFormat()
        {
            var dateFolders = this.ConfigXDocument.Descendants("FolderNameFormat").Single();

            var monthFormat = dateFolders.Descendants("MonthFormat").Single().Value;

            return monthFormat;
        }



        /// <summary>
        /// Gets day format
        /// </summary>
        /// <returns></returns>
        public string GetDayFormat()
        {
            var dateFolders = this.ConfigXDocument.Descendants("FolderNameFormat").Single();

            var dayFormat = dateFolders.Descendants("DayFormat").Single().Value;

            return dayFormat;
        }
    }
}
