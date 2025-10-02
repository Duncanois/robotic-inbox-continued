using Newtonsoft.Json;

namespace RoboticInbox.Utilities;

internal class Json<T>
{
	public static string Serialize(T data)
	{
		return JsonConvert.SerializeObject(data);
	}

	public static T Deserialize(string json)
	{
		return JsonConvert.DeserializeObject<T>(json);
	}
}
