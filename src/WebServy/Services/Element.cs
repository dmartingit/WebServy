namespace WebServy.Services;

public sealed class Element : IService
{
    public string DomainName => "app.element.io";

    public string NotificationJavascriptHook => @"
var observer = new MutationObserver(mutationRecords => {
    var mutationRecord = mutationRecords.find(mutation => mutation.target.className && mutation.target.className.includes('mx_SpacePanel_badgeContainer'));
    if (mutationRecord && mutationRecord.target.hasChildNodes()) {
        window.chrome.webview.postMessage('Unknown');
    }
});
var container = document.documentElement || document.body;
var config = { childList: true, subtree: true };
observer.observe(container, config);
";
}
