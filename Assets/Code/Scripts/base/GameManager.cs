using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

    private float _lastRecordTime = 0;
    private int _fps = 0;
    private int _lastFrameNum = 0;
    private GUIStyle _versionGUIStyle;

    private double _serverTime = 0;

    // Use this for initialization
    void Start () {
        _versionGUIStyle = new GUIStyle();
        _versionGUIStyle.normal.background = null;
        _versionGUIStyle.normal.textColor = Color.red;
        _versionGUIStyle.fontSize = 15;
    }
	
	// Update is called once per frame
	void Update () {
        CalcFPS();
        _serverTime += Time.deltaTime;
    }

    void OnGUI()
    {
        string msg = string.Format("fps : {0}", _fps);
        GUI.Label(new Rect(0, 0, 100, 15), msg, _versionGUIStyle);
    }

    /// <summary>
    /// 计算帧频
    /// </summary>
    private void CalcFPS()
    {
        if(Time.time - _lastRecordTime >= 1.0f)
        {
            _fps = System.Convert.ToInt32(Time.frameCount - _lastFrameNum);
            _lastFrameNum = Time.frameCount;
            _lastRecordTime = Time.time;
        }
    }

    public void SetServerTime(double value)
    {
        _serverTime = value;
    }

    public double GetServerTime()
    {
        return _serverTime;
    }
}
