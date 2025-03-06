using System.ComponentModel;
using ChatApp.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
#pragma warning disable SKEXP0001

namespace ChatApp.WebApi.Skills
{
    public class KnowledgeBaseSkills
    {
        private readonly VectorStoreTextSearch<KnowledgeBaseSnippet> _vectorStoreTextSearch;

        public KnowledgeBaseSkills(VectorStoreTextSearch<KnowledgeBaseSnippet> vectorStoreTextSearch)
        {
            _vectorStoreTextSearch = vectorStoreTextSearch;
        }

        [KernelFunction]
        [Description(
            "allows finding TSG, playbooks and solutions to problems encountered before with kubernetes and Azure resources")]
        public async Task<TextSearchResult[]> SearchKnowledgebase(string query, [Description("Number of search results to return")]int top=5, [Description("The index of the first result to return")] int skip=0)
        {
            var results = await _vectorStoreTextSearch.GetTextSearchResultsAsync(query, searchOptions: new TextSearchOptions(){Top = top, Skip = skip});
            TextSearchResult[] resultsArray = await results.Results.ToArrayAsync();
            return resultsArray;
        }
    }
}
