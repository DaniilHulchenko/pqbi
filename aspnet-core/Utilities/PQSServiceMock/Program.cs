using PQSServiceMock.Startup;

var builder = WebApplication.CreateBuilder(args);


builder.Services.RegisterAllServices(builder.Environment);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.ConfigureSwagger();

app.UseAuthorization();

app.MapControllers();

app.Run();
