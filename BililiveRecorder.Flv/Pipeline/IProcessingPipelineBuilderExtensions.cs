using System;
using BililiveRecorder.Flv.Pipeline.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace BililiveRecorder.Flv.Pipeline
{
    public static class IProcessingPipelineBuilderExtensions
    {
        public static IProcessingPipelineBuilder ConfigureServices(this IProcessingPipelineBuilder builder, Action<IServiceCollection> configure)
        {
            configure?.Invoke(builder.ServiceCollection);
            return builder;
        }

        public static IProcessingPipelineBuilder AddRule<T>(this IProcessingPipelineBuilder builder) where T : IProcessingRule =>
            builder.AddRule((next, services) => ActivatorUtilities.GetServiceOrCreateInstance<T>(services) switch
            {
                ISimpleProcessingRule simple => context => simple.Run(context, () => next(context)),
                IFullProcessingRule full => context => full.Run(context, next),
                _ => throw new ArgumentException($"Type ({typeof(T).FullName}) does not ISimpleProcessingRule or IFullProcessingRule")
            });

        public static IProcessingPipelineBuilder AddRule<T>(this IProcessingPipelineBuilder builder, T instance) where T : IProcessingRule =>
            instance switch
            {
                ISimpleProcessingRule simple => builder.AddRule((next, services) => context => simple.Run(context, () => next(context))),
                IFullProcessingRule full => builder.AddRule((next, services) => context => full.Run(context, next)),
                _ => throw new ArgumentException($"Type ({typeof(T).FullName}) does not ISimpleProcessingRule or IFullProcessingRule")
            };

        public static IProcessingPipelineBuilder AddDefaultRules(this IProcessingPipelineBuilder builder) =>
            builder
            .AddRule<HandleEndTagRule>()
            .AddRule<HandleDelayedAudioHeaderRule>()
            // .AddRule<UpdateTimestampOffsetRule>()
            .AddRule<UpdateTimestampJumpRule>()
            .AddRule<HandleNewScriptRule>()
            .AddRule<HandleNewHeaderRule>()
            .AddRule<RemoveDuplicatedChunkRule>()
            ;

        public static IProcessingPipelineBuilder AddRemoveFillerDataRule(this IProcessingPipelineBuilder builder) =>
            builder.AddRule<RemoveFillerDataRule>();

        /// <summary>
        /// 添加只录音频规则（根据条件）
        /// </summary>
        /// <param name="builder">Pipeline 构建器</param>
        /// <param name="audioOnly">是否启用只录音频模式</param>
        public static IProcessingPipelineBuilder AddAudioOnlyRule(this IProcessingPipelineBuilder builder, bool audioOnly)
        {
            if (audioOnly)
                return builder.AddRule<AudioOnlyRule>();
            return builder;
        }
    }
}
