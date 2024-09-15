using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

using Microsoft.SemanticKernel;
using Microsoft.BotBuilderSamples;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Models;
using System;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;



namespace Plugins;
public class GetAgreementFileNamePlugin
{
    private readonly AzureOpenAIClient _aoaiClient;
    private readonly SearchClient _searchClient;
    private readonly AzureOpenAITextEmbeddingGenerationService _embeddingClient;
    private readonly string _searchSemanticConfig;
    private ITurnContext<IMessageActivity> _turnContext;
    private ConversationData _conversationData;

    public GetAgreementFileNamePlugin(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext, AzureOpenAIClient aoaiClient, SearchClient searchClient, AzureOpenAITextEmbeddingGenerationService embeddingClient, string searchSemanticConfig)
    {
        _aoaiClient = aoaiClient;
        _searchClient = searchClient;
        _embeddingClient = embeddingClient;
        _searchSemanticConfig = searchSemanticConfig;
        _turnContext = turnContext;
        _conversationData = conversationData;
    }


    /// <summary>
    /// Find the agreement's file name.
    /// </summary>
    /// <param name="agreement"></param>
    /// <returns></returns>
    [KernelFunction, Description("Find the agreement's file name.")]
    [return: Description("The file name of the agreement with file extension.")]
    public async Task<string> FindInfoAsync([Description("Name of the agreement. Do not include the word agreement or contract in the response.")] string agreement)  
    {
        await _turnContext.SendActivityAsync($"Finding the agreement file info...");

        var embedding = await _embeddingClient.GenerateEmbeddingsAsync(new List<string> { agreement });
        var vector = embedding.First().ToArray();

        // Search for the first agreement chunk in the index to get the file name in title field
        var searchOptions = new SearchOptions
        {
            VectorSearch = new()
            {
                Queries = { new VectorizedQuery(vector) { KNearestNeighborsCount = 1, Fields = { "vector" }, Exhaustive = true } }
            },
            SemanticSearch = new()
            {
                SemanticConfigurationName = _searchSemanticConfig,
                QueryCaption = new(QueryCaptionType.Extractive),
                QueryAnswer = new(QueryAnswerType.Extractive)
            },
            QueryType = SearchQueryType.Semantic,
            Size = 1

        };

        try
        {
            var response = await _searchClient.SearchAsync<RetrievedPassage>(searchOptions);
            var searchResults = response.Value.GetResults();
            if (searchResults.Count() == 0)
                return "No agreement found.";
            var filename = response.Value.GetResults().First().Document.Title;

            //save to conversation data to be used for later conversation
            _conversationData.History.Add(new ConversationTurn { Role = "assistant", Message = $"agreement file name is {@filename}" });

            return filename;
        }
        catch (Exception e)
        {
            return e.Message;
        }

    }


}