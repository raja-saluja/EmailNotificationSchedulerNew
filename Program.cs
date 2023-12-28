using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using EmailNotificationNew.Data;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Net;
using System.Data.SqlClient;

namespace EmailNotificationNew
{
    class Program
    {
        private static readonly ILog log =
        LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static void Main(string[] args)
        {

            try
            {
                SendEmailNotificationPickup();

                SendEmailNotificationDelivered();

                SendEmailNotificationOFD();

                SendEmailNotificationExceptions();
            }
            catch (Exception ex)
            {
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
            }

        }

        public static void SendEmailNotificationPickup()
        {
            try
            {
                var EmailIsSent = false;
                ERPNaqelEntities1 db = new ERPNaqelEntities1();
                List<EmailNotificationPickup> viwEmailNotificationPickups = new List<EmailNotificationPickup>();

                viwEmailNotificationPickups = db.EmailNotificationPickups.ToList();
                string htmlformat = "";
                string To = ConfigurationManager.AppSettings["Myemail"];


                List<EmailNotificationPickup> PickupEmailList = viwEmailNotificationPickups;
                if (PickupEmailList.Count != 0)
                {
                   
                    foreach (var item in PickupEmailList)
                    {
                        string EmailFormatLanguage = "";
                        if (item.CneeName != null)
                        {

                            if (Regex.IsMatch(item.CneeName, "^[a-zA-Z0-9_ ]"))
                            {
                                EmailFormatLanguage = "EN";
                            }
                            else
                            {
                                EmailFormatLanguage = "AR";
                            }

                        }
                        var Msg = db.Database.SqlQuery<string>("select CoreText from smssentmessage where StatusID = 1 and PurposeID = 24 and RefNo='" + item.waybillno + "' order by date desc").FirstOrDefault();
                        // Size the control to fill the form with a margin
                        MatchCollection ms = Regex.Matches(Msg, @"\b(?:https?://|www\.)\S+\b");
                        string URLLink = ms[0].Value.ToString();
                        if (EmailFormatLanguage == "EN")
                        {
                            htmlformat = pickedupFormatBodyEN(item, URLLink);
                            EmailIsSent =  EmailBody(htmlformat, To, item.waybillno.ToString()); // replace email with user email 
                        }
                        else
                        {
                            htmlformat = PickedupFormatEmailBodyAR(item, URLLink);
                            EmailIsSent = EmailBody(htmlformat, To, item.waybillno.ToString()); // replace email with user email 
                        }

                        if (EmailIsSent)
                        {
                            //insert log row for this waybill
                            EmailNotificationLog EmailNotification = db.EmailNotificationLogs.Where(y => y.WaybillNo == item.waybillno).FirstOrDefault();
                            if (EmailNotification == null)
                            {
                                EmailNotificationLog Obj = new EmailNotificationLog();

                                Obj.IsPickup_EmailSent = true;
                                Obj.WaybillNo = item.waybillno;
                                Obj.EmailAddress = item.CneeEmail;
                                Obj.CreatedDate = DateTime.Now;
                                Obj.LastUpdatedDate = DateTime.Now;
                                Obj.StatusID = 1;
                                db.EmailNotificationLogs.Add(Obj);
                                db.SaveChanges();

                            }
                            else
                            {
                                EmailNotification.IsPickup_EmailSent = true;
                                EmailNotification.LastUpdatedDate = DateTime.Now;
                                db.SaveChanges();
                            }

                        }

                    }

                }
            }
            catch (Exception ex)
            {
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
            }
        }
        public static void SendEmailNotificationDelivered()
        {
            try
            {
                var EmailIsSent = false;
                ERPNaqelEntities1 db = new ERPNaqelEntities1(); 

                List<ViwEmailNotificationDelivered> viwEmailNotificationDelivered = new List<ViwEmailNotificationDelivered>();

                viwEmailNotificationDelivered = db.ViwEmailNotificationDelivereds.ToList();
                string htmlformat = "";
                string To = ConfigurationManager.AppSettings["Myemail"];


                List<ViwEmailNotificationDelivered> DeliveredEmailList = viwEmailNotificationDelivered;
                if (DeliveredEmailList.Count != 0)
                {
                    

                    foreach (var item in DeliveredEmailList)
                    {
                        string EmailFormatLanguage = "";
                        //string ratingURL = getRatingURL(Convert.ToInt32(item.waybillno));
                        if (item.CneeName != null)
                        {

                            if (Regex.IsMatch(item.CneeName, "^[a-zA-Z0-9_ ]"))
                            {
                                EmailFormatLanguage = "EN";
                            }
                            else
                            {
                                EmailFormatLanguage = "AR";
                            }

                        }
                        var Msg = db.Database.SqlQuery<string>("select CoreText from smssentmessage where StatusID = 1 and PurposeID = 8 and RefNo='" + item.waybillno + "' order by date desc").FirstOrDefault();
                        // Size the control to fill the form with a margin
                        MatchCollection ms = Regex.Matches(Msg, @"\b(?:https?://|www\.)\S+\b");
                        string URLLink = ms[0].Value.ToString();

                        if (EmailFormatLanguage == "EN")
                        {
                            htmlformat = DeliveredFormatBodyEN(item, URLLink);

                            EmailIsSent =  EmailBody(htmlformat, To, item.waybillno.ToString()); // replace email with user email 
                        }
                        else
                        {
                            htmlformat = DeliveredFormatEmailBodyAR(item, URLLink);

                            EmailIsSent =  EmailBody(htmlformat, To, item.waybillno.ToString()); // replace email with user email 
                        }

                        if (EmailIsSent)
                        {
                            //insert log row for this waybill
                            EmailNotificationLog EmailNotification = db.EmailNotificationLogs.Where(y => y.WaybillNo == item.waybillno).FirstOrDefault();
                            if (EmailNotification == null)
                            {
                                EmailNotificationLog Obj = new EmailNotificationLog();

                                Obj.IsDelivered_EmailSent = true;
                                Obj.IsPickup_EmailSent = null;
                                Obj.IsOFD_EmailSent = null;
                                Obj.WaybillNo = item.waybillno;
                                Obj.EmailAddress = item.CneeEmail;
                                Obj.CreatedDate = DateTime.Now;
                                Obj.LastUpdatedDate = DateTime.Now;
                                Obj.StatusID = 1;
                                db.EmailNotificationLogs.Add(Obj);
                                db.SaveChanges();

                            }
                            else
                            {
                                EmailNotification.IsDelivered_EmailSent = true;
                                EmailNotification.LastUpdatedDate = DateTime.Now;
                                db.SaveChanges();
                            }

                        }




                    }

                }
            }
            catch (Exception ex)
            {
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
            }
        }
        public static void SendEmailNotificationOFD()
        {
            try
            {
                var EmailIsSent = false;
                ERPNaqelEntities1 db = new ERPNaqelEntities1();
                List<ViwEmailNotificationOFD> viwEmailNotificationOFD = new List<ViwEmailNotificationOFD>();

                viwEmailNotificationOFD = db.ViwEmailNotificationOFDs.ToList();
                string htmlformat = "";
                string To = ConfigurationManager.AppSettings["Myemail"];


                List<ViwEmailNotificationOFD> OFDEmailList = viwEmailNotificationOFD;
                if (OFDEmailList.Count != 0)
                {

                    foreach (var item in OFDEmailList)
                    {
                        string EmailFormatLanguage = "";
                        if (item.CneeName != null)
                        {

                            if (Regex.IsMatch(item.CneeName, "^[a-zA-Z0-9_ ]"))
                            {
                                EmailFormatLanguage = "EN";
                            }
                            else
                            {
                                EmailFormatLanguage = "AR";
                            }

                        }
                        var Msg = db.Database.SqlQuery<string>("select CoreText from smssentmessage where StatusID = 1 and PurposeID in (29 ,31)  and CAST(Date as Date) = CAST(GETDATE() AS DATE) and RefNo ='" + item.waybillno + "' order by date desc").FirstOrDefault();
                        var OTP = db.Database.SqlQuery<string>(" select OTP from ViwOTPByWaybillNo where waybillno =" + item.waybillno).FirstOrDefault();

                        if (Msg == null || OTP == null)
                            continue;
                        else
                        {


                            // Size the control to fill the form with a margin
                            MatchCollection ms = Regex.Matches(Msg, @"\b(?:https?://|www\.)\S+\b");
                            string URLLink = ms[0].Value.ToString();

                            OFDEmailNotificationLog CheckEmailNotificationIsSent = db.OFDEmailNotificationLogs.Where(y => y.WaybillNo == item.waybillno).FirstOrDefault();

                            if (EmailFormatLanguage == "EN")
                            {

                                if (CheckEmailNotificationIsSent != null && CheckEmailNotificationIsSent.CreatedDate.Value.Date == DateTime.Now.Date)
                                {
                                    continue;

                                }
                                else
                                {
                                    htmlformat = OFDFormatBodyEN(item, URLLink);
                                    var newstring = htmlformat.Replace("{OTP}", OTP.ToString());
                                    htmlformat = newstring;


                                    EmailIsSent = EmailBody(htmlformat, To, item.waybillno.ToString()); // replace email with user email 
                                }

                            }
                            else
                            {
                                if (CheckEmailNotificationIsSent != null && CheckEmailNotificationIsSent.CreatedDate.Value.Date == DateTime.Now.Date)
                                {
                                    continue;

                                }
                                else
                                {
                                    htmlformat = OFDFormatEmailBodyAR(item, URLLink);
                                    var newstring = htmlformat.Replace("{OTP}", OTP.ToString());
                                    htmlformat = newstring;


                                    EmailIsSent = EmailBody(htmlformat, To, item.waybillno.ToString()); // replace email with user email 
                                }

                            }





                            if (EmailIsSent)
                            {
                                //insert log row for this waybill
                                EmailNotificationLog EmailNotification = db.EmailNotificationLogs.Where(y => y.WaybillNo == item.waybillno).FirstOrDefault();
                                if (EmailNotification == null)
                                {
                                    EmailNotificationLog Obj = new EmailNotificationLog();

                                    Obj.IsPickup_EmailSent = null;
                                    Obj.IsDelivered_EmailSent = null;
                                    Obj.IsOFD_EmailSent = true;
                                    Obj.WaybillNo = item.waybillno;
                                    Obj.EmailAddress = item.CneeEmail;
                                    Obj.CreatedDate = DateTime.Now;
                                    Obj.LastUpdatedDate = DateTime.Now;
                                    Obj.StatusID = 1;
                                    db.EmailNotificationLogs.Add(Obj);
                                    db.SaveChanges();

                                }
                                else
                                {
                                    EmailNotification.IsOFD_EmailSent = true;
                                    EmailNotification.LastUpdatedDate = DateTime.Now;
                                    db.SaveChanges();
                                }
                                // store OFD History for each waybill
                                OFDEmailNotificationLog OFDHistory = new OFDEmailNotificationLog();

                                OFDHistory.WaybillNo = item.waybillno; ;
                                OFDHistory.IsOFD_EmailSent = true;
                                OFDHistory.EmailAddress = item.CneeEmail;
                                OFDHistory.CreatedDate = DateTime.Now;
                                OFDHistory.StatusID = 1;


                                db.OFDEmailNotificationLogs.Add(OFDHistory);
                                db.SaveChanges();

                            }


                        }
                    }

                }
            }
            catch (Exception ex)
            {
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
            }
        }
        public static void SendEmailNotificationExceptions()
        {
            try
            {
                var EmailIsSent = false;
                ERPNaqelEntities1 db = new ERPNaqelEntities1();
                List<ViwEmailNotificationException> viwEmailNotificationExceptions = new List<ViwEmailNotificationException>();

                viwEmailNotificationExceptions = db.ViwEmailNotificationExceptions.ToList();
                string htmlformat = "";
                string To = ConfigurationManager.AppSettings["Myemail"];


                List<ViwEmailNotificationException> ExceptionsEmailList = viwEmailNotificationExceptions;
                if (ExceptionsEmailList.Count != 0)
                {

                    foreach (var item in ExceptionsEmailList)
                    {
                        string EmailFormatLanguage = "";
                        if (item.CneeName != null)
                        {

                            if (Regex.IsMatch(item.CneeName, "^[a-zA-Z0-9_ ]"))
                            {
                                EmailFormatLanguage = "EN";
                            }
                            else
                            {
                                EmailFormatLanguage = "AR";
                            }

                        }
                        OFDEmailNotificationLog CheckEmailNotificationIsSent = db.OFDEmailNotificationLogs.Where(y => y.WaybillNo == item.waybillno).FirstOrDefault();


                        if (EmailFormatLanguage == "EN")
                        {

                            if (CheckEmailNotificationIsSent != null && CheckEmailNotificationIsSent.CreatedDate.Value.Date == DateTime.Now.Date)
                            {
                                continue;

                            }
                            else
                            {
                                htmlformat = ExceptionsFormatBodyEN(item);
                                EmailIsSent = EmailBody(htmlformat, To, item.waybillno.ToString()); // replace email with user email 
                            }

                        }
                        else
                        {
                            if (CheckEmailNotificationIsSent != null && CheckEmailNotificationIsSent.CreatedDate.Value.Date == DateTime.Now.Date)
                            {
                                continue;

                            }
                            else
                            {
                                htmlformat = ExceptionsFormatEmailBodyAR(item);
                                EmailIsSent = EmailBody(htmlformat, To, item.waybillno.ToString()); // replace email with user email 
                            }

                        }
                       

                        if (EmailIsSent)
                        {
                            //insert log row for this waybill
                            EmailNotificationLog EmailNotification = db.EmailNotificationLogs.Where(y => y.WaybillNo == item.waybillno).FirstOrDefault();
                            if (EmailNotification == null)
                            {
                                EmailNotificationLog Obj = new EmailNotificationLog();
                                Obj.IsExceptions_EmailSent = true;
                                Obj.WaybillNo = item.waybillno;
                                Obj.EmailAddress = item.CneeEmail;
                                Obj.CreatedDate = DateTime.Now;
                                Obj.LastUpdatedDate = DateTime.Now;
                                Obj.StatusID = 1;
                                db.EmailNotificationLogs.Add(Obj);
                                db.SaveChanges();

                            }
                            else
                            {
                                EmailNotification.IsExceptions_EmailSent = true;
                                EmailNotification.LastUpdatedDate = DateTime.Now;
                                db.SaveChanges();
                            }
                            // store OFD History for each waybill
                            OFDEmailNotificationLog OFDHistory = new OFDEmailNotificationLog();

                            OFDHistory.WaybillNo = item.waybillno; ;
                            OFDHistory.IsExceptions_EmailSent = true;
                            OFDHistory.EmailAddress = item.CneeEmail;
                            OFDHistory.CreatedDate = DateTime.Now;
                            OFDHistory.StatusID = 1;


                            db.OFDEmailNotificationLogs.Add(OFDHistory);
                            db.SaveChanges();

                        }



                    }

                }
            }
            catch (Exception ex)
            {
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
            }
        }

