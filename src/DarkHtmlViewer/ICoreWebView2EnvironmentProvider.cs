using Microsoft.Web.WebView2.Core;
using System.Threading.Tasks;

namespace DarkHtmlViewer
{
    public interface ICoreWebView2EnvironmentProvider
    {
        Task<CoreWebView2Environment> CreateEnvironmentAsync();
    }
}