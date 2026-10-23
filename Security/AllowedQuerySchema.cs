
using System;
using System.Collections.Generic;

namespace BeautyManager.Security;


public static class AllowedQuerySchema
{
    

    public static readonly
        IReadOnlyDictionary<string,
            IReadOnlyDictionary<string, Type>>
        Tables = CreateTables();

    private static IReadOnlyDictionary<string,
        IReadOnlyDictionary<string, Type>> CreateTables()
    {
        var tables = new Dictionary<string,
            IReadOnlyDictionary<string, Type>>(
                StringComparer.OrdinalIgnoreCase)
        {
            ["Clients"] = new Dictionary<string, Type>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = typeof(int),
                ["Name"] = typeof(string),
                ["Phone"] = typeof(string),
                ["Email"] = typeof(string),
                ["Service"] = typeof(string),
                ["Specialist"] = typeof(string)
            },

            ["Employees"] = new Dictionary<string, Type>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = typeof(int),
                ["Name"] = typeof(string),
                ["Specialty"] = typeof(string),
                ["Phone"] = typeof(string)
            },

            ["Services"] = new Dictionary<string, Type>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = typeof(int),
                ["Name"] = typeof(string),
                ["Category"] = typeof(string),
                ["Price"] = typeof(double),
                ["DurationMinutes"] = typeof(int)
            }
        };

        return tables;
    }


    public static readonly HashSet<string>
        AllowedOperations = new(
            StringComparer.OrdinalIgnoreCase)
        {
            "select"
        };


    public static readonly HashSet<string>
        AllowedOperators = new(
            StringComparer.OrdinalIgnoreCase)
        {
            "eq",        
            "neq",       
            "contains",  
            "gt",        
            "gte",       
            "lt",        
            "lte"        
        };


    public const int MaxLimit = 100;
}
