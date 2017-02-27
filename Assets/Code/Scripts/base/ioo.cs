using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ioo {

    private static GameObject _manager = null;
    public static GameObject manager
    {
        get
        {
            if (_manager == null)
                _manager = GameObject.FindWithTag("GameManager");
            return _manager;
        }
    }

    private static GameManager _gameManager = null;
    public static GameManager gameManager
    {
        get
        {
            if (_gameManager == null)
                _gameManager = manager.GetComponent<GameManager>();
            return _gameManager;
        }
    }
}
