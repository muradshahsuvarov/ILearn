using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RegistrationAndLogin.Models;
using System.Net.Mail;
using System.Net;
using System.Web.Security;
using System.Diagnostics;

namespace RegistrationAndLogin.Controllers
{
    public class UserController : Controller
    {
        // Is the actual database
        private UserDBContext db = new UserDBContext();

        [HttpGet]
        [Authorize]
        public ActionResult ListOfUsers()
        {
            var users = from e in db.Users
                        orderby e.EmailID
                        select e;
            return View(users);
        }

        [HttpGet]
        [Authorize]
        public ActionResult ListOfEvents(int id)
        {
            var events = new SampleDataContext().Events.Where(r => r.userId == id && r.status == "AVAILABLE");

            return View(events);
        }

        [HttpPost]
        [Authorize]
        public ActionResult Subscribe(int? eventId, string email, string returnUrl) // eventID - id of the event to which user with email wants to subscribe.  // Done by the student
        {
            System.Diagnostics.Debug.WriteLine("EventId: " + eventId);
            System.Diagnostics.Debug.WriteLine("SubscriberEmail: " + email);
            System.Diagnostics.Debug.WriteLine("ReturnUrl: " + returnUrl);

            // For saving the changed event
            using (SampleDataContext sContext  = new SampleDataContext())
            {
                var targetEvent = (from e in sContext.Events
                                   where e.id == eventId
                                   select e).Single();
                targetEvent.status = "PENDING";
                targetEvent.subscriberEmail = email;

                sContext.SubmitChanges();
            }
                
            return Redirect(returnUrl);
        }

        [HttpPost]
        [Authorize]
        public ActionResult AcceptSubscription(int? eventId, string returnUrl) // Done by the tutor
        {
            System.Diagnostics.Debug.WriteLine("EventId: " + eventId);
            System.Diagnostics.Debug.WriteLine("ReturnUrl: " + returnUrl);

            using (SampleDataContext sContext = new SampleDataContext())
            {
                var targetEvent = (from e in sContext.Events
                                   where e.id == eventId && e.status == "PENDING"
                                   select e).Single();

                targetEvent.status = "ACCEPTED";
                sContext.SubmitChanges();
            }
                return Redirect(returnUrl);
        }

        [HttpPost]
        [Authorize]
        public ActionResult RejectSubscription(int? eventId, string returnUrl) //  // Done by the student
        {
            System.Diagnostics.Debug.WriteLine("EventId: " + eventId);
            System.Diagnostics.Debug.WriteLine("ReturnUrl: " + returnUrl);

            using (SampleDataContext sContext = new SampleDataContext())
            {
                var targetEvent = (from e in sContext.Events
                                   where e.id == eventId && e.status == "PENDING"
                                   select e).Single();

                targetEvent.status = "AVAILABLE";
                targetEvent.subscriberEmail = String.Empty;
                sContext.SubmitChanges();
            }
            return Redirect(returnUrl);
        }

        [HttpGet]
        [Authorize]
        public ActionResult GetApplications()
        {
            List<Event> targetEvents = new List<Event>();
            using (SampleDataContext context = new SampleDataContext())
            {
                 targetEvents = (from e in context.Events
                                   where e.subscriberEmail == User.Identity.Name  && e.status == "ACCEPTED"
                                   select e).ToList();
            }
                return View(targetEvents);
        }

        [HttpGet]
        [Authorize]
        public ActionResult GetTutorClasses()
        {

            List<Event> targetEvents = new List<Event>();
            using (SampleDataContext context = new SampleDataContext())
            {
                var user = (from e in db.Users
                            where e.EmailID == User.Identity.Name
                            select e).Single();
                targetEvents = (from e in context.Events
                                where e.userId == user.UserID && e.start_date >= DateTime.Now && e.status == "ACCEPTED" 
                                select e).ToList();
            }
            return View(targetEvents);
        }

        [HttpGet]
        [Authorize]
        public ActionResult GetTutorDashboard()
        {
            using (UserDBContext context = new UserDBContext())
            {
                var tutor = (from t in context.Users
                             where t.EmailID == User.Identity.Name && t.Role == "Tutor"
                             select t).Single();

                // TODO
                // Instantiate dashboard for the tutor
                // Return it in View
            }
                return View();
        }

        [HttpGet]
        [Authorize]
        public ActionResult ListOfTutorEvents()
        {
            // Finding authenticated user
            var user = db.Users.Where(u => u.EmailID == User.Identity.Name).Single();
            // Getting UserID
            int userID = user.UserID;
            // Getting events which belong to the user with userId == userID
            var events = new SampleDataContext().Events.Where(r => r.userId == userID && r.status == "PENDING");

            return View(events);
        }


        [HttpGet]
        [Authorize]
        public ActionResult Teachers()
        {

            var teachers = from e in db.Users
                        where e.Role == "Tutor"
                        select e;
            return View(teachers);
        }


        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(User usr)
        {
            try
            {
                db.Users.Add(usr);
                db.SaveChanges();
                return RedirectToAction("ListOfUsers");
            }
            catch (Exception)
            {

                return View();
            }
        }


