namespace Notify.Desktop;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        if (e.Args.Contains("--native-host"))
        {
            ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
            NativeHost.Run();
            Shutdown();
            return;
        }
        base.OnStartup(e);
    }
}
