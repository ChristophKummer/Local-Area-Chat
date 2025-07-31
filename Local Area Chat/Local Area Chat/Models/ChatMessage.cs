using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Local_Area_Chat.Models
{
    public class ChatMessage
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string ChatId { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string User { get; set; } = "";
        public string Content { get; set; } = "";
    }
}