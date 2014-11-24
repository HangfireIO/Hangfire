using Sharp.Data.Databases;

namespace Hangfire.SharpData {
    public static class SharpDataBootstrapperConfigurationExtensions {
        public static SharpDataStorage UseOracleStorage(
          this IBootstrapperConfiguration configuration,
          string nameOrConnectionString) {
            var storage = new SharpDataStorage(nameOrConnectionString, DataProviderNames.OracleManaged);
            configuration.UseStorage(storage);
            return storage;
        } 
    }
}