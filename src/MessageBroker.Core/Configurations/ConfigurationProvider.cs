namespace MessageBroker.Core.Configurations
{
    public class ConfigurationProvider
    {
        
        private static ConfigurationProvider _shared;
        public static ConfigurationProvider Shared
        {
            get
            {
                if (_shared == null)
                    _shared = new ConfigurationProvider();
                return _shared;
            }
        }
        
        
        public BaseConfiguration BaseConfiguration { get; private set; }

        public ConfigurationProvider()
        {
            BaseConfiguration = BaseConfiguration.Default;
        }

        public void SetBaseConfiguration(BaseConfiguration baseConfiguration)
        {
            BaseConfiguration = baseConfiguration;
        }
        
        
    }
}