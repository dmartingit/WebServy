using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Wpf;
using System.Collections.Specialized;
using System.Windows;
using WebServy.Data;

namespace WebServy;

public partial class MainWindow : Window
{
    private readonly AppState appState = new();
    private readonly Dictionary<string, WebView2> webViews = new();

    public MainWindow()
    {
        foreach (WebService webService in appState.Config.WebServices)
        {
            AddWebView(webService);
        }

        appState.Config.WebServices.CollectionChanged += (_, e) => UpdateWebViews(e);
        appState.Config.LastUsedWebServiceUuid.Changed += (_, e) => UpdateLayout(e.Value);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddBlazorWebView();
        serviceCollection.AddSingleton(appState);
        Resources.Add("services", serviceCollection.BuildServiceProvider());

        InitializeComponent();

        UpdateLayout(appState.Config.LastUsedWebServiceUuid.Value);

    }

    private void UpdateWebViews(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (WebService webService in e.OldItems)
            {
                webViews.Remove(webService.Uuid);
            }
        }
        if (e.NewItems is not null)
        {
            foreach (WebService webService in e.NewItems)
            {
                AddWebView(webService);
            }
        }
    }

    private void UpdateLayout(string? webServiceUuid)
    {
        if (webServiceUuid is not null)
        {
            grid.ColumnDefinitions[0].Width = new(100);
            grid.ColumnDefinitions[1].Width = new(1, GridUnitType.Star);
            WebService webService = appState.Config.WebServices.Single(ws => ws.Uuid == webServiceUuid);
            if (webViews.TryGetValue(webService.Uuid, out WebView2? webView))
            {
                webViewContentControl.Content = webView;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
        else
        {
            grid.ColumnDefinitions[0].Width = new(1, GridUnitType.Star);
            grid.ColumnDefinitions[1].Width = GridLength.Auto;
            webViewContentControl.Content = null;
        }
    }

    private void AddWebView(WebService webService)
    {
        WebView2 webView = new();
        webView.Source = new(webService.Url);
        // NOTE(dmartin): Currently Notification Permission Requests are currently auto-rejected and no event is raised:
        // https://github.com/MicrosoftEdge/WebView2Feedback/issues/308
        webView.CoreWebView2InitializationCompleted += (_, _) =>
        {
            webView.CoreWebView2.PermissionRequested += (_, e) =>
            {
                if (e.PermissionKind == Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.Notifications)
                {
                    e.State = Microsoft.Web.WebView2.Core.CoreWebView2PermissionState.Allow;
                }
            };
        };
        webViews.Add(webService.Uuid, webView);
    }
}

// NOTE(dmartin): WPF's runtime build cannot find the type 'local:Main'" so declare it again.
public partial class Main { }
