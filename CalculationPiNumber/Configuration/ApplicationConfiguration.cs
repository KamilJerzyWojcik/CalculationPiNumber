using StackExchange.Redis;

namespace Configuration
{
    public class ApplicationConfiguration
    {
        private ApplicationConfiguration()
        {
        }
        private static readonly object padlock = new object();

        private static ApplicationConfiguration instance = null;

        public static ApplicationConfiguration Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new ApplicationConfiguration();
                    }
                    return instance;
                }
            }
        }

        public string QueueName => "rpc_queue29";

        public string QueueManagment => "rpc_queue_man29";

        public IDatabase context = ConnectionMultiplexer.Connect("localhost").GetDatabase();

        public string StopIdsKey => "stopIds";
    }
}
