using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System;

namespace Local_Area_Chat.MVP.Models
{
    public class Chat
    {
        [BsonId]
        public ObjectId Id { get; set; }
        
        [BsonElement("chatID")]
        public string ChatId { get; set; } = "";
        
        [BsonElement("chatName")]
        public string ChatName { get; set; } = "";
        
        [BsonElement("userID")]
        public List<string> UserIds { get; set; } = new List<string>();
        
        [BsonElement("adminUserID")]
        public string AdminUserId { get; set; } = "";
        
        [BsonElement("isPrivate")]
        public bool IsPrivate { get; set; } = true;
        
        // NEU: Chat-spezifischer Verschlüsselungsschlüssel
        [BsonElement("encryptionKey")]
        public string EncryptionKey { get; set; } = "";
        
        // NEU: Initialisierungsvektor für den Chat
        [BsonElement("encryptionIV")]
        public string EncryptionIV { get; set; } = "";
        
        // NEU: Zeitstempel der Schlüsselerstellung
        [BsonElement("keyCreatedAt")]
        public DateTime KeyCreatedAt { get; set; } = DateTime.UtcNow;
        
        // NEU: Version des Schlüssels (für Key-Rotation)
        [BsonElement("keyVersion")]
        public int KeyVersion { get; set; } = 1;
    }
}