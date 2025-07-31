using Local_Area_Chat.Data; // Namespace für das Repository
using Local_Area_Chat.Dialogs; // Falls du sie in Dialogs ablegst
using Local_Area_Chat.MVP;
using Local_Area_Chat.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Local_Area_Chat
{
    public partial class MainWindow : Window, IMainView
    {
        private MainPresenter presenter;
        private DispatcherTimer? loginCloseTimer;

        public MainWindow()
        {
            InitializeComponent();
            presenter = new MainPresenter(this, new MongoChatRepository("mongodb://localhost:27017", "DeineDatenbank"));
        }

        public void SetChatrooms(List<string> chatrooms)
        {
            ChatroomListBox.ItemsSource = chatrooms;
            ChatroomListBox.SelectedIndex = 0;
        }

        public void SetMessages(List<string> messages)
        {
            MessagesListBox.ItemsSource = null;
            MessagesListBox.ItemsSource = messages;
        }

        public string GetMessageInput() => MessageTextBox.Text;
        public void ClearMessageInput() => MessageTextBox.Text = "";
        public int GetSelectedChatroomIndex() => ChatroomListBox.SelectedIndex;

        private void ChatroomListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (presenter != null)
                presenter.OnChatroomChanged(GetSelectedChatroomIndex());

            SelectedChatroomTextBlock.Text = ChatroomListBox.SelectedItem?.ToString() ?? "";
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            presenter.OnSendMessage(GetSelectedChatroomIndex());
        }

        private void LoginToggleButton_Click(object sender, RoutedEventArgs e)
        {
            LoginPopup.IsOpen = true;
        }

        private void LoginPopup_MouseLeave(object sender, MouseEventArgs e)
        {
            // Timer starten, wenn Maus das Popup verlässt
            if (loginCloseTimer == null)
            {
                loginCloseTimer = new DispatcherTimer();
                loginCloseTimer.Interval = TimeSpan.FromSeconds(1);
                loginCloseTimer.Tick += LoginCloseTimer_Tick;
            }
            loginCloseTimer.Start();
        }

        private void LoginPopup_MouseEnter(object sender, MouseEventArgs e)
        {
            // Timer stoppen, wenn Maus wieder im Popup ist
            loginCloseTimer?.Stop();
        }

        private void LoginCloseTimer_Tick(object? sender, EventArgs e)
        {
            loginCloseTimer?.Stop();
            LoginPopup.IsOpen = false;
            UserTextBox.Text = "";
            PasswordBox.Password = "";
            PrivateKeyTextBox.Text = "";
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Hier kannst du die Login-Logik einfügen
            LoginPopup.IsOpen = false;
            UserTextBox.Text = "";
            PasswordBox.Password = "";
            PrivateKeyTextBox.Text = "";
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            HamburgerButton.ContextMenu.IsOpen = true;
        }

        private void HamburgerMenu_Profile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Profil geöffnet");
            ProfilePanel.Visibility = Visibility.Visible;
        }

        private void ProfileSaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Hier kannst du die Logik zum Speichern der Profil-Daten einfügen
            ProfilePanel.Visibility = Visibility.Collapsed;
        }

        private void ProfileCloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Felder leeren (optional)
            ProfileUserTextBox.Text = "";
            ProfilePasswordTextBox.Text = "";
            ProfilePrivateKeyTextBox.Text = "";
            ProfileRoleComboBox.SelectedIndex = -1;
            ProfilePanel.Visibility = Visibility.Collapsed;
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendButton_Click(null, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void EditMessage_Click(object sender, RoutedEventArgs e)
        {
            var selected = MessagesListBox.SelectedItem as ChatMessage;
            if (selected != null)
            {
                // Zeige Eingabefeld für neuen Text (z.B. Dialog)
                var dialog = new EditMessageDialog(selected.Content);
                if (dialog.ShowDialog() == true)
                {
                    presenter.OnEditMessage(selected, dialog.NewContent);
                }
            }
        }
    }
}