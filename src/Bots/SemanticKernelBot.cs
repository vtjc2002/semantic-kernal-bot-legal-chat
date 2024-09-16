// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Storage.Blobs;

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

using Plugins;


namespace Microsoft.BotBuilderSamples
{
    public class SemanticKernelBot<T> : StateManagementBot<T> where T : Dialog
    {
        private Kernel kernel;
        private string _aoaiModel;
        private readonly AzureOpenAIClient _aoaiClient;
        private readonly SearchClient _searchClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly AzureOpenAITextEmbeddingGenerationService _embeddingsClient;

        private readonly string _welcomeMessage;
        private readonly List<string> _suggestedQuestions;
        private readonly string _searchSemanticConfig, _blobContainer;


        public SemanticKernelBot(
            IConfiguration config,
            ConversationState conversationState,
            UserState userState,
            AzureOpenAIClient aoaiClient,
            AzureOpenAITextEmbeddingGenerationService embeddingsClient,
            T dialog,
            SearchClient searchClient = null,
            BlobServiceClient blobServiceClient = null) :
            base(config, conversationState, userState, dialog)
        {
            _aoaiModel = config.GetValue<string>("AOAI_GPT_MODEL");
            _welcomeMessage = config.GetValue<string>("PROMPT_WELCOME_MESSAGE");

            _systemMessage = config.GetValue<string>("PROMPT_SYSTEM_MESSAGE");
            _systemMessage += "\n\n" + config.GetValue<string>("PROMPT_SYSTEM_MESSAGE_2");
            _systemMessage += "\n\n" + config.GetValue<string>("PROMPT_SYSTEM_MESSAGE_3");
            _systemMessage += "\n\n" + config.GetValue<string>("PROMPT_SYSTEM_MESSAGE_4");

            _suggestedQuestions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(config.GetValue<string>("PROMPT_SUGGESTED_QUESTIONS"));

            _searchSemanticConfig = config.GetValue<string>("SEARCH_SEMANTIC_CONFIG");
            _aoaiClient = aoaiClient;
            _searchClient = searchClient;
            _blobServiceClient = blobServiceClient;
            _embeddingsClient = embeddingsClient;
            _blobContainer = config.GetValue<string>("BLOB_CONTAINER_NAME");

        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(new Activity()
            {
                Type = "message",
                Text = _welcomeMessage,
                SuggestedActions = new SuggestedActions()
                {
                    Actions = _suggestedQuestions
                        .Select(value => new CardAction(type: "postBack", value: value))
                        .ToList()
                }
            });
        }


        public override async Task<string> ProcessMessage(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
        {

            await turnContext.SendActivityAsync(new Activity(type: "typing"));

            kernel = Kernel.CreateBuilder()
                    .AddAzureOpenAIChatCompletion(
                        deploymentName: _aoaiModel,
                        _aoaiClient
                    )
                    .Build();

            // Import the plugins
            kernel.ImportPluginFromObject(new GetAgreementFileNamePlugin(conversationData, turnContext, _searchClient, _embeddingsClient, _searchSemanticConfig), "GetAgreementFileNamePlugin");
            kernel.ImportPluginFromObject(new FindBuyerSellerPlugin(conversationData, turnContext, _searchClient, _embeddingsClient, _searchSemanticConfig), "FindBuyerSellerPlugin");
            kernel.ImportPluginFromObject(new SearchAgreementPlugin(conversationData, turnContext, _searchClient, _embeddingsClient, _searchSemanticConfig), "SearchAgreementPlugin");
            kernel.ImportPluginFromObject(new LoadAgreementPdfPlugin(conversationData, turnContext, _blobServiceClient, _blobContainer ), "LoadAgreementPdfPlugin");

            // Use Auto Function calling setting to let llm decide which Plugin to invoke.
            OpenAIPromptExecutionSettings settings = new() { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };
            
            // Save to conversation history
            string prompt = FormatConversationHistory(conversationData);

            //var result = await kernel.InvokePromptAsync(turnContext.Activity.Text, new(settings));
            var result = await kernel.InvokePromptAsync(prompt, new(settings));

            return result.ToString();
        }
    }

}
