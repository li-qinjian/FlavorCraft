using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace FlavorCraft.BannerBearerFix
{
    // 使用 Harmony 库对 Agent 类的 Formation 属性设置方法进行补丁
    [HarmonyPatch(typeof(Agent), "Formation", MethodType.Setter)]
    internal class BBFPatch
    {
        // 前置补丁方法，在设置 Agent 的编队时调用
        // __instance: 当前 Agent 实例
        // value: 要设置的新编队
        public static bool Prefix(Agent __instance, Formation value)
        {
            if (__instance != null && __instance.IsActive() && __instance.Formation != null && __instance.Formation.AI != null && Mission.Current != null && !Mission.Current.IsMissionEnding)
            {
                // 获取旗手跟随行为组件
                BBFBehaviorFollow followBehavior = __instance.Formation.AI.GetBehavior<BBFBehaviorFollow>();

                // 检查是否应阻止编队变更（旗手正在有效跟随队友）
                bool shouldBlockFormationChange = followBehavior != null &&
                                                 !followBehavior.DisableThisBehaviorManually &&
                                                 followBehavior.IsAlive();

                // 返回 false 表示阻止原方法执行，true 表示允许
                return !shouldBlockFormationChange;
            }

            return true;
        }
    }
}