using NexoStock.Application.Auth;
using NexoStock.Infrastructure;
using NexoStock.Infrastructure.Authorization;
using NexoStock.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ApplicationPolicies.ProfileRead, policy =>
        policy.AddRequirements(new PermissionRequirement(ApplicationPermissions.ProfileRead)));
    options.AddPolicy(ApplicationPolicies.UsersRead, policy =>
        policy.AddRequirements(new PermissionRequirement(ApplicationPermissions.UsersRead)));
    options.AddPolicy(ApplicationPolicies.UsersCreate, policy =>
        policy.AddRequirements(new PermissionRequirement(ApplicationPermissions.UsersCreate)));
    options.AddPolicy(ApplicationPolicies.ArticlesRead, policy =>
        policy.AddRequirements(new PermissionRequirement(ApplicationPermissions.ArticlesRead)));
    options.AddPolicy(ApplicationPolicies.ArticlesCreate, policy =>
        policy.AddRequirements(new PermissionRequirement(ApplicationPermissions.ArticlesCreate)));
});
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedRolesAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

//http://localhost:5028/swagger/index.html
