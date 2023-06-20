using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Payment.GCPay.Client;

public static class GCPayUtility
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Converters = {
        }
    };
}