namespace Armageddon.Web.Services;

public interface IApiAuthenticationStateProvider
{
    void NotifyUserAuthentication(string token);
    void NotifyUserLogout();
}
