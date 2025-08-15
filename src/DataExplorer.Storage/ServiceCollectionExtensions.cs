using Cloudbrick.DataExplorer.Storage.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.DataExplorer.Storage
{
    public static class ServiceCollectionExtensions
    {
        public static IResourceManagerServiceContext AddResourceManagerStorage(this IServiceCollection services)
        {
            services.AddResourceManagerStorageCore();

            return new ResourceManagerServiceContext(services);
        }
    }
}
