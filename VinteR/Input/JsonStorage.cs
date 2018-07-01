﻿using System;
using System.Collections;
using System.Collections.Generic;
using VinteR.Model;
using System.IO;
using System.Linq;
using System.Numerics;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using VinteR.Configuration;

namespace VinteR.Input
{
    public class JsonStorage : IQueryService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private const string SessionsListFile = "sessions.json";
        private readonly string _homeDir;

        public JsonStorage(IConfigurationService configurationService)
        {
            _homeDir = configurationService.GetConfiguration().HomeDir;
        }

        public IList<Session> GetSessions()
        {


            var filePath = Path.Combine(_homeDir, "LoggingData", SessionsListFile);

            return ReadSessions(filePath);
        }

        public Session GetSession(string name, int startTimestamp = 0, int endTimestamp = -1)
        {

            var file = Path.Combine(_homeDir, "LoggingData", "sessions.json");
            try
            {
                using (var reader = new StreamReader(file))
                {

                    JsonTextReader jsonReader = new JsonTextReader(reader)
                    {
                        SupportMultipleContent = true
                    };
              
                    var format = "dd-MM-yyyy HH:mm:ss.fff";
                    var dateTimeConverter = new IsoDateTimeConverter { DateTimeFormat = format };
                    while (jsonReader.Read())
                    {
                        var obj = JObject.Load(jsonReader);
                        Session session = JsonConvert.DeserializeObject<Session>(obj.ToString(), dateTimeConverter);
                        if (string.Equals(session.Name, name))
                        {
                            session.MocapFrames = GetSessionMocapFrames(session, startTimestamp, endTimestamp);
                            return session;
                           
                        }
                    }
                }

            }
            catch (System.IO.FileNotFoundException e)
            {
                Logger.Error("error {0}", e.Message);
            }

            return new Session("No Name");
        }

        private IList<Session> ReadSessions(string file)
        {
            IList<Session> sessions = new List<Session>();
            try
            {
                using (var reader = new StreamReader(file))
                {
                    JsonTextReader jsonReader = new JsonTextReader(reader)
                    {
                        SupportMultipleContent = true
                    };
                    JsonSerializer serializer = new JsonSerializer();
                    var format = "dd-MM-yyyy HH:mm:ss.fff";
                    var dateTimeConverter = new IsoDateTimeConverter { DateTimeFormat = format };
                    while (jsonReader.Read())
                    {
                        var obj = JObject.Load(jsonReader);
                        Session session = JsonConvert.DeserializeObject<Session>(obj.ToString(), dateTimeConverter);
                        session.MocapFrames = GetSessionMocapFrames(session);

                        sessions.Add(session);


                    }

                    return sessions;
                }

            }
            catch (System.IO.FileNotFoundException e)
            {
                Logger.Error("error {0}", e.Message);
            }

            return new List<Session>();
        }

        private IList<MocapFrame> GetSessionMocapFrames(Session session, int startTimestamp = 0, int endTimestamp = -1)
        {
            
            IList<MocapFrame> mocapFrames = new List<MocapFrame>();
            var file = Path.Combine(_homeDir, "LoggingData", session.Name + ".json");
            try
            {
                using (var reader = new StreamReader(file))
                {
                    JsonTextReader jsonReader = new JsonTextReader(reader)
                    {
                        SupportMultipleContent = true
                    };
                    JsonSerializer serializer = new JsonSerializer();
                    if (endTimestamp == -1) endTimestamp = (int) session.Duration;

                    while (jsonReader.Read())
                    {
                        var obj = JObject.Load(jsonReader);

                        TimeSpan timeStamp = obj["time"].ToObject<DateTime>().Subtract(session.Datetime).Duration();

                        int timeStampInMillise = (int)timeStamp.TotalMilliseconds;

                        if (timeStampInMillise >= startTimestamp && timeStampInMillise <= endTimestamp)
                        {
                            MocapFrame mocapFrame = JsonConvert.DeserializeObject<MocapFrame>(obj.ToString(), new MocapFrameConverter());
                            mocapFrames.Add(mocapFrame);
                        }


                       
                        

                      
                    }

                    return mocapFrames;
                }


            }catch (System.IO.FileNotFoundException  e)
            {
                Console.WriteLine(e);
                
            }


            return mocapFrames;
        }
    }




    public class MocapFrameConverter : JsonConverter
    {
        public MocapFrame MocapFrame { get; set; }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);
            JObject jo = jObject.GetValue("eventProperties")["MocapFrame"].ToObject<JObject>();
            string sourceId = (string)jo["SourceId"];
            string adapter = (string)jo["AdapterType"];

            serializer.Converters.Add(new BodyTypeConverter());
            IList<Body> bodies = new List<Body>();
            foreach (var child in jo["Bodies"])
            {
                Body body = child.ToObject<Body>(serializer);
                bodies.Add(body);
            }

            MocapFrame mocapFrame = new MocapFrame(sourceId, adapter, bodies)
            {
                ElapsedMillis = (long) jo["ElapsedMillis"],
                Gesture = (string) jo["Gesture"]
                
            };
            return mocapFrame;

        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(VinteR.Model.MocapFrame));
        }
        public override bool CanWrite => false;
    }

    public class BodyTypeConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Body body = new Body();
            JObject jObject = JObject.Load(reader);

            body.BodyType = (Body.EBodyType)Enum.Parse(typeof(Body.EBodyType), jObject["BodyType"].ToString());
            body.Side = (ESideType) Enum.Parse(typeof(ESideType), jObject["Side"].ToString());
            body.Name = (string) jObject["Name"];
            string certroidString = jObject["Centroid"].ToString().TrimStart('<').TrimEnd('>');
            
            string[] certroids = certroidString.Split(',');
            
            body.Centroid = new Vector3(
                float.Parse(certroids[0]),
                float.Parse(certroids[1]),
                float.Parse(certroids[2])
                );

            body.Rotation = jObject["Rotation"].ToObject<Quaternion>();
            //body.Rotation.IsIdentity is read only??
            body.Rotation = Quaternion.Identity;
            serializer.Converters.Add(new PointTypeConverter());
            IList<Point> points = new List<Point>();
            foreach (var child in jObject["Points"])
            {
                Point point = child.ToObject<Point>(serializer);
                points.Add(point);
              
            }

            body.Points = points;
            return body;
        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(VinteR.Model.Body));
        }
    }

    public class PointTypeConverter: JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
           // throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);
            string positionString = jObject["Position"].ToString().TrimStart('<').TrimEnd('>');
            string[] positions = positionString.Split(',');

            Point point = new Point(new Vector3(
                float.Parse(positions[0]),
                float.Parse(positions[1]),
                float.Parse(positions[2])
            ))
            {
                Name = (string) jObject["Name"],
                State = (string) jObject["State"]
            };

            return point;
        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(VinteR.Model.Point));
        }
    }
}