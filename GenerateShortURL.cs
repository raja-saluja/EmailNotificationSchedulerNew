using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Configuration;

namespace EmailNotificationNew
{
    internal class GenerateShortURL
    {
        private readonly HttpClient _httpClient;
        private string ApiUrl;

        public GenerateShortURL()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            _httpClient = new HttpClient();
            //ApiUrl = "https://cs.naqelksa.com/generate/"; // Fixed URL
            //ApiUrl = GlobalVarCommon.GV.GetSystemVariables("SMSShortURLApi");
            using (var db = new ERPNaqelEntitiesLive())
            {
                ApiUrl = db.Database.SqlQuery<string>("SELECT TOP 1 VariableValue FROM SystemVariables WHERE VariableKey LIKE '%SMSShortURLApi%'").FirstOrDefault();

                if (string.IsNullOrEmpty(ApiUrl))
                    ApiUrl = "https://cs.naqelksa.com/generate/";
            }
        }

        /// Calls the API to get the short URL for a waybill.
        /// Returns empty string if API fails or isError == true.
        /// Logs failures to ShortURLSMSFail table (only if it fails).
        public string GetWaybillShortLink(int waybillNo, string type)
        {
            if (waybillNo <= 0 || string.IsNullOrEmpty(type))
                return "";

            try
            {
                var payload = new
                {
                    waybillno = waybillNo,
                    type = type
                };

                string json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = _httpClient.PostAsync(ApiUrl, content)
                                          .GetAwaiter()
                                          .GetResult();

                string respString = response.Content.ReadAsStringAsync()
                                                  .GetAwaiter()
                                                  .GetResult();

                // HTTP failure
                if (!response.IsSuccessStatusCode)
                {
                    InsertShortUrlLog(waybillNo, type, respString, (int)response.StatusCode); // keep full response
                    return "";
                }

                var result = JsonConvert.DeserializeObject<WaybillLinkResponse>(respString);

                // API failure
                if (result == null || result.isError)
                {
                    InsertShortUrlLog(waybillNo, type, respString, (int)response.StatusCode); // usually 200
                    return "";
                }

                //Success
                return result.shortlink;
            }
            catch (Exception ex)
            {
                // Exception (no HTTP status)
                InsertShortUrlLog(waybillNo, type, ex.ToString(), null);
                return "";
            }
        }

        /// Inserts a failure log into ShortURLSMSFail table.
        private void InsertShortUrlLog(int waybillNo, string type, string shipmentMessageLog, int? statusCode)
        {
            try
            {
                using (var db = new ERPNaqelEntitiesLive())
                {
                    db.Database.ExecuteSqlCommand(
                        @"INSERT INTO ShortURLSMSFail 
                        (WayBillNo, Type, ShipmentMessageLog, ResponseStatusCode)
                        VALUES (@p0, @p1, @p2, @p3)",
                        waybillNo,
                        type,
                        string.IsNullOrEmpty(shipmentMessageLog) ? null :
                            (shipmentMessageLog.Length > 4000 ? shipmentMessageLog.Substring(0, 4000) : shipmentMessageLog),
                        (object)statusCode ?? DBNull.Value
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log ShortURLSMSFail: {ex.Message}");
            }
        }

    }

    //response class
    public class WaybillLinkResponse
    {
        public bool isError { get; set; }
        public string shortlink { get; set; }
        public string fulllink { get; set; }
    }

}
