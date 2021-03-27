using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Threading.Tasks;

namespace WebView2CSharpBrowser
{
    public class Tab : WebView2
    {
        public int Tabid = 0;
        public bool ShouldBeActive;
        private BrowserWindow MainBrowser;
        public Tab() { }
        public Tab(CoreWebView2Environment env, int id, bool shouldBeActive, BrowserWindow BrowserWin)
        {
            Tabid = id;
            MainBrowser = BrowserWin;
            ShouldBeActive = shouldBeActive;
            InitAsync(env, shouldBeActive);
        }

        public async Task CreateTabFromNewWindowRequested(CoreWebView2Environment env, bool shouldBeActive, BrowserWindow BrowserWin)
        {
            MainBrowser = BrowserWin;
            ShouldBeActive = shouldBeActive;
            await this.EnsureCoreWebView2Async(env);
            this.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            this.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
            this.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            this.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            this.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
            this.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequestedAsync;
            this.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            await this.CoreWebView2.CallDevToolsProtocolMethodAsync("Security.enable", "{}");

            this.CoreWebView2.GetDevToolsProtocolEventReceiver("Security.securityStateChanged").DevToolsProtocolEventReceived += Tab_DevToolsProtocolEventReceived;

        }

        

        private async void InitAsync(CoreWebView2Environment env, bool shouldBeActive)
        {
            await this.EnsureCoreWebView2Async(env);
            this.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            this.CoreWebView2.Navigate("https://www.bing.com");

            this.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
            this.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            this.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            this.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
            this.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            this.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequestedAsync;
            await this.CoreWebView2.CallDevToolsProtocolMethodAsync("Security.enable", "{}");

            this.CoreWebView2.GetDevToolsProtocolEventReceiver("Security.securityStateChanged").DevToolsProtocolEventReceived += Tab_DevToolsProtocolEventReceived;

        }

        private void CoreWebView2_DocumentTitleChanged(object sender, object e)
        {
            if (!string.Equals(this.CoreWebView2.Source, this.CoreWebView2.DocumentTitle))
            MainBrowser.HandleTabDocumentTitleChange(Tabid, this.CoreWebView2.Source, this.CoreWebView2.DocumentTitle);
        }

        private async void CoreWebView2_NewWindowRequestedAsync(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            CoreWebView2Deferral def = e.GetDeferral();
            Tab nTab = new();
            MainBrowser.HandleNewTabRequestAsync(nTab);
            await nTab.CreateTabFromNewWindowRequested(CoreWebView2.Environment, true, MainBrowser);
            e.NewWindow = nTab.CoreWebView2;
            e.Handled = true;
            def.Complete();
        }

        private void Tab_DevToolsProtocolEventReceived(object sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
        {
            MainBrowser.HandleTabSeucityUpdate(Tabid, CoreWebView2, e);
        }

        private void CoreWebView2_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            MainBrowser.HandleTabURIUpdate(Tabid, CoreWebView2);
        }

        private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            MainBrowser.HandleTabNavigationStarting(Tabid, CoreWebView2);
        }

        private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            MainBrowser.HandleTabNavigationCompleteAsync(Tabid, CoreWebView2, e);
        }

        private void CoreWebView2_HistoryChanged(object sender, object e)
        {
            MainBrowser.HandleTabHistory(Tabid, CoreWebView2);
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            MainBrowser.HandleTabMessageReceived(Tabid, CoreWebView2, e);
        }
    }
}
