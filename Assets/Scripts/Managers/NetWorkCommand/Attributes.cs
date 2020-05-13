using System;

public enum CommandUsage
{
    SendOnly = 0,
    RecieveOnly,
    DualWay,
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
class NetworkCommandInfoAttribute : Attribute
{
    public CommandUsage Usage { get; set; } = CommandUsage.DualWay;
    public NetWorkCommandType CommandType { get; set; } = NetWorkCommandType.Move;
}
