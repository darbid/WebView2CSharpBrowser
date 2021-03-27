using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WebView2CSharpBrowser
{
    public class UIMessages
    {
        public UIMessages()
        {
            Args = new();
        }
        public UIMessages(MessageOptions messageType)
        {
            Message = messageType;
            Args = new();
        }
        public enum MessageOptions
        {
            INVALID_TAB_ID = 0,
            MG_NAVIGATE = 1,
            MG_UPDATE_URI = 2,
            MG_GO_FORWARD = 3,
            MG_GO_BACK = 4,
            MG_NAV_STARTING = 5,
            MG_NAV_COMPLETED = 6,
            MG_RELOAD = 7,
            MG_CANCEL = 8,
            MG_NEW_WINDOW_REQUESTED = 9,
            MG_CREATE_TAB = 10,
            MG_UPDATE_TAB = 11,
            MG_SWITCH_TAB = 12,
            MG_CLOSE_TAB = 13,
            MG_CLOSE_WINDOW = 14,
            MG_SHOW_OPTIONS = 15,
            MG_HIDE_OPTIONS = 16,
            MG_OPTIONS_LOST_FOCUS = 17,
            MG_OPTION_SELECTED = 18,
            MG_SECURITY_UPDATE = 19,
            MG_UPDATE_FAVICON = 20,
            MG_GET_SETTINGS = 21,
            MG_GET_FAVORITES = 22,
            MG_REMOVE_FAVORITE = 23,
            MG_CLEAR_CACHE = 24,
            MG_CLEAR_COOKIES = 25,
            MG_GET_HISTORY = 26,
            MG_REMOVE_HISTORY_ITEM = 27,
            MG_CLEAR_HISTORY = 28
        }

        [JsonPropertyName("message")]
        public MessageOptions Message { get; set; }

        [JsonPropertyName("args")]
        public Arguments Args { get; set; }

    }

    public class Arguments
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("tabId")]
        public int TabId { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("isError")]
        public bool IsError { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("encodedSearchURI")]
        public string EncodedSearchURI { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("uriToShow")]
        public string UriToShow { get; set; }

        [JsonPropertyName("canGoBack")]
        public bool CanGoBack { get; set; }

        [JsonPropertyName("canGoForward")]
        public bool CanGoForward { get; set; }

        [JsonPropertyName("content")]
        public bool Content { get; set; }

        [JsonPropertyName("controls")]
        public bool Controls { get; set; }

        [JsonPropertyName("favorites")]
        public List<Favorite> Favorites { get; set; }

        [JsonPropertyName("from")]
        public int From { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("items")]
        public List<HistoryItems> Items { get; set; }

        [JsonPropertyName("settings")]
        public BrowserSettings Settings { get; set; }
    }

    public class BrowserSettings
    {
        [JsonPropertyName("scriptsEnabled")]
        public bool ScriptsEnabled { get; set; }

        [JsonPropertyName("blockPopups")]
        public bool BlockPopups { get; set; }
    }

    public class HistoryItems
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("item")]
        public HistoryItem Item { get; set; }
    }

    public class HistoryItem
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("favicon")]
        public string Favicon { get; set; }
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }

    public class InsecureContentStatus
    {
        public bool containedMixedForm { get; set; }
        public bool displayedContentWithCertErrors { get; set; }
        public string displayedInsecureContentStyle { get; set; }
        public bool displayedMixedContent { get; set; }
        public bool ranContentWithCertErrors { get; set; }
        public string ranInsecureContentStyle { get; set; }
        public bool ranMixedContent { get; set; }
    }

    public class DevToolsEventMessage
    {
        public List<object> explanations { get; set; }
        public InsecureContentStatus insecureContentStatus { get; set; }
        public bool schemeIsCryptographic { get; set; }
        public string securityState { get; set; }
        public string summary { get; set; }
    }

    public class Favorite
    {
        public string uri { get; set; }
        public object uriToShow { get; set; }
        public string title { get; set; }
        public string favicon { get; set; }
    }
}




