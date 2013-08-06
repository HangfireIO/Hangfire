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
            using (var client = Factory.CreateClient())
            {
                client.Enqueue(typeof (TWorker), arg);
            }
        }
    }
}
