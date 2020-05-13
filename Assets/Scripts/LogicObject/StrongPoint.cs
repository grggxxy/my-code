using UnityEngine;

class StrongPoint
{
    public static readonly StrongPoint Instance = new StrongPoint();

    public int Hp { get; set; }

    public Vector3 Position { get; private set; } = new Vector3(-270.0f, 2.5f, 60.0f);
    private StrongPoint()
    {
    }
}
