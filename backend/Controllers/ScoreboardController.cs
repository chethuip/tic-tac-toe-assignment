using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/scoreboard")]
public sealed class ScoreboardController(GameService service) : ControllerBase
{
    [HttpGet]
    public ActionResult<Scoreboard> Get() => Ok(service.GetScoreboard());

    [HttpPost("reset")]
    public ActionResult<Scoreboard> Reset() => Ok(service.ResetScoreboard());
}