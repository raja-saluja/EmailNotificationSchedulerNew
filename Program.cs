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
                //SendEmailNotificationPickup();

                //SendEmailNotificationDelivered();

                //SendEmailNotificationOFD();

                //SendEmailNotificationExceptions();

                ComplaintClose.sendComplaintCloseNotifications();
                SendEmailWhenPhoneIsEmpty();
            }
            catch (Exception ex)
            {
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
            }

        }

        public static void SendEmailWhenPhoneIsEmpty()
        {
            try
            {
                Console.WriteLine("Send Email When Phone Is Empty job start");
                var EmailIsSent = false;
                ERPNaqelEntitiesLive db = new ERPNaqelEntitiesLive(); // live server

                string htmlformat = "";
                //string To = ""; // test email

                //var VIWEmailCommunicationData = db.Database.SqlQuery<VIWEmailCommunication>("SELECT WayBillNo, ConsigneeEmail, CneeName, PickUpDate, EmployID, PurposeID, Balance,ClientID,CompanyName FROM VIWEmailCommunicationYUNEXPRESS
                //WHERE WayBillNo = 395005710 or WayBillNo = 395213270 or  WayBillNo = 404054051").ToList(); // this is for testing
                var VIWEmailCommunicationData = db.Database.SqlQuery<VIWEmailCommunication>("SELECT WayBillNo, ConsigneeEmail, CneeName, PickUpDate, EmployID, PurposeID, Balance,ClientID,CompanyName FROM VIWEmailCommunicationYUNEXPRESS ORDER BY PurposeID").ToList();


                if (VIWEmailCommunicationData.Count != 0)
                {
                    Console.WriteLine("Data Exists");

                    foreach (var item in VIWEmailCommunicationData)
                    {
                        Console.WriteLine("entered loop");

                        var alreadySent = db.Database.SqlQuery<int>(
                        @"SELECT COUNT(1) 
                        FROM CustomerEmailCommunicationLog
                        WHERE WayBillNo = @p0",
                        item.WayBillNo
                        ).FirstOrDefault();

                        if (alreadySent > 0)
                        {
                            Console.WriteLine($"Email already sent for WayBillNo {item.WayBillNo}, skipping.");
                            continue;
                        }

                        Console.WriteLine(item);
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
                        //var Msg24 = db.Database.SqlQuery<string>("select CoreText from smssentmessage where StatusID = 1 and PurposeID = 24 and RefNo='" + item.WayBillNo + "' order by date desc").FirstOrDefault();
                        //var Msg26 = db.Database.SqlQuery<string>("select CoreText from smssentmessage where StatusID = 1 and PurposeID = 26 and RefNo='" + item.WayBillNo + "' order by date desc").FirstOrDefault();
                        string URLLink;
                        if (item.PurposeID == 24)
                        {
                            URLLink = "https://infotrackmain.naqelksa.com/SMS/Pickup/Pickupsms/" +
                                item.EmployID + "|" +
                                item.PurposeID + "|" +
                                item.Balance;
                        }
                        else
                        {
                            URLLink = "https://infotrackmain.naqelksa.com/PLSMS/DropOff/GeneralPickup/" +
                                item.EmployID + "|" +
                                item.PurposeID + "|" +
                                item.Balance +
                                "/CollectFrom/1";
                        }

                        try
                        {
                            var ShortURLGenerator = new GenerateShortURL();
                            string shortUrl = ShortURLGenerator.GetWaybillShortLink(item.WayBillNo, "pickup");
                            if (string.IsNullOrEmpty(shortUrl))
                            {
                                Console.WriteLine("API failed or returned error, Full URL Was Sent");
                            }
                            else
                            {
                                URLLink = shortUrl;
                            }
                            Console.WriteLine("Final URL Sent: " + URLLink);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error, URL was not shortened" + ex.Message);
                        }

                        if (EmailFormatLanguage == "EN")
                        {
                            htmlformat = EmailWhenPhoneIsEmptyFormatBodyEN(item ,URLLink);
                            //EmailIsSent = EmailBody(htmlformat, To, "Update Address"); // testing
                            EmailIsSent = EmailBody(htmlformat, item.ConsigneeEmail, "Update Address");
                        }
                        else
                        {
                            htmlformat = EmailWhenPhoneIsEmptyFormatBodyAR(item, URLLink);
                            //EmailIsSent = EmailBody(htmlformat, To, "تحديث العنوان"); // testing
                            EmailIsSent = EmailBody(htmlformat, item.ConsigneeEmail, "تحديث العنوان");
                        }

                        if (EmailIsSent)
                        {
                            Console.WriteLine("Sent");
                            db.Database.ExecuteSqlCommand(
                            @"INSERT INTO CustomerEmailCommunicationLog 
                            (WayBillNo, ClientID, ClientName, ToEmail, SentTime)
                            VALUES (@p0, @p1, @p2, @p3, @p4)",
                            item.WayBillNo,
                            item.ClientID,                 
                            item.CompanyName,                 
                            item.ConsigneeEmail,
                            DateTime.Now
                            );
                        }
                        else
                        {
                            Console.WriteLine("Not Sent");

                        }

                    }

                }
                else
                {
                    Console.WriteLine("No Data Exists");

                }

            }
            catch (Exception ex)
            {
                log.Error("Error Message: " + ex.Message.ToString(), ex);
                log.Error("Error Message: " + ex.StackTrace);
                log.Error("Error Message: " + ex.InnerException);
                Console.WriteLine("Error Message: " + ex.Message.ToString(), ex);
                Console.WriteLine("Error Message: " + ex.StackTrace);
                Console.WriteLine("Error Message: " + ex.InnerException);

            }
        }

        public static void SendEmailNotificationPickup()
        {
            try
            {
                Console.WriteLine(" pick up job start");
                var EmailIsSent = false;
                ERPNaqelEntities1 db = new ERPNaqelEntities1();
                //ERPNaqelEntitiesLive db1 = new ERPNaqelEntitiesLive();
                //var SQLData = db1.Database.SqlQuery<EmailNotificationPickup>("select w.waybillno , c.Email As CneeEmail , c.Name As CneeName , cl.Name As ShipperNameEN ,cl.FName As ShipperNameAR  from waybill w WITH(NOLOCK)  left join pickup p WITH(NOLOCK) on p.WaybillNo = w.WayBillNo  left join Consignee c WITH(NOLOCK) on c.ID =  w.ConsigneeID  left join Client cl WITH(NOLOCK) on w.ClientID = cl.ID  left join EmailNotificationLog EN WITH(NOLOCK) on EN.WayBillNo = w.WayBillNo  where ltrim(rtrim(c.Email)) != ''  and c.Email != '0'  and c.Email LIKE '%[^0-9]%'  and c.Email  LIKE '%@%'  and w.LoadTypeID not in (66,136,204)  and w.IsCancelled = 0  and w.IsRTO = 0  and EN.IsPickup_EmailSent is Null  and  CAST(p.TimeIn as Date) = CAST(GETDATE()-8 AS DATE)  and w.WayBillNo in(271420433, 271420476,271420475,271420474 ,271420555,271420558,271420556,271420557,271420587,271420588,271420594,271420595,271420596,271420597)").ToList();

                List<EmailNotificationPickup> viwEmailNotificationPickups = new List<EmailNotificationPickup>();

                viwEmailNotificationPickups = db.EmailNotificationPickups.ToList();
                string htmlformat = "";
                string To = ConfigurationManager.AppSettings["Myemail"];


                List<EmailNotificationPickup> PickupEmailList = viwEmailNotificationPickups;
                if (PickupEmailList.Count != 0)
                {
                   
                    foreach (var item in PickupEmailList)
                    {
                        Console.WriteLine(item);
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
                            EmailIsSent =  EmailBody(htmlformat, item.CneeEmail, item.waybillno.ToString()); // replace TO email with item.CneeEmail 
                        }
                        else
                        {
                            htmlformat = PickedupFormatEmailBodyAR(item, URLLink);
                            EmailIsSent = EmailBody(htmlformat, item.CneeEmail, item.waybillno.ToString()); // replace TO email with item.CneeEmail 
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
                Console.WriteLine("Delivered job start");

                var EmailIsSent = false;
                ERPNaqelEntities1 db = new ERPNaqelEntities1();
                //ERPNaqelEntitiesLive db1 = new ERPNaqelEntitiesLive();

                List<ViwEmailNotificationDelivered> viwEmailNotificationDelivered = new List<ViwEmailNotificationDelivered>();

                viwEmailNotificationDelivered = db.ViwEmailNotificationDelivereds.ToList();
                string htmlformat = "";
                string To = ConfigurationManager.AppSettings["Myemail"];
                //var SQLData = db1.Database.SqlQuery<ViwEmailNotificationDelivered>("select w.waybillno , c.Email As CneeEmail , c.Name As CneeName , cl.Name As ShipperNameEN ,cl.FName As ShipperNameAR  from waybill w WITH(NOLOCK)  left join delivery D WITH(NOLOCK) on D.WaybillID = w.id  left join Consignee c WITH(NOLOCK) on c.ID =  w.ConsigneeID  left join Client cl WITH(NOLOCK) on w.ClientID = cl.ID  left join EmailNotificationLog EN WITH(NOLOCK) on EN.WayBillNo = w.WayBillNo  where ltrim(rtrim(c.Email)) != ''  and c.Email != '0'  and c.Email LIKE '%[^0-9]%'  and c.Email  LIKE '%@%'  and w.LoadTypeID not in (66,136,204)  and D.DeliveryStatusID =   5 and D.StatusID = 1  and w.IsCancelled = 0  and EN.IsDelivered_EmailSent is Null  and  CAST(D.DeliveryDate as Date) = CAST(GETDATE() AS DATE)  and w.WayBillNo in( 271420476,271420475,271420474,271420555,271420558,271420556,271420557,271420587,271420588,,271420594,271420595,271420596,271420597)").ToList();


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
                        string URLLink = getRatingURL(Convert.ToInt32(item.waybillno));


                        //var Msg = db1.Database.SqlQuery<string>("select CoreText from smssentmessage where StatusID = 1 and PurposeID = 8 and RefNo='" + item.waybillno + "' order by date desc").FirstOrDefault();
                        //// Size the control to fill the form with a margin
                        //MatchCollection ms = Regex.Matches(Msg, @"\b(?:https?://|www\.)\S+\b");
                        //string URLLink = ms[0].Value.ToString();

                        if (EmailFormatLanguage == "EN")
                        {
                            htmlformat = DeliveredFormatBodyEN(item, URLLink);

                            EmailIsSent =  EmailBody(htmlformat, item.CneeEmail, item.waybillno.ToString()); // replace email with user email 
                        }
                        else
                        {
                            htmlformat = DeliveredFormatEmailBodyAR(item, URLLink);

                            EmailIsSent =  EmailBody(htmlformat, item.CneeEmail, item.waybillno.ToString()); // replace email with user email 
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
                Console.WriteLine("OFD job start");

                var EmailIsSent = false;
                ERPNaqelEntities1 db = new ERPNaqelEntities1();
                //ERPNaqelEntitiesLive db1 = new ERPNaqelEntitiesLive();

                List<ViwEmailNotificationOFD> viwEmailNotificationOFD = new List<ViwEmailNotificationOFD>();

                viwEmailNotificationOFD = db.ViwEmailNotificationOFDs.ToList();
                string htmlformat = "";
                string To = ConfigurationManager.AppSettings["Myemail"];
                //var SQLData = db1.Database.SqlQuery<ViwEmailNotificationOFD>("select w.waybillno , c.Email As CneeEmail , c.Name As CneeName , cl.Name As ShipperNameEN ,cl.FName As ShipperNameAR  from waybill w WITH(NOLOCK)  left join Tracking T WITH(NOLOCK) on T.WaybillNo = w.WayBillNo  left join Consignee c WITH(NOLOCK) on c.ID =  w.ConsigneeID  left join Client cl WITH(NOLOCK) on w.ClientID = cl.ID   where ltrim(rtrim(c.Email)) != ''  and c.Email != '0'  and c.Email LIKE '%[^0-9]%'  and c.Email  LIKE '%@%'  and w.LoadTypeID not in (66,136,204)  and T.TrackingTypeID =  5  and T.StatusID = 1  and w.IsDelivered = 0  and w.IsRTO = 0  and  CAST(T.Date as Date) = CAST(GETDATE() AS DATE)  and w.WayBillNo in( 271420476,271420475,271420474,271420555,271420558,271420556,271420557,271420587,271420588,271420594,271420595,271420596,271420597,271420784)").ToList();


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


                                    EmailIsSent = EmailBody(htmlformat, item.CneeEmail, item.waybillno.ToString()); // replace email with user email 
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


                                    EmailIsSent = EmailBody(htmlformat, item.CneeEmail, item.waybillno.ToString()); // replace email with user email 
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
                Console.WriteLine(" Exceptions job start");

                var EmailIsSent = false;
                ERPNaqelEntities1 db = new ERPNaqelEntities1();
                //ERPNaqelEntitiesLive db1 = new ERPNaqelEntitiesLive();

                List<ViwEmailNotificationException> viwEmailNotificationExceptions = new List<ViwEmailNotificationException>();

                viwEmailNotificationExceptions = db.ViwEmailNotificationExceptions.ToList();
                string htmlformat = "";
                string To = ConfigurationManager.AppSettings["Myemail"];
                //var SQLData = db1.Database.SqlQuery<ViwEmailNotificationException>("  select w.waybillno , c.Email As CneeEmail , c.Name As CneeName , cl.Name As ShipperNameEN ,cl.FName As ShipperNameAR  from delivery D WITH(NOLOCK)  inner join DeliveryStatus DS WITH(NOLOCK) on D.DeliveryStatusID =DS.id  left join Waybill W WITH(NOLOCK) on D.WaybillID = W.ID  left join Consignee c WITH(NOLOCK) on c.ID =  w.ConsigneeID  left join Client cl WITH(NOLOCK) on w.ClientID = cl.ID  where  DS.id in (8 ,17 , 27, 35 , 36 , 37 , 133,176 ,174 )  and w.LoadTypeID not in (66,136,204)  and w.IsRTO = 0  and ltrim(rtrim(c.Email)) != ''  and c.Email != '0'  and c.Email LIKE '%[^0-9]%'  and c.Email  LIKE '%@%'  and w.IsCancelled = 0  and w.IsDelivered = 0  and  CAST(D.InTime as Date) = CAST(GETDATE() AS DATE)  and w.WayBillNo in( 271420476,271420475,271420474,271420555,271420558,271420556,271420557,271420587,271420588,271420594,271420595,271420596,271420597)").ToList();


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
                        var Msg = db.Database.SqlQuery<string>("select CoreText from smssentmessage where StatusID = 1 and PurposeID in (29 ,31) and RefNo ='" + item.waybillno + "' order by date desc").FirstOrDefault();

                        if (Msg == null)
                            continue;
                        else
                        {


                            // Size the control to fill the form with a margin
                            MatchCollection ms = Regex.Matches(Msg, @"\b(?:https?://|www\.)\S+\b");
                            string URLLink = ms[0].Value.ToString();

                            OFDEmailNotificationLog CheckEmailNotificationIsSent = db.OFDEmailNotificationLogs.Where(y => y.WaybillNo == item.waybillno && y.IsExceptions_EmailSent == true).OrderBy(x => x.CreatedDate).FirstOrDefault();


                            if (EmailFormatLanguage == "EN")
                            {

                                if (CheckEmailNotificationIsSent != null && CheckEmailNotificationIsSent.CreatedDate.Value.Date == DateTime.Now.Date)
                                {
                                    continue;

                                }
                                else
                                {
                                    htmlformat = ExceptionsFormatBodyEN(item, URLLink);
                                    EmailIsSent = EmailBody(htmlformat, item.CneeEmail, item.waybillno.ToString()); // replace email with user email 
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
                                    htmlformat = ExceptionsFormatEmailBodyAR(item, URLLink);
                                    EmailIsSent = EmailBody(htmlformat, item.CneeEmail, item.waybillno.ToString()); // replace email with user email 
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
        public static string EmailWhenPhoneIsEmptyFormatBodyEN(VIWEmailCommunication x, string URLLink)//EmailNotificationPickup x, string URLLink)
        {

            try
            {
                string HTMLPATH = ConfigurationManager.AppSettings["DLHTML"];
                string body = "HTML\\Delivery_Confirm.html";
                //string body = HTMLPATH + "\\HTML\\EmailFormat.html"; // for server
                //string body = string.Empty;

                using (StreamReader reader = new StreamReader(body))
                //using (StreamReader reader = new StreamReader(System.Web.HttpContext.Current.Server.MapPath("~/HTML/EmailFormat.html")))

                {
                    body = reader.ReadToEnd();
                }

                body = body.Replace("{CustomerName}", x.CneeName);
                body = body.Replace("{Company}", x.CompanyName);
                body = body.Replace("{ShipmentNumber}", x.WayBillNo.ToString());
                body = body.Replace("{DeliveryLink}", URLLink);

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
        public static string EmailWhenPhoneIsEmptyFormatBodyAR(VIWEmailCommunication x, string URLLink)//EmailNotificationPickup x, string URLLink)
        {

            try
            {
                string HTMLPATH = ConfigurationManager.AppSettings["DLHTML"];
                string body = "HTML\\Delivery_Confirm_ar.html";
                //string body = HTMLPATH + "\\HTML\\EmailFormat.html"; // for server
                //string body = string.Empty;
                using (StreamReader reader = new StreamReader(body))
                //using (StreamReader reader = new StreamReader(System.Web.HttpContext.Current.Server.MapPath("~/HTML/EmailFormat.html")))

                {
                    body = reader.ReadToEnd();
                }

                body = body.Replace("{CustomerName}", x.CneeName);
                body = body.Replace("{Company}", x.CompanyName);
                body = body.Replace("{ShipmentNumber}", x.WayBillNo.ToString());
                body = body.Replace("{DeliveryLink}", URLLink);

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
        public static string pickedupFormatBodyEN(EmailNotificationPickup x ,string URLLink)
        {

            try
            {
                string HTMLPATH = ConfigurationManager.AppSettings["DLHTML"];
                string body = "HTML\\EmailFormat.html";  
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
               
                string body = "HTML\\EmailFormatA.html";
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
                string body = "HTML\\EmailFormatOFDE.html"; 
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
                string body = "HTML\\EmailFormatOFDAR.html";
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
        public static string ExceptionsFormatBodyEN(ViwEmailNotificationException x, string URLLink)
        {

            try
            {
                string HTMLPATH = ConfigurationManager.AppSettings["DLHTML"];
                string body = "HTML\\EmailFormatExceptionsEN.html";
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
        public static string ExceptionsFormatEmailBodyAR(ViwEmailNotificationException x, string URLLink)
        {

            try
            {
                string HTMLNATH = ConfigurationManager.AppSettings["DLHTML"];
                string body = "HTML\\EmailFormatExceptionsAR.html"; 
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
        public static string DeliveredFormatBodyEN(ViwEmailNotificationDelivered x,string URLLink)
        {

            try
            {
                string HTMLPATH = ConfigurationManager.AppSettings["DLHTML"];
                string body = "HTML\\DeliveredEmailFormatE.html"; 
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
                string body = "HTML\\EmailFormatDeliveredAR.html";
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
        public static string getRatingURL(int WaybillNo)
        {
            string SMSURL = "";
            ERPNaqelEntitiesLive db = new ERPNaqelEntitiesLive();


            try
            {
                var data = db.Database.SqlQuery<ViwRateCheck>("select ID,WaybillNo,DeliveryID,EmployID,Balance from ViwRateCheck where Waybillno=" + WaybillNo).ToList();
                if (data.Count > 0)
                {
                    if (data.FirstOrDefault().EmployID == 0)
                    {
                        string Balance = Get8Digits();
                        SMSSentMessage sMSSentMessage = new SMSSentMessage();
                        sMSSentMessage.Balance = "1";
                        sMSSentMessage.RefNo = Convert.ToString(WaybillNo);
                        sMSSentMessage.StatusID = 3;
                        sMSSentMessage.Date = DateTime.Now;
                        sMSSentMessage.MobileNo = "";
                        sMSSentMessage.Message = "hi";
                        sMSSentMessage.EmployID = (int)data.FirstOrDefault().DeliveryID;
                        sMSSentMessage.SMSSendingStatusID = 101;
                        sMSSentMessage.PurposeID = 8;
                        db.SMSSentMessages.Add(sMSSentMessage);
                        db.SaveChanges();
                        //SMSURL = "https://infotrack.naqelexpress.com/GPS/CBU/Home/SMS/" + data.FirstOrDefault().DeliveryID + "|" + Convert.ToInt32(8) + "|" + "1";
                        SMSURL = "https://infotrackmain.naqelksa.com/SMS/Home/RatingNew/" + data.FirstOrDefault().DeliveryID + "|" + Convert.ToInt32(8) + "|" + "1";
                    }
                    else
                    {
                        //SMSURL = "https://infotrack.naqelexpress.com/GPS/CBU/Home/SMS/" + data.FirstOrDefault().EmployID + "|" + Convert.ToInt32(8) + "|" + data.FirstOrDefault().Balance;
                        SMSURL = "https://infotrackmain.naqelksa.com/SMS/Home/RatingNew/" + data.FirstOrDefault().EmployID + "|" + Convert.ToInt32(8) + "|" + data.FirstOrDefault().Balance;
                    }
                    SMSURL = MakeTinyUrl(SMSURL);
                }

                
            }
            catch (Exception ex)
            {

            }

            return SMSURL;
        }
        public static string Get8Digits()
        {
            var bytes = new byte[4];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            uint random = BitConverter.ToUInt32(bytes, 0) % 100000000;
            return String.Format("{0:D8}", random);
        }
        public static string MakeTinyUrl(string Url)
        {
            try
            {
                if (Url.Length <= 12)
                {
                    return Url;
                }
                if (!Url.ToLower().StartsWith("http") && !Url.ToLower().StartsWith("ftp"))
                {
                    Url = "https://" + Url;
                }
                var request = WebRequest.Create("http://tinyurl.com/api-create.php?url=" + Url);
                var res = request.GetResponse();
                string text;
                using (var reader = new StreamReader(res.GetResponseStream()))
                {
                    text = reader.ReadToEnd();
                }
                return text;
            }
            catch (Exception)
            {
                return Url;
            }
        }



    }

}
