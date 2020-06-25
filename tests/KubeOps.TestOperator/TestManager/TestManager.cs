using KubeOps.TestOperator.Entities;
using Microsoft.Extensions.Logging;

namespace KubeOps.TestOperator.TestManager
{
    public class TestManager : IManager
    {
        private readonly ILogger<TestManager> _logger;

        public TestManager(ILogger<TestManager> logger)
        {
            _logger = logger;
        }

        public void Created(TestEntity entity)
        {
            _logger.LogDebug(nameof(Created));
        }

        public void Updated(TestEntity entity)
        {
            _logger.LogDebug(nameof(Updated));
        }

        public void StatusModified(TestEntity entity)
        {
            _logger.LogDebug(nameof(StatusModified));
        }

        public void NotModified(TestEntity entity)
        {
            _logger.LogDebug(nameof(NotModified));
        }

        public void Deleted(TestEntity entity)
        {
            _logger.LogDebug(nameof(Deleted));
        }

        public void Finalized(TestEntity entity)
        {
            _logger.LogDebug(nameof(Finalized));
        }
    }
}
