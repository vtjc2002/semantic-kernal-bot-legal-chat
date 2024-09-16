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
public class SearchAgreementPlugin
{
    private readonly SearchClient _searchClient;
    private readonly AzureOpenAITextEmbeddingGenerationService _embeddingClient;
    private readonly string _searchSemanticConfig;
    private ITurnContext<IMessageActivity> _turnContext;
    private ConversationData _conversationData;

    public SearchAgreementPlugin(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext, SearchClient searchClient, AzureOpenAITextEmbeddingGenerationService embeddingClient, string searchSemanticConfig)
    {
        _searchClient = searchClient;
        _embeddingClient = embeddingClient;
        _searchSemanticConfig = searchSemanticConfig;
        _turnContext = turnContext;
        _conversationData = conversationData;
    }


    /// <summary>
    /// Search agreement for information.
    /// </summary>
    /// <param name="agreement"></param>
    /// <returns></returns>
    [KernelFunction, Description("Search the agreement for information that's not handled by other plugins.")]
    public async Task<string> FindInfoAsync([Description("File name of the agreement")] string agreement, [Description("User question in terms of stock purchase agreement or Securites Purchase Agreement or Asset Purchase Agreement")] string query)
    {
        await _turnContext.SendActivityAsync($"Searching ...{@agreement}");

        var embedding = await _embeddingClient.GenerateEmbeddingsAsync(new List<string> { query });
        var vector = embedding.First().ToArray();

        var searchOptions = new SearchOptions
        {
            VectorSearch = new()
            {
                Queries = { new VectorizedQuery(vector) { KNearestNeighborsCount = 3, Fields = { "vector" }, Exhaustive = true } }
            },
            SemanticSearch = new()
            {
                SemanticConfigurationName = _searchSemanticConfig,
                QueryCaption = new(QueryCaptionType.Extractive),
                QueryAnswer = new(QueryAnswerType.Extractive)
            },
            QueryType = SearchQueryType.Semantic,
            Size = 5,
            Filter = $"title eq '{agreement}'"

        };

        try
        {
            var response = await _searchClient.SearchAsync<RetrievedPassage>(searchOptions);
            var searchResults = response.Value.GetResults();
            if (searchResults.Count() == 0)
                return "No info found.";

            var textResults = "[SEARCH RESULTS]\n\n";

            foreach (SearchResult<RetrievedPassage> result in searchResults)
            {
                textResults += $"Title: {result.Document.Title} \n\n";
                // if (!result.Document.Path.IsNullOrEmpty())
                // {
                //     textResults += $"Link: \"{createSasUri(result.Document.Path)} \n\n\"";
                // }
                textResults += $"Content: {result.Document.Chunk}\n*****\n\n";
            }
            return textResults;


        }
        catch (Exception e)
        {
            return e.Message;
        }

    }


}