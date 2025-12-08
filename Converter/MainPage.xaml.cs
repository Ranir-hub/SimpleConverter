
namespace Converter
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new ConvertersViewModel();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (BindingContext is ConvertersViewModel viewModel)
            {
                viewModel.Save();
            }
        }
    }
}
