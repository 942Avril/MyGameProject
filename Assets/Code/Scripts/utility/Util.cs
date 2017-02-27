using UnityEngine;


public class Util{

    public static T Json2Object<T>(string content)
    {
        return LitJson.JsonMapper.ToObject<T>(content);
    }

    public static string Object2Json(object obj)
    {
        LitJson.JsonWriter writer = new LitJson.JsonWriter();
        writer.PrettyPrint = true;
        writer.IndentValue = 1;
        LitJson.JsonMapper.ToJson(obj, writer);
        return writer.ToString();
    }
}
