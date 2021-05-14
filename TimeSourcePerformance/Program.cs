using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;

namespace TimeSourcePerformance
{
    public static class StaticTimeSource
    {
        public static Func<DateTime> UtcTime;

        static StaticTimeSource()
        {
            ResetImplementation();
        }

        public static void ResetImplementation()
        {
            UtcTime = () => DateTime.UtcNow;
        }

        public static DateTime UtcNow => UtcTime();
    }
    
    public class TimeSourceBenchmarks
    {
        private readonly ServiceProvider _normalTimeSource;
        private readonly ServiceProvider _staticController;
        private readonly ServiceProvider _staticWrappingTimeSource;

        public TimeSourceBenchmarks()
        {
            _normalTimeSource = new ServiceCollection()
                .AddSingleton<ITimeSource, TimeSource>()
                .AddScoped<IDateController, InjectedController>()
                .BuildServiceProvider();
            
            _staticWrappingTimeSource = new ServiceCollection()
                .AddSingleton<ITimeSource, TimeSourceWrappingStatic>()
                .AddScoped<IDateController, InjectedController>()
                .BuildServiceProvider();
            
            _staticController = new ServiceCollection()
                .AddScoped<IDateController, StaticController>()
                .BuildServiceProvider();
        }
        
        [Benchmark]
        public DateTime RawInjectedSingletonSource()
        {
            return _normalTimeSource.GetRequiredService<IDateController>().GetDate();
        }
        
        [Benchmark]
        public DateTime StaticWrappedInjectedSingletonSource()
        {
            return _staticWrappingTimeSource.GetRequiredService<IDateController>().GetDate();
        }
        
        [Benchmark]
        public DateTime StaticUnWrappedDirectCallNoInjection()
        {
            return _staticController.GetRequiredService<IDateController>().GetDate();
        }
        
        [Benchmark]
        public DateTime RawTimeSource()
        {
            return new TimeSource().UtcNow;
        }
        
        [Benchmark]
        public DateTime RawStaticSource()
        {
            return StaticTimeSource.UtcNow;
        }
    }

    public interface IDateController
    {
        DateTime GetDate();
    }

    public class StaticController : IDateController
    {
        public DateTime GetDate()
        {
            return StaticTimeSource.UtcNow;
        }
    }

    public class InjectedController : IDateController
    {
        private readonly ITimeSource _source;

        public InjectedController(ITimeSource source)
        {
            _source = source;
        }
        
        public DateTime GetDate()
        {
            return _source.UtcNow;
        }
    }
    
    public interface ITimeSource
    {
        public DateTime UtcNow { get; }
    }
    
    public class TimeSource : ITimeSource
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    public class TimeSourceWrappingStatic : ITimeSource
    {
        public DateTime UtcNow => StaticTimeSource.UtcNow;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<TimeSourceBenchmarks>();
        }
    }
    
    
}