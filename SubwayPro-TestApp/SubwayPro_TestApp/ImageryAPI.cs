using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SubwayPro_TestApp
{
    class ImageryAPI
    {
        private static string API_KEY = "E6KLakTcYL4PZhCVt5XBD2cM9x5VLN";

        private static string BASE_URL = "http://omnispective.subwaypro.com:1977/";

        private static readonly HttpClient client = new HttpClient();

        public ImageryAPI()
        {
            client.DefaultRequestHeaders.Add("fuck-you-hackers", API_KEY);
        }

        public class ImageQuery
        {
            public double latitude { get; set; }
            public double longitude { get; set; }
            public string stationId { get; set; }
        }

        // get all images for associated StationID
        public async Task<JObject> GetStationImages(string stationId)
        {
            string query_url = BASE_URL + "getimgs?station=" + stationId;

            try
            {
                string json = await client.GetStringAsync(query_url);
                JObject obj = JObject.Parse(json);
                return obj;
            }
            catch (Exception e)
            {
                Console.WriteLine("getStationImages QUERY FAILED: " + e.StackTrace);
            }
            return null;

        }

        // get the image URL for given Entrance ID (OpenData ObjectID)
        public async Task<JObject> GetEntranceImage(string entranceId)
        {
            string query_url = BASE_URL + "entrance/" + entranceId;

            try
            {
                string json = await client.GetStringAsync(query_url);
                JObject obj = JObject.Parse(json);
                return obj;
            }
            catch (Exception e)
            {
                Console.WriteLine("getEntranceImage QUERY FAILED: " + e.StackTrace);
            }
            return null;

        }

    }
}
