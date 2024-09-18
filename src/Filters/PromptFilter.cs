
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Microsoft.BotBuilderSamples;

public class PromptFilter : IPromptRenderFilter
{

    private readonly ILogger<PromptFilter> _logger;

    public PromptFilter(ILogger<PromptFilter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Log the rendered prompt to LLM.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {

        await next(context);

        _logger.LogDebug("PromptFilter.OnPromptRenderAsync RenderedPrompt: {0}", context.RenderedPrompt);
        
    }
}
