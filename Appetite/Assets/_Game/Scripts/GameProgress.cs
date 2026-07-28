/// <summary>
/// 简单的静态游戏进度追踪器。
/// 用于在不同场景之间传递状态（例如：从精神世界返回后触发不同的对话）。
/// Demo 阶段使用。
/// </summary>
public static class GameProgress
{
    /// <summary>主角是否刚从精神世界返回</summary>
    public static bool hasReturnedFromSpiritWorld = false;

    /// <summary>主角是否已经吃过面包</summary>
    public static bool hasEatenBread = false;

    /// <summary>主角是否已经见过黑猫</summary>
    public static bool hasMetBlackCat = false;

    /// <summary>重置所有进度（开始新游戏时调用）</summary>
    public static void ResetAll()
    {
        hasReturnedFromSpiritWorld = false;
        hasEatenBread = false;
        hasMetBlackCat = false;
    }
}
