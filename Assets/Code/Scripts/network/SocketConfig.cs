using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SocketConfig
{
    public Dictionary<string, SocketFunction> function_cfg = new Dictionary<string, SocketFunction>();
    public List<SocketClass> class_cfg = new List<SocketClass>();
    public const int ARRAY_VAR = 0x00010000;
    public const int TYPE_INT = 0;
    public const int TYPE_STRING = 1;
    public const int TYPE_DOUBLE = 3;
    public const int TYPE_BUFF = 5;

    public SocketFunction getSocketFun(int key)
    {
        string keyStr = key.ToString();
        if (function_cfg.ContainsKey(keyStr))
        {
            return function_cfg[keyStr];
        }
        return null;
    }

    public SocketClass getSocketClass(int classIndex)
    {
        if (classIndex >= 0 && classIndex < class_cfg.Count)
        {
            return class_cfg[classIndex];
        }
        return null;
    }

}

public class SocketFunction
{
    public string function_name;
    public List<SocketFunctionArgs> args = new List<SocketFunctionArgs>();
}

public class SocketFunctionArgs
{
    public int type;
    public int class_index;
}

public class SocketClass
{
    public string class_name;
    public List<SocketClassField> field = new List<SocketClassField>();
}

public class SocketClassField
{
    public int type;
    public int class_index;
    public string field_name;
}