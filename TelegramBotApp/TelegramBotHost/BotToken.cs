namespace TelegramBotHost
{
    public class BotToken
    {
        public string Token { get; set; }

        public long Id { get; set; }

        public string Secret { get; set; }
    }

    public class AppConfiguration
    {
        public bool ShouldRun { get; set; }
    }
}
