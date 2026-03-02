using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailNotificationNew
{
    public class VIWEmailCommunication
    {
        public int WayBillNo { get; set; }
        public string ConsigneeEmail { get; set; }
        public string CneeName { get; set; }
        public int EmployID { get; set; }
        public int PurposeID { get; set; }
        public string Balance { get; set; }
        public int ClientID { get; set; }
        public string CompanyName { get; set; }
        public DateTime? PickUpDate { get; set; }

    }
}
