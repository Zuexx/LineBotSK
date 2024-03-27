using System.Text;

namespace Linebot.Extensions;

public static class HttpContextAccessorExtension
{
    public async static Task<HttpRequestMessage> GetHttpMessageRequest(
        this IHttpContextAccessor accessor,
        string callbackUri = "http://localhost:5217")
    {
        if (accessor.HttpContext is null)
            throw new Exception("No HttpContext existing for further operations.");

        using (var reader = new StreamReader(accessor.HttpContext.Request.Body))
        {
            var content = await reader.ReadToEndAsync();
            var reqMessage = new HttpRequestMessage
            {
                RequestUri = new Uri($"{callbackUri}/callback"),
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };
            var signature = accessor.HttpContext.Request.Headers.Where(x => x.Key == "X-Line-Signature").First();
            reqMessage.Headers.Add(signature.Key, signature.Value.ToString());
            return reqMessage;
        }
    }

}