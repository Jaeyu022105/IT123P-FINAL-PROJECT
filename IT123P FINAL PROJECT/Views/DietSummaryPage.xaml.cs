using IT123P_FINAL_PROJECT.ViewModels;

namespace IT123P_FINAL_PROJECT.Views
{
    public partial class DietSummaryPage : ContentPage
    {
        private readonly DietSummaryViewModel _viewModel;

        public DietSummaryPage(DietSummaryViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadDataAsync();
        }
    }
}
