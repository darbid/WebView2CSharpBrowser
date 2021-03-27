using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WebView2CSharpBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class BrowserWindow : Window, INotifyPropertyChanged
    {

		private enum BrowserSchemeUrls
        {
			Favorites,
			Settings,
			History
        }

		const string browserScheme = "browser://";

		private static readonly object postMessageLock = new();

		private Microsoft.Web.WebView2.Core.CoreWebView2Environment userEnviron;

		private int _SelectedTabIndex = 0;
		public int SelectedTabIndex
        {
			get { return _SelectedTabIndex; }
			set { _SelectedTabIndex = value;
				NotifyPropertyChanged();
			}
        }

		private ObservableCollection<Tab> _TabCollection;
		public ObservableCollection<Tab> TabCollection
        {
            get { return _TabCollection ??= new ObservableCollection<Tab>(); }
			set { _TabCollection = value; NotifyPropertyChanged(); }
        }

		private ControlsBrowser _UIControlsBrowser;
		public ControlsBrowser UIControlsBrowser
		{
			get { return _UIControlsBrowser ??= new(); }
			set
			{
				_UIControlsBrowser = value;
				NotifyPropertyChanged();
			}
		}

		private ControlsBrowser _OptionsControlsBrowser;
        public ControlsBrowser OptionsControlsBrowser
		{
			get { return _OptionsControlsBrowser ??= new(); }
			set { _OptionsControlsBrowser = value;
				NotifyPropertyChanged();
			}
		}

        public BrowserWindow()
        {
            InitializeComponent();
			InitializeAsync();
            this.Closing += BrowserWindow_Closing;
        }

        private void BrowserWindow_Closing(object sender, CancelEventArgs e)
        {
			UIMessages mess = new();
			mess.Message = UIMessages.MessageOptions.MG_CLOSE_WINDOW;
            PostJsonToWebView(mess, UIControlsBrowser.CoreWebView2);
        }

        private async void InitializeAsync()
        {
            // Get directory for user data. This will be kept separated from the
            // directory for the browser UI data.
            string userDataDirectory = GetAppDataDirectory("User Data");
            userEnviron = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, userDataDirectory, null);
			//Set up the Browser which shows all of the UI Controls at the top 
			
			UIControlsBrowser.WebMessageReceived += UIWebView_WebMessageReceivedAsync;
			await UIControlsBrowser.InitializeUIControlsBrowserAsync(GetAppDataDirectory("Browser Data"), ControlsBrowser.ControlsBrowserTypes.UIControlsBrowser, GetFullPathFor(@"wvbrowser_ui\controls_ui\default.html"));

			OptionsControlsBrowser.WebMessageReceived += UIWebView_WebMessageReceivedAsync;
			await OptionsControlsBrowser.InitializeOptionsControlsBrowserAsync(UIControlsBrowser.ControlsCoreWebView2Environment, ControlsBrowser.ControlsBrowserTypes.OptionsControlsBrowser, GetFullPathFor(@"wvbrowser_ui\controls_ui\options.html"));

			OptionsControlsBrowser.OptionsControlsLostFocusBrowserEvent += () =>
			{
				UIMessages mess = new(UIMessages.MessageOptions.MG_OPTIONS_LOST_FOCUS);
				Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() => 
				{
                    string json = System.Text.Json.JsonSerializer.Serialize(mess, typeof(UIMessages));
                    UIControlsBrowser.CoreWebView2.PostWebMessageAsJson(json);
                }));
			};
        }


		/// <summary>
		/// These messages come from the UI controls browser and the Options controls browser.
		/// </summary>
        private void UIWebView_WebMessageReceivedAsync(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            string json = e.WebMessageAsJson; 
            if (string.IsNullOrEmpty(json))
                return;
			System.Diagnostics.Debug.Print("++++++++++++++++++++++++++++++++++++++++++++++++");
			System.Diagnostics.Debug.Print("UIWebView_WebMessageReceivedAsync " + System.Environment.NewLine + json);
			System.Diagnostics.Debug.Print("++++++++++++++++++++++++++++++++++++++++++++++++");

			UIMessages mess = (UIMessages)System.Text.Json.JsonSerializer.Deserialize(json, typeof(UIMessages));

			int id = mess.Args.TabId;

			switch (mess.Message)
			{
				case UIMessages.MessageOptions.MG_CREATE_TAB:
					{
						Tab nTab = new(userEnviron, id, mess.Args.Active, this);

						if (TabCollection.Count != 0) { TabCollection[GetActiveTab()].ShouldBeActive = false; }
						
						TabCollection.Add(nTab);
						SelectedTabIndex = TabCollection.Count - 1;
					}
					break;

				case UIMessages.MessageOptions.MG_NAVIGATE:
					{
						string uri = mess.Args.Uri;
						

						if (uri.StartsWith(browserScheme))
                        {
							if (uri.Contains("favorites"))
							{
								string url = GetBrowserSchemeUrl(BrowserSchemeUrls.Favorites);
								TabCollection[GetActiveTab()].CoreWebView2.Navigate(url);
							} else if (uri.Contains("settings"))
                            {
								string url = GetBrowserSchemeUrl(BrowserSchemeUrls.Settings);
								TabCollection[GetActiveTab()].CoreWebView2.Navigate(url);
							} else if (uri.Contains("history"))
                            {
								string url = GetBrowserSchemeUrl(BrowserSchemeUrls.History);
								TabCollection[GetActiveTab()].CoreWebView2.Navigate(url);
							}
							else
							{
								System.Diagnostics.Debug.Print("Requested unknown browser page\n");
							}

						} else
                        {
							TabCollection[GetActiveTab()].CoreWebView2.Navigate(uri);
						}

					}
					break;
				case UIMessages.MessageOptions.MG_GO_FORWARD:
					{
						TabCollection[GetActiveTab()].CoreWebView2.GoForward();
					}
					break;
				case UIMessages.MessageOptions.MG_GO_BACK:
					{
						TabCollection[GetActiveTab()].CoreWebView2.GoBack();
					}
					break;
				case UIMessages.MessageOptions.MG_RELOAD:
					{
						TabCollection[GetActiveTab()].CoreWebView2.Reload();
					}
					break;
				case UIMessages.MessageOptions.MG_CANCEL:
					{
						TabCollection[GetActiveTab()].CoreWebView2.CallDevToolsProtocolMethodAsync("Page.stopLoading", "{}");
					}
					break;
				case UIMessages.MessageOptions.MG_SWITCH_TAB:
					{
						TabCollection[GetActiveTab()].ShouldBeActive = false;
						int tabIndex = GetTabCollectionIdexForID(id);
						TabCollection[tabIndex].ShouldBeActive = true;
						SelectedTabIndex = tabIndex;
					}
					break;
				case UIMessages.MessageOptions.MG_CLOSE_TAB:
					{
						TabCollection.Remove(TabCollection[GetTabCollectionIdexForID(id)]);
						SelectedTabIndex = TabCollection.Count - 1;
						TabCollection[SelectedTabIndex].ShouldBeActive = true;
					}
					break;
				case UIMessages.MessageOptions.MG_CLOSE_WINDOW:
					{
						System.Windows.Application.Current.Shutdown();
					}
					break;
				case UIMessages.MessageOptions.MG_SHOW_OPTIONS:
					{
						System.Diagnostics.Debug.Print("OptionsControlsBrowser Index: " + Panel.GetZIndex(OptionsControlsBrowser).ToString());
						//System.Diagnostics.Debug.Print("0 " + Panel.GetZIndex(TabCollection[0]).ToString());
						//System.Diagnostics.Debug.Print("1 " + Panel.GetZIndex(TabCollection[1]).ToString());
						Panel.SetZIndex(OptionsControlsBrowser, 1);
						//Panel.SetZIndex((UIElement)OptionsControlsBrowser.Parent, 1);
						System.Diagnostics.Debug.Print("OptionsControlsBrowser Index: " + Panel.GetZIndex(OptionsControlsBrowser).ToString());
						OptionsControlsBrowser.Visibility = Visibility.Visible;
						OptionsControlsBrowser.Focus();
                    }
					break;
				case UIMessages.MessageOptions.MG_HIDE_OPTIONS:
					{
                        OptionsControlsBrowser.Visibility = Visibility.Hidden;
                    }
					break;
				case UIMessages.MessageOptions.MG_OPTION_SELECTED:
					{
						Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() =>
						{
							TabCollection[GetActiveTab()].Focus();
						}));
					}
					break;
				case UIMessages.MessageOptions.MG_GET_FAVORITES:
				case UIMessages.MessageOptions.MG_GET_SETTINGS:
				case UIMessages.MessageOptions.MG_GET_HISTORY:
					{
						mess.Args.TabId = mess.Args.TabId;
                        PostJsonToWebView(mess, TabCollection[GetTabCollectionIdexForID(mess.Args.TabId)].CoreWebView2);
					}
					break;
				default:
					{
						
					}
					break;
			}
		}

		public void HandleNewTabRequestAsync(Tab tab)
		{
			tab.Tabid = TabCollection.Count + 1;
			TabCollection[GetActiveTab()].ShouldBeActive = false;
			TabCollection.Add(tab);
			//Handling a newly requested window is not in the C++ version. 
			//First Create the new tab and add it to the collection of tabs
			//Send a message to the UIControls which is a new message of MG_NEW_WINDOW_REQUESTED
			//In the Javascript it creates a new tab and then sends a switch message so that the TabCollection switches to this newly created tab item.
			UIMessages mess = new(UIMessages.MessageOptions.MG_NEW_WINDOW_REQUESTED);
			mess.Args.TabId = tab.Tabid;
			PostJsonToWebView(mess, UIControlsBrowser.CoreWebView2);
			
		}
		
		public void HandleTabURIUpdate(int id, CoreWebView2 webview)
		{
			UIMessages mess = new(UIMessages.MessageOptions.MG_UPDATE_URI);
			mess.Args.TabId = id;
			mess.Args.Uri = webview.Source;
			mess.Args.UriToShow = GetCorrectUriToShow(mess.Args.Uri);
			PostJsonToWebView(mess, UIControlsBrowser.CoreWebView2);
		}

		public void HandleTabDocumentTitleChange(int id, string url, string title)
        {
			UIMessages UTMess = new(UIMessages.MessageOptions.MG_UPDATE_TAB);
			UTMess.Args.TabId = id;
			UTMess.Args.Title = title;
			UTMess.Args.Uri = url;
			PostJsonToWebView(UTMess, UIControlsBrowser.CoreWebView2);
		}

		public void HandleTabHistory(int id, CoreWebView2 webview)
		{
			UIMessages mess = new(UIMessages.MessageOptions.MG_UPDATE_URI);
			mess.Args.TabId = id;
			mess.Args.Uri = webview.Source;
			mess.Args.Title = webview.DocumentTitle;
			mess.Args.CanGoBack = webview.CanGoBack;
			mess.Args.CanGoForward = webview.CanGoForward;

			//This is something different to the C++ version. If you do not add this then you will get these URLs in your history and the URL shown in the browser will be the actual path.
			mess.Args.UriToShow = GetCorrectUriToShow(mess.Args.Uri);

			PostJsonToWebView(mess, UIControlsBrowser.CoreWebView2);
		}

		public void HandleTabNavigationStarting(int id, CoreWebView2 webview)
		{
			UIMessages mess = new(UIMessages.MessageOptions.MG_NAV_STARTING);
			mess.Args.TabId = id;
            PostJsonToWebView(mess, UIControlsBrowser.CoreWebView2);
		}

		public async void HandleTabNavigationCompleteAsync(int id, CoreWebView2 webview, CoreWebView2NavigationCompletedEventArgs args)
		{
			string getTitleScript = @"(() => {
				const titleTag = document.getElementsByTagName('title')[0];
				if (titleTag) 
					{
					return titleTag.innerHTML;
					}
				// No title tag, look for the file name				
				pathname = window.location.pathname;
				var filename = pathname.split('/').pop();
				if (filename) {
				return filename;
				}
				// No file name, look for the hostname
				const hostname =  window.location.hostname;
				if (hostname) {
				return hostname;
				}
				// Fallback: let the UI use a generic title
				return '';})();";

			string getFaviconURI = @"(() => {
        // Let the UI use a fallback favicon
            let faviconURI = '';
            let links = document.getElementsByTagName('link');
        // Test each link for a favicon
            Array.from(links).map(element => {
                let rel = element.rel;
        // Favicon is declared, try to get the href
                if (rel && (rel == 'shortcut icon' || rel == 'icon')) {
                    if (!element.href) {
                        return;
                    }
        // href to icon found, check it's full URI
                    try {
                        let urlParser = new URL(element.href);
                        faviconURI = urlParser.href;
                    } catch(e) {
        // Try prepending origin
                        let origin = window.location.origin;
                        let faviconLocation = `${origin}/${element.href}`;
                        try {
                            urlParser = new URL(faviconLocation);
                            faviconURI = urlParser.href;
                        } catch (e2) {
                            return;
                        }
                    }
                }
            });
            return faviconURI;
        })();";

			UIMessages UTMess = new(UIMessages.MessageOptions.MG_UPDATE_TAB);
			UTMess.Args.TabId = id;
			string title = await webview.ExecuteScriptAsync(getTitleScript);
			title = title.Substring(1, title.Length - 2);
			UTMess.Args.Title = title;
            PostJsonToWebView(UTMess, UIControlsBrowser.CoreWebView2);

			UIMessages UFMess = new(UIMessages.MessageOptions.MG_UPDATE_FAVICON);
			UFMess.Args.TabId = id;
			string favuri = await webview.ExecuteScriptAsync(getFaviconURI);
			favuri = favuri.Substring(1, favuri.Length - 2);
			UFMess.Args.Uri = favuri;
			PostJsonToWebView(UFMess, UIControlsBrowser.CoreWebView2);

			UIMessages NCMess = new(UIMessages.MessageOptions.MG_NAV_COMPLETED);
			NCMess.Args.TabId = id;
			NCMess.Args.IsError = (!args.IsSuccess);
            PostJsonToWebView(NCMess, UIControlsBrowser.CoreWebView2);
		}

		public void HandleTabSeucityUpdate(int id, CoreWebView2 webview, CoreWebView2DevToolsProtocolEventReceivedEventArgs args)
        {
			string jsonArgs = args.ParameterObjectAsJson;
			DevToolsEventMessage devMess = (DevToolsEventMessage)System.Text.Json.JsonSerializer.Deserialize(jsonArgs, typeof(DevToolsEventMessage));
			
			UIMessages mess = new(UIMessages.MessageOptions.MG_SECURITY_UPDATE);
			mess.Args.TabId = id;
			mess.Args.State = devMess.securityState;
            PostJsonToWebView(mess, UIControlsBrowser.CoreWebView2);
		}

		public void HandleTabMessageReceived(int id, CoreWebView2 webview, CoreWebView2WebMessageReceivedEventArgs eventArgs)
		{
			string source = webview.Source;
			string json = eventArgs.WebMessageAsJson;
			System.Diagnostics.Debug.Print("====================================================");
			System.Diagnostics.Debug.Print("HandleTabMessageReceived" + json);
			System.Diagnostics.Debug.Print("====================================================");
			UIMessages mess = (UIMessages)System.Text.Json.JsonSerializer.Deserialize(json, typeof(UIMessages));

			switch (mess.Message)
			{
				case UIMessages.MessageOptions.MG_GET_FAVORITES:
				case UIMessages.MessageOptions.MG_REMOVE_FAVORITE:
					{
						string fileURI = new Uri(GetFullPathFor(@"wvbrowser_ui\content_ui\favorites.html")).AbsoluteUri;
						// Only the favorites UI can request favorites
						if (fileURI.CompareTo(source) == 0)
						{
							mess.Args.TabId = id;
                            PostJsonToWebView(mess, UIControlsBrowser.CoreWebView2);
						}
					}
					break;
				case UIMessages.MessageOptions.MG_GET_SETTINGS:
					{
						string fileURI = new Uri(GetFullPathFor(@"wvbrowser_ui\content_ui\settings.html")).AbsoluteUri;
						// Only the settings UI can request settings
						if (fileURI.CompareTo(source) == 0)
						{
							mess.Args.TabId = id;
                            PostJsonToWebView(mess, UIControlsBrowser.CoreWebView2);
						}
					}
					break;
				case UIMessages.MessageOptions.MG_CLEAR_CACHE:
					{
						string fileURI = new Uri(GetFullPathFor(@"wvbrowser_ui\content_ui\settings.html")).AbsoluteUri;
						// Only the settings UI can request cache clearing
						if (fileURI.CompareTo(source) == 0)
						{
							ClearContentCacheAsync();
							mess.Args.Content = true;
							
							ClearControlsCache();
							mess.Args.Controls = true;

                            PostJsonToWebView(mess, TabCollection[GetActiveTab()].CoreWebView2);
						}
					}
					break;

				case UIMessages.MessageOptions.MG_CLEAR_COOKIES:
					{
						string fileURI = new Uri(GetFullPathFor("wvbrowser_ui\\content_ui\\settings.html")).AbsoluteUri;
						// Only the settings UI can request cache clearing
						if (fileURI.CompareTo(source) == 0)
						{
							ClearContentCookies() ;
							mess.Args.Content = true;

							ClearControlCookies();
							mess.Args.Controls = true;

                            PostJsonToWebView(mess, TabCollection[GetActiveTab()].CoreWebView2);
						}
					}
					break;
				case UIMessages.MessageOptions.MG_GET_HISTORY:
				case UIMessages.MessageOptions.MG_REMOVE_HISTORY_ITEM:
				case UIMessages.MessageOptions.MG_CLEAR_HISTORY:
					{
						string fileURI = new Uri(GetFullPathFor("wvbrowser_ui\\content_ui\\history.html")).AbsoluteUri;
						// Only the history UI can request history
						if (fileURI.CompareTo(source) == 0)
						{
							mess.Args.TabId = id;
                            PostJsonToWebView(mess, UIControlsBrowser.CoreWebView2);
						}
					}
					break;
				default:
					{
						System.Diagnostics.Debug.Print("Unexpected message\n");
					}
					break;
			}

		}

		private async void ClearContentCacheAsync()
        {
			string json = await TabCollection[GetActiveTab()].CoreWebView2.CallDevToolsProtocolMethodAsync("Network.clearBrowserCache", "{}");
			System.Diagnostics.Debug.Print(json);
		}

		private async void ClearControlsCache()
        {
			string json = await UIControlsBrowser.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.clearBrowserCache", "{}");
			System.Diagnostics.Debug.Print(json);
		}

		private async void ClearContentCookies()
        {
			string json = await TabCollection[GetActiveTab()].CoreWebView2.CallDevToolsProtocolMethodAsync("Network.clearBrowserCookies", "{}");
			System.Diagnostics.Debug.Print(json);
		}

		private async void ClearControlCookies()
        {
			string json = await UIControlsBrowser.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.clearBrowserCookies", "{}");
			System.Diagnostics.Debug.Print(json);
		}


		private int GetTabCollectionIdexForID(int id)
        {
			foreach (Tab t in TabCollection)
			{
				if (t.Tabid == id)
				{
					return TabCollection.IndexOf(t);
				}
			}
			return 0;
		}
		private int GetActiveTab()
        {
			foreach (Tab t in TabCollection)
            {
				if (t.ShouldBeActive)
                {
					return TabCollection.IndexOf(t);
                }
            }
			return 0;
        }

		private static string GetBrowserSchemeUrl(BrowserSchemeUrls urlType)
        {
			string url = @"wvbrowser_ui\content_ui\settings.html";

			switch (urlType)
            {
				case BrowserSchemeUrls.Favorites:
					url = @"wvbrowser_ui\content_ui\favorites.html";
					break;

				case BrowserSchemeUrls.Settings:
					url = @"wvbrowser_ui\content_ui\settings.html";
					break;

				case BrowserSchemeUrls.History:
					url = @"wvbrowser_ui\content_ui\history.html";
					break;
            }
			
			return GetFullPathFor(url);
		}
		private static string GetFullPathFor(string pathEnding)
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location), pathEnding); // Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return path;

        }

        private static string GetAppDataDirectory(string webview2foldername)
        {
            string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "WebView2CSharp", webview2foldername);
            System.IO.Directory.CreateDirectory(path);
            return path;
        }

		private static string GetCorrectUriToShow(string url)
		{
			if (url.Contains(@"wvbrowser_ui/content_ui/favorites.html"))
			{
				return "browser://favorites";
			}
			else if (url.Contains(@"wvbrowser_ui/content_ui/settings.html"))
			{
				return "browser://settings";
			}
			else if (url.Contains(@"wvbrowser_ui/content_ui/history.html"))
			{
				return "browser://history";
			}
			return null;
		}

		private static void PostJsonToWebView(UIMessages obj, CoreWebView2 webview)
        {
            lock (postMessageLock)
            {
				string json = System.Text.Json.JsonSerializer.Serialize(obj, typeof(UIMessages));
				webview.PostWebMessageAsJson(json);
				System.Diagnostics.Debug.Print("###############################################################");
				System.Diagnostics.Debug.Print("Message: " + obj.Message.ToString());
				System.Diagnostics.Debug.Print("JSON: " + json);
				System.Diagnostics.Debug.Print("###############################################################");
			}
        }

		#region "Notify Property Change"
		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion   //"Notify Property Change"
	}

}
