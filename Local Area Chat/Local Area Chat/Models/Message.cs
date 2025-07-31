using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Local_Area_Chat.Models
{
    public class Message
    {
        [BsonId]
        public ObjectId Id { get; set; }
        [BsonElement("chatID")]
        public string ChatId { get; set; } = "";
        [BsonElement("userID")]
        public string UserId { get; set; } = "";
        [BsonElement("content")]
        public string Content { get; set; } = "";
        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}