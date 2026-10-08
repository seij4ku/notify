namespace Notify.Desktop;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        // Firefox starts native hosts without command-line arguments and connects stdin/stdout pipes.
        if (e.Args.Contains("--native-host") || NativeHost.HasStandardPipeHandles)
        {
            ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
            NativeHost.Run();
            Shutdown();
            return;
        }
        base.OnStartup(e);
    }
}
