using System.Text.Json.Serialization;

namespace NetSuiteIntegrationAPI.Models
{
    public class TypesenseField
    {
        public string name { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? facet { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? optional { get; set; }
    }

    public class TypesenseSearchResult
    {
        public int Found { get; set; }
        public int Page { get; set; }
        public List<TypesenseHit> Hits { get; set; } = new();
        public List<TypesenseFacet> Facet_Counts { get; set; } = new();
    }

    public class TypesenseHit
    {
        public ProductDto Document { get; set; } = new();
    }

    public class TypesenseFacet
    {
        public string Field_Name { get; set; } = string.Empty;
        public List<TypesenseFacetCount> Counts { get; set; } = new();
    }

    public class TypesenseFacetCount
    {
        public string Value { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