        public static bool EmailBody(string s, string Email, string Subject)
        {
            var Email_IsSent = true;
            try
            {
                
                string MailName = ConfigurationManager.AppSettings["MailName"];
                string MailUserName = ConfigurationManager.AppSettings["MailUserName"];
                string MailPwd = ConfigurationManager.AppSettings["MailPwd"];
                string SMTPClientHost = ConfigurationManager.AppSettings["SMTPClientHost"];
                int SMTPClientPort = Int32.Parse(ConfigurationManager.AppSettings["SMTPClientPort"]);
                StringBuilder body = new StringBuilder();

                body.Append(s);


                System.Net.Mail.MailMessage mailmessage = new System.Net.Mail.MailMessage();
                DateTime dateTimenow = DateTime.Now;
                mailmessage.Subject = " Naqel Notifications " + Subject;
                mailmessage.IsBodyHtml = true;
                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(body.ToString(), null, "text/html");
                mailmessage.AlternateViews.Add(htmlView);
                string emailid = MailName;
                mailmessage.From = new System.Net.Mail.MailAddress(emailid);
                if (Email != null)
                {
                    string[] strMulti = Email.Split(';');
                    foreach (string strM in strMulti)
                        if (strM != "")
                            mailmessage.To.Add(new System.Net.Mail.MailAddress(strM.Trim()));
                }

                mailmessage.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = SMTPClientHost;
                smtp.EnableSsl = true;
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential();
                credentials.UserName = MailUserName;
                credentials.Password = MailPwd;
                smtp.UseDefaultCredentials = true;
                smtp.Credentials = credentials;
                smtp.Port = SMTPClientPort;
                try
                {
                    smtp.Send(mailmessage);
                    Email_IsSent = true;
                }
                catch (SmtpFailedRecipientException ex)
                {
                    log.Error("Email Error Message: " + ex.FailedRecipient, ex);
                    log.Error("Email Error Message: " + ex.GetBaseException(), ex);
                    Email_IsSent = false;


                }

            }

            catch (Exception ex)
            {
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
            }

            return Email_IsSent;
        }
        public static string pickedupFormatBodyEN(EmailNotificationPickup x ,string URLLink)
        {

            try
            {
                string HTMLPATH = ConfigurationManager.AppSettings["DLHTML"];
                string body = "HTML\\EmailFormat.html"; // for local 
                //string body = HTMLPATH + "\\HTML\\EmailFormat.html"; // for server
                //string body = string.Empty;
                using (StreamReader reader = new StreamReader(body))
                //using (StreamReader reader = new StreamReader(System.Web.HttpContext.Current.Server.MapPath("~/HTML/EmailFormat.html")))

                {
                    body = reader.ReadToEnd();
                }

                body = body.Replace("{CustomerName}", x.CneeName);
                body = body.Replace("{waybillno}", x.waybillno.ToString());
                body = body.Replace("{shipperName}", x.ShipperNameEN);
                body = body.Replace("{URLLink}", URLLink);

                return body;
            }

            catch (Exception ex)
            {
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
                return ex.Message.ToString();
            }


        }
        public static string PickedupFormatEmailBodyAR(EmailNotificationPickup x, string URLLink)
        {

            try
            {
                string HTMLNATH = ConfigurationManager.AppSettings["DLHTML"];
               
                string body = "HTML\\EmailFormatA.html"; // for local 
                //string body = HTMLPATH + "\\HTML\\EmailFormat.html"; // for server
                using (StreamReader reader = new StreamReader(body))

                {
                    body = reader.ReadToEnd();
                }

                body = body.Replace("{CustomerName}", x.CneeName);
                body = body.Replace("{waybillno}", x.waybillno.ToString());
                body = body.Replace("{shipperName}", x.ShipperNameAR);
                body = body.Replace("{URLLink}", URLLink);

                return body;
            }

            catch (Exception ex)
            {
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
                return ex.Message.ToString();
            }


        }
        public static string OFDFormatBodyEN(ViwEmailNotificationOFD x,string URLLink)
        {

            try
            {
                string HTMLPATH = ConfigurationManager.AppSettings["DLHTML"];
                string body = "HTML\\EmailFormatOFDE.html"; // for local 
                //string body = HTMLPATH + "\\HTML\\EmailFormat.html"; // for server
                //string body = string.Empty;
                using (StreamReader reader = new StreamReader(body))
                //using (StreamReader reader = new StreamReader(System.Web.HttpContext.Current.Server.MapPath("~/HTML/EmailFormat.html")))

                {
                    body = reader.ReadToEnd();
                }

                body = body.Replace("{CustomerName}", x.CneeName);
                body = body.Replace("{waybillno}", x.waybillno.ToString());
                body = body.Replace("{shipperName}", x.ShipperNameEN);
                body = body.Replace("{URLLink}", URLLink);

                return body;
            }

            catch (Exception ex)
            {
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
                return ex.Message.ToString();
            }


        }
        public static string OFDFormatEmailBodyAR(ViwEmailNotificationOFD x ,string URLLink)
        {

            try
            {
                string HTMLNATH = ConfigurationManager.AppSettings["DLHTML"];
                string body = "HTML\\EmailFormatOFDAR.html"; // for local 
                //string body = HTMLPATH + "\\HTML\\EmailFormat.html"; // for server
                using (StreamReader reader = new StreamReader(body))

                {
                    body = reader.ReadToEnd();
                }

                body = body.Replace("{CustomerName}", x.CneeName);
                body = body.Replace("{waybillno}", x.waybillno.ToString());
                body = body.Replace("{shipperName}", x.ShipperNameAR);
                body = body.Replace("{URLLink}", URLLink);

                return body;
            }

            catch (Exception ex)
            {
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
                return ex.Message.ToString();
            }


        }
        public static string ExceptionsFormatBodyEN(ViwEmailNotificationException x)
        {

            try
            {
                string HTMLPATH = ConfigurationManager.AppSettings["DLHTML"];
                string body = "HTML\\EmailFormatExceptionsEN.html"; // for local 
                //string body = HTMLPATH + "\\HTML\\EmailFormat.html"; // for server
                //string body = string.Empty;
                using (StreamReader reader = new StreamReader(body))
                //using (StreamReader reader = new StreamReader(System.Web.HttpContext.Current.Server.MapPath("~/HTML/EmailFormat.html")))

                {
                    body = reader.ReadToEnd();
                }

                body = body.Replace("{CustomerName}", x.CneeName);
                body = body.Replace("{waybillno}", x.waybillno.ToString());
                body = body.Replace("{shipperName}", x.ShipperNameEN);
                body = body.Replace("{URLLink}", "test");

                return body;
            }

            catch (Exception ex)
            {
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
                return ex.Message.ToString();
            }


        }
        public static string ExceptionsFormatEmailBodyAR(ViwEmailNotificationException x)
        {

            try
            {
                string HTMLNATH = ConfigurationManager.AppSettings["DLHTML"];
                string body = "HTML\\EmailFormatExceptionsAR.html"; // for local 
                //string body = HTMLPATH + "\\HTML\\EmailFormat.html"; // for server
                using (StreamReader reader = new StreamReader(body))

                {
                    body = reader.ReadToEnd();
                }

                body = body.Replace("{CustomerName}", x.CneeName);
                body = body.Replace("{waybillno}", x.waybillno.ToString());
                body = body.Replace("{shipperName}", x.ShipperNameAR);
                body = body.Replace("{URLLink}", "test");

                return body;
            }

            catch (Exception ex)
            {
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
                return ex.Message.ToString();
            }


        }
        public static string DeliveredFormatBodyEN(ViwEmailNotificationDelivered x,string URLLink)
        {

            try
            {
                string HTMLPATH = ConfigurationManager.AppSettings["DLHTML"];
                string body = "HTML\\DeliveredEmailFormatE.html"; // for local 
                //string body = HTMLPATH + "\\HTML\\EmailFormat.html"; // for server
                using (StreamReader reader = new StreamReader(body))

                {
                    body = reader.ReadToEnd();
                }

                body = body.Replace("{CustomerName}", x.CneeName);
                body = body.Replace("{waybillno}", x.waybillno.ToString());
                body = body.Replace("{shipperName}", x.ShipperNameEN);
                body = body.Replace("{URLLink}", URLLink);

                return body;
            }

            catch (Exception ex)
            {
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
                return ex.Message.ToString();
            }


        }
        public static string DeliveredFormatEmailBodyAR(ViwEmailNotificationDelivered x,string URLLink)
        {

            try
            {
                string HTMLNATH = ConfigurationManager.AppSettings["DLHTML"];
                string body = "HTML\\EmailFormatDeliveredAR.html"; // for local 
                //string body = HTMLPATH + "\\HTML\\EmailFormat.html"; // for server
                using (StreamReader reader = new StreamReader(body))

                {
                    body = reader.ReadToEnd();
                }

                body = body.Replace("{CustomerName}", x.CneeName);
                body = body.Replace("{waybillno}", x.waybillno.ToString());
                body = body.Replace("{shipperName}", x.ShipperNameAR);
                body = body.Replace("{URLLink}", URLLink);

                return body;
            }

            catch (Exception ex)
            {
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
                return ex.Message.ToString();
            }


        }


    }

}
