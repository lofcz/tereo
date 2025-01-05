
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
			Title = "Te Reo .NET"
		};
		
		return window;
	}
}
