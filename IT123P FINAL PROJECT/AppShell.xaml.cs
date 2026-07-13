using IT123P_FINAL_PROJECT.Views;

namespace IT123P_FINAL_PROJECT
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(PortionPage), typeof(PortionPage));
        }
    }
}
