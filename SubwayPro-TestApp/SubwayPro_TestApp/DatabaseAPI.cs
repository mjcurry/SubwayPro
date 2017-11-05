using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace SubwayPro_TestApp
{
    class DatabaseAPI
    {
        private static string API_KEY = "fb7qLxpn3sbZMB3gNYjYG4Lk8dUXvB";

        private static string BASE_URL = "https://api.subwaypro.com/";


        private static readonly HttpClient client = new HttpClient();

        public class device
        {
            public string deviceId { get; set; }
            public string deviceOS { get; set; }
            public string deviceOSVersion { get; set; }
        }

        public DatabaseAPI()
        {
            client.DefaultRequestHeaders.Add("fuck-you-hackers", API_KEY);
        }

        // see if a device is registered in the database
        // if device is registered all currently stored data about device is returned
        public async Task<JObject> GetRegistrationInfo(string deviceId)
        {
            string query_url = BASE_URL + "user?device_id=" + deviceId;
            try
            {
                string json = await client.GetStringAsync(query_url);
                JObject obj = JObject.Parse(json);
                return obj;
            }
            catch (Exception e)
            {
                Console.WriteLine("getRegistrationInfo QUERY FAILED: " + e.StackTrace);
            }
            return null;

        }
        
        // register a new device using a unique device identifier and OS information
        public async Task<JObject> RegisterNewDevice(string deviceId, string deviceOS, string deviceOSVersion)
        {

            string query_url = BASE_URL + "user/" + deviceId;

            device d = new device { deviceId = deviceId, deviceOS = deviceOS, deviceOSVersion = deviceOSVersion };

            var jsonRequest = JsonConvert.SerializeObject(d);

            try
            {
                var json = await client.PutAsync(query_url, new StringContent(jsonRequest, Encoding.UTF8, "application/json"));
                JObject obj = JObject.Parse(json.Content.ReadAsStringAsync().Result);
                return obj;
            }
            catch (Exception e)
            {
                Console.WriteLine("registerNewDevice QUERY FAILED: " + e.StackTrace);
            }

            return null;
        }
    }
}
