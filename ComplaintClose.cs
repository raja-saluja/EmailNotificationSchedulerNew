using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.HtmlControls;

namespace EmailNotificationNew
{
    public class ComplaintClose
    {
        private static readonly ILog log =
        LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void sendComplaintCloseNotifications()
        {
            ERPNaqelEntitiesLive en = new ERPNaqelEntitiesLive();
            var notifications = en.ViwComplaintNotifications.ToList();
            foreach (var item in notifications)
            {

                var htmlformat = ComplaintFormatBodyEN(item, "");
                bool EmailIsSent = EmailBody(htmlformat, item.EmailID, item.ComplaintID + " Complaint has been Solved with NAQEL Express");
                item.IsClosedEmailSent = true;
                ERPNaqelEntitiesLive en1 = new ERPNaqelEntitiesLive();
                var cn = en1.ComplaintNotifications.Where(x => x.Id == item.Id).FirstOrDefault();
                cn.IsClosedEmailSent = true;
                en1.ComplaintNotifications.AddOrUpdate(cn);
                en1.SaveChanges();
            }

        }

        public static string ComplaintFormatBodyEN(ViwComplaintNotification x, string URLLink)
        {

            try
            {
                string HTMLPATH = ConfigurationManager.AppSettings["DLHTML"];
                string body = "HTML\\EmailFormatComplaintE.html";
                using (StreamReader reader = new StreamReader(body))
                {
                    body = reader.ReadToEnd();
                }

                //body = body.Replace("{CustomerName}", x.CneeName);
                //body = body.Replace("{waybillno}", x.waybillno.ToString());
                body = body.Replace("{ComplaintID}", x.ComplaintID.ToString());
                body = body.Replace("{Complaint Type}", x.Name.ToString());
                body = body.Replace("{Closed Reason}", x.ClosedDescription);
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


        public static bool EmailBody(string s, string Email, string Subject)
        {
        startLoop:
            var Email_IsSent = true;
            try
            {
            Loop:
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                      | SecurityProtocolType.Tls11
                                      | SecurityProtocolType.Tls12;
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
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = credentials;
                smtp.ServicePoint.MaxIdleTime = 2;

                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Port = SMTPClientPort;
                try
                {
                    ServicePointManager.SecurityProtocol =  SecurityProtocolType.Tls12;
                    ServicePointManager.Expect100Continue = true;
                    smtp.Send(mailmessage);
                    Email_IsSent = true;
                }
                catch (SmtpFailedRecipientException ex)
                {
                    goto Loop;
                    log.Error("Email Error Message: " + ex.FailedRecipient, ex);
                    log.Error("Email Error Message: " + ex.GetBaseException(), ex);
                    Email_IsSent = false;


                }

            }

            catch (Exception ex)
            {
                goto startLoop;
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
            }

            return Email_IsSent;
        }
    }
}
