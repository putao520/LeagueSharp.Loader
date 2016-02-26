namespace LeagueSharp.Loader.Class
{
    using PlaySharp.Service;

    /// <summary>
    /// The web service.
    /// </summary>
    internal static class WebService
    {
        public static ServiceClient Client { get; }

        static WebService()
        {
            Client = new ServiceClient();
        }
    }
}