namespace uDateFoldersy.UmbracoEvents
{
    using System;
    using System.Linq;
    using System.Web;

    using Umbraco.Core.Publishing;

    using uDateFoldersy;
    using uDateFoldersy.Helpers;

    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Services;

    using umbraco;

    public class UmbracoNodeEventsForDateFolders : ApplicationEventHandler
    {
        private const string CacheKey = "uDateFoldersy.UmbracoEvents.";

        private const string EnsureCorrectParentForPost = CacheKey + "EnsureCorrectParentForPost";

        private const string EnsureCorrectDate = CacheKey + "EnsureCorrectParentForPost";

        private const string IsBeingCreated = CacheKey + "IsBeingCreated";

       // private const string SaveEventIgnoreFirst = CacheKey + "SaveEventIgnoreFirst";


        protected override void ApplicationInitialized(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
{
 	     base.ApplicationInitialized(umbracoApplication, applicationContext);
            ContentService.Creating += this.ContentService_Creating;
            //ContentService.Created += this.ContentService_Created;
            ContentService.Saved += this.ContentService_Saved;
            ContentService.Moved += this.ContentService_Moved;
            PublishingStrategy.Published += new Umbraco.Core.Events.TypedEventHandler<IPublishingStrategy, Umbraco.Core.Events.PublishEventArgs<IContent>>(PublishingStrategy_Published);
    }


        //public void OnApplicationInitialized(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        //{
        //    ContentService.Creating += this.ContentService_Creating;
        //    //ContentService.Created += this.ContentService_Created;
        //    ContentService.Saved += this.ContentService_Saved;
        //    ContentService.Moved += this.ContentService_Moved;
        //    PublishingStrategy.Published += new Umbraco.Core.Events.TypedEventHandler<IPublishingStrategy, Umbraco.Core.Events.PublishEventArgs<IContent>>(PublishingStrategy_Published);
        //}



        /// <summary>
        /// This is a hack/work around because Umbraco 6.0.3 does not update it's cach properly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PublishingStrategy_Published(IPublishingStrategy sender, Umbraco.Core.Events.PublishEventArgs<IContent> e)
        {
            var config = ConfigReader.Instance;

            if (!config.UseAutoDateFolders()) { return; }

            foreach (var entity in e.PublishedEntities)
            {
                if (!config.GetTargetDocTypeAliases().Contains(
                        entity.ContentType.Alias))
                {
                    continue;
                }
                library.UpdateDocumentCache(entity.Id);
            }
        }

        void ContentService_Creating(IContentService sender, Umbraco.Core.Events.NewEventArgs<IContent> e)
        {
            var config = ConfigReader.Instance;

            if (!config.UseAutoDateFolders()) { return; }
            if (!config.GetTargetDocTypeAliases().Contains(e.Entity.ContentType.Alias)) { return; }

            // entity id is always 0 at this point
            SetFlag(IsBeingCreated, string.Empty);
        }


        /// <summary>
        /// After a node has been moved, we must change the date.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arg.</param>
        void ContentService_Moved(IContentService sender, Umbraco.Core.Events.MoveEventArgs<IContent> e)
        {
            var config = ConfigReader.Instance;

            if (!config.UseAutoDateFolders()) { return; }

            if (!config.GetTargetDocTypeAliases().Contains(e.Entity.ContentType.Alias)) { return; }

            // if this event is occuring after the created event, so do nothing
            if (this.Flagged(IsBeingCreated, string.Empty)) { return; }

            // if ensure correct date was called by another event, do nothing
            if (this.Flagged(EnsureCorrectDate, e.Entity.Id.ToString())) { return; }

            // if thie move was a result of EnsureCorrectParentForPost, do nothing
            if (this.Flagged(EnsureCorrectParentForPost, e.Entity.Id.ToString())) { return; } 

            var contentService = ApplicationContext.Current.Services.ContentService;

            // this may have been a move to the recycle bin!
            bool isMoveToBin = e.Entity.ParentId == -20;

            // check up the .parent path for recycle bin
            var current = contentService.GetById(e.Entity.Id);
            while (current.ParentId != -1)
            {
                if (current.ParentId == -20)
                {
                    isMoveToBin = true;
                    break;
                }

                current = contentService.GetById(current.ParentId);
            }

            if (!isMoveToBin)
            {
                SetFlag(EnsureCorrectDate, e.Entity.Id.ToString());
                DateFolderService.Instance.EnsureCorrectDate(e.Entity);
            }
        }




        /// <summary>
        /// Ensures that a post exists under the correct year/month node determined by its date.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arg.</param>
        void ContentService_Saved(IContentService sender, Umbraco.Core.Events.SaveEventArgs<IContent> e)
        {
            // If this event isn't invoked from a Request then HttpContext.Current will be null.
            // Which MAY indicate that the event was fired by a timer to handler the
            // Publish At/Remove At functionality. It would be better if the event object 
            // provided some context to state this.
            // http://our.umbraco.org/projects/backoffice-extensions/udatefoldersy/general/44987-Publish-At-throws-Null-Reference-Within-uDateFoldersy?p=0#comment161872
            if (HttpContext.Current == null) { return; }

            var config = ConfigReader.Instance;

            if (!config.UseAutoDateFolders()) { return; }

            var targetAliases = config.GetTargetDocTypeAliases();
            foreach (var entity in e.SavedEntities)
            {
                // if not a valid alias, do nothing
                if (!targetAliases.Contains(entity.ContentType.Alias)) { continue; }

                if (entity.Id == 0)
                {
                    continue;
                }

                // check for created event
                if (this.Flagged(IsBeingCreated, string.Empty))
                {
                    // ignore first save event because umbraco hits it too many times
                    //if (this.Flagged(SaveEventIgnoreFirst, entity.Id.ToString()) == false)
                    //{
                    //    this.SetFlag(SaveEventIgnoreFirst, entity.Id.ToString());
                    //    return;
                    //}

                    if (this.Flagged(EnsureCorrectParentForPost, entity.Id.ToString()) == true)
                    {
                        // this save event is a result of a save after EnsureCorrectParentForPost was called, so do nothing
                        return;
                    }

                    // set EnsureCorrectParentForPost flag and run
                    SetFlag(EnsureCorrectParentForPost, entity.Id.ToString());
                    DateFolderService.Instance.EnsureCorrectParentForPost(entity, true);

                    return;
                }

                // set EnsureCorrectParentForPost flag and run
                SetFlag(EnsureCorrectParentForPost, entity.Id.ToString());
                DateFolderService.Instance.EnsureCorrectParentForPost(entity, false);
            }
        }




        /// <summary>
        /// Returns true if cache is null. Otherwise, sets cache and returns false.
        /// </summary>
        /// <param name="suffix">The suffix.</param>
        /// <param name="cachePrefix">The cachePrefix.</param>
        /// <returns>Returns true if cache is null.</returns>
        private bool Flagged(string cachePrefix, string suffix)
        {
            var key = cachePrefix + suffix;
            var cached = HttpContext.Current.Items[key] as string;

            if (cached == null)
            {
                return false;
            }

            return true;
        }

        private void SetFlag(string cachePrefix, string suffix)
        {
            var key = cachePrefix + suffix;
            var cached = HttpContext.Current.Items[key] as string;

            if (cached == null)
            {
                HttpContext.Current.Items.Add(key, "true");
            }
        }

        //public void OnApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        //{

        //}

        //public void OnApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        //{

        //}
    }
}