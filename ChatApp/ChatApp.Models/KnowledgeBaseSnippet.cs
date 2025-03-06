using Microsoft.Extensions.VectorData;

namespace ChatApp.Models
{
    public sealed class KnowledgeBaseSnippet
    {
        [VectorStoreRecordKey]
        public required string Key { get; set; }

        [VectorStoreRecordData]
        public string? Text { get; set; }

        [VectorStoreRecordData]
        public string? ReferenceDescription { get; set; }

        [VectorStoreRecordData]
        public string? ReferenceLink { get; set; }

        [VectorStoreRecordVector(1536)]
        public ReadOnlyMemory<float> TextEmbedding { get; set; }
    }
}
