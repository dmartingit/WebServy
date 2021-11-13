namespace WebServy.Services;

public sealed class WhatsApp : IService
{
    public string DomainName => "web.whatsapp";

    public string NotificationJavascriptHook => @"
var observer = new MutationObserver(mutationRecords => {
    var mutationRecord = mutationRecords.find(mutation => mutation.target.ariaLabel && mutation.target.ariaLabel.includes('ungelesene Nachricht'));
    if (mutationRecord) {
        window.chrome.webview.postMessage(mutationRecord.target.textContent);
    }
    else {
        mutationRecords.forEach(mutation => {
            if (mutation.target.hasChildNodes()) {
                var childNodes = mutation.target.children;
                for (let i = 0; i < childNodes.length; ++i) {
                    var childNode = childNodes[i];
                    if (childNode.nodeName === 'DIV' && childNode.hasChildNodes()) {
                        var childChildNodes = childNode.children;
                        for (var j = 0; j < childChildNodes.length; ++j) {
                            var childChildNode = childChildNodes[j];
                            if (childChildNode.ariaLabel && childChildNode.ariaLabel.includes('ungelesene Nachricht')) {
                                window.chrome.webview.postMessage(childChildNode.textContent);
                            }
                        }
                    }
                }
            }
        });
    }
});
var container = document.documentElement || document.body;
var config = { attributeFilter: ['aria-label'], childList: true, subtree: true };
observer.observe(container, config);
";
}
