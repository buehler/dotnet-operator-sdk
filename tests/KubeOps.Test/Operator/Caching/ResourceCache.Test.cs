using FluentAssertions;
using k8s.Models;
using KubeOps.Operator.Caching;
using KubeOps.Test.TestEntities;
using Xunit;

namespace KubeOps.Test.Operator.Caching;

public class ResourceCacheTest
{
    private readonly ResourceCache<TestStatusEntity> _cache = new(new(new()), new());

    [Fact]
    public void Throws_When_Not_Found()
        => Assert.Throws<KeyNotFoundException>(() => _cache.Get("foobar"));

    [Theory]
    [MemberData(nameof(Data))]
    internal void Should_Correctly_Compare_Objects(
        TestStatusEntity? firstInsert,
        TestStatusEntity secondInsert,
        CacheComparisonResult expectedResult)
    {
        _cache.Clear();

        if (firstInsert != null)
        {
            _cache.Upsert(firstInsert, out _);
        }

        _cache.Upsert(secondInsert, out var result);

        result.Should().Be(expectedResult);
    }

    [Fact]
    public void Should_Remove_Object()
    {
        _cache.Clear();

        var entity = new TestStatusEntity { Metadata = new V1ObjectMeta { Uid = "test" } };
        _cache.Upsert(entity, out _);
        _cache.Get("test");
        _cache.Remove(entity);

        Assert.Throws<KeyNotFoundException>(() => _cache.Get("test"));
    }

    public static IEnumerable<object?[]> Data =>
        new List<object?[]>
        {
            new object?[]
            {
                null,
                new TestStatusEntity { Metadata = new V1ObjectMeta { Uid = "test" } },
                CacheComparisonResult.Other,
            },
            new object?[]
            {
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test" },
                    Spec = new TestStatusEntitySpec { SpecString = "test" },
                },
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test2" },
                    Spec = new TestStatusEntitySpec { SpecString = "test" },
                },
                CacheComparisonResult.Other,
            },
            new object?[]
            {
                new TestStatusEntity { Metadata = new V1ObjectMeta { Uid = "test" } },
                new TestStatusEntity { Metadata = new V1ObjectMeta { Uid = "test" } },
                CacheComparisonResult.Other,
            },
            new object?[]
            {
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test" },
                    Spec = new TestStatusEntitySpec { SpecString = "test" },
                },
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test" },
                    Spec = new TestStatusEntitySpec { SpecString = "test" },
                },
                CacheComparisonResult.Other,
            },
            new object?[]
            {
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test" },
                    Spec = new TestStatusEntitySpec { SpecString = "test" },
                },
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test" },
                    Spec = new TestStatusEntitySpec { SpecString = "test2" },
                },
                CacheComparisonResult.Other,
            },
            new object?[]
            {
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test" },
                    Status = new TestStatusEntityStatus { StatusString = "status" },
                },
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test" },
                    Status = new TestStatusEntityStatus { StatusString = "status" },
                },
                CacheComparisonResult.Other,
            },
            new object?[]
            {
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test" },
                    Status = new TestStatusEntityStatus { StatusString = "status" },
                },
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test" },
                    Status = new TestStatusEntityStatus { StatusString = "status2" },
                },
                CacheComparisonResult.StatusModified,
            },
            new object?[]
            {
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test" },
                    Status = new TestStatusEntityStatus { StatusString = "status" },
                },
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test" },
#pragma warning disable 8625
                    Status = null,
#pragma warning restore 8625
                },
                CacheComparisonResult.StatusModified,
            },
            new object?[]
            {
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test" },
                    Status = new TestStatusEntityStatus
                    {
                        StatusString = "status",
                        StatusList = new List<ComplexStatusObject>
                        {
                            new()
                            {
                                ObjectName = "status", LastModified = DateTime.Parse("2020-01-01"),
                            },
                        },
                    },
                },
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test" },
                    Status = new TestStatusEntityStatus
                    {
                        StatusString = "status",
                        StatusList = new List<ComplexStatusObject>
                        {
                            new()
                            {
                                ObjectName = "status", LastModified = DateTime.Parse("2020-01-02"),
                            },
                        },
                    },
                },
                CacheComparisonResult.StatusModified,
            },
            new object?[]
            {
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test", Finalizers = new List<string> { "f1" } },
                },
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test", Finalizers = new List<string> { "f2" } },
                },
                CacheComparisonResult.FinalizersModified,
            },
            new object?[]
            {
                new TestStatusEntity
                {
                    Metadata = new V1ObjectMeta { Uid = "test", Finalizers = new List<string> { "f1" } },
                },
                new TestStatusEntity { Metadata = new V1ObjectMeta { Uid = "test" }, },
                CacheComparisonResult.FinalizersModified,
            },
        };
}
