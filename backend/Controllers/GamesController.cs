using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/games")]
public sealed class GamesController(GameService service) : ControllerBase
{
    [HttpPost]
    public ActionResult<GameResponse> Create(CreateGameRequest request) => Ok(service.Create(request.Mode));

    [HttpGet("{id:guid}")]
    public ActionResult<GameResponse> Get(Guid id) => service.Get(id) is { } game ? Ok(game) : NotFound();

    [HttpPost("{id:guid}/moves")]
    public ActionResult<GameResponse> Move(Guid id, MoveRequest request)
    {
        var result = service.Move(id, request);
        if (result.Response is null) return NotFound(result.Error);
        return result.Error is null ? Ok(result.Response) : BadRequest(new { message = result.Error, state = result.Response });
    }

    [HttpPost("{id:guid}/undo")]
    public ActionResult<GameResponse> Undo(Guid id)
    {
        var result = service.Undo(id);
        if (result.Response is null) return NotFound(result.Error);
        return result.Error is null ? Ok(result.Response) : BadRequest(new { message = result.Error, state = result.Response });
    }

    [HttpPost("{id:guid}/reset")]
    public ActionResult<GameResponse> Reset(Guid id) => service.Reset(id) is { } game ? Ok(game) : NotFound();
}