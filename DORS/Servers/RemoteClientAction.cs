namespace DORS.Servers
{
    public struct RemoteClientAction
    {
        public RemoteClient RemoteClient { get; }
        public object Action { get; }

        public RemoteClientAction(RemoteClient remoteClient, object action)
        {
            RemoteClient = remoteClient;
            Action = action;
        }
    }
}
