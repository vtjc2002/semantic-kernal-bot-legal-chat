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
public class FindBuyerSellerPlugin
{
    private readonly SearchClient _searchClient;
    private readonly AzureOpenAITextEmbeddingGenerationService _embeddingClient;
    private readonly string _searchSemanticConfig;
    private ITurnContext<IMessageActivity> _turnContext;
    private ConversationData _conversationData;

    public FindBuyerSellerPlugin(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext, SearchClient searchClient, AzureOpenAITextEmbeddingGenerationService embeddingClient, string searchSemanticConfig)
    {
        _searchClient = searchClient;
        _embeddingClient = embeddingClient;
        _searchSemanticConfig = searchSemanticConfig;
        _turnContext = turnContext;
        _conversationData = conversationData;
    }


    /// <summary>
    /// Find seller / buyer information.  Including seller law firm, buyer law firm, and counsel names.
    /// </summary>
    /// <param name="agreement"></param>
    /// <returns></returns>
    [KernelFunction, Description("Find information about the buyer or seller, seller or buyer law firms, and the counsel names.")]
    [return: Description("Include address and names if available.")]
    public async Task<string> FindInfoAsync([Description("File name of the agreement")] string agreement)
    {
        await _turnContext.SendActivityAsync($"Searching ...{@agreement}");

        var query = "with copies to or copy to";

        var embedding = await _embeddingClient.GenerateEmbeddingsAsync(new List<string> { query });
        var vector = embedding.First().ToArray();

        // Return the top 3 chunks that are most similar to the query and filter by agreement file name.
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
            Size = 3,
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