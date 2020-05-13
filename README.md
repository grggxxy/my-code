# A Simple Multiplayer Shooting Game

## 启动方法
1. 启动服务器
```
cd server
./startup.py
```

2. 启动游戏

## 游戏功能
- 登录并加入游戏
- 注册用户
- 退出游戏
- 玩家登陆时读取玩家数据，玩家退出时保存玩家数据（保存的数据包括玩家当前的`生命值`、`武器类型`、`弹药量`、`敌人击杀数`、`位置坐标`和`朝向`
- `WASD`键控制左右前后移动、鼠标控制朝向
- `T`键切换武器的点射/连射模式
- `R`键换弹
- 每消灭一波敌人，服务器随机刷新一波敌人
- 敌人分为`近战`和`远程`两种类型
- 击杀敌人会掉落道具，道具有四种类型：
- - 回血道具
- - 补充子弹道具
- - 扇形枪（每一次射击都会向正前方发射扇形子弹）
- - 二连发枪（每一次射击都会向正前方连续发射两发子弹）
- 玩家血量归零会死亡，5秒钟后自动复活
- 玩家右键会释放令一定区域内敌人眩晕3秒的子弹，冷却10秒

## 相关说明
- 服务器可以进行相关游戏数据的配置，具体见`server/network/configure.py`
- 客户端和服务端交互所使用到的命令分别被定义在`Assets/Scripts/Managers/NetWorkCommand/NetworkCommand.cs`以及`server/logic/commands.py`两个文件中
- 服务器会指定第一个加入游戏的玩家为host端，一旦该玩家退出，则所有其他玩家都会退出游戏
- 敌人的寻路、移动、攻击判定等操作在host端完成并同步到其他玩家的客户端上，具体参考`Assets/Scripts/Controllers/EnmeyController.cs`以及`Assets/Scripts/Controllers/EnmeyControllerSync.cs`
- 敌人寻路使用`NavMeshAgent`组件
- 玩家数据使用`sqlite`保存，默认名字为`game.db`

## 其他
提供三组默认账号供测试：

| username | password |
| --- | --- |
| player1  | 123456 |
| player2  | 123456 |
| player3  | 123456 |