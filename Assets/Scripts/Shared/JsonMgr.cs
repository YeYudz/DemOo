using LitJson;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YokiFrame;

/// <summary>
/// 序列化和反序列化Json时  使用的是哪种方案
/// </summary>
public enum JsonType
{
    JsonUtlity,
    LitJson,
}

/// <summary>
/// Json数据管理类 主要用于进行 Json的序列化存储到硬盘 和 反序列化从硬盘中读取到内存中
/// </summary>
public class JsonMgr
{
    private static JsonMgr instance = new JsonMgr();
    public static JsonMgr Instance => instance;

    private JsonMgr()
    {
        JsonMapper.RegisterImporter<int, RelicSubStatConfig>(CreateLegacyRelicSubStatConfig);
    }

    private static RelicSubStatConfig CreateLegacyRelicSubStatConfig(int statTypeValue)
    {
        var statType = (RelicMainStatType)statTypeValue;
        bool isPercent = statType == RelicMainStatType.CritRate ||
                         statType == RelicMainStatType.CritDamage ||
                         statType == RelicMainStatType.MaxHpPercent ||
                         statType == RelicMainStatType.AttackPercent ||
                         statType == RelicMainStatType.DefensePercent;

        switch (statType)
        {
            case RelicMainStatType.MaxHp:
                return new RelicSubStatConfig { StatType = statType, MinValue = 25, MaxValue = 50, IsPercent = false };
            case RelicMainStatType.Attack:
                return new RelicSubStatConfig { StatType = statType, MinValue = 8, MaxValue = 15, IsPercent = false };
            case RelicMainStatType.Speed:
                return new RelicSubStatConfig { StatType = statType, MinValue = 5, MaxValue = 10, IsPercent = false };
            case RelicMainStatType.CritDamage:
                return new RelicSubStatConfig { StatType = statType, MinValue = 5, MaxValue = 10, IsPercent = true };
            default:
                return new RelicSubStatConfig
                {
                    StatType = statType,
                    MinValue = isPercent ? 2 : 1,
                    MaxValue = isPercent ? 5 : 1,
                    IsPercent = isPercent
                };
        }
    }

    //存储Json数据 序列化
    public void SaveData(object data, string fileName, JsonType type = JsonType.LitJson)
    {
        //确定存储路径
        string path = Application.persistentDataPath + "/" + fileName + ".json";
        //序列化 得到Json字符串
        string jsonStr = "";
        switch (type)
        {
            case JsonType.JsonUtlity:
                jsonStr = JsonUtility.ToJson(data);
                break;
            case JsonType.LitJson:
                // 配置LitJson，强制字符串类型保留引号
                JsonWriter writer = new JsonWriter();
                writer.PrettyPrint = true;
                JsonMapper.ToJson(data, writer);
                jsonStr = writer.ToString();
                break;
        }
        //把序列化的Json字符串 存储到指定路径的文件中
        File.WriteAllText(path, jsonStr);
    }

    // 从 persistentDataPath 读取玩家存档和设置。
    public T LoadData<T>(string fileName, JsonType type = JsonType.LitJson) where T : new()
    {
        string path = Path.Combine(Application.persistentDataPath, fileName + ".json");
        if (!File.Exists(path))
            return new T();

        return Deserialize<T>(File.ReadAllText(path), type);
    }

    // 通过 ResKit 读取由 YooAsset 管理的只读配置。
    // YooAsset 收集地址使用 AddressByFileName，因此地址不包含扩展名和目录。
    public T LoadConfig<T>(string fileName, JsonType type = JsonType.LitJson) where T : new()
    {
        var handle = ResKit.LoadAsset<TextAsset>(fileName);
        if (handle == null || handle.Asset == null)
        {
#if UNITY_EDITOR
            string editorPath = Path.Combine(Application.dataPath, "DataConfig", fileName + ".json");
            if (File.Exists(editorPath))
                return Deserialize<T>(File.ReadAllText(editorPath), type);
#endif
            LogKit.Error($"[JsonMgr] 配置加载失败: {fileName}");
            return new T();
        }

        try
        {
            return Deserialize<T>(handle.Asset.text, type);
        }
        finally
        {
            handle.Release();
        }
    }

    private static T Deserialize<T>(string jsonStr, JsonType type)
    {
        switch (type)
        {
            case JsonType.JsonUtlity:
                return JsonUtility.FromJson<T>(jsonStr);
            case JsonType.LitJson:
                return JsonMapper.ToObject<T>(jsonStr);
            default:
                return default(T);
        }
    }
}
