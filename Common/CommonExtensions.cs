namespace Microsoft.Extensions.DependencyInjection;

public static class LabelingCommonExtensions
{

    extension(IServiceCollection services)
    {
        
        public IServiceCollection AddLabelingCommon()
        {
            services.AddServiceSharp();
            services.AddDbContext<LabelingContext>((serv, builder) =>
            {
                LabelingContext.Configure(builder, serv.GetRequiredService<ConfigProvider>());
            });

            return services;
        }

    }

}
