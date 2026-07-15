using IT123P_FINAL_PROJECT.ViewModels;

namespace IT123P_FINAL_PROJECT.Views
{
    public partial class ChatPage : ContentPage
    {
        public ChatPage(ChatViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
