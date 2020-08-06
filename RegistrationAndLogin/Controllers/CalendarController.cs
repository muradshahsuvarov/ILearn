using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


using DHTMLX.Scheduler;
using DHTMLX.Common;
using DHTMLX.Scheduler.Data;
using DHTMLX.Scheduler.Controls;

using RegistrationAndLogin.Models;
using System.Diagnostics;

namespace RegistrationAndLogin.Controllers
{
    
    public class CalendarController : Controller
    {

        // Is the actual database
        private UserDBContext db = new UserDBContext();

        public ActionResult Index(int? id, bool? notAuth)
        {
            //Being initialized in that way, scheduler will use CalendarController. Data as a the datasource and CalendarController. Save to process changes
            var scheduler = new DHXScheduler(this);

            /*
             * It's possible to use different actions of the current controller
             *      var scheduler = new DHXScheduler(this);     
             *      scheduler.DataAction = "ActionName1";
             *      scheduler.SaveAction = "ActionName2";
             * 
             * Or to specify full paths
             *      var scheduler = new DHXScheduler();
             *      scheduler.DataAction = Url.Action("Data", "Calendar");
             *      scheduler.SaveAction = Url.Action("Save", "Calendar");
             */

            /*
             * The default codebase folder is ~/Scripts/dhtmlxScheduler. It can be overriden:
             *      scheduler.Codebase = Url.Content("~/customCodebaseFolder");
             */

            scheduler.LoadData = true;
            scheduler.EnableDataprocessor = true;

            return View(scheduler);
        }

        // Get authenticated user
        User getAuthenticatedUser()
        {
            string emailID = User.Identity.Name;

            User authenticatedUser = (from e in db.Users
                                     where e.EmailID == emailID
                                     select e).Single();

            return authenticatedUser;
        }

        // Loads data from events
        public ContentResult Data()
        {
            User authenticatedUser = new User();
            SchedulerAjaxData data = new SchedulerAjaxData();

                authenticatedUser = getAuthenticatedUser();

                // SampleDataContext is the LINQ converted Event Table's object
                // To find particular events based on their IDs
                data = new SchedulerAjaxData(new SampleDataContext().Events.Where(r => r.userId == authenticatedUser.UserID));



            

            //var data = new SchedulerAjaxData( new SampleDataContext().Events);
            return (ContentResult)data;
        }

        
        // Save Changes
        public ContentResult Save(int? id, FormCollection actionValues)
        {
            var action = new DataAction(actionValues);

            // Parse athenticated user based on its email address
            var authenticatedUser = getAuthenticatedUser();


            try
            {
                // Enable Save of actual changes
                var changedEvent = (Event)DHXEventsHelper.Bind(typeof(Event), actionValues);
                changedEvent.userId = authenticatedUser.UserID; // Bind authenticated user's id to the event
                changedEvent.status = "AVAILABLE";
                var data = new SampleDataContext(); // Database context of the scheduler
     

                switch (action.Type)
                {
                    
                    case DataActionTypes.Insert:  // Insert logic is defined here
                        data.Events.InsertOnSubmit(changedEvent);
                        break;
                    case DataActionTypes.Delete:  // Delete logic is defined here
                        changedEvent = data.Events.SingleOrDefault(ev => ev.id == action.SourceId);
                        data.Events.DeleteOnSubmit(changedEvent);
                        break;
                    default:// "update". Update logic is defined here                      
                        var eventToUpdate = data.Events.SingleOrDefault(ev => ev.id == action.SourceId); // New changes = Last Added Event
                        DHXEventsHelper.Update(eventToUpdate, changedEvent, new List<string>() { "Id" });
                        break;
                }
                // Submission of changes
                data.SubmitChanges();
                action.TargetId = changedEvent.id;
            }
            catch(Exception e)
            {
                Debug.WriteLine($"ERROR: {e}");
                action.Type = DataActionTypes.Error;
            }
            return (ContentResult)new AjaxSaveResponse(action);
        }
    }
}

