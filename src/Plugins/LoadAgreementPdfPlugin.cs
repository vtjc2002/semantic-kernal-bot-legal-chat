using Azure.Storage.Blobs;

using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.BotBuilderSamples;
using Microsoft.SemanticKernel;


using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;


namespace Plugins;
public class LoadAgreementPdfPlugin
{
    private readonly string _containerName;
    private ITurnContext<IMessageActivity> _turnContext;
    private ConversationData _conversationData;
    private readonly BlobServiceClient _blobServiceClient;

    public LoadAgreementPdfPlugin(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext, BlobServiceClient blobServiceClient, string containerName)
    {
        _containerName = containerName;
        _conversationData = conversationData;
        _turnContext = turnContext;
        _blobServiceClient = blobServiceClient;
    }

    [KernelFunction, Description("Load the entire agreement.")]
    public async Task<string> GetPdfContentAsStringAsync([Description("File name of the agreement")] string agreement)
    {
        await _turnContext.SendActivityAsync($"Loading ...{@agreement}");

        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        BlobClient blobClient = containerClient.GetBlobClient(agreement);

        if (await blobClient.ExistsAsync())
        {
            using (var ms = new MemoryStream())
            {
                await blobClient.DownloadToAsync(ms);
                ms.Position = 0;

                using (PdfDocument document = PdfDocument.Open(ms))
                {
                    StringBuilder pdfContent = new StringBuilder();

                    foreach (Page page in document.GetPages())
                    {
                        pdfContent.Append(page.Text);
                    }

                    // Save to conversation data to be used for later conversation
                    _conversationData.History.Add(new ConversationTurn { Role = "assistant", Message = $"[ENTIRE PDF CONTENT] {pdfContent.ToString()}" });

                    return "[ENTIRE PDF CONTENT]\n\n" + pdfContent.ToString();
                }
            }
        }
        else
        {
            //throw new FileNotFoundException($"Blob '{agreement}' not found in container '{_containerName}'.");
            await _turnContext.SendActivityAsync($"{@agreement} not found.");
            return "agreement not found.";
        }
    }


}
