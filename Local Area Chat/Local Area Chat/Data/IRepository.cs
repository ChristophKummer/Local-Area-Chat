using System.Collections.Generic;
using System.Threading.Tasks;
using Local_Area_Chat.Models;
using System.Windows;

namespace Local_Area_Chat.Data
{
    public interface IRepository
    {
        Task<List<ChatMessage>> GetMessagesAsync(string chatId);
        Task AddMessageAsync(ChatMessage message);
        Task UpdateMessageAsync(ChatMessage message);
    }

}
