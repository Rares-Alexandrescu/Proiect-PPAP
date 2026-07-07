using Backend.Endpoints;
using Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDataProtection();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero 
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermiteAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("PermiteAngular");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); 
app.UseAuthorization();


app.MapRegisterEndpoint();
app.MapLoginEndpoint();
app.MapConfirmEndpoint();
app.MapResetPasswordEndpoint();
app.MapEditAccountEndpoint();
app.MapDashboardEndpoint();

app.Run();

