namespace uDateFoldersy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using uDateFoldersy.Comparers;
    using uDateFoldersy.Extensions;
    using uDateFoldersy.Helpers;

    using uHelpsy.Helpers;

    using Umbraco.Core;
    using Umbraco.Core.Composing;
    using Umbraco.Core.Models;
	using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;

    using Umbraco.Core.Services;

    interface IDateFolderService
    {
        IContent EnsureCorrectDate(IContent doc);
        IContent EnsureCorrectParentForPost(IContent doc, bool isCreatedEvent);
        IPublishedContent GetFirstRoot(IPublishedContent current);

    }



    public class DateFolderService : IDateFolderService
    {
        #region Singleton

        protected static volatile DateFolderService m_Instance = new DateFolderService(Current.Services.ContentService);
        protected static object syncRoot = new Object();
        private readonly IContentService _contentService;

        public DateFolderService(IContentService contentService) {
            _contentService = contentService;
        }

        public static DateFolderService Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    lock (syncRoot)
                    {
                        if (m_Instance == null)
                            m_Instance = new DateFolderService(Current.Services.ContentService);
                    }
                }

                return m_Instance;
            }
        }

        #endregion





        /// <summary>
        /// Ensures that a post date is correct depending on it's parent.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public IContent EnsureCorrectDate(IContent doc)
        {
            var config = ConfigReader.Instance;

            var dateAlias = config.GetDatePropertyAlias();
            var yearFolderAlias = config.GetYearFolderDocTypeAlias();
            var monthFolderAlias = config.GetMonthFolderDocTypeAlias();
            var dayFolderAlias = config.GetDayFolderDocTypeAlias();

            // get post date
            var postDate = doc.GetValue<DateTime>(dateAlias);

            var parent = _contentService.GetById(doc.ParentId);

            if (parent.ContentType.Alias == yearFolderAlias)
            {
                handleYearFolderAlias(doc, parent, postDate, dateAlias, _contentService);
            }
            else if (parent.ContentType.Alias == monthFolderAlias)
            {
                handleMonthFolderAlias(doc, _contentService, parent, postDate, dateAlias);
            }
            else if (parent.ContentType.Alias == dayFolderAlias)
            {
                handleDayFolderAlias(doc, _contentService, parent, postDate, dateAlias);
            }
            else
            {
                handleDashboardFolderAlias(doc, dateAlias, postDate, _contentService);
            }

            return doc;
        }

        // case where node was created on the dashboard
        private static void handleDashboardFolderAlias(IContent doc, string dateAlias, DateTime postDate, IContentService contentService)
        {
            doc.SetValue(dateAlias, postDate);
            contentService.Save(doc, raiseEvents: false);
        }

        // when parent is a day, get year and month, create date as: year/month/day
        private static void handleDayFolderAlias(IContent doc, IContentService contentService, IContent parent, DateTime postDate, string dateAlias)
        {
            var parentMonth = contentService.GetById(parent.ParentId);

            int year = int.Parse(contentService.GetById(parentMonth.ParentId).Name);
            int month = parentMonth.Name.GetMonthNumberFromName(); // eg. month can be 1, 01, Jan, January
            int day = parent.Name.GetDayNumberFromString(); // eg. day can be in formats 1, 01

            if (day != postDate.Day || month != postDate.Month || year != postDate.Year)
            {
                var newDate = new DateTime(year, month, day, postDate.Hour, postDate.Minute, postDate.Second,
                                           postDate.Millisecond);
                doc.SetValue(dateAlias, newDate);
                contentService.Save(doc, raiseEvents: false);
            }
        }

        // when parent is a month, get year and create date as: year/month/1
        private static void handleMonthFolderAlias(IContent doc, IContentService contentService, IContent parent, DateTime postDate, string dateAlias)
        {
            int year = int.Parse((contentService.GetById(parent.ParentId)).Name);
            int month = parent.Name.GetMonthNumberFromName();

            if (month != postDate.Month || year != postDate.Year)
            {
                var newDate = new DateTime(year, month, postDate.Day, postDate.Hour, postDate.Minute, postDate.Second,
                                           postDate.Millisecond);
                doc.SetValue(dateAlias, newDate);
                contentService.Save(doc, raiseEvents: false);
            }
        }

        private static void handleYearFolderAlias(IContent doc, IContent parent, DateTime postDate, string dateAlias,
                                                  IContentService contentService)
        {
            // when parent is a year, create date as: year/1/1
            int year = int.Parse(parent.Name);
            if (year != postDate.Year)
            {
                var newDate = new DateTime(year, postDate.Month, postDate.Day, postDate.Hour, postDate.Minute, postDate.Second,
                                           postDate.Millisecond);
                doc.SetValue(dateAlias, newDate);
                contentService.Save(doc, raiseEvents: false);
            }
        }


        /// <summary>
        /// Moves post to the correct parent, based on the post date.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public IContent EnsureCorrectParentForPost(IContent doc, bool isCreatedEvent)
        {
            var config = ConfigReader.Instance;

            var dateAlias = config.GetDatePropertyAlias();
            var dayFolderAlias = config.GetDayFolderDocTypeAlias();
            var monthFolderAlias = config.GetMonthFolderDocTypeAlias();
            var yearFolderAlias = config.GetYearFolderDocTypeAlias();

            if (doc.GetValue(dateAlias) == null || doc.GetValue(dateAlias).ToString() == string.Empty || doc.GetValue(dateAlias).ToString() == DateTime.MinValue.ToString())
            {
                doc.SetValue(dateAlias, doc.CreateDate);
                _contentService.Save(doc, raiseEvents: false);
            }

            IContent newParent;
            if (isCreatedEvent)
            {
                //var parent = ApplicationContext.Current.Services.ContentService.GetById(doc.ParentId);
                //if (this.IsDateFolder(parent))
                //{
                //    // created doc at another node
                //    return doc;
                //}

                // create date folders and get new parent
                newParent = this.GetCorrectParentForPost(doc, doc.CreateDate, dayFolderAlias, monthFolderAlias, yearFolderAlias);
            }
            else
            {
                newParent = this.EnsureCurrentCorrectParentForPost(doc, dateAlias, dayFolderAlias, monthFolderAlias, yearFolderAlias);
            }

            if (newParent != null && newParent.Id != doc.ParentId)
            {
                // move the node to the new parent
                _contentService.Move(doc, newParent.Id);

                // sort
                //var nodes = new List<IContent>(contentService.GetChildren(newParent.Id));
                //nodes.Sort(new PostDateComparer());
            }

            return doc;
        }




        /// <summary>
        /// Iterates over RootDocTypeAliases, looks up tree to find a root.
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        public IPublishedContent GetFirstRoot(IPublishedContent current)
        {
            var config = ConfigReader.Instance;

            var list = config.GetRootDocTypeAliases().ToList();
            list.Add(config.GetDayFolderDocTypeAlias());
            list.Add(config.GetMonthFolderDocTypeAlias());
            list.Add(config.GetYearFolderDocTypeAlias());

            return list.Select(current.AncestorOrSelf).FirstOrDefault(node => node != null);
        }

        /// <summary>
        /// Gets the correct parent for a post based on it's new post date.
        /// </summary>
        /// <returns></returns>
        protected IContent EnsureCurrentCorrectParentForPost(IContent doc, string dateAlias, string dayFolderAlias, string monthFolderAlias, string yearFolderAlias)
        {
            var parent = _contentService.GetById(doc.ParentId);

            // commented this out because of umbraco 7
            //if (!this.IsDateFolder(parent))
            //{
            //    // case where parent is a landing node
            //    return parent;
            //}
            DateTime date;
            if (!this.IsDateFolder(parent))
            {
                date = DateTime.TryParse(doc.GetValue<string>(ConfigReader.Instance.GetDatePropertyAlias()), out date) 
                    ? date : doc.CreateDate;

                parent = this.GetCorrectParentForPost(doc, date, dayFolderAlias, monthFolderAlias, yearFolderAlias);
            }


            var correctParent = HasCorrectDateFolders(doc);

            if (correctParent)
            {
                return parent;
            }

            date = doc.GetValue<DateTime>(ConfigReader.Instance.GetDatePropertyAlias());
            return this.GetCorrectParentForPost(doc, date, dayFolderAlias, monthFolderAlias, yearFolderAlias);
        }




        /// <summary>
        /// Checks ancestor nodes for correct names according to the nodes date.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        protected bool HasCorrectDateFolders(IContent doc)
        {
            var config = ConfigReader.Instance;

            var date = doc.GetValue<DateTime>(config.GetDatePropertyAlias());
            var parent = _contentService.GetById(doc.ParentId);

            if (parent.ContentType.Alias == config.GetDayFolderDocTypeAlias())
            {
                // check ancestors when post is in a day folder
                var hasCorrectParent = IsCorrectParent(parent, date);
                if (hasCorrectParent == false) { return false; }

                var monthFolder = _contentService.GetById(parent.ParentId);

                hasCorrectParent = IsCorrectParent(monthFolder, date);
                if (hasCorrectParent == false) { return false; }

                var yearFolder = _contentService.GetById(monthFolder.ParentId);

                hasCorrectParent = IsCorrectParent(yearFolder, date);
                if (hasCorrectParent == false) { return false; }
            }

            if (parent.ContentType.Alias == config.GetMonthFolderDocTypeAlias())
            {
                // check ancestors when post is in a month folder
                var hasCorrectParent = IsCorrectParent(parent, date);
                if (hasCorrectParent == false) { return false; }

                var yearFolder = _contentService.GetById(parent.ParentId);

                hasCorrectParent = IsCorrectParent(yearFolder, date);
                if (hasCorrectParent == false) { return false; }
            }

            if (parent.ContentType.Alias == config.GetYearFolderDocTypeAlias())
            {
                // check ancestor when post is in a year folder
                var hasCorrectParent = IsCorrectParent(parent, date);
                if (hasCorrectParent == false) { return false; }
            }

            return true;
        }


        /// <summary>
        /// Checks date against name of parent.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        protected bool IsCorrectParent(IContent parent, DateTime date)
        {
            var config = ConfigReader.Instance;

            var correctParent = true;

            // check year folder
            if (parent.ContentType.Alias == config.GetYearFolderDocTypeAlias())
            {
                if (parent.Name != date.Year.ToString())
                {
                    // must create new nodes
                    correctParent = false;
                }
            }

            // check month folder...
            if (parent.ContentType.Alias == config.GetMonthFolderDocTypeAlias())
            {
                string monthName = DateHelper.GetMonthNameWithFormat(date.Month, config.GetMonthFormat());

                if (parent.Name != monthName)
                {
                    // create new nodes
                    correctParent = false;
                }
            }

            // check month folder...
            if (parent.ContentType.Alias == config.GetDayFolderDocTypeAlias())
            {
                string dayName = DateHelper.GetDayNameWithFormat(date.Day, config.GetDayFormat());

                if (parent.Name != dayName)
                {
                    // create new nodes
                    correctParent = false;
                }
            }

            return correctParent;
        }




        /// <summary>
        /// Gets the correct parent for a post based on post date, creates it if it does not exist.
        /// </summary>
        /// <returns></returns>
        protected IContent GetCorrectParentForPost(IContent doc, DateTime date, string dayFolderAlias, string monthFolderAlias, string yearFolderAlias)
        {
            var config = ConfigReader.Instance;

            // get post date 
            var postDate = date; // doc.GetValue<DateTime>(dateAlias);

            IContent root = null;
            var parent = _contentService.GetById(doc.ParentId);

            if (config.GetRootDocTypeAliases().Contains(parent.ContentType.Alias))
            {
                // parent is a valid alias, so set as root
                root = parent;
            }
            else
            {
                if (this.IsDateFolder(parent))
                {
                    root = parent;
                }

                foreach (var alias in config.GetRootDocTypeAliases())
                {
                    root = IContentHelper.GetParentIContentByAlias(doc, null, alias);
                    if (root != null) { break; }
                }
            }

            // node was created as top level
            if (root == null) { return null; }

            // year folders are disabled
            if (!config.UseYears()) { return root; }

            var yearFolder = this.GetYearFolder(root, postDate, yearFolderAlias);

            if (yearFolder.Id == doc.ParentId)
            {
                // case where a move has occured to a year or when node was created using dashboard/live writer
                return yearFolder;
            }

            // month folders are disabled
            if (!config.UseMonths()) { return yearFolder; }

            // get or create month based on date
            var monthFolder = this.GetMonthFolder(yearFolder, postDate, monthFolderAlias);

            if (monthFolder.Id == doc.ParentId)
            {
                // case where a move has occured to a month or when node was created using dashboard/live writer
                return monthFolder;
            }

            // day folders are disabled
            if (!config.UseDays()) { return monthFolder; }

            var dayFolder = this.GetDayFolder(monthFolder, postDate, dayFolderAlias);

            return dayFolder;
        }





        /// <summary>
        /// Returns true if parent is a data folder.
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        private bool IsDateFolder(IContent parent)
        {
            var config = ConfigReader.Instance;

            if (parent.ContentType.Alias == config.GetDayFolderDocTypeAlias()
                || parent.ContentType.Alias == config.GetMonthFolderDocTypeAlias()
                || parent.ContentType.Alias == config.GetYearFolderDocTypeAlias())
            {
                return true;
            }
            return false;
        }








        /// <summary>
        /// Gets year folder. Creates if doesnt exist.
        /// </summary>
        /// <param name="root">
        /// The root.
        /// </param>
        /// <param name="postDate">
        /// The post Date.
        /// </param>
        /// <param name="yearFolderAlias">
        /// The year Folder Alias.
        /// </param>
        /// <returns>
        /// </returns>
        protected IContent GetYearFolder(IContent root, DateTime postDate, string yearFolderAlias)
        {
	        int temp;
            long temp2;

            var yearFolders = _contentService
									.GetPagedChildren(root.Id, 0, 0, out temp2)
                                    .Where(x => int.TryParse(x.Name, out temp)) // only take nodes which have a valid year
                                    .Where(x => x.ContentType.Alias == yearFolderAlias);

            var yearFolder = yearFolders.FirstOrDefault(x => int.Parse(x.Name) == postDate.Year);

            if (yearFolder != null)
            {
                return yearFolder;
            }

            // year not found so create it!
            yearFolder = IContentHelper.CreateContentNode(postDate.Year.ToString(), yearFolderAlias, new Dictionary<string, object>(), root.Id, root.Published);

            // add new year folder to list and sort
            // ensure list is updated, and add non-folder nodes
            long temp3;
            var allNodes = _contentService
	            .GetPagedChildren(root.Id, 0, 0, out temp3)
	            .Where(x => x.ContentType.Alias != yearFolderAlias)
	            .ToList();
            var nodes = _contentService
	            .GetPagedChildren(root.Id, 0, 0, out temp3)
	            .Where(x => x.ContentType.Alias == yearFolderAlias);
            allNodes.AddRange(nodes);

            IContentHelper.SortNodes(yearFolder.ParentId, allNodes, new YearComparer());

            return yearFolder;
        }






        /// <summary>
        /// Gets month folder from year folder. Creates month folder if it does not exist.
        /// </summary>
        /// <param name="yearFolder"></param>
        /// <param name="postDate"></param>
        /// <returns></returns>
        protected IContent GetMonthFolder(IContent yearFolder, DateTime postDate, string monthFolderAlias)
        {
	        long temp;

            // parent should be a month folder so lets get the months
            var monthFolders = _contentService
	            .GetPagedChildren(yearFolder.Id, 0, 0, out temp)
	            .Where(x => x.ContentType.Alias == monthFolderAlias);

            // search for correct month
            var monthFolder = monthFolders.FirstOrDefault(x => x.Name.GetMonthNumberFromName() == postDate.Month);
            if (monthFolder != null)
            {
                return monthFolder;
            }

            // month not found so create it
            string monthName = DateHelper.GetMonthNameWithFormat(postDate.Month, ConfigReader.Instance.GetMonthFormat());
            monthFolder = IContentHelper.CreateContentNode(monthName, monthFolderAlias, new Dictionary<string, object>(), yearFolder.Id, yearFolder.Published);

            // ensure list is updated, and add non-folder nodes
            long temp2;
            var allNodes = _contentService
	            .GetPagedChildren(yearFolder.Id, 0, Int32.MaxValue, out temp2)
	            .Where(x => x.ContentType.Alias != monthFolderAlias)
	            .ToList();
            var nodes = _contentService
	            .GetPagedChildren(yearFolder.Id, 0, Int32.MaxValue, out temp2)
	            .Where(x => x.ContentType.Alias == monthFolderAlias);
            allNodes.AddRange(nodes);

            IContentHelper.SortNodes(yearFolder.Id, allNodes, new MonthComparer());

            return monthFolder;
        }






        /// <summary>
        /// Gets month folder from year folder. Creates month folder if it does not exist.
        /// </summary>
        /// <param name="monthFolder"></param>
        /// <param name="postDate"></param>
        /// <returns></returns>
        protected IContent GetDayFolder(IContent monthFolder, DateTime postDate, string dayFolderAlias)
        {
            long temp;

            // parent should be a month folder so lets get the months
            var dayFolders = _contentService
	            .GetPagedChildren(monthFolder.Id, 0, 0, out temp)
	            .Where(x => x.ContentType.Alias == dayFolderAlias);

            // search for correct day
            var dayFolder = dayFolders.FirstOrDefault(x => x.Name.GetDayNumberFromString() == postDate.Day);
            if (dayFolder != null)
            {
                return dayFolder;
            }

            // day not found so create it
            string dayName = DateHelper.GetDayNameWithFormat(postDate.Day, ConfigReader.Instance.GetDayFormat());
            dayFolder = IContentHelper.EnsureNodeExists(monthFolder.Id, dayFolder, dayFolderAlias, dayName, monthFolder.Published);

            // ensure list is updated, and add non-folder nodes
            long temp2;
            var allNodes = _contentService
	            .GetPagedChildren(monthFolder.Id, 0, Int32.MaxValue, out temp2)
	            .Where(x => x.ContentType.Alias != dayFolderAlias)
	            .ToList();
            var nodes = _contentService
	            .GetPagedChildren(monthFolder.Id, 0, Int32.MaxValue, out temp2)
                .Where(x => x.ContentType.Alias == dayFolderAlias);
            allNodes.AddRange(nodes);

            IContentHelper.SortNodes(monthFolder.Id, allNodes, new DayComparer());

            return dayFolder;
        }
    }
}
