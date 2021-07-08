using Umbraco.Core.Cache;
using Umbraco.Core.Events;

namespace uDateFoldersy.UmbracoEvents
{
    using System;
    using System.Linq;
    using System.Web;

    using Umbraco.Core.Composing;
    using Umbraco.Core.Services.Implement;

    using uDateFoldersy;
    using uDateFoldersy.Helpers;

    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Services;

    public class UmbracoNodeEventsComposer : ComponentComposer<UmbracoNodeEventsForDateFolders>, IUserComposer
    {

    }

    public class UmbracoNodeEventsForDateFolders : IComponent
    {
	    private readonly IAppCache _requestCache;

	    private const string CacheKey = "uDateFoldersy.UmbracoEvents.";

        private const string EnsureCorrectParentForPost = CacheKey + "EnsureCorrectParentForPost";

        private const string EnsureCorrectDate = CacheKey + "EnsureCorrectParentForPost";

        private const string IsBeingCreated = CacheKey + "IsBeingCreated";

        public UmbracoNodeEventsForDateFolders(AppCaches appCaches)
        {
	        _requestCache = appCaches.RequestCache;
        }

        /// <summary>
        /// This is a hack/work around because Umbraco 6.0.3 does not update it's cach properly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContentService_Publishing(IContentService sender, ContentPublishingEventArgs e)
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
                _requestCache.Clear(entity.Id.ToString());
	        }
        }

        void ContentService_Creating(IContentService sender, ContentSavingEventArgs e)
        {
	        var content = e.SavedEntities.Select(c => c.ContentType.Alias);
            var config = ConfigReader.Instance;

            if (!config.UseAutoDateFolders()) { return; }
            if (!config.GetTargetDocTypeAliases().Any(p => content.Contains(p))) { return; }

	        // entity id is always 0 at this point
            SetFlag(IsBeingCreated, string.Empty);
        }


        /// <summary>
        /// After a node has been moved, we must change the date.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arg.</param>
        void ContentService_Moved(IContentService sender, Umbraco.Core.Events.MoveEventArgs<IContent> entities)
        {
	        var allContent = entities.MoveInfoCollection.Select(c => c.Entity.ContentType.Alias);
            var config = ConfigReader.Instance;

            if (!config.UseAutoDateFolders()) { return; }

            if (!config.GetTargetDocTypeAliases().Any(p => allContent.Contains(p))) { return; }

            // if this event is occuring after the created event, so do nothing
            if (this.Flagged(IsBeingCreated, string.Empty)) { return; }

            foreach (var e in entities.MoveInfoCollection)
            {
	            // if ensure correct date was called by another event, do nothing
	            if (this.Flagged(EnsureCorrectDate, e.Entity.Id.ToString())) { continue; }

	            // if thie move was a result of EnsureCorrectParentForPost, do nothing
	            if (this.Flagged(EnsureCorrectParentForPost, e.Entity.Id.ToString())) { continue; }

	            // this may have been a move to the recycle bin!
	            bool isMoveToBin = e.Entity.ParentId == -20;

	            // check up the .parent path for recycle bin
	            var current = sender.GetById(e.Entity.Id);
	            while (current.ParentId != -1)
	            {
		            if (current.ParentId == -20)
		            {
			            isMoveToBin = true;
			            break;
		            }

		            current = sender.GetById(current.ParentId);
	            }

	            if (!isMoveToBin)
	            {
		            SetFlag(EnsureCorrectDate, e.Entity.Id.ToString());
		            DateFolderService.Instance.EnsureCorrectDate(e.Entity);
	            }
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
            return bool.Parse(_requestCache.GetCacheItem<string>(key));
            
        }

        private void SetFlag(string cachePrefix, string suffix)
        {
            var key = cachePrefix + suffix;
            var cached = _requestCache.Get(key) as string;

            if (cached == null)
            {
	            _requestCache.GetCacheItem<string>(key, () => "true");
            }
        }

        public void Initialize()
        {
	        ContentService.Saving += this.ContentService_Creating;
	        ContentService.Saved += this.ContentService_Saved;
	        ContentService.Moved += this.ContentService_Moved;
            ContentService.Publishing += ContentService_Publishing;
        }

        public void Terminate()
        {
	        
        }
    }
}