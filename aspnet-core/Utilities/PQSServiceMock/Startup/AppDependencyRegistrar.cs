namespace PQSServiceMock.Startup;

public static class AppDependencyRegistrar
{
    public static WebApplication RegisterFeatures(this WebApplication app)
    {

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();


        return app;
    }

}
