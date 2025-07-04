using API.Data;
using API.Models;
using API.Services;
using API.Services.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AduDbcontext>(option => option.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IStudent, StudentsRepos>();
builder.Services.AddScoped<IUserRepos, UserRepos>();
builder.Services.AddScoped<IClassRepos, ClassRepos>();
builder.Services.AddTransient<IEmailRepos, EmailRepos>();
builder.Services.AddScoped<IStatistical, StatisticalRepos>();
builder.Services.AddScoped<IShedulesRepos, ScheduleRepos>();
builder.Services.AddScoped<IAuditLogRepos, AuditLogRepos>();
builder.Services.AddScoped<ISubject ,SubjectRepos > ();
builder.Services.AddScoped<IRoleRepos, RoleRepos>();
builder.Services.AddScoped<IPermissionRepos, PermissionRepos>();
builder.Services.AddScoped<IAttendance , AttendanceRepos>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("https://localhost:7298")
              .AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"];
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // không delay khi hết hạn
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CreateUS", policy => policy.RequireClaim("Permission", "Create"));
    options.AddPolicy("DetailUS", policy => policy.RequireClaim("Permission", "Detail"));
    options.AddPolicy("EditUS", policy => policy.RequireClaim("Permission", "Edit"));
    options.AddPolicy("SearchUS", policy => policy.RequireClaim("Permission", "Search"));
    options.AddPolicy("ProcessComplaintUS", policy => policy.RequireClaim("Permission", "ProcessComplaint"));
    options.AddPolicy("AddRoleUS", policy => policy.RequireClaim("Permission", "AddRole"));
});
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EduStar", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT token như: Bearer {token}",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AduDbcontext>();
    // Chỉ tạo user nếu chưa có user nào trong hệ thống
    if (!context.Users.Any())
    {
        var defaultRole = context.Roles.FirstOrDefault(r => r.Id == 1);
        if (defaultRole == null)
        {
            throw new Exception("Không tìm thấy Role với Id = 1. Hãy kiểm tra lại dữ liệu trong bảng Roles.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "string",
            Email = "string@gmail.com",
            Statuss = true,
            CreateAt = DateTime.Now,
            Roles = new List<Role> { defaultRole }
        };

        user.PassWordHash = UserRepos.PasswordHasher.HashPassword("string");

        context.Users.Add(user);
        context.SaveChanges();
    }
}
//Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    app.UseCors("AllowAll");
    app.UseStaticFiles();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseMiddleware<CheckUserStatus>(); // Kiểm tra trạng thái người dùng trước khi xử lý yêu cầu
    app.UseAuthorization();

    app.MapControllers();

    app.Run();