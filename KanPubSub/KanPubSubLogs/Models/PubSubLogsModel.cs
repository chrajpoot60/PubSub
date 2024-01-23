﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KanPubSubLogs.Models
{
    [BsonIgnoreExtraElements]
    public class PubSubLogsModel : MongoDocumentBase
    {
        public string? message_id { get; set; }
        public string? application_name { get; set; }
        public string? broker_action { get; set; }
        public string? broker { get; set; }
        public BsonValue? payload { get; set; }
        public string? queue_name { get; set; }
        public string? exchange_name { get; set; }
        public string? routing_key { get; set; }
        public string? topic { get; set; }
        public string? status { get; set; }
        public string? status_message { get; set; }
    }
}
