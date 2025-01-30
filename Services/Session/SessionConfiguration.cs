using System;

namespace ModernMusicPlayer.Services
{
    public class SessionConfiguration
    {
        public string HubUrl { get; set; } = "http://localhost:5000/sessionHub";
        public bool StartLocalServer { get; set; } = true;
        
        public static SessionConfiguration CreateLocalDevelopment()
        {
            return new SessionConfiguration
            {
                HubUrl = "http://localhost:5000/sessionHub",
                StartLocalServer = true
            };
        }

        public static SessionConfiguration CreateAzureProduction(string azureUrl)
        {
            return new SessionConfiguration
            {
                HubUrl = $"{azureUrl.TrimEnd('/')}/sessionHub",
                StartLocalServer = false
            };
        }
    }
}
