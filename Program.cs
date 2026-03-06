using System.ComponentModel.DataAnnotations;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
builder.Services.AddOpenApi();

// JWT Security setup
var secretKey = "super_secret_key_that_is_at_least_32_characters_long";
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// --- Middleware Pipeline ---

// 1. Standardized Error Handling (Step 3)
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        var errorResponse = new { error = "Internal server error." };
        await context.Response.WriteAsJsonAsync(errorResponse);
    });
});
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 2. Security Middleware (Step 4)
app.UseAuthentication();
app.UseAuthorization();

// 3. Custom Logging Middleware (Step 2 - "Logging last")
app.Use(async (context, next) =>
{
    var request = context.Request;
    Console.WriteLine($"[AUDIT-LOG] Request: {request.Method} {request.Path} started at {DateTime.UtcNow}");
    
    await next();
    
    var response = context.Response;
    Console.WriteLine($"[AUDIT-LOG] Response: {request.Method} {request.Path} finished with Status: {response.StatusCode} at {DateTime.UtcNow}");
});

// --- Data & Helpers ---

// Thread-safe in-memory data store
var users = new ConcurrentDictionary<int, User>();
users.TryAdd(1, new User { Id = 1, Name = "John Doe", Email = "john.doe@techhive.com", Role = "Developer" });
users.TryAdd(2, new User { Id = 2, Name = "Jane Smith", Email = "jane.smith@techhive.com", Role = "Manager" });

// Helper for validation
IResult ValidateModel<T>(T model)
{
    var validationResults = new List<ValidationResult>();
    var context = new ValidationContext(model!);
    if (!Validator.TryValidateObject(model!, context, validationResults, true))
    {
        return Results.ValidationProblem(validationResults.ToDictionary(
            v => v.MemberNames.FirstOrDefault() ?? "Error",
            v => new[] { v.ErrorMessage ?? "Invalid value" }));
    }
    return null!;
}

// --- Endpoints ---

// Login Endpoint for JWT Generation
app.MapPost("/login", (LoginRequest login) =>
{
    // Simplified logic: Check for specific username/password
    if (login.Username == "admin" && login.Password == "password")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, login.Username) }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Results.Ok(new { Token = tokenHandler.WriteToken(token) });
    }
    return Results.Unauthorized();
})
.WithName("Login");

// Root Endpoint (For browser testing)
app.MapGet("/", () => Results.Ok(new { Message = "Welcome to the TechHive User Management API. Access endpoints via /users." }));

// Secure Endpoints
var userGroup = app.MapGroup("/users").RequireAuthorization();

// GET: Retrieve users with pagination
userGroup.MapGet("/", (int page = 1, int pageSize = 10) =>
{
    var items = users.Values
        .OrderBy(u => u.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(u => new UserDto(u.Id, u.Name, u.Email, u.Role));

    return Results.Ok(new { Page = page, PageSize = pageSize, Data = items, TotalCount = users.Count });
});

// GET: Retrieve a specific user by ID
userGroup.MapGet("/{id}", (int id) =>
{
    if (users.TryGetValue(id, out var user))
    {
        return Results.Ok(new UserDto(user.Id, user.Name, user.Email, user.Role));
    }
    return Results.NotFound(new { Message = $"User with ID {id} not found." });
});

// POST: Add a new user
userGroup.MapPost("/", (CreateUpdateUserDto input) =>
{
    var validationError = ValidateModel(input);
    if (validationError != null) return validationError;

    var newId = users.Keys.Any() ? users.Keys.Max() + 1 : 1;
    var newUser = new User { Id = newId, Name = input.Name!, Email = input.Email!, Role = input.Role! };
    
    if (users.TryAdd(newId, newUser))
    {
        return Results.Created($"/users/{newId}", new UserDto(newUser.Id, newUser.Name, newUser.Email, newUser.Role));
    }
    return Results.StatusCode(500);
});

// PUT: Update an existing user
userGroup.MapPut("/{id}", (int id, CreateUpdateUserDto input) =>
{
    if (!users.ContainsKey(id))
    {
        return Results.NotFound(new { Message = $"User with ID {id} not found." });
    }

    var validationError = ValidateModel(input);
    if (validationError != null) return validationError;

    users.AddOrUpdate(id, 
        _ => new User { Id = id, Name = input.Name!, Email = input.Email!, Role = input.Role! },
        (_, existing) => {
            existing.Name = input.Name!;
            existing.Email = input.Email!;
            existing.Role = input.Role!;
            return existing;
        });

    return Results.NoContent();
});

// DELETE: Remove a user by ID
userGroup.MapDelete("/{id}", (int id) =>
{
    if (users.TryRemove(id, out _))
    {
        return Results.NoContent();
    }
    return Results.NotFound(new { Message = $"User with ID {id} not found." });
});

app.Run();

// --- Models & DTOs ---

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public record UserDto(int Id, string Name, string Email, string Role);

public class CreateUpdateUserDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Role is required")]
    public string? Role { get; set; }
}

public record LoginRequest(string Username, string Password);
