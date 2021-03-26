using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebView2CSharpBrowser
{
    public class ControlsBrowser : WebView2
    {
        public enum ControlsBrowserTypes
        {
            UIControlsBrowser,
            OptionsControlsBrowser
        }

        public Microsoft.Web.WebView2.Core.CoreWebView2Environment ControlsCoreWebView2Environment;

        public delegate void OptionsControlsBrowserLostFocusEventHandler();
        public event OptionsControlsBrowserLostFocusEventHandler OptionsControlsLostFocusBrowserEvent;

        public ControlsBrowser() {}

        public async Task InitializeOptionsControlsBrowserAsync(Microsoft.Web.WebView2.Core.CoreWebView2Environment env, ControlsBrowserTypes browserType, string navigationPath)
        {
            ControlsCoreWebView2Environment = env;
            await InitializeAsync(null, browserType, navigationPath);
        }

        public async Task InitializeUIControlsBrowserAsync(string userDataFolder, ControlsBrowserTypes browserType, string navigationPath)
        {
            await InitializeAsync(userDataFolder, browserType, navigationPath);
        }

        async Task InitializeAsync(string userDataFolder, ControlsBrowserTypes browserType, string navigationPath)
        {
            if (browserType == ControlsBrowserTypes.UIControlsBrowser)
            {  //set up an Environment which we can then use for the Options Browser as well.
                ControlsCoreWebView2Environment = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, userDataFolder, null);
            }
            
            await EnsureCoreWebView2Async(ControlsCoreWebView2Environment);
            CoreWebView2.Settings.AreDevToolsEnabled = true;
            CoreWebView2.Settings.IsZoomControlEnabled = false;
            CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            DefaultBackgroundColor = System.Drawing.Color.Transparent;

            ZoomFactorChanged += ControlsBrowser_ZoomFactorChanged;

            CoreWebView2.Navigate(navigationPath);

            if (browserType == ControlsBrowserTypes.OptionsControlsBrowser)
            {
                LostFocus += OptionsControlsBrowser_LostFocus;
                Visibility = System.Windows.Visibility.Hidden;
            }
            
        }

        private void OptionsControlsBrowser_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            e.Handled = true;
            OptionsControlsLostFocusBrowserEvent?.Invoke();
        }

        private void ControlsBrowser_ZoomFactorChanged(object sender, EventArgs e)
        {
            ZoomFactor = 1.0;
        }
    }
}
