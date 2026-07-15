using IT123P_FINAL_PROJECT.ViewModels;

namespace IT123P_FINAL_PROJECT.Views
{
    public partial class CameraPage : ContentPage
    {
        public CameraPage(CameraViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is CameraViewModel viewModel)
            {
                viewModel.ResetSearchState();
            }
        }
    }
}
