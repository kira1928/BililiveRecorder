using BililiveRecorder.Flv.Pipeline.Actions;
using System.Collections.Generic;
using System.Linq;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 只录音频规则：移除所有视频标签，只保留音频标签
    /// </summary>
    public class AudioOnlyRule : ISimpleProcessingRule
    {
        public void Run(FlvProcessingContext context, System.Action next)
        {
            // 先执行后续规则
            next();

            // 遍历所有 action，移除视频数据
            context.Actions.RemoveAll(action =>
            {
                if (action is PipelineDataAction dataAction)
                {
                    // 过滤掉视频标签，只保留音频标签和脚本标签
                    var filteredTags = dataAction.Tags
                        .Where(tag => tag.Type != TagType.Video)
                        .ToList();
                    
                    // 如果过滤后没有音频标签了，移除整个 action
                    if (filteredTags.Count == 0)
                        return true;
                    
                    // 替换标签列表（使用 setter 替代 Clear/Add）
                    dataAction.Tags = filteredTags;
                    
                    return false;
                }
                else if (action is PipelineHeaderAction headerAction)
                {
                    // 对于 header，只保留音频头信息
                    // 移除视频 header，只保留音频 header
                    if (headerAction.VideoHeader != null)
                    {
                        headerAction.VideoHeader = null;
                    }
                    // 不移除整个 header action，因为还需要音频头
                    return false;
                }
                return false;
            });

            // 添加处理注释
            context.AddComment(new ProcessingComment(CommentType.Logging, true, "已移除视频数据（只录音频模式）"));
        }
    }
}
