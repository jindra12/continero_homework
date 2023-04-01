namespace Backend_Homework.UI
{
    public class Loading
    {
        private Task awaited;

        public Loading(Task awaited)
        {
            this.awaited = awaited ?? throw new ArgumentNullException(nameof(awaited));
        }

        public async Task Wait()
        {
            while(!this.awaited.IsCompleted)
            {
                Console.WriteLine("Loading...");
                await Task.Delay(5000);
            }
        }
    }
}