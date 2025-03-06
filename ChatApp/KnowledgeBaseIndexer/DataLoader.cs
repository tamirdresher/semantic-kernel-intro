using System.Net;
using ChatApp.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
#pragma warning disable SKEXP0001

internal sealed class DataLoader(
    IOptions<RagConfig> ragConfigOptions,
    IVectorStoreRecordCollection<string, KnowledgeBaseSnippet> vectorStoreRecordCollection,
    ITextEmbeddingGenerationService textEmbeddingGenerationService,
    IChatCompletionService chatCompletionService)
{

    public async Task LoadDataAsync(CancellationToken cancellationToken)
    {
        try
        {           

            var rootPath = ragConfigOptions.Value.RootFolder;
            var markdownFiles = Directory.GetFiles(rootPath, "*.md", SearchOption.AllDirectories);
            foreach (var markdownFile in markdownFiles)
            {
                Console.WriteLine($"Loading documents into vector store: {markdownFile}");
                await LoadMarkdown(
                    markdownFile,
                    ragConfigOptions.Value.DataLoadingBatchSize,
                    ragConfigOptions.Value.DataLoadingBetweenBatchDelayInMilliseconds,
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load PDFs: {ex}");
            throw;
        }
    }

    private async Task LoadMarkdown(string mdPath, int batchSize, int betweenBatchDelayInMs, CancellationToken cancellationToken)
    {
        await vectorStoreRecordCollection.CreateCollectionIfNotExistsAsync(cancellationToken).ConfigureAwait(false);

        var text = File.ReadAllText(mdPath);

        // Map each paragraph to a KnowledgeBaseSnippet and generate an embedding for it.
        var recordTask = new KnowledgeBaseSnippet
        {
            Key = Guid.NewGuid().ToString(),
            Text = text,
            ReferenceDescription = $"{new FileInfo(mdPath).Name}",
            ReferenceLink = $"{new Uri(new FileInfo(mdPath).FullName).AbsoluteUri}",
            TextEmbedding = await GenerateEmbeddingsWithRetryAsync(textEmbeddingGenerationService, text!, cancellationToken: cancellationToken).ConfigureAwait(false)
        };

        // Upsert the records into the vector store.
        var records = new[]
        {
            recordTask
        };
        var upsertedKeys = vectorStoreRecordCollection.UpsertBatchAsync(records, cancellationToken: cancellationToken);
        await foreach (var key in upsertedKeys.ConfigureAwait(false))
        {
            Console.WriteLine($"Upserted record '{key}' into VectorDB");
        }

        await Task.Delay(betweenBatchDelayInMs, cancellationToken).ConfigureAwait(false);
    }


    /// Add a simple retry mechanism to embedding generation.
    /// </summary>
    /// <param name="textEmbeddingGenerationService">The embedding generation service.</param>
    /// <param name="text">The text to generate the embedding for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The generated embedding.</returns>
    private static async Task<ReadOnlyMemory<float>> GenerateEmbeddingsWithRetryAsync(ITextEmbeddingGenerationService textEmbeddingGenerationService, string text, CancellationToken cancellationToken)
    {
        var tries = 0;

        while (true)
        {
            try
            {
                return await textEmbeddingGenerationService.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                tries++;

                if (tries < 3)
                {
                    Console.WriteLine($"Failed to generate embedding. Error: {ex}");
                    Console.WriteLine("Retrying embedding generation...");
                    await Task.Delay(10_000, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw;
                }
            }
        }
    }



}