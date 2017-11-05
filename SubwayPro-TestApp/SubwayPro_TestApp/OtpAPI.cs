using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using Polylines;

namespace SubwayPro_TestApp
{

    class OtpAPI
    {
        private static string BaseURL = "http://routing.subwaypro.com/otp/routers/default/";
        private static readonly HttpClient client = new HttpClient();

        // Queries the OpenTripPlanner API to get a list of directions
        // Takes from & to coordiantes and a Maximum Walking distance in meters
        public static async Task<JObject> Query(double[] fromPlace, double[] toPlace, double maxWalkDistance)
        {

            string query_url = BaseURL + "plan?fromPlace=" + fromPlace[0].ToString() + "%2C" + fromPlace[1].ToString() + "&toPlace=" +
                toPlace[0].ToString() + "%2C" + toPlace[1].ToString() + "&mode=TRANSIT%2CWALK&maxWalkDistance=" + maxWalkDistance.ToString() +
                "&arriveBy=false&wheelchair=false" +"&locale=en";
            Console.Write(query_url);
            try
            {
                string json = await client.GetStringAsync(query_url);
                JObject obj = JObject.Parse(json);
                return obj;
            }
            catch (Exception e)
            {
                Console.WriteLine("QUERY FAILED: " + e.StackTrace);
            }
            return null;

        }

        //Query function with default max walk distance
        public static async Task<JObject> Query(double[] fromPlace, double[] toPlace)
        {
            return await Query(fromPlace, toPlace, 900);
        }

        public static String[] getLegPolylines(JObject response, int itineraryIndex)
        {
            var legs = response["plan"]["itineraries"][itineraryIndex]["legs"];
            String[] polylines = new String[legs.Count()];
            for (int i = 0; i < legs.Count(); i++)
            {
                polylines[i] = (string)legs[i]["legGeometry"]["points"];
                Console.WriteLine(polylines[i]);
            }
            return polylines;
        }

        public static List<double[]> ConvertLegPolylinesToPointSequence(String[] polylines)
        {
            List<double[]> ret = new List<double[]>();
            foreach (String polyline in polylines) {
                foreach (var decodedPoint in Polyline.DecodePolyline(polyline))
                {
                    ret.Add(new double[] { decodedPoint.Latitude, decodedPoint.Longitude });
                }
            }
            return ret;
        }

        public static List<List<double[]>> GetRoutePoints(JObject jobj)
        {
            List<List<double[]>> routes = new List<List<double[]>>();
            //If the response object is null, or if there is an error indication in it, return a blank set of routes.
            if (jobj == null || jobj["error"] != null)
            {
                return routes;
            }
            for (int i = 0; i < jobj["plan"]["itineraries"].Count(); i++)
            {
                String[] polylineStrings = getLegPolylines(jobj, i);
                List<double[]> pointSequence = ConvertLegPolylinesToPointSequence(polylineStrings);
                routes.Add(pointSequence);
            }
            return routes;
        }

        public static Plan ParsePlan(JToken plan)
        {
            var date = (long)plan["date"];
            var from = ParseStop(plan["from"]);
            var to = ParseStop(plan["to"]);
            var itineraries = new Itinerary[plan["itineraries"].Count()];
            for(int i = 0; i < plan["itineraries"].Count(); i++)
            {
                Console.WriteLine("Parsing itinerary " + i + "/" + itineraries.Count());
                itineraries[i] = ParseItinerary(plan["itineraries"][i]);
            }
            Console.WriteLine("Plan parsed");
            return new Plan(date, from, to, itineraries);
        }

        public static Itinerary ParseItinerary(JToken itinerary)
        {
            int duration;
            Console.WriteLine("DURATION: " + itinerary["duration"]);
            if (itinerary["duration"] != null)
            {
                duration = (int)itinerary["duration"];
            }
            else
            {
                duration = -1;
            }

            long start;
            if(itinerary["start"] != null)
            {
                start = (long)itinerary["start"];
            }
            else
            {
                start = -1;
            }

            long end;
            if(itinerary["end"] != null)
            {
                end = (long)itinerary["end"];
            }
            else
            {
                end = -1;
            }

            int walkTime;
            if(itinerary["walkTime"] != null)
            {
                walkTime = (int)itinerary["walkTime"];
            }
            else
            {
                walkTime = -1;
            }

            int transitTime;
            if(itinerary["transitTime"] != null)
            {
                transitTime = (int)itinerary["transitTime"];
            }
            else
            {
                transitTime = -1;
            }

            int waitTime;
            if(itinerary["waitingTime"] != null)
            {
                waitTime = (int)itinerary["waitingTime"];
            }
            else
            {
                waitTime = -1;
            }

            double walkDistance;
            if(itinerary["walkDistance"] != null)
            {
                walkDistance = (double)itinerary["walkDistance"];
            }
            else
            {
                walkDistance = -1;
            }

            int transfers;
            if(itinerary["transfers"] != null)
            {
                transfers = (int)itinerary["transfers"];
            }
            else
            {
                transfers = -1;
            }

            Leg[] legs;
            if (itinerary["legs"] != null)
            {
                legs = new Leg[itinerary["legs"].Count()];
                //Parses each leg into a list
                for (int i = 0; i < itinerary["legs"].Count(); i++)
                {
                    Console.WriteLine("Parsing leg " + i + "/" + legs.Count());
                    legs[i] = ParseLeg(itinerary["legs"][i]);
                }
            }
            else
            {
                legs = new Leg[0];
            }
            Console.WriteLine("Itinerary Parsed");
            return new Itinerary(duration, start, end, walkTime, transitTime, waitTime, walkDistance, transfers, legs);
        }

