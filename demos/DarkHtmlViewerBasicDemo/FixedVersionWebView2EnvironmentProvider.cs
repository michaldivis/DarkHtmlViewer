using DarkHtmlViewer;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DarkHtmlViewerBasicDemo
{
    public class FixedVersionWebView2EnvironmentProvider : ICoreWebView2EnvironmentProvider
    {
        private readonly string _pathToFixedVersionRuntime;

        public FixedVersionWebView2EnvironmentProvider(string pathToFixedVersionRuntime)
        {
            _pathToFixedVersionRuntime = pathToFixedVersionRuntime;
        }

        public async Task<CoreWebView2Environment> CreateEnvironmentAsync()
        {
            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var dataFolder = Path.Combine(appDataDir, "webView2_env");

            var env = await CoreWebView2Environment.CreateAsync(_pathToFixedVersionRuntime, dataFolder);
            return env;
        }
    }
}
