namespace IotEdgePerf.Shared
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    [JsonObject(MemberSerialization.OptIn)]
    public class PerfTelemetryMessage
    {
        // sessionId
        [JsonProperty]
        public string   id;

        // burst counter
        [JsonProperty]
        public int      bc;

        // time elapsed since burst start (milliseconds)
        [JsonProperty]
        public double   el;

        // rolling rate (calculated over the burst)
        [JsonProperty]
        public double?  rt;

        // message counter (since burst start)
        [JsonProperty]
        public int      mc;

        // message time stamp (epoch)
        [JsonProperty]
        public double   ts;

        // transmission duration (milliseconds) of previous message
        [JsonProperty]
        public double?  pt;

        // cycle duration (milliseconds) of previous message
        [JsonProperty]
        public double?  pc;

        // timing violations counter
        [JsonProperty]
        public int      vc;

        public string KeyName = "p";

        public string  Json
        {
            get { return JsonConvert.SerializeObject(this); }
            set { }
        }

        /// <summary>
        /// Add the profiling telemetry to the "baseObjectJson" under key "KeyName".
        /// </summary>
        /// <param name="addOnMessage"></param>
        /// <returns></returns>
        public string AddTo(object baseObject)
        {
            var jObject = JObject.FromObject(baseObject);
            jObject.Add(new JProperty(this.KeyName, JToken.FromObject(this)));

            return jObject.ToString(Formatting.None);
        }

        public string ToPrettyJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}