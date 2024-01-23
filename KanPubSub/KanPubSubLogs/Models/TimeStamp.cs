using MongoDB.Bson.Serialization.Attributes;

namespace KanPubSubLogs.Models
{
    [BsonIgnoreExtraElements]
    public class TimeStamp
    {
        public DateTime? created_on;
        public string created_by_userid;
        public string created_by_user_name;
        public DateTime? last_modified_on;
        public string last_modified_userid;
        public string last_modified_user_name;
        public TimeStamp()
        {
            created_on = DateTime.UtcNow;
        }
    }
}
