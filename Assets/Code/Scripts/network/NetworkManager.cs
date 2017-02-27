using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using LuaInterface;


public class NetworkManager : MonoBehaviour
{
    private static Queue<KeyValuePair<int, ByteBuffer>> sEvents = new Queue<KeyValuePair<int, ByteBuffer>>();
    private static SocketConfig _socketCfg;
    private KeyValuePair<int, ByteBuffer> _event;
    private ByteBuffer _outBuffer;

    public void Awake()
    {
    }

    //Lua那边初始化调用此方法
    //初始化json成map，key 对 value
    public void InitSocketJson(string str)
    {
        _socketCfg = Util.Json2Object<SocketConfig>(str); //解析成SocketConfig
        //Debug.Log(str);
        string[] netWorkEvent = new string[3] { "OnConnect", "OnDisconnect", "OnException" };

        for (int i = 0; i < 3; i++)
        {
            SocketFunction socketFun = new SocketFunction();
            socketFun.function_name = netWorkEvent[i];
            _socketCfg.function_cfg.Add((100 + i + 1).ToString(), socketFun);
        }
    }

    ///------------------------------------------------------------------------------------
    public static void AddEvent(int _event, ByteBuffer data)
    {
        sEvents.Enqueue(new KeyValuePair<int, ByteBuffer>(_event, data));
    }
    void Update()
    {
        if (sEvents.Count > 0)
        {
            while (sEvents.Count > 0)
            {
                _event = sEvents.Dequeue();
                //byte[] temp = _event.Value.ToBytes();

                switch (_event.Key)
                {
                    default:
                        //ioo.gameManager.CallLuaMethod("OnSocket", _event.Key, _event.Value);
                        OnPacket(_event.Key, _event.Value);
                        break;
                }
            }
        }
    }


    //解析数据 --先解析json
    private void OnPacket(int key, ByteBuffer value)
    {
        SocketFunction socketFun = _socketCfg.getSocketFun(key);
        if (socketFun == null)
        {
            Debugger.LogWarning("receive unknown packet:" + key);
            return;
        }
        else
        {
            //Debug.Log("ParseValue: " + socketFun.function_name);
        }

        LuaState luaState = LuaClient.GetMainState();

        int oldTop = luaState.LuaGetTop();//获取当前的堆栈位置

        try
        {
            luaState.LuaGetGlobal("OnSocket");//获取方法名
            LuaPush(socketFun.function_name);
            LuaCreateTable();//封装的table

            int argsLens = socketFun.args.Count;

            for (int i = 0; i < argsLens; i++)
            {
                LuaPush((i + 1));//push进去key,lua中的key从1开始
                SocketFunctionArgs argInfo = socketFun.args[i];
                if (argInfo.class_index != -1)
                {
                    UnPackStruct(argInfo.class_index, argInfo.type);
                }
                else
                    UnPackBaseType(argInfo.type);
            }

            luaState.PCall(2, oldTop);//两个参数，一个协议号，一个table
            luaState.LuaSetTop(oldTop);//重置堆栈位置
        }
        catch (Exception e)
        {
            luaState.LuaSetTop(oldTop);//重置堆栈位置
            throw e;
        }
    }

    private void UnPackStruct(int index, int btype)
    {
        //int type = (btype < SocketConfig.ARRAY_VAR) ? btype : btype - SocketConfig.ARRAY_VAR;
        bool isArray = btype >= SocketConfig.ARRAY_VAR;
        int arrayCount = 0;
        if (isArray) //如果是数组类型
            arrayCount = _event.Value.ReadInt();
        else
            arrayCount = 1;//默认为1

        SocketClass structs = _socketCfg.getSocketClass(index);
        List<SocketClassField> fieldList = structs.field;
        //int fieldLens = fieldList.Count;

        if (isArray)//这里如果是数组，才要封装多一层
        {
            LuaCreateTable();
            for (int i = 0; i < arrayCount; i++)
            {
                LuaPush(i + 1);
                UnPackStructItem(fieldList);
            }
            LuaSetTable();//如果是数组才需要设置堆栈
        }
        else
        {
            UnPackStructItem(fieldList);
        }
    }

    private void UnPackStructItem(List<SocketClassField> fieldList)
    {
        LuaCreateTable();
        int fieldLens = fieldList.Count;
        for (int j = 0; j < fieldLens; j++)
        {
            SocketClassField fieldInfo = fieldList[j];
            LuaPush(fieldInfo.field_name);//push key进去
            if (fieldInfo.class_index != -1)
                UnPackStruct(fieldInfo.class_index, fieldInfo.type);//后面数据再进行递归
            else
                UnPackBaseType(fieldInfo.type);
        }
        LuaSetTable();
    }

    private void LuaSetTable()
    {
        LuaClient.GetMainState().LuaSetTable(-3);
    }

    private void LuaCreateTable()
    {
        LuaClient.GetMainState().LuaCreateTable();
    }

    private void LuaPush(object value)
    {
        LuaClient.GetMainState().Push(value);
    }


    private void UnPackBaseType(int btype)
    {
        int type = (btype < SocketConfig.ARRAY_VAR) ? btype : btype - SocketConfig.ARRAY_VAR;
        bool isArray = btype >= SocketConfig.ARRAY_VAR;
        int arrayCount = 0;
        if (isArray) //如果是数组类型
            arrayCount = _event.Value.ReadInt();
        else
            arrayCount = 1;//默认为1

        if (isArray)
        {
            LuaCreateTable();
            for (int i = 0; i < arrayCount; i++)
            {
                LuaPush(i + 1);
                PushBaseValue(type, _event.Value);//添加到数据中
                LuaSetTable();
            }
            //LuaSetTable();
        }
        else
        {
            PushBaseValue(type, _event.Value);
        }
        LuaSetTable();
    }

