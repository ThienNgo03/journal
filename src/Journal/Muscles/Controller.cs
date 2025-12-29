using Journal.Models.PaginationResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Journal.Muscles;

[ApiController]
[Authorize]
[AllowAnonymous]
[Route("api/muscles")]
public class Controller(IMessageBus messageBus, 
                        JournalDbContext context, 
                        ILogger<Controller> logger, 
                        IHubContext<Hub> hubContext) : ControllerBase
{
    private readonly IMessageBus _messageBus = messageBus;
    private readonly JournalDbContext _context = context;
    private readonly ILogger<Controller> _logger = logger;
    private readonly IHubContext<Hub> _hubContext = hubContext;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Get.Parameters parameters)
    {
        var query = _context.Muscles.AsQueryable();
        var all = query;

        if (!string.IsNullOrEmpty(parameters.Ids))
        {
            var ids = parameters.Ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : (Guid?)null)
            .Where(guid => guid.HasValue)
            .Select(guid => guid.Value)
            .ToList();
            query = query.Where(x => ids.Contains(x.Id));
        }

        if (!string.IsNullOrEmpty(parameters.Name))
            query = query.Where(x => x.Name.Contains(parameters.Name));

        if (parameters.CreatedDate.HasValue)
            query = query.Where(x => x.CreatedDate == parameters.CreatedDate);

        if (parameters.LastUpdated.HasValue)
            query = query.Where(x => x.LastUpdated == parameters.LastUpdated);

        if (!string.IsNullOrEmpty(parameters.SortBy))
        {
            var sortBy = typeof(Table)
                .GetProperties()
                .FirstOrDefault(p => p.Name.Equals(parameters.SortBy, StringComparison.OrdinalIgnoreCase))
                ?.Name;
            if (sortBy != null)
            {
                query = parameters.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(x => EF.Property<object>(x, sortBy))
                    : query.OrderBy(x => EF.Property<object>(x, sortBy));
            }
        }

        if (parameters.PageSize.HasValue && parameters.PageIndex.HasValue && parameters.PageSize > 0 && parameters.PageIndex >= 0)
            query = query.Skip(parameters.PageIndex.Value * parameters.PageSize.Value).Take(parameters.PageSize.Value);


        var result = await query.AsNoTracking().ToListAsync();

        var paginationResults = new Builder<Table>()
          .WithAll(await all.CountAsync())
          .WithIndex(parameters.PageIndex)
          .WithSize(parameters.PageSize)
          .WithTotal(result.Count)
          .WithItems(result)
          .Build();

        return Ok(paginationResults);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Post.Payload payload)
    {
        //if (User.Identity is null)
        //    return Unauthorized();

        //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //if (userId is null)
        //    return Unauthorized("User Id not found");

        Table item = new()
        {
            Id = Guid.NewGuid(),
            Name = payload.Name,
            CreatedDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        _context.Muscles.Add(item);
        await _context.SaveChangesAsync();
        await _messageBus.PublishAsync(new Post.Messager.Message(item.Id));
        await _hubContext.Clients.All.SendAsync("muscle-created", item.Id);
        return CreatedAtAction(nameof(Get), item.Id);
    }

    [HttpPatch]
    public async Task<IActionResult> Patch([FromQuery] Guid id,
                                           [FromBody] JsonPatchDocument<Table> patchDoc,
                                           CancellationToken cancellationToken = default!)
    {
        //if (User.Identity is null)
        //    return Unauthorized();

        //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //if (userId is null)
        //    return Unauthorized("User Id not found");

        var changes = new List<(string Path, object? Value)>();
        foreach (var op in patchDoc.Operations)
        {
            if (op.OperationType != OperationType.Replace && op.OperationType != OperationType.Test)
                return BadRequest("Only Replace and Test operations are allowed in this patch request.");
            changes.Add((op.path, op.value));
        }

        if (patchDoc is null)
            return BadRequest("Patch document cannot be null.");

        var entity = await _context.Muscles.FindAsync(id, cancellationToken);
        if (entity == null)
            return NotFound(new ProblemDetails
            {
                Title = "Muscle not found",
                Detail = $"Muscle with ID {id} does not exist.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });

        patchDoc.ApplyTo(entity);

        entity.LastUpdated = DateTime.UtcNow;

        _context.Muscles.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await _hubContext.Clients.All.SendAsync("muscle-updated", entity.Id);
        await _messageBus.PublishAsync(new Patch.Messager.Message(entity.Id, changes));
        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] Update.Payload payload)
    {
        //if (User.Identity is null)
        //    return Unauthorized();

        //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //if (userId is null)
        //    return Unauthorized("User Id not found");

        var muscle = await _context.Muscles.FindAsync(payload.Id);
        if (muscle == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Muscle not found",
                Detail = $"Muscle with ID {payload.Id} does not exist.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        muscle.Name = payload.Name;
        muscle.LastUpdated = DateTime.UtcNow;
        _context.Muscles.Update(muscle);
        await _context.SaveChangesAsync();
        await _messageBus.PublishAsync(new Update.Messager.Message(payload.Id, muscle));
        await _hubContext.Clients.All.SendAsync("muscle-updated", payload.Id);
        return NoContent();
    }

    [HttpDelete]

    public async Task<IActionResult> Delete([FromQuery] Delete.Parameters parameters)
    {
        //if (User.Identity is null)
        //    return Unauthorized();

        //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //if (userId is null)
        //    return Unauthorized("User Id not found");

        var muscle = await _context.Muscles.FindAsync(parameters.Id);
        if (muscle == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Muscle not found",
                Detail = $"Muscle with ID {parameters.Id} does not exist.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        _context.Muscles.Remove(muscle);
        await _context.SaveChangesAsync();
        await _messageBus.PublishAsync(new Delete.Messager.Message(parameters.Id));
        await _hubContext.Clients.All.SendAsync("muscle-deleted", parameters.Id);
        return NoContent();
    }
}
