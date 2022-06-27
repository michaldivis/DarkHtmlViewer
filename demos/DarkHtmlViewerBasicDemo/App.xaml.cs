using DarkHtmlViewer;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;
using System.Windows;

namespace DarkHtmlViewerBasicDemo
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //configure logger
            DarkHtmlViewer.DarkHtmlViewer.ConfigureLogger(() => NullLoggerFactory.Instance);

            var appLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var htmlAssetsDir = Path.Combine(Path.GetDirectoryName(appLocation), "Files");

            DarkHtmlViewer.DarkHtmlViewer.ConfigureVirtualHostNameToFolderMappingSettings(new VirtualHostNameToFolderMappingSettings
            {
                IsEnabled = true,
                Hostname = "darkassets.local",
                FolderPath = htmlAssetsDir,
                AccessKind = Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow
            });

            //use fixed version WebView2 runtime
            //DarkHtmlViewer.DarkHtmlViewer.ConfigureEnvironmentProvider(new FixedVersionWebView2EnvironmentProvider("path/to/fixed/version/runtime"));
        }
    }
}
