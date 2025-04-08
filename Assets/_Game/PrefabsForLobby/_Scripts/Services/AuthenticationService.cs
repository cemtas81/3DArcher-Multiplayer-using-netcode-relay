using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

public static class Authentication
{
    public static string PlayerId { get; private set; }

    private static int profileCounter = 0;
    private static readonly object counterLock = new object();
    public static async Task Login()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            var options = new InitializationOptions();

            // Use a unique profile for each editor instance to differentiate the clients
            options.SetProfile(GetUniqueProfile());

            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            PlayerId = AuthenticationService.Instance.PlayerId;
        }
    }


    private static string GetUniqueProfile()
    {
        lock (counterLock)
        {
            profileCounter++;
            return "Player_" + profileCounter.ToString();
        }
    }
}