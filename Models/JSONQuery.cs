
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeautyManager.Models;


public sealed class JSONQuery
{
 
    [JsonPropertyName("table")]
    public string Table { get; set; } = string.Empty;


    [JsonPropertyName("operation")]
    public string Operation { get; set; } = string.Empty;

 
    [JsonPropertyName("columns")]
    public List<string> Columns { get; set; } = new();


    [JsonPropertyName("filters")]
    public List<JSONQueryFilter> Filters { get; set; } = new();


    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 50;
}



public sealed class JSONQueryFilter
{

    [JsonPropertyName("column")]
    public string Column { get; set; } = string.Empty;

 
    [JsonPropertyName("operator")]
    public string Operator { get; set; } = string.Empty;


    [JsonPropertyName("value")]
    public JsonElement Value { get; set; }
}
