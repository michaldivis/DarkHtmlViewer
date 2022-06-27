using DarkHelpers.Commands;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DarkHelpers;
using System.Linq;

namespace DarkHtmlViewer
{
    public partial class DarkHtmlViewer : UserControl
    {
        private readonly ILogger<DarkHtmlViewer> _logger;
        private readonly Guid _instanceId;
        private readonly DarkHtmlTempFileManager _fileManager;

        #region Bindable properties

        public string HtmlContent
        {
            get => (string)GetValue(HtmlContentProperty);
            set => SetValue(HtmlContentProperty, value);
        }

        public static readonly DependencyProperty HtmlContentProperty =
            DependencyProperty.Register(
                nameof(HtmlContent),
                typeof(string),
                typeof(DarkHtmlViewer),
                new PropertyMetadata(
                    string.Empty,
                    new PropertyChangedCallback(OnHtmlContentCallBack)));

        public ICommand LinkClickedCommand
        {
            get => (ICommand)GetValue(LinkClickedCommandProperty);
            set => SetValue(LinkClickedCommandProperty, value);
        }

        public static readonly DependencyProperty LinkClickedCommandProperty =
            DependencyProperty.Register(
                nameof(LinkClickedCommand),
                typeof(ICommand),
                typeof(DarkHtmlViewer),
                new PropertyMetadata(null));

        #endregion

        #region Commands

        public ICommand LoadCommand => new DarkCommand<string>(Load);
        public ICommand ScrollCommand => new DarkAsyncCommand<string>(ScrollAsync);
        public ICommand ScrollOnNextLoadCommand => new DarkCommand<string>(ScrollOnNextLoad);
        public ICommand SearchCommand => new DarkAsyncCommand<string>(SearchAsync);
        public ICommand SearchOnNextLoadCommand => new DarkCommand<string>(SearchOnNextLoad);
        public ICommand PrintCommand => new DarkAsyncCommand(PrintAsync);
        public ICommand ZoomCommand => new DarkCommand<double>(Zoom);

        #endregion

        public DarkHtmlViewer()
        {
            InitializeComponent();

            _instanceId = Guid.NewGuid();

            _fileManager = new DarkHtmlTempFileManager(_instanceId, GetLogger<DarkHtmlTempFileManager>());

            _logger = GetLogger<DarkHtmlViewer>();

            _logger.LogDebug("DarkHtmlViewer-{InstanceId}: Initializing", _instanceId);

            InitializeWebView2();
        }

        #region Initialization

        private bool _initialized;
        private string _loadAfterInitialization = null;
        private async void InitializeWebView2()
        {
            var initFilePath = _fileManager.GetFilePath();
            _logger.LogDebug("DarkHtmlViewer-{InstanceId}: {Method}, init file: {InitFile}", _instanceId, nameof(InitializeWebView2), initFilePath);

            webView2.CoreWebView2InitializationCompleted += WebView2_CoreWebView2InitializationCompleted;
            webView2.NavigationStarting += WebView2_NavigationStarting;
            webView2.NavigationCompleted += WebView2_NavigationCompleted;

            var environmentProvider = GetCoreWebView2EnvironmentProvider();
            var env = await environmentProvider.CreateEnvironmentAsync();

            await webView2.EnsureCoreWebView2Async(env);

            SetWebViewSource(initFilePath);

            webView2.Visibility = Visibility.Visible;

            _initialized = true;

            LoadQeuedContentIfAny();
        }

        private void SetWebViewSource(string uri)
        {
            webView2.CoreWebView2?.Navigate(uri);
        }

        private void LoadQeuedContentIfAny()
        {
            if (_loadAfterInitialization is null)
            {
                return;
            }

            Load(_loadAfterInitialization);
        }

        private void WebView2_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            _logger.LogDebug("DarkHtmlViewer-{InstanceId}: {Method}", _instanceId, nameof(WebView2_CoreWebView2InitializationCompleted));
            DisableAllExtraFunctionality();
            SetupVirtualHostForAssets();
        }

        #endregion

        #region Loading HTML content

        private static void OnHtmlContentCallBack(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var viewer = sender as DarkHtmlViewer;
            if (viewer != null)
            {
                viewer.Load(e.NewValue?.ToString());
            }
        }

        /// <summary>
        /// Loads an HTML string
        /// </summary>
        public void Load(string html)
        {
            if (_initialized is false)
            {
                _loadAfterInitialization = html;
                return;
            }

            _fileManager.Create(html);
            var htmlFilePath = _fileManager.GetFilePath();
            _logger.LogDebug("DarkHtmlViewer-{InstanceId}: {Method}, file: {HtmlFilePath}", _instanceId, nameof(Load), htmlFilePath);
            SetWebViewSource(htmlFilePath);
        }

        #endregion

        #region Navigation

        private void WebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            var linkName = Path.GetFileName(e.Uri);

            var isTempFileName = _fileManager.IsTempFilePath(linkName);

            if (isTempFileName)
            {
                return;
            }

            e.Cancel = true;

