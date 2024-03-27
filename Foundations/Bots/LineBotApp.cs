
using System.Net;
using Line.Messaging;
using Line.Messaging.Webhooks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;

namespace Linebot.Foundations.Bots;

public interface ILineBotApp
{
    Task RunAsync(IEnumerable<WebhookEvent> events);
}

public class LineBotApp : WebhookApplication, ILineBotApp
{
    private ILineMessagingClient MessagingClient { get; }
    private ILogger<LineBotApp> Logger { get; }
    private IConfiguration Config { get; }

    private string ServiceUir { get; } = WebUtility.HtmlEncode("https://75b1-2401-e180-8850-40c9-f9a3-8437-e2e0-684.ngrok-free.app");

    private string DATE_FORMAT { get; } = "yyyy-MM-dd tt h:mm";

    static Dictionary<string, ChatHistory> ChatHistoryByUser = new Dictionary<string, ChatHistory>();

    public LineBotApp(
        ILineMessagingClient lineMessagingClient,
        IConfiguration config,
        ILogger<LineBotApp> logger)
    {
        MessagingClient = lineMessagingClient;
        Config = config;
        Logger = logger;
    }

    protected override async Task OnMessageAsync(MessageEvent ev)
    {
        //var entry = await SourceState.FindAsync(ev.Source.Type.ToString(), ev.Source.Id);
        //var blobDirectoryName = ev.Source.Type + "_" + ev.Source.Id;

        var userName = await GetUserInfo(ev);

        switch (ev.Message.Type)
        {
            case EventMessageType.Text:
                var builder =
                    Kernel.CreateBuilder()
                    .AddOpenAIChatCompletion(
                        "gpt-4-0125-preview",
                         Config.GetValue<string>("Linebot:OpenAISecret"));

                // builder.Plugins.AddFromType<>();
                Kernel kernel = builder.Build();

                var history = GetHistory(ev.Source.UserId);
                if (history == null || history.Count() <= 0)
                    history = new ChatHistory(Config.GetValue<string>("Linebot:AIPersonality"));

                var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

                var responseMsg = "";

                history.AddUserMessage(((TextEventMessage)ev.Message).Text);

                OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                };

                // Get the response from the AI
                var result = chatCompletionService.GetChatMessageContentAsync(
                    history,
                    executionSettings: openAIPromptExecutionSettings,
                    kernel: kernel).Result;

                // Add the message from the agent to the chat history
                history.AddMessage(result.Role, result.Content ?? string.Empty);
                // Save the chat history
                SaveHistory(ev.Source.UserId, history);
                responseMsg = result.Content;

                var messageText = responseMsg;
                // string.IsNullOrWhiteSpace(userName)
                //     ? ((TextEventMessage)ev.Message).Text
                //     : $"{userName} 說了：{((TextEventMessage)ev.Message).Text}";

                try
                {
                    await EchoAsync(ev.ReplyToken, messageText);
                }
                catch (Exception)
                {
                    string[] messages = [messageText];
                    await MessagingClient.PushMessageAsync(userName, messages);
                }
                break;

            // case EventMessageType.Image:
            //     await EchoImageAsync(ev.ReplyToken, ev.Message.Id, blobDirectoryName);
            //     break;

            // case EventMessageType.Audio:
            // case EventMessageType.Video:
            // case EventMessageType.File:
            //     await UploadMediaContentAsync(ev.ReplyToken, ev.Message.Id, blobDirectoryName, ev.Message.Id);
            //     break;

            case EventMessageType.Location:
                var location = ((LocationEventMessage)ev.Message);
                await EchoAsync(ev.ReplyToken, $"@{location.Latitude},{location.Longitude}");
                break;

            case EventMessageType.Sticker:
                await ReplyRandomStickerAsync(ev.ReplyToken);
                break;

        }
    }

    protected override async Task OnFollowAsync(FollowEvent ev)
    {
        Console.WriteLine($"SourceType:{ev.Source.Type}, SourceId:{ev.Source.Id}");

        //await SourceState.AddAsync(ev.Source.Type.ToString(), ev.Source.Id);

        var userName = "";
        if (!string.IsNullOrEmpty(ev.Source.Id))
        {
            var userProfile = await MessagingClient.GetUserProfileAsync(ev.Source.Id);
            userName = userProfile?.DisplayName ?? "";
        }

        await MessagingClient.ReplyMessageAsync(ev.ReplyToken, $"Hello {userName}! Thank you for following !");
    }

    protected override async Task OnUnfollowAsync(UnfollowEvent ev)
    {
        await Task.Run(
            () => Console.WriteLine($"SourceType:{ev.Source.Type}, SourceId:{ev.Source.Id}"));
    }

    protected override async Task OnJoinAsync(JoinEvent ev)
    {
        Console.WriteLine($"SourceType:{ev.Source.Type}, SourceId:{ev.Source.Id}");
        await MessagingClient.ReplyMessageAsync(
            ev.ReplyToken,
            $"Thank you for letting me join your {ev.Source.Type.ToString().ToLower()}!");
    }

    protected override async Task OnLeaveAsync(LeaveEvent ev)
    {
        await Task.Run(
        () => Console.WriteLine($"SourceType:{ev.Source.Type}, SourceId:{ev.Source.Id}"));
    }

    protected override async Task OnBeaconAsync(BeaconEvent ev)
    {
        Console.WriteLine($"SourceType:{ev.Source.Type}, SourceId:{ev.Source.Id}");
        var message = "";
        switch (ev.Beacon.Type)
        {
            case BeaconType.Enter:
                message = "You entered the beacon area!";
                break;
            case BeaconType.Leave:
                message = "You leaved the beacon area!";
                break;
            case BeaconType.Banner:
                message = "You tapped the beacon banner!";
                break;
        }
        await MessagingClient.ReplyMessageAsync(ev.ReplyToken, $"{message}(Dm:{ev.Beacon.Dm}, Hwid:{ev.Beacon.Hwid})");
    }

    protected override async Task OnPostbackAsync(PostbackEvent ev)
    {
        Logger.LogInformation("PostbackEvent:{@ev}", ev);

        var userProfile = await MessagingClient.GetUserProfileAsync(ev.Source.Id);

    }

    private Task EchoAsync(string replyToken, string userMessage)
    {
        return MessagingClient.ReplyMessageAsync(replyToken, userMessage);
    }

    // private async Task EchoImageAsync(string replyToken, string messageId, string blobDirectoryName)
    // {
    //     var imageName = messageId + ".jpeg";
    //     var previewImageName = messageId + "_preview.jpeg";

    //     var imageStream = await MessagingClient.GetContentStreamAsync(messageId);

    //     var image = System.Drawing.Image.FromStream(imageStream);
    //     var previewImage = image.GetThumbnailImage((int)(image.Width * 0.25), (int)(image.Height * 0.25), () => false, IntPtr.Zero);

    //     var blobImagePath = await BlobStorage.UploadImageAsync(image, blobDirectoryName, imageName);
    //     var blobPreviewPath = await BlobStorage.UploadImageAsync(previewImage, blobDirectoryName, previewImageName);

    //     await MessagingClient.ReplyMessageAsync(replyToken, new[] { new ImageMessage(blobImagePath.ToString(), blobPreviewPath.ToString()) });
    // }

    // private async Task UploadMediaContentAsync(string replyToken, string messageId, string blobDirectoryName, string blobName)
    // {
    //     var stream = await MessagingClient.GetContentStreamAsync(messageId);
    //     var ext = GetFileExtension(stream.ContentHeaders.ContentType.MediaType);
    //     var uri = await BlobStorage.UploadFromStreamAsync(stream, blobDirectoryName, blobName + ext);
    //     await MessagingClient.ReplyMessageAsync(replyToken, uri.ToString());
    // }

    public async Task ReplyRandomStickerAsync(string replyToken)
    {
        //Sticker ID of basic stickers (packge ID =1)
        //see https://devdocs.line.me/files/sticker_list.pdf
        var stickerids = Enumerable.Range(1, 17)
            .Concat(Enumerable.Range(21, 1))
            .Concat(Enumerable.Range(100, 139 - 100 + 1))
            .Concat(Enumerable.Range(401, 430 - 400 + 1)).ToArray();

        var rand = new Random(Guid.NewGuid().GetHashCode());
        var stickerId = stickerids[rand.Next(stickerids.Length - 1)].ToString();
        await MessagingClient.ReplyMessageAsync(replyToken, new[] {
                        new StickerMessage("1", stickerId)
                    });
    }

    private string GetFileExtension(string mediaType)
    {
        switch (mediaType)
        {
            case "image/jpeg":
                return ".jpeg";
            case "audio/x-m4a":
                return ".m4a";
            case "video/mp4":
                return ".mp4";
            default:
                return "";
        }
    }

    async private Task<string> GetUserInfo(MessageEvent ev)
    {
        var userProfile = await MessagingClient.GetUserProfileAsync(ev.Source.Id);
        string userName = userProfile?.UserId ?? "";
        //Logger.LogInformation("User Info:{@user}", userProfile);
        return userName;
    }


    private ChatHistory GetHistory(string userId)
    {
        if (ChatHistoryByUser.ContainsKey(userId))
            return ChatHistoryByUser[userId];

        return new ChatHistory();
    }

    private void SaveHistory(string userId, ChatHistory chatHistory)
    {
        if (ChatHistoryByUser.ContainsKey(userId))
            ChatHistoryByUser[userId] = chatHistory;
        else
            ChatHistoryByUser.Add(userId, chatHistory);
    }
}