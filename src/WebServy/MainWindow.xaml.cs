using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Web.WebView2.Wpf;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using WebServy.Data;
using WebServy.Services;

namespace WebServy;

public partial class MainWindow : Window
{
    private readonly AppState appState = new();
    private readonly Dictionary<string, WebView2> webViews = new();

    public MainWindow()
    {
        foreach (var webService in appState.Config.WebServices)
        {
            if (webService.Enabled.Value) AddWebView(webService);
            webService.Enabled.Changed += (_, e) =>
            {
                if (e.Value)
                {
                    AddWebView(webService);
                }
                else
                {
                    webViews.Remove(webService.Uuid);
                }
            };
        }

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddWpfBlazorWebView();
        serviceCollection.AddSingleton(appState);
        Resources.Add("services", serviceCollection.BuildServiceProvider());

        InitializeComponent();
    }

    protected override void OnActivated(EventArgs e)
    {
        ClearWebServiceUnreadMessagesCount();
        base.OnActivated(e);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        if (appState.Config.WindowPlacement.Value is WindowPlacement windowPlacement)
        {
            Top = windowPlacement.Top;
            Left = windowPlacement.Left;
            Height = windowPlacement.Height;
            Width = windowPlacement.Width;
            if (windowPlacement.IsMaximized) WindowState = WindowState.Maximized;
        }

        appState.Config.WebServices.CollectionChanged += (_, e) => UpdateWebViews(e);
        appState.Config.LastUsedWebServiceUuid.Changed += (_, e) =>
        {
            UpdateLayout(e.Value);
            ClearWebServiceUnreadMessagesCount();
            ToastNotificationManagerCompat.History.Clear();
        };
        ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompatOnActivated;
        UpdateLayout(appState.Config.LastUsedWebServiceUuid.Value);
        base.OnSourceInitialized(e);
    }

    private void AddWebView(WebService webService)
    {
        WebView2 webView = new();
        webView.Source = new Uri(webService.Url);

        // NOTE(dmartin): Prevent flashing when switching services, because most web services have a defaulted dark theme.
        webView.DefaultBackgroundColor = System.Drawing.Color.FromArgb(0xff, 0x22, 0x22, 0x22);

        webView.CoreWebView2InitializationCompleted += (_, _) =>
        {
            if (appState.Services.FirstOrDefault(service => webService.Url.Contains(service.DomainName)) is IService service)
            {
                webView.CoreWebView2.DOMContentLoaded += async (_, _) => await webView.ExecuteScriptAsync(service.NotificationJavascriptHook);
                webView.CoreWebView2.WebMessageReceived += (_, e) =>
                {
                    var unreadMessages = e.TryGetWebMessageAsString();
                    new ToastContentBuilder().AddArgument(webService.Uuid).AddText($"{webService.Name}: You have {unreadMessages} unread messages.").Show();
                    int.TryParse(unreadMessages, out var count);
                    webService.UnreadMessagesCount.Value = count;
                };
            }

            // NOTE(dmartin): Currently Notification Permission Requests are auto-rejected and no event is raised:
            // https://github.com/MicrosoftEdge/WebView2Feedback/issues/308
            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2permissionkind?view=webview2-dotnet-1.0.864.35#fields
            webView.CoreWebView2.PermissionRequested += (_, e) =>
            {
                if (e.PermissionKind == Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.Notifications)
                {
                    e.State = Microsoft.Web.WebView2.Core.CoreWebView2PermissionState.Allow;
                }
            };

            webView.CoreWebView2.ProcessFailed += (_, e) => webView.Reload();
            webView.CoreWebView2.NewWindowRequested += (_, e) => e.Handled = OpenUrl(e.Uri);
        };

        webViews.Add(webService.Uuid, webView);
    }

    private void ClearWebServiceUnreadMessagesCount()
    {
        if (appState.Config.LastUsedWebServiceUuid.Value is string uuid)
        {
            if (appState.Config.WebServices.FirstOrDefault(ws => ws.Uuid == uuid) is WebService webService)
            {
                webService.UnreadMessagesCount.Value = 0;
            }
        }
    }

    private bool OpenUrl(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
        else
        {
            return false;
        }
        return true;
    }

    private void ToastNotificationManagerCompatOnActivated(ToastNotificationActivatedEventArgsCompat e)
    {
        Dispatcher.Invoke(() =>
        {
            appState.Config.LastUsedWebServiceUuid.Value = e.Argument;
            Activate();
        });
        ToastNotificationManagerCompat.History.Clear();
    }

    private void UpdateLayout(string? webServiceUuid)
    {
        if (webServiceUuid is not null)
        {
            grid.ColumnDefinitions[0].Width = new(100);
            grid.ColumnDefinitions[1].Width = new(1, GridUnitType.Star);
            var webService = appState.Config.WebServices.Single(ws => ws.Uuid == webServiceUuid);
            if (!webService.Enabled.Value) return;
            if (webViews.TryGetValue(webService.Uuid, out var webView))
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
                if (webService.Enabled.Value) AddWebView(webService);
                webService.Enabled.Changed += (_, e) =>
                {
                    if (e.Value)
                    {
                        AddWebView(webService);
                    }
                    else
                    {
                        webViews.Remove(webService.Uuid);
                    }
                };
            }
        }
    }

    private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            appState.Config.WindowPlacement.Value = new()
            {
                Top = RestoreBounds.Top,
                Left = RestoreBounds.Left,
                Height = RestoreBounds.Height,
                Width = RestoreBounds.Width,
                IsMaximized = true
            };
        }
        else
        {
            appState.Config.WindowPlacement.Value = new()
            {
                Top = Top,
                Left = Left,
                Height = Height,
                Width = Width,
                IsMaximized = false
            };
        }
    }
}

// NOTE(dmartin): WPF's runtime build cannot find the type 'local:Main' so make sure it knows atleast something.
public partial class Main { }
