namespace AlidnsSyncService
{
    public interface IPush
    {
        bool CanPush { get; }
        void Push(string title, string message);
    }
}
