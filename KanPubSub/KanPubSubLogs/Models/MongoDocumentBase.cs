using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KanPubSubLogs.Models
{
    public abstract class MongoDocumentBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public TimeStamp time_stamp { get; set; }
        public MongoDocumentBase()
        {
            if (time_stamp == null)
                time_stamp = new TimeStamp();
        }
    }
}
