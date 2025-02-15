
namespace TeReoLocalizer;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}
	
	protected override Window CreateWindow(IActivationState? activationState)
	{
		Window window = new Window(new MainPage())
		{
			Title = "Te Reo",
			Width = 780,
			Height = 768,
			MinimumWidth = 780,
			MinimumHeight = 768
		};
		
		return window;
	}
}
