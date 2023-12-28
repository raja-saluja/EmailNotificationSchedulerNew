using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailNotificationNew.Data
{
    class ConsigneeData
    {

        public int ID { get; set; }
    public string Name { get; set; }
    public string FName { get; set; }
    public int StatusID { get; set; }
    public string Email { get; set; }
    public Nullable<int> CenterCode { get; set; }
    public Nullable<int> ClientID { get; set; }
    public Nullable<int> UserID { get; set; }
    public string VATID { get; set; }
    public string ConsigneePassportNo { get; set; }
    public string ConsigneePassportExp { get; set; }
    public string ConsigneeNationality { get; set; }
}
}
