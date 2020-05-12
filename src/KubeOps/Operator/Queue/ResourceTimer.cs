using System;
using System.Threading.Tasks;
using System.Timers;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Queue
{
    internal class ResourceTimer<TEntity> : IDisposable
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        private readonly TEntity _resource;
        private readonly Func<TEntity, Task> _elapsedHandler;
        private readonly Timer _timer;

        public ResourceTimer(TEntity resource, TimeSpan delay, Func<TEntity, Task> elapsedHandler)
        {
            _resource = resource;
            _elapsedHandler = elapsedHandler;
            _timer = new Timer
            {
                AutoReset = false,
                Interval = delay.TotalMilliseconds,
            };
            _timer.Elapsed += TimerElapsed;
        }

        public void Start() => _timer.Start();

        public void Destroy()
        {
            _timer.Stop();
            _timer.Elapsed -= TimerElapsed;
            _timer.Dispose();
        }

        public void Dispose() => Destroy();

        private async void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            Destroy();
            await _elapsedHandler(_resource);
        }
    }
}
