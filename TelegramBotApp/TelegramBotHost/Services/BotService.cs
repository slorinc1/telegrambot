using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotHost
{
    public class TelegramBotService : BackgroundService
    {
        private readonly IWebHostEnvironment _env;

        private readonly HttpClient _httpClient;

        private readonly BotToken _options;

        private readonly WordsService _wordsService;

        private MyBotClient _botClient;

        public TelegramBotService(
            IWebHostEnvironment env,
            WordsService wordsService,
            IOptions<BotToken> options,
            MyBotClient myBotClient)
        {
            _env = env;
            _options = options.Value;
            _wordsService = wordsService;
            _botClient = myBotClient;

            _httpClient = new HttpClient();

            Init().Wait();
        }

        public async Task Init()
        {
            var me = await _botClient.Bot.GetMeAsync();
            Console.Title = me.Username;

            _botClient.Bot.OnMessage += BotOnMessageReceived;
            _botClient.Bot.OnMessageEdited += BotOnMessageReceived;

            _botClient.Bot.OnCallbackQuery += BotOnCallbackQueryReceived;

            _botClient.Bot.OnInlineQuery += BotOnInlineQueryReceived;
            _botClient.Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            _botClient.Bot.OnReceiveError += BotOnReceiveError;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _botClient.Bot.StartReceiving(Array.Empty<UpdateType>());

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _botClient.Bot.StopReceiving();

            return base.StopAsync(cancellationToken);
        }

        private async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            if (message == null || message.Type != MessageType.Text)
                return;

            switch (message.Text.Split(' ').First())
            {
                // Send inline keyboard
                case "/inline":
                    await SendInlineKeyboard(message);
                    break;

                // send custom keyboard
                case "/word":
                    await SendReplyKeyboard(message);
                    break;

                default:
                    await Usage(message);
                    break;
            }
        }

        // Send inline keyboard
        // You can process responses in BotOnCallbackQueryReceived handler
        async Task SendInlineKeyboard(Message message)
        {
            await _botClient.Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            // Simulate longer running task
            await Task.Delay(500);

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Yes", "Yeee"),
                        InlineKeyboardButton.WithCallbackData("No", "Booo"),
                    }
            });

            await _botClient.Bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Are you a squirrel?",
                replyMarkup: inlineKeyboard
            );
        }

        async Task SendReplyKeyboard(Message message)
        {
            var newWord = await _wordsService.GetWordAsync();

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Infinitive", $"inf;{newWord}"),
                        InlineKeyboardButton.WithCallbackData("Gerund", $"ger;{newWord}"),
                        InlineKeyboardButton.WithCallbackData("Both", $"both;{newWord}"),
                    }
            });

            await _botClient.Bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: newWord,
                replyMarkup: inlineKeyboard
            );
        }

        async Task SendDocument(Message message, bool squarel)
        {
            await _botClient.Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

            string picName = $"{(squarel ? ("yes.jpg") : ("no.jpg")) }";
            string filePath = Path.Combine(_env.WebRootPath, picName);
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();
            await _botClient.Bot.SendPhotoAsync(
                chatId: message.Chat.Id,
                photo: new InputOnlineFile(fileStream, fileName),
                caption: squarel ? "Yeee" : "Boo"
            );
        }

        async Task Usage(Message message)
        {
            const string usage = "Usage:\n" +
                                    "/inline   - send inline keyboard\n" +
                                    "/word - ask a word\n" +
                                    "Contact:    lorinc@mail.com";
            await _botClient.Bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: usage,
                replyMarkup: new ReplyKeyboardRemove()
            );
        }

        // Process Inline Keyboard callback data
        private async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;

            if (callbackQuery.Data == "Yeee")
            {
                await SendDocument(callbackQuery.Message, true);
            }
            else if (callbackQuery.Data == "Boo")
            {
                await SendDocument(callbackQuery.Message, false);
            }

            if (callbackQuery.Data.StartsWith("inf"))
            {
                await SendAnswerToWord(callbackQuery.Message, callbackQuery.Data);
            }
            else if (callbackQuery.Data.StartsWith("ger"))
            {
                await SendAnswerToWord(callbackQuery.Message, callbackQuery.Data);
            }
            if (callbackQuery.Data.StartsWith("both"))
            {
                await SendAnswerToWord(callbackQuery.Message, callbackQuery.Data);
            }
        }

        private string GetWordType(string type)
        {
            return type.Split(new char[] { ';' })[0];
        }

        private string GetWord(string type)
        {
            return type.Split(new char[] { ';' })[1];
        }

        private async Task SendAnswerToWord(Message message, string data)
        {
            var w = GetWord(data);
            var t = GetWordType(data);

            var answer = await _wordsService.GetAnswerAsync(w);

            if (answer.Type == t)
            {
                await _botClient.Bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Yee :))))" );
            }
            else
            {
                await _botClient.Bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Almost");
            }

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(answer.Sentence1))
            {
                sb.AppendLine("To: " + answer.Sentence1);
            }

            if (!string.IsNullOrEmpty(answer.Sentence2))
            {
                sb.AppendLine("Ing: " + answer.Sentence2);
            }

            sb.AppendLine("/word - Ask a new word");

            await _botClient.Bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: sb.ToString()
            );
        }

        #region Inline Mode

        private async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            Console.WriteLine($"Received inline query from: {inlineQueryEventArgs.InlineQuery.From.Id}");

            InlineQueryResultBase[] results = {
                // displayed result
                new InlineQueryResultArticle(
                    id: "3",
                    title: "TgBots",
                    inputMessageContent: new InputTextMessageContent(
                        "hello"
                    )
                )
            };
            await _botClient.Bot.AnswerInlineQueryAsync(
                inlineQueryId: inlineQueryEventArgs.InlineQuery.Id,
                results: results,
                isPersonal: true,
                cacheTime: 0
            );
        }

        private void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        #endregion

        private void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message
            );
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _httpClient.GetAsync("https://telegrambothost.herokuapp.com/");
                }
                catch
                {
                }

                await Task.Delay(TimeSpan.FromMinutes(15));
            }
        }
    }
}
