using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KubeOps.Operator.Client;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Entities.Extensions;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Queue;
using KubeOps.Operator.Rbac;
using KubeOps.TestOperator.Entities;
using KubeOps.TestOperator.Finalizer;
using KubeOps.TestOperator.TestManager;
using Microsoft.Extensions.Logging;

namespace KubeOps.TestOperator.Controller
{
    [EntityRbac(typeof(TestEntity), Verbs = RbacVerb.All)]
    public class TestController : ResourceControllerBase<TestEntity>
    {
        private readonly IManager _manager;

        public TestController(IManager manager, ResourceServices<TestEntity> services) 
            : base(services)
        {
            _manager = manager;
        }

        protected override async Task<TimeSpan?> Created(TestEntity resource)
        {
            _manager.Created(resource);
            await resource.RegisterFinalizer<TestEntityFinalizer, TestEntity>(Services);
            return await base.Created(resource);
        }

        protected override async Task<TimeSpan?> Updated(TestEntity resource)
        {
            _manager.Updated(resource);
            return await base.Updated(resource);
        }

        protected override async Task<TimeSpan?> NotModified(TestEntity resource)
        {
            _manager.NotModified(resource);
            return await base.NotModified(resource);
        }

        protected override async Task StatusModified(TestEntity resource)
        {
            _manager.StatusModified(resource);
            await base.StatusModified(resource);
        }

        protected override async Task Deleted(TestEntity resource)
        {
            _manager.Deleted(resource);
            await base.Deleted(resource);
        }
    }
}