    //添加基础数据到堆栈中
    private void PushBaseValue(int type, ByteBuffer value)
    {
        object data = null;

        switch (type)
        {
            case SocketConfig.TYPE_INT:
                data = value.ReadInt();
                break;
            case SocketConfig.TYPE_STRING:
                data = value.ReadLongString();
                break;
            case SocketConfig.TYPE_DOUBLE:
                data = value.ReadDouble();
                break;
            default:
                Debug.Assert(false, "Socket Push Value is Null");
                break;
        }

        LuaPush(data);
    }

    /// <summary>
    /// 下面是封包
    /// </summary>
    /// 
    public void PackServerProxy(int key, LuaTable args)
    {
        SocketFunction socketFun = _socketCfg.getSocketFun(key);

        string functionName = socketFun.function_name;//协议 名字
        int argsLen = args.Length; //获取参数长度
        int rpcArgsLen = socketFun.args.Count;//协议参数长度

        Debug.Assert(argsLen == rpcArgsLen, string.Format("{0} args count error", functionName));//参数个数不正确

        _outBuffer = new ByteBuffer();//二进制流

        _outBuffer.WriteInt(key);//写入协议号

        for (int i = 0; i < rpcArgsLen; i++)
        {
            SocketFunctionArgs argInfo = socketFun.args[i];
            object arg = args[i + 1];//lua从1开始的
            Debug.Assert(arg != null, string.Format("fun {0} arg {1} is nil", functionName, i + 1));
            if (argInfo.class_index != -1)//这是一个类
            {
                PackStruct((LuaTable)arg, argInfo.class_index, argInfo.type);
            }
            else
            {
                PackBaseType(arg, argInfo.type);
            }
        }
        SendMessage(_outBuffer);
        args.Dispose();
    }

    private void PackStruct(LuaTable value, int index, int btype)
    {
        //int type = (btype < SocketConfig.ARRAY_VAR) ? btype : btype - SocketConfig.ARRAY_VAR;
        bool isArray = btype >= SocketConfig.ARRAY_VAR;

        SocketClass structs = _socketCfg.getSocketClass(index);
        List<SocketClassField> fieldList = structs.field;
        if (isArray)
        {
            int valueLen = value.Length;
            _outBuffer.WriteInt(valueLen);//写入长度
            for (int i = 0; i < valueLen; i++)
            {
                PackStructItem(fieldList, (LuaTable)value[i + 1]);
            }
        }
        else
        {
            PackStructItem(fieldList, value);
        }
    }

    private void PackStructItem(List<SocketClassField> fieldList, LuaTable value)
    {
        int fieldLen = fieldList.Count;
        for (int j = 0; j < fieldLen; j++)
        {
            SocketClassField fieldInfo = fieldList[j];
            string fieldName = fieldInfo.field_name;
            object fieldValue;
            if (value[j + 1] != null)//lua从1开始的啊
                fieldValue = value[j + 1];
            else
                fieldValue = value[fieldName];
            if (fieldInfo.class_index != -1)
            {
                PackStruct(fieldValue as LuaTable, fieldInfo.class_index, fieldInfo.type);
            }
            else
            {
                PackBaseType(fieldValue, fieldInfo.type);
            }
        }
    }


    private void PackBaseType(object value, int btype)
    {
        int type = (btype < SocketConfig.ARRAY_VAR) ? btype : btype - SocketConfig.ARRAY_VAR;
        bool isArray = btype >= SocketConfig.ARRAY_VAR;

        if (isArray)//如果是数组
        {
            LuaTable table = (LuaTable)value;
            int valueLen = table.Length;
            _outBuffer.WriteInt(valueLen);//写入长度
            for (int i = 0; i < valueLen; i++)
            {
                OutPush(type, table[i + 1]);//lua从1开始
            }
        }
        else
        {
            OutPush(type, value);
        }
    }


    //添加到输出二进制流
    private void OutPush(int type, object value)
    {
        //Debug.Log("OutPush......");
        //Debug.Log("value: " + value + "  type: " + value.GetType());
        switch (type)
        {
            case SocketConfig.TYPE_INT:
                _outBuffer.WriteInt(Convert.ToInt32(value));
                break;
            case SocketConfig.TYPE_STRING:
                _outBuffer.WriteString(Convert.ToString(value));
                break;
            case SocketConfig.TYPE_DOUBLE:
                _outBuffer.WriteDouble(Convert.ToDouble(value));
                break;
            case SocketConfig.TYPE_BUFF:
                _outBuffer.WriteString(Convert.ToString(value));
                break;
            default:
                Debug.Assert(false, "pack base type args unsupport"); //不支持的输出类型
                break;
        }
        //Debug.Log("......OutPush");
    }



    public void Logout()
    {
        SocketClient.Logout();
    }

    public void SendConnect(string addr, int port)
    {
        SocketClient.SendConnect(addr, port);
    }

    public void SendMessage(ByteBuffer buffer)
    {
        SocketClient.SendMessage(buffer);
    }

    public bool IsConnectSuccess()
    {
        return SocketClient.ConnectSuccess();
    }

    public string GetErrorMsg()
    {
        return SocketClient.GetErrorMsg();
    }
}
