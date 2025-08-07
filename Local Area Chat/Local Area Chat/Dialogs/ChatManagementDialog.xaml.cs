using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Local_Area_Chat.MVP.Models;

namespace Local_Area_Chat.Dialogs
{
    public partial class ChatManagementDialog : Window
    {
        public string? NewChatName { get; private set; }
        public bool IsPrivate { get; private set; }
        public List<User> AddedUsers { get; private set; } = new();
        public List<string> RemovedUsers { get; private set; } = new();
        public bool DeleteChat { get; private set; } = false;
        
        private List<User> availableUsers = new();
        private List<string> currentParticipants = new();
        private string chatAdminName;

        public ChatManagementDialog(string currentChatName, bool isPrivate, List<string> participants, 
                                  List<User> availableUsers, string adminName = "")
        {
            InitializeComponent();
            
            // Initialize data
            ChatNameTextBox.Text = currentChatName;
            StatusComboBox.SelectedIndex = isPrivate ? 1 : 0; // 0 = Öffentlich, 1 = Privat
            this.currentParticipants = participants;
            this.availableUsers = availableUsers;
            this.chatAdminName = adminName;
            
            // Set admin display
            ChatAdminTextBlock.Text = string.IsNullOrEmpty(adminName) ? 
                "Chatadmin: Unbekannt" : $"Chatadmin: {adminName}";
            
            // Populate lists
            ParticipantsListBox.ItemsSource = participants;
            AvailableUsersListBox.ItemsSource = availableUsers;
            
            // Enable remove button if participants are selected
            ParticipantsListBox.SelectionChanged += (s, e) => {
                RemoveUserButton.IsEnabled = ParticipantsListBox.SelectedItem != null;
            };
        }

        private void AvailableUsersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddUserButton.IsEnabled = AvailableUsersListBox.SelectedItem != null;
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (AvailableUsersListBox.SelectedItem is User selectedUser)
            {
                // Add user to participants
                currentParticipants.Add(selectedUser.UserName);
                AddedUsers.Add(selectedUser);
                
                // Remove from available users
                availableUsers.Remove(selectedUser);
                
                // Refresh lists
                RefreshLists();
                AddUserButton.IsEnabled = false;
            }
        }

        private void RemoveUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (ParticipantsListBox.SelectedItem is string selectedParticipant)
            {
                // Check if trying to remove admin
                if (selectedParticipant == chatAdminName)
                {
                    MessageBox.Show("Der Chat-Administrator kann nicht entfernt werden.", 
                                  "Entfernung nicht möglich", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                    return;
                }

                // Remove from participants
                currentParticipants.Remove(selectedParticipant);
                RemovedUsers.Add(selectedParticipant);
                
                // Add back to available users (create simplified user object)
                var user = new User { UserName = selectedParticipant, UserId = selectedParticipant };
                availableUsers.Add(user);
                
                // Refresh lists
                RefreshLists();
                RemoveUserButton.IsEnabled = false;
            }
        }

        private void RefreshLists()
        {
            ParticipantsListBox.ItemsSource = null;
            ParticipantsListBox.ItemsSource = currentParticipants;
            AvailableUsersListBox.ItemsSource = null;
            AvailableUsersListBox.ItemsSource = availableUsers;
        }

        private void DeleteChatButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Sind Sie sicher, dass Sie diesen Chat löschen möchten?\n\n" +
                "Diese Aktion kann nicht rückgängig gemacht werden!\n" +
                "Alle Nachrichten und Chat-Daten gehen verloren.",
                "Chat löschen bestätigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                DeleteChat = true;
                DialogResult = true;
                Close();
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            NewChatName = ChatNameTextBox.Text?.Trim();
            IsPrivate = StatusComboBox.SelectedIndex == 1; // 1 = Privat
            
            if (string.IsNullOrEmpty(NewChatName))
            {
                MessageBox.Show("Bitte geben Sie einen Chat-Namen ein.", "Fehler", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}