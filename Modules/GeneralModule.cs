using MediatR;

namespace Linebot.Modules;

public class GeneralModule : IModule
{
    public IServiceCollection RegisterModule(IServiceCollection services)
    {
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/linebot", async (IMediator mediator) =>
        {
            var resp = await mediator.Send(new WebHookRequest());

            return Results.Ok(resp);
        }).WithTags("Line Bot");

        // endpoints.MapGet("/group/{groupId}/rooms", async (IMediator mediator, string groupId, int? top) =>
        // {
        //     var resp = await mediator.Send(new GetRoomIdsByGroupQuery
        //     {
        //         GroupId = groupId,
        //         Top = top.HasValue ? top.Value : 10,
        //     });

        //     return Results.Ok(resp);
        // }).WithTags("Group");

        // endpoints.MapGet("/calendar/room/{roomId}", async (IMediator mediator, string roomId, string? startDate, string? endDate, string? timeZone, int? top) =>
        // {
        //     var resp = await mediator.Send(new GetCalendarViewByRoomQuery
        //     {
        //         RoomIds = [roomId],
        //         StartDate = !string.IsNullOrEmpty(startDate) ? startDate : string.Format("{0:D4}-{1:D2}-{2:D2}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day),
        //         EndDate = !string.IsNullOrEmpty(endDate) ? endDate : CaculateEndDate(startDate),
        //         Timezone = !string.IsNullOrEmpty(timeZone) ? timeZone : "UTC",
        //         Top = top.HasValue ? top.Value : 10,
        //     });

        //     return Results.Ok(resp);
        // }).WithTags("Calendar");

        // endpoints.MapGet("/calendar/group/{groupName}", async (IMediator mediator, GraphServiceClient client, string? groupName, string? timeZone) =>
        // {
        //     var group =
        //         await mediator.Send(new GetRoomGroupsQuery
        //         {
        //             Search = groupName,
        //             Top = 1
        //         });

        //     var room =
        //         await mediator.Send(new GetRoomIdsByGroupQuery
        //         {
        //             GroupId = group.FirstOrDefault()!.Id,
        //             Top = 100
        //         });

        //     var startDate = string.Format("{0:D4}-{1:D2}-{2:D2}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

        //     var roomIds = room.RoomIds;
        //     var resp = await mediator.Send(new GetCalendarViewByRoomQuery
        //     {
        //         RoomIds = roomIds,
        //         StartDate = startDate,
        //         EndDate = CaculateEndDate(startDate),
        //         Timezone = !string.IsNullOrEmpty(timeZone) ? timeZone : "UTC",
        //         Top = 1000
        //     });

        //     return Results.Ok(resp);

        // }).WithTags("Calendar");

        return endpoints;
    }
}
