﻿using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VinteR.Configuration
{
    [JsonObject]
    public class Configuration
    {
        [JsonProperty("home.dir")] public string HomeDir { get; set; }

        [JsonProperty("adapters")] public Adapters Adapters { get; set; }
    }

    [JsonArray]
    public class Adapters
    {
        public IList<Adapter> Items { get; set; }
    }

    public class Adapter
    {
        [JsonProperty("enabled")] public bool Enabled { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("adaptertype")] public string AdapterType { get; set; }

        [JsonProperty("isGlobalRoot")]
        public bool IsGlobalRoot { get; set; }
    }

    [JsonObject]
    public class Kinect : Adapter
    {
        [JsonProperty("colorStream.enabled")] public bool ColorStreamEnabled { get; set; }
        [JsonProperty("colorStream.flush")] public bool ColorStreamFlush { get; set; }
        [JsonProperty("depthStream.enabled")] public bool DepthStreamEnabled { get; set; }
        [JsonProperty("depthStream.flush")] public bool DepthStreamFlush { get; set; }
        [JsonProperty("skeletonStream.flush")] public bool SkeletonStreamFlush { get; set; }

    }

    [JsonObject]
    public class LeapMotion : Adapter
    {
    }

    [JsonObject]
    public class OptiTrack : Adapter
    {
        [JsonProperty("server.ip")] public string ServerIp { get; set; }

        [JsonProperty("client.ip")] public string ClientIp { get; set; }

        [JsonProperty("connection.type")] public string ConnectionType { get; set; }
    }
}