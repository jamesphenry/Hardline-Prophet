using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HardlineProphet.Tests.Helpers;

public static class JsonHelper
{
    public static string DumpObjectToJson(object obj)
    {
        // Serialize the object to a JSON string with indented formatting
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(obj, options);
    }
}