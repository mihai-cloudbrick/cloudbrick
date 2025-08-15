using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.DataExplorer.Storage.Tests
{
    internal static class Constants
    {
        public static string AzureStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=devcloudbrickssa;AccountKey=D7jVK7GxM2geNeZ9bUuEhmpAfhq/Gtk2CjuMJX0YkpK+Yd7nxITRPErANdpcKdv4P0IqAqpNHtD5+AStz5Gs0g==;EndpointSuffix=core.windows.net";
        public static string SqlConnectionString = "Server=tcp:devcloudbrickssrv.database.windows.net,1433;Initial Catalog=devcloudbricksdb01;Persist Security Info=False;User ID=cloudbricks;Password=Q!w2e3r4t5y6;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        public static string CosmosAccountEndpoint = "https://devcloudbrickscosmos.documents.azure.com:443/";
        public static string CosmosAccountKey = "0QQl4akwUn0Z66VE4kN9cBG09MeFfMu5tzTrd710YMI9RUocNHEg5l0ueSX1udqAYdk1O29yPl3KACDb3khVfQ==";
    }
}
