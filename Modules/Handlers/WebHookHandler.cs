using Linebot.Foundations.Bots;
using MediatR;
using Linebot.Extensions;
using Line.Messaging.Webhooks;


public record WebHookRequest() : IRequest<IResult> { }


public class WebHookHandler(IHttpContextAccessor accessor, IConfiguration config, ILineBotApp lineBot) : IRequestHandler<WebHookRequest, IResult>
{
    public async Task<IResult> Handle(WebHookRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var channelSecret = config.GetValue<string>("LineBot:ChannelSecret");
            var req = await accessor.GetHttpMessageRequest();
            var events = await req.GetWebhookEventsAsync(channelSecret);
            var app = lineBot;
            await app.RunAsync(events);
        }
        catch (Exception ex)
        {
            var error = ex.Message;
        }

        return TypedResults.Ok();
    }
}


// public static async Task<IResult> Webhook(
//         IHttpContextAccessor accessor,
//         IConfiguration config,
//         ILineBotApp linebot)
//     {
//         string? channelSecret = config.GetValue<string>(AppSettingKeys.ChannelSecret);
//         try
//         {
//             var request = await accessor.GetHttpMessageRequest();
//             var events = await request.GetWebhookEventsAsync(channelSecret);
//             var app = linebot;
//             await app.RunAsync(events);
//         }
//         catch (Exception ex)
//         {
//             return TypedResults.BadRequest();
//         }

//         return TypedResults.Ok();

//     }