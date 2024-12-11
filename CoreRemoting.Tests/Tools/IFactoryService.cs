using System.Threading.Tasks;

namespace CoreRemoting.Tests.Tools
{
    public interface IFactoryService
    {
        ITestService GetTestService();

        Task<ITestService> GetTestServiceAsync();
    }
}