namespace WebServy.Services;

public interface IService
{
    string DomainName { get; }

    string NotificationJavascriptHook { get; }
}
