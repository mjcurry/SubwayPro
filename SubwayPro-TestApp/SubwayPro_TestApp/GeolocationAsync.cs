using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SubwayPro_TestApp
{
    class GeolocationAsync
    {

        public static async Task<Position> UpdateLocation()
        {
            var locator = CrossGeolocator.Current;
            locator.DesiredAccuracy = 50;

            locator.AllowsBackgroundUpdates = true;

            var position = await locator.GetPositionAsync(10000);
            Console.WriteLine("Position: " + position.Latitude + ", " + position.Longitude);
            return position;
        }

    }
}
