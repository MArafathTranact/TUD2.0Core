using TUDCoreService2._0;
using TUDCoreService2._0.Utilities.Interface;
using TUDCoreService2._0.Utilities;
using TUDCoreService2._0.WorkStation;
using TUDCoreService2._0.Scale_Reader;
using TUDCoreService2._0.WebSocket;
using TUDCoreService2._0.Camera;
using TUDCoreService2._0.SignalR;
using MapsterMapper;
using Mapster;

public class Program
{
    public static void Main(string[] args)
    {


        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
        .UseWindowsService()
    //.ConfigureLogging(logging =>
    //{
    //    logging.ClearProviders();
    //    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    //    logging.AddNLog();  // Add NLog
    //})
    .ConfigureServices((hostContext, services) =>
    {
        // Load the configuration
        var configuration = hostContext.Configuration;
        IDictionary<string, IHandleScaleReader> keyValuePairs = new Dictionary<string, IHandleScaleReader>();

        // 1. Use default global settings — no custom mapping required
        var config = TypeAdapterConfig.GlobalSettings;
        // Optional: Scan assemblies if you want Mapster to pick up any [AdaptFrom]/[AdaptTo] attributes
        config.Scan(typeof(Program).Assembly);

        services.AddSingleton(config);

        // 2. Register IMapper for DI
        services.AddSingleton<IMapper, ServiceMapper>();


        services.AddHostedService<TudWorkerService>();
        services.AddSingleton<INLogger, NLogger>();
        services.AddSingleton<ICamera, Camera>();
        services.AddSingleton<ICameraGroup, CameraGroup>();
        services.AddSingleton<ITUDSettings, TUDSettings>();
        services.AddSingleton<IWorkStation, WorkStation>();
        services.AddSingleton<IAPI, API>();
        services.AddSingleton<IHandleCamera, HandleCamera>();
        services.AddSingleton<IHandleScaleReader, HandleScaleReader>();
        services.AddSingleton<IDictionary<string, IHandleScaleReader>>(keyValuePairs);
        services.AddSingleton<IWebSocketListener, WebSocketListener>();
        services.AddSingleton<ISignalRListener, SignalRListener>();
        services.AddSingleton<ISubscriptionHubClient, SubscriptionHubClient>();
        services.AddSingleton<IAPIConnection, APIConnection>();


        services.AddHttpClient();
    });
}