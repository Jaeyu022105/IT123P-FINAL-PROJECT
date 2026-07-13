using IT123P_FINAL_PROJECT.ViewModels;

namespace IT123P_FINAL_PROJECT.Views
{
    public partial class PortionPage : ContentPage
    {
        private readonly PortionViewModel _viewModel;

        public PortionPage(PortionViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadBudgetAsync();
        }
    }
}
