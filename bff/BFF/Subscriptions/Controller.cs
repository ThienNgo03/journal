using BFF.Subscriptions.All;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;

namespace BFF.Subscriptions;

[Route("api/subscriptions")]
[ApiController]
public class Controller : ControllerBase
{
    private readonly IMapper _mapper;
    public Controller(IMapper mapper)
    {
        _mapper = mapper;
    }

    [HttpGet("all")]
    public async Task<IActionResult> All([FromQuery] All.Parameters parameters)
    {
        var item = new Item{};
        for (int i = 0; i < 5; i++) 
        {
            item.AppUsages.Add(new AppUsage());
        }

        _mapper.All.SetMonth(item);
        _mapper.All.SetCompany(item.AppUsages);
        _mapper.All.SetIcon(item.AppUsages);
        _mapper.All.SetSubscription(item.AppUsages);
        _mapper.All.SetUsagePercent(item.AppUsages);
        _mapper.All.SetPrice(item.AppUsages);
        _mapper.All.SetDiscount(item.AppUsages);
        _mapper.All.SetDiscountedPrice(item.AppUsages);
        _mapper.All.SetHex(item.AppUsages);
        _mapper.All.SetDayLeft(item.AppUsages);
        _mapper.All.SetIsPaid(item.AppUsages);
        _mapper.All.SetIsDiscountApplied(item.AppUsages);
        _mapper.All.SetIsDiscountAvailable(item.AppUsages);
        _mapper.All.SetCustomBrush(item.CustomBrush, item.AppUsages);

        return Ok(item);
    }
}
