using System.Windows;

namespace Local_Area_Chat.Dialogs
{
    public partial class EditMessageDialog : Window
    {
        public string NewContent { get; private set; }
        public EditMessageDialog(string oldContent)
        {
            InitializeComponent();
            // TextBox mit oldContent vorbelegen
        }
        // OK-Button setzt NewContent und schlieﬂt Dialog
    }
}