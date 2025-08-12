using System.Collections.Generic;
using Local_Area_Chat.MVP.Models;

namespace Local_Area_Chat.MVP.ViewModels
{
    public class UserSelectionViewModel
    {
        public string Title { get; set; } = "";
        public List<User> AvailableUsers { get; set; } = new();
        
        // Results from dialog
        public bool DialogResult { get; set; }
        public User? SelectedUser { get; set; }
    }
}