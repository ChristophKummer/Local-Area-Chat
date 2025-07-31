using Local_Area_Chat.Data;
using Local_Area_Chat.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Local_Area_Chat.Data
{
    public class MongoChatRepository : IRepository
    {
        private readonly IMongoCollection<ChatMessage> _messages;

        public MongoChatRepository(string connectionString, string dbName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(dbName);
            _messages = database.GetCollection<ChatMessage>("messages");
        }

        public async Task<List<ChatMessage>> GetMessagesAsync(string chatId)
            => await _messages.Find(m => m.ChatId == chatId).SortBy(m => m.Timestamp).ToListAsync();

        public async Task AddMessageAsync(ChatMessage message)
            => await _messages.InsertOneAsync(message);

        public async Task UpdateMessageAsync(ChatMessage message)
            => await _messages.ReplaceOneAsync(m => m.Id == message.Id, message);
    }
}