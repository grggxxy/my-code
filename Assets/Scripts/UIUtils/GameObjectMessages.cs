using System.Collections.Generic;
using UnityEngine;

enum GameObjectMessage
{
    QueryPlayerPosition = 0,
    ReturnPlayerPosition,

    HidePlayer,
    ShowPlayer,

    ShowPlayerSync,

    SwitchCursorState,

    NewGame,

    StartGame,
}


class PlayerPositionQueryResult
{
    public List<PlayerInfo> PlayerInfoList { get; private set; } = new List<PlayerInfo>();
    public List<Vector3> PlayerPositionList { get; private set; } = new List<Vector3>();

    public Vector3 TankPosition { get; set; }

    public void Clear()
    {
        this.PlayerInfoList.Clear();
        this.PlayerPositionList.Clear();
    }
}
