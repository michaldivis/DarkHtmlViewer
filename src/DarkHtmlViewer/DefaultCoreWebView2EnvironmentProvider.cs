using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DarkHtmlViewer
{
    public class DefaultCoreWebView2EnvironmentProvider : ICoreWebView2EnvironmentProvider
    {
        public async Task<CoreWebView2Environment> CreateEnvironmentAsync()
        {
            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var dataFolder = Path.Combine(appDataDir, "webView2_env");
            var env = await CoreWebView2Environment.CreateAsync(null, dataFolder);
            return env;
        }
    }
}
