using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Azure.Search.Documents;
using Azure.Search.Documents.Models;

using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.BotBuilderSamples;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

using Models;


namespace Plugins;
public class GetAgreementFileNamePlugin
{
    private readonly SearchClient _searchClient;
    private readonly AzureOpenAITextEmbeddingGenerationService _embeddingClient;
    private readonly string _searchSemanticConfig;
    private ITurnContext<IMessageActivity> _turnContext;
    private ConversationData _conversationData;

    // Minimum score threshold to consider the search result as relevant.  This is used to filter out partial matches for file name.
    // You should play with this threshold to get the best results for your data.
    private readonly double _mimimumScoreThreshold = 0.6;

    public GetAgreementFileNamePlugin(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext, SearchClient searchClient, AzureOpenAITextEmbeddingGenerationService embeddingClient, string searchSemanticConfig)
    {
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
    [return: Description("The file name of the agreement with file extension. For example, 'Agreement.pdf'.")]
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
            // only the result must be higher than the threshold to avoid partial matches
            var response = await _searchClient.SearchAsync<RetrievedPassage>(searchOptions);
            var searchResults = response.Value.GetResults();
            if (searchResults.Count() == 0 || searchResults.First().Score < _mimimumScoreThreshold)
                return "No agreement found.";

            var filename = searchResults.First().Document.Title;

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