using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;

namespace SubwayPro_TestApp
{
    class MapBoxInterface
    {
        private Dictionary<string, double[]> queryToGps = new Dictionary<string, double[]>();

        private double[] latLongCached = new double[] { 0, 0 };

        private static readonly HttpClient client = new HttpClient();
        private static readonly String mapBoxAccessToken = "pk.eyJ1IjoiYW5kcmV3aGFycmlzIiwiYSI6ImNqNDdvOWJxNjA5NXozM3Ayd3N6aHJwOTgifQ.IYTTSKv9I3N0ObSHC-0fjg";

        public async Task<String[]> FetchAutocompleteSuggestions(String query, int count, double latitude, double longitude)
        {
            latLongCached = new double[] { latitude, longitude };
            JObject response = await ForwardGeocodingRequest(query, 3, latitude, longitude, 2);
            if(response == null)
            {
                return new String[0];
            }
            int suggestionCount = response["features"].Count();
            String[] suggestions = new String[suggestionCount];
            for(int i = 0; i < suggestionCount; i++)
            {
                suggestions[i] = (string)response["features"][i]["place_name"];
                Console.WriteLine("SUGGESTION" + suggestions[i]);
                try
                {
                    queryToGps.Add(suggestions[i], ConvertAddressToGpsCoordinates(response, i));
                }
                catch { }
            }
            return suggestions;
        }

        public double[] ConvertAddressToGpsCoordinates(String address, double latitude, double longitude)
        {
            latLongCached = new double[] { latitude, longitude };
            if (address.Equals("Current Location"))
            {
                return latLongCached;
            }
            double[] gps;
            if(queryToGps.TryGetValue(address, out gps))
            {
                return gps;
            }
            else
            {
                throw new Exception("Address not found in dictionary.");
            }
        }

        public double[] ConvertAddressToGpsCoordinates(String address)
        {
            return ConvertAddressToGpsCoordinates(address, latLongCached[0], latLongCached[1]);
        }

        private double[] ConvertAddressToGpsCoordinates(JObject response, int index)
        {
            double lon = Double.Parse((string)response["features"][index]["center"][0]);
            double lat = Double.Parse((string)response["features"][index]["center"][1]);
            return new double[] { lat, lon };
        }

        public async Task<bool> AddressIsValid(String address)
        {
            return await AddressIsValid(address, latLongCached[0], latLongCached[1]);
        }

        public async Task<bool> AddressIsValid(String address, double latitude, double longitude)
        {
            latLongCached = new double[] { latitude, longitude };
            if(address.Equals("Current Location") || queryToGps.ContainsKey(address))
            {
                return true;
            }
            try
            {
                await FetchAutocompleteSuggestions(address, 1, latitude, longitude);
            }
            catch(Exception e)
            {

            }
            return false;
        }


        public async Task<JObject> ForwardGeocodingRequest(String query, int count, int attempts)
        {
            return await ForwardGeocodingRequest(query, count, latLongCached[0], latLongCached[1], attempts);
        }

        /**
         * Sends a forward geocoding request to MapBox, returns the raw JSON file received in the response.
         * Params:
         * - 
         * 
         * */
        public async Task<JObject> ForwardGeocodingRequest(String query, int count, double latitude, double longitude, int attempts)
        {
            while (attempts > 0)
            {
                try
                {
                    var url = "https://api.mapbox.com/geocoding/v5/mapbox.places/" + query +
                        ".json?access_token=" + mapBoxAccessToken +
                        "&country=us&bbox=-74.5038,40.426,-71.7902,41.2819&autocomplete=true&limit="
                        + count.ToString() + "&proximity=" + longitude.ToString() + "," + latitude.ToString();
                    string response = await client.GetStringAsync(url);
                    Console.WriteLine(url);
                    JObject json = JObject.Parse(response);
                    return json;
                }
                catch (Exception e)
                {
                    Console.WriteLine("GEOCODING REQUEST ERROR: " + attempts);
                    Console.WriteLine(e.Message);
                    attempts--;
                }
            }
            return null;
        }

    }
}
