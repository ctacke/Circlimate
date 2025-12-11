internal class Program
{
    private static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // Add the API project
        var api = builder.AddProject<Projects.Circlimate_Api>("api")
            .WithExternalHttpEndpoints();

        // Add the Blazor project with a reference to the API
        builder.AddProject<Projects.Circlimate_Blazor>("blazor")
            .WithReference(api)
            .WithExternalHttpEndpoints();

        builder.Build().Run();
    }
}