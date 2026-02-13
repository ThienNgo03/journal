using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Journal.Authentication;

[ApiController]
[Route("api/authentication")]
public class Controller : ControllerBase
{
    private readonly ILogger<Controller> _logger;
    private readonly IMessageBus _messageBus;
    private readonly Databases.Identity.IdentityContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly BlobContainerClient _blobContainerClient;
    private readonly IConfiguration _configuration;
    public Controller(ILogger<Controller> logger, 
        Databases.Identity.IdentityContext context, 
        UserManager<IdentityUser> userManager, 
        IConfiguration configuration,
        IMessageBus messageBus,
        BlobContainerClient blobContainerClient)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
        _messageBus = messageBus;
        _blobContainerClient = blobContainerClient;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var users = await _context.Users.ToListAsync();
        if (users == null || users.Count == 0)
        {
            return NotFound("No users found.");
        }
        return Ok(users);
    }

    [HttpPost]
    [Route("register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterAsync([FromForm] Register.Payload form)
    {
        var newAccount = new IdentityUser
        {
            UserName = form.UserName,
            Email = form.Email,
            PhoneNumber = form.PhoneNumber,
        };

        if (form.Password != form.ConfirmPassword)
            return BadRequest("Passwords do not match.");

        newAccount.EmailConfirmed = true;
        var result = await _userManager.CreateAsync(newAccount, form.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _messageBus.PublishAsync(new Register.Messager.Message(
                                           Guid.Parse(newAccount.Id),
                                           null,
                                           form.UserName,
                                           form.Email,
                                           form.PhoneNumber
         ));

        return NoContent();
    }

    [HttpPost]
    [Route("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync([FromBody] Signin.Payload payload)
    {
        var user = await _userManager.FindByEmailAsync(payload.Account);
        if (user == null)
        {
            return Unauthorized("Invalid email or password.");
        }

        var result = await _userManager.CheckPasswordAsync(user, payload.Password);
        if (!result)
        {
            return Unauthorized("Invalid email or password.");
        }

        var token = GenerateToken(payload.Account);
        var response = new
        {
            Token = token,
        };
        return CreatedAtAction(nameof(Get), response);
    }

    [HttpGet]
    [Route("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfileAsync()
    {
        var email = User.FindFirstValue(JwtRegisteredClaimNames.Email);
        if (email == null)
            return Unauthorized("Invalid token.");
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return NotFound("User not found.");

        var profile = new
        {
            user.UserName,
            user.Email,
            user.PhoneNumber
        };
        return Ok(profile);
    }

    private string GenerateToken(string email)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["JWT:Key"]!);
        var issuer = _configuration["JWT:Issuer"];
        var audience = _configuration["JWT:Audience"];

        var user = _context.Users.FirstOrDefault(u => u.Email == email);
        if (user == null)
            throw new InvalidOperationException("User not found");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var securityKey = new SymmetricSecurityKey(key);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
         
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
