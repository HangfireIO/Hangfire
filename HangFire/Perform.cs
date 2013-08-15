namespace HangFire
{
    public static class Perform
    {
        public static void Async<TWorker>()
            where TWorker : Worker
        {
            Async<TWorker>(null);
        }

        public static void Async<TWorker>(object arg)
            where TWorker : Worker
        {
            Client.Enqueue(typeof (TWorker), arg);
        }
    }
}