            TriggerLinkClicked(linkName);
        }

        private void TriggerLinkClicked(string link)
        {
            LinkClickedCommand?.TryExecute(link);
        }

        private async void WebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (_scrollToNext is not null)
            {
                await ScrollAsync(_scrollToNext);
                _scrollToNext = null;
            }

            if (_textToFind is not null)
            {
                await SearchAsync(_textToFind);
                _textToFind = null;
            }
        }

        #endregion

        #region Scroll to

        private string _scrollToNext = null;

        /// <summary>
        /// Tries to scroll to an element by ID
        /// </summary>
        public async Task ScrollAsync(string elementId)
        {
            var script = $"document.getElementById(\"{elementId}\").scrollIntoView();";
            await webView2.ExecuteScriptAsync(script);
        }

        /// <summary>
        /// Sets the elementId to be scrolled to next time HTML content is loaded
        /// </summary>
        public void ScrollOnNextLoad(string elementId)
        {
            _scrollToNext = elementId;
        }

        #endregion

        #region Search

        private string _textToFind = null;

        /// <summary>
        /// Finds text in the loaded HTML
        /// </summary>
        public async Task SearchAsync(string text)
        {
            var clean = CleanSearchText(text);
            if (string.IsNullOrEmpty(clean))
            {
                return;
            }

            //TODO try to find a way to highlight all occurances instead of just the first one
            var script = $"window.find('{clean}');";
            await webView2.ExecuteScriptAsync(script);
        }

        private string CleanSearchText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            var clean = text.Replace("'", "");

            if (string.IsNullOrEmpty(clean))
            {
                return null;
            }

            return clean;
        }

        /// <summary>
        /// Sets the text to be searched for next time HTML content is loaded
        /// </summary>
        public void SearchOnNextLoad(string text)
        {
            _textToFind = text;
        }

        #endregion

        #region Printing

        /// <summary>
        /// Invokes the browser print dialog
        /// </summary>
        public async Task PrintAsync()
        {
            var script = "window.print();";
            await webView2.ExecuteScriptAsync(script);
        }

        #endregion

        #region Zoom

        /// <summary>
        /// Sets the zoom factor of the browser
        /// </summary>
        /// <param name="zoom">Zoom factor, 1.0 is default</param>
        public void Zoom(double zoom)
        {
            webView2.ZoomFactor = zoom;
        }

        #endregion

        #region Virtual host

        private static readonly VirtualHostNameToFolderMappingSettingsValidator _virtualAssetSettingsValidator = new();
        private static VirtualHostNameToFolderMappingSettings _virtualAssetSettings;

        /// <summary>
        /// Configure the virtual host to folder mapping
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="VirtualHostNameToFolderMappingSettingsException"></exception>
        public static void ConfigureVirtualHostNameToFolderMappingSettings(VirtualHostNameToFolderMappingSettings settings)
        {
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            var validationResult = _virtualAssetSettingsValidator.Validate(settings);

            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.FirstOrDefault();
                throw new VirtualHostNameToFolderMappingSettingsException(firstError.PropertyName, firstError.ErrorMessage);
            }

            _virtualAssetSettings = settings;
        }

        private void SetupVirtualHostForAssets()
        {
            if (_virtualAssetSettings is null)
            {
                return;
            }

            if(_virtualAssetSettings.IsEnabled is false)
            {
                return;
            }

            webView2.CoreWebView2.SetVirtualHostNameToFolderMapping(_virtualAssetSettings.Hostname, _virtualAssetSettings.FolderPath, _virtualAssetSettings.AccessKind);
        }

        #endregion

        #region Environment settings

        private static readonly DefaultCoreWebView2EnvironmentProvider _defaultCoreWebView2EnvironmentProvider = new();
        private static ICoreWebView2EnvironmentProvider _coreWebView2EnvironmentProvider;

        /// <summary>
        /// Configure a custom Core WebView2 environment provider
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public static void ConfigureEnvironmentProvider(ICoreWebView2EnvironmentProvider coreWebView2EnvironmentProvider)
        {
            if (coreWebView2EnvironmentProvider is null) throw new ArgumentNullException(nameof(coreWebView2EnvironmentProvider));

            _coreWebView2EnvironmentProvider = coreWebView2EnvironmentProvider;
        }

        private static ICoreWebView2EnvironmentProvider GetCoreWebView2EnvironmentProvider()
        {
            if(_coreWebView2EnvironmentProvider is null)
            {
                return _defaultCoreWebView2EnvironmentProvider;
            }

            return _coreWebView2EnvironmentProvider;
        }

        #endregion

        #region Browser settings

        private void DisableAllExtraFunctionality()
        {
            webView2.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
            webView2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView2.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            webView2.CoreWebView2.Settings.AreDevToolsEnabled = false;
            
            webView2.CoreWebView2.Settings.IsScriptEnabled = false;
            webView2.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView2.CoreWebView2.Settings.IsWebMessageEnabled = false;
            webView2.CoreWebView2.Settings.IsZoomControlEnabled = false;
            webView2.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
            
            webView2.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
            webView2.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
            webView2.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;
        }

        #endregion

        #region Logging

        private static Func<ILoggerFactory> LoggerFactoryProvider;

        public static void ConfigureLogger(Func<ILoggerFactory> loggerFactoryProvider)
        {
            LoggerFactoryProvider = loggerFactoryProvider;
        }

        private static ILogger<T> GetLogger<T>()
        {
            if(LoggerFactoryProvider is null)
            {
                return NullLogger<T>.Instance;
            }

            return LoggerFactoryProvider.Invoke().CreateLogger<T>();
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Removes the temporary files created by the control
        /// </summary>
        public void Cleanup()
        {
            _fileManager.TryDeleteCurrentTempFile();
        }

        #endregion
    }
}
