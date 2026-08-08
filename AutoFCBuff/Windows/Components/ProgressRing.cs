using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AutoFCBuff.Windows.Components;

internal static class ProgressRing
{
    private const float Top = -MathF.PI / 2f;

    private static Vector2 Dir(float a) => new(MathF.Cos(a), MathF.Sin(a));

    private static void Arc(Vector2 c, float r, float thickness, float a0, float a1, uint col)
    {
        var dl = ImGui.GetWindowDrawList();
        var span = MathF.Abs(a1 - a0);
        var seg = Math.Max(2, (int)MathF.Ceiling(span / (MathF.PI / 48f)));
        var prev = c + Dir(a0) * r;
        for (var i = 1; i <= seg; i++)
        {
            var a = a0 + (a1 - a0) * (i / (float)seg);
            var cur = c + Dir(a) * r;
            dl.AddLine(prev, cur, col, thickness);
            prev = cur;
        }
        var cap = thickness * 0.5f;
        dl.AddCircleFilled(c + Dir(a0) * r, cap, col);
        dl.AddCircleFilled(c + Dir(a1) * r, cap, col);
    }

    public static void Draw(float progress, float radius = 32f)
    {
        var center = ImGui.GetCursorScreenPos() + new Vector2(radius, radius);
        var thickness = 4f;

        var bgCol = ImGui.GetColorU32(Styling.CardBgSoft);
        var activeCol = ImGui.GetColorU32(Styling.AccentTeal);

        Arc(center, radius, thickness, Top, Top + MathF.PI * 2f, bgCol);

        if (progress > 0f)
        {
            var fraction = Math.Clamp(progress, 0f, 1f);
            Arc(center, radius, thickness, Top, Top + fraction * MathF.PI * 2f, activeCol);
        }

        ImGui.Dummy(new Vector2(radius * 2, radius * 2));
    }
}