        // GET: User/Edit/5
        [HttpGet]
        public ActionResult Edit()
        {
            string emailID = User.Identity.Name;


            var user = (from e in db.Users
                        where e.EmailID == emailID
                        select e).Single();

            return View(user);
        }

        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                var user = db.Users.Single(m => m.UserID == id);
                if(TryUpdateModel(user))
                {
                    db.SaveChanges();
                    return RedirectToAction("ListOfUsers");
                }
                return View(user);
            }
            catch (Exception)
            {
                return View();
            }
        }

       //Registration Action
        [HttpGet]
        public ActionResult Registration()
        {
            return View();
        }



        //Registration POST action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registration([Bind(Exclude = "IsEmailVerified,ActivationCode")] User user)
        {
            bool Status = false;
            string message = "";
            
            // Model Validation 
            if (ModelState.IsValid)
            {

                #region //Email is already Exist 
                var isExist = IsEmailExist(user.EmailID);
                if (isExist)
                {
                    ModelState.AddModelError("EmailExist", "Email already exist");
                    return View(user);
                }
                #endregion

                #region Generate Activation Code 

                user.ActivationCode = Guid.NewGuid();
                #endregion

                #region  Password Hashing
                user.Password = Crypto.Hash(user.Password);
                user.ConfirmPassword = Crypto.Hash(user.ConfirmPassword);
                #endregion
                user.IsEmailVerified = false;

                #region Save to Database
                using (MyDatabaseEntities1 dc = new MyDatabaseEntities1())
                {
                    dc.Users.Add(user);
                    dc.SaveChanges();

                    //Send Email to User
                    SendVerificationLinkEmail(user.EmailID, user.ActivationCode.ToString());
                    message = "Registration successfully done. Account activation link " + 
                        " has been sent to your email: " + user.EmailID;
                    Status = true;
                }
                #endregion
            }
            else
            {
                message = "Invalid Request";
            }

            ViewBag.Message = message;
            ViewBag.Status = Status;
            return View(user);
        }
        //Verify Account  
        
        [HttpGet]
        public ActionResult VerifyAccount(string id)
        {
            bool Status = false;
            using (MyDatabaseEntities1 dc = new MyDatabaseEntities1())
            {
                dc.Configuration.ValidateOnSaveEnabled = false; // This line I have added here to avoid 
                                                                // Confirm password does not match issue on save changes
                var v = dc.Users.Where(a => a.ActivationCode == new Guid(id)).FirstOrDefault();
                if (v != null)
                {
                    v.IsEmailVerified = true;
                    dc.SaveChanges();
                    Status = true;
                }
                else
                {
                    ViewBag.Message = "Invalid Request";
                }
            }
            ViewBag.Status = Status;
            return View();
        }

        //Login 
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public ActionResult MyProfile()
        {
            string emailID = User.Identity.Name;
           

            var user = (from e in db.Users
                        where e.EmailID == emailID
                        select e).Single();

            // To parse the authenticated user's with his ID
            /* var userWithId = (from e in db.Users
                              where e.UserID == user.UserID
                              select e).Single(); */

            return View(user);
        }

        [HttpGet]
        [Authorize]
        public ActionResult GetSchedule()
        {
            return View();
        }


        [HttpGet]
        [Authorize]
        public ActionResult GetDetails(int id)
        {


            var user = (from e in db.Users
                        where e.UserID == id
                        select e).Single();

            return View(user);
        }


        //Login POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UserLogin login, string ReturnUrl="")
        {
            string message = "";
            using (MyDatabaseEntities1 dc = new MyDatabaseEntities1())
            {
                var v = dc.Users.Where(a => a.EmailID == login.EmailID).FirstOrDefault();
                if (v != null)
                {
                    /*
                    if (!v.IsEmailVerified)
                    {
                        ViewBag.Message = "Please verify your email first";
                        return View();
                    } */

                    if (string.Compare(Crypto.Hash(login.Password),v.Password) == 0)
                    {
                        int timeout = login.RememberMe ? 525600 : 20; // 525600 min = 1 year
                        var ticket = new FormsAuthenticationTicket(login.EmailID, login.RememberMe, timeout);
                        string encrypted = FormsAuthentication.Encrypt(ticket);
                        var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted);
                        cookie.Expires = DateTime.Now.AddMinutes(timeout);
                        cookie.HttpOnly = true;
                        Response.Cookies.Add(cookie);


                        if (Url.IsLocalUrl(ReturnUrl))
                        {
                            return Redirect(ReturnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        message = "Invalid credential provided";
                    }
                }
                else
                {
                    message = "Invalid credential provided";
                }
            }
            ViewBag.Message = message;
            return View();
        }

        //Logout
        [Authorize]
        [HttpPost]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "User");
        }


        [NonAction]
        public bool IsEmailExist(string emailID)
        {
            using (MyDatabaseEntities1 dc = new MyDatabaseEntities1())
            {

                try
                {
                    var v = dc.Users.Where(a => a.EmailID == emailID).FirstOrDefault();
                    return v != null;
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error in line 176: " + e);
                }

                return false;
            }
        }

        [NonAction]
        public void SendVerificationLinkEmail(string emailID, string activationCode)
        {
            var verifyUrl = "/User/VerifyAccount/" + activationCode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

            var fromEmail = new MailAddress("muradshahsuvarov@gmail.com", "ILearn");
            var toEmail = new MailAddress(emailID);
            var fromEmailPassword = "********"; // Replace with actual password
            string subject = "Your account is successfully created!";

            string body = "<br/><br/>We are excited to tell you that your Dotnet Awesome account is" + 
                " successfully created. Please click on the below link to verify your account" + 
                " <br/><br/><a href='"+link+"'>"+link+"</a> ";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
            };

            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })

                try
                {
                    smtp.Send(message);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Sending the message error: " + e.ToString());
                }
           
        }



    }
}