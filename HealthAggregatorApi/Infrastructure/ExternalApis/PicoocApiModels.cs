using System.Text.Json.Serialization;

namespace HealthAggregatorApi.Infrastructure.ExternalApis;

public class PicoocLoginRequest
{
    [JsonPropertyName("appver")]
    public string Appver { get; set; } = string.Empty;
    
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
    
    [JsonPropertyName("lang")]
    public string Lang { get; set; } = "en";
    
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
    
    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;
    
    [JsonPropertyName("sign")]
    public string Sign { get; set; } = string.Empty;
    
    [JsonPropertyName("push_token")]
    public string PushToken { get; set; } = string.Empty;
    
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;
    
    [JsonPropertyName("req")]
    public PicoocLoginRequestRec Req { get; set; } = new();
}

public class PicoocLoginRequestRec
{
    [JsonPropertyName("app_channel")]
    public string AppChannel { get; set; } = string.Empty;
    
    [JsonPropertyName("app_version")]
    public string AppVersion { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
    
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;
    
    [JsonPropertyName("phone_system")]
    public string PhoneSystem { get; set; } = string.Empty;
    
    [JsonPropertyName("phone_type")]
    public string PhoneType { get; set; } = string.Empty;
}

public class PicoocLoginResponse
{
    public int Code { get; set; }
    public string Msg { get; set; } = string.Empty;
    public PicoocLoginResponseData Resp { get; set; } = new();
}

public class PicoocLoginResponseData
{
    public string User_id { get; set; } = string.Empty;
    public string Role_id { get; set; } = string.Empty;
    public List<PicoocRole> Roles { get; set; } = [];
}

public class PicoocRole
{
    public string Role_id { get; set; } = string.Empty;
    public string Role_name { get; set; } = string.Empty;
}

public class PicoocBodyIndexResponse
{
    public int Code { get; set; }
    public string Msg { get; set; } = string.Empty;
    public PicoocBodyIndexResponseData Resp { get; set; } = new();
}

public class PicoocBodyIndexResponseData
{
    public List<PicoocBodyIndex> Records { get; set; } = [];
    public int? LastTime { get; set; }
    public bool Continue { get; set; }
}

public class PicoocBodyIndex
{
    public long BodyTime { get; set; }
    public int DataType { get; set; }
    public int Body_index_id { get; set; }
    public int Role_id { get; set; }
    public float Body_fat { get; set; }
    public float Weight { get; set; }
    public float Bmi { get; set; }
    public int Visceral_fat_level { get; set; }
    public float Muscle_race { get; set; }
    public int Body_age { get; set; }
    public float Bone_mass { get; set; }
    public int Basic_metabolism { get; set; }
    public float Water_race { get; set; }
    public float Skeletal_muscle { get; set; }
    public int Local_time { get; set; }
    public float Subcutaneous_fat { get; set; }
    public int Server_time { get; set; }
    public int Server_id { get; set; }
    public int Is_del { get; set; }
    public int Abnormal_flag { get; set; }
    public string Mac { get; set; } = string.Empty;
}
