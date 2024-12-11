using System.Threading.Tasks;

namespace CoreRemoting.Tests.Tools
{
    public class FactoryService : IFactoryService
    {
        public ITestService GetTestService()
        {
            return new TestService();
        }

        public async Task<ITestService> GetTestServiceAsync()
        {
            await Task.Yield();
            return GetTestService();
        }
    }
}