        public static Leg ParseLeg(JToken leg)
        {
            //Gets the mode of transportation
            var mode = (String)leg["mode"];
            //Gets the duration
            var duration = (double)leg["duration"];
            //Gets the polyline representing the geometry
            var polyline = (String)leg["legGeometry"]["points"];
            var from = ParseStop(leg["from"]);
            var to = ParseStop(leg["to"]);
            //Initializes an array to store the resultes
            Step[] steps = new Step[leg["steps"].Count()];
            //Iterates through the steps, adding them to the array
            for (int i = 0; i < leg["steps"].Count(); i++)
            {
                steps[i] = ParseStep(leg["steps"][i]);
            }
            var route = (String)leg["route"];
            Console.WriteLine("Leg parsed");
            return new Leg(mode, duration, polyline, from, to, steps, route);
        }

        public static Step ParseStep(JToken step)
        {
            double distance = (double)step["distance"];
            var relativeDirection = (string)step["relativeDirection"];
            var absoluteDirection = (string)step["absoluteDirection"];
            String streetName = (String)step["streetName"];
            double[] latLong = new double[] { (double)step["lat"], (double)step["lon"] };

            return new Step(distance, relativeDirection, absoluteDirection, streetName, latLong);
        }

        public static Stop ParseStop(JToken stop)
        {
            //Gets the stop name
            var name = (String)stop["name"];
            //Gets the arrival and departure times, uses -1 in the case that there is no appropriate time (for first and last nodes)
            long arrival = -1;
            if (stop["arrival"] != null)
            {
                arrival = (long)stop["arrival"];
            }
            long departure = -1;
            if (stop["departure"] != null)
            {
                departure = (long)stop["departure"];
            }
            //Gets the coordinates of the stop
            var lat = (double)stop["lat"];
            var lon = (double)stop["lon"];
            var latLon = new double[] { lat, lon };
            Console.WriteLine("Stop parsed");
            return new Stop(name, latLon, arrival, departure);
        }

    }

    class Itinerary
    {
        public int Duration { get; }
        public long Start { get; }
        public long End { get; }
        public int WalkTime { get; }
        public int TransitTime { get; }
        public int WaitTime { get; }
        public double WalkDistance { get; }
        public int Transfers { get; }
        public Leg[] Legs { get; }
        
        public Itinerary(int duration, long start, long end, int walkTime, int transitTime, int waitTime, double walkDistance, int transfers, Leg[] legs)
        {
            Duration = duration;
            Start = start;
            End = end;
            WalkDistance = walkDistance;
            WalkTime = walkTime;
            Transfers = transfers;
            TransitTime = transitTime;
            Legs = legs;
            WaitTime = waitTime;
        }
    }

    class Plan
    {
        public long Date { get; }
        public Stop From { get; }
        public Stop To { get; }
        public Itinerary[] Itineraries { get; }

        public Plan(long date, Stop from, Stop to, Itinerary[] itineraries)
        {
            Date = date;
            From = from;
            To = to;
            Itineraries = itineraries;
        }
    }



    class Leg
    {
        public String Mode { get; }
        public double Duration { get; }
        public String Polyline { get; }
        public Stop From { get; }
        public Stop To { get; }
        public Step[] Steps { get; }
        public String Line { get; }

        public Leg(String mode, double duration, String polyline, Stop from, Stop to, Step[] steps, String line)
        {
            Mode = mode;
            Duration = duration;
            Polyline = polyline;
            From = from;
            To = to;
            Steps = steps;
            Line = line;
        }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Mode);
            if(!Line.Equals(""))
            {
                sb.Append(" (Line " + Line + ")");
            }
            sb.Append(" from ").Append(From.Name).Append(" to ").Append(To.Name);
            return sb.ToString();
        }
        
    }

    class Stop
    {
        public double[] LatLong { get; }
        public long Arrival { get; }
        public long Departure { get; }
        public String Name { get; }

        public Stop(String name, double[] latLong, long arrival, long departure)
        {
            Name = name;
            LatLong = latLong;
            Arrival = arrival;
            Departure = departure;
        }
    }

    class Step
    {
        public enum RelativeDirections { DEPART, LEFT, RIGHT, SLIGHTLY_LEFT, SLIGHTLY_RIGHT, CONTINUE}
        public enum AbsoluteDirections { NORTH, NORTHEAST, EAST, SOUTHEAST, SOUTH, SOUTHWEST, WEST, NORTHWEST}
        public double Distance { get; } //(meters)
        public String RelativeDirection { get; }
        public String AbsoluteDirection { get; }
        public String StreetName { get; }
        public double[] LatLong { get; }
        
        public Step(double distance, string relativeDirection, string absoluteDirection, String streetName, double[] latLong)
        {
            Distance = distance;
            RelativeDirection = relativeDirection;
            AbsoluteDirection = absoluteDirection;
            StreetName = streetName;
            LatLong = latLong;
        }

        public override String ToString()
        {
            switch (RelativeDirection)
            {
                case "DEPART":
                    return "Depart on " + StreetName;
                case "LEFT":
                    return "Left on " + StreetName;

                case "RIGHT":
                    return "Right on " + StreetName;

                case "SLIGHTLY_LEFT":
                    return "Slight left on " + StreetName;

                case "SLIGHTLY_RIGHT":
                    return "Slight right on " + StreetName;

                case "CONTINUE":
                    return "Continue on " + StreetName; 
            }
            return RelativeDirection + " on " + StreetName;            
        }
    }
      
}
