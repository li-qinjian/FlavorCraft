using TaleWorlds.MountAndBlade;

namespace FlavorCraft.BannerBearerFix
{
    // 旗手单位的跟随行为类，继承自 BehaviorComponent
    internal class BBFBehaviorFollow : BehaviorComponent
    {
        // 构造函数，初始化跟随行为
        // formation: 要应用行为的编队
        public BBFBehaviorFollow(Formation formation)
            : base(formation)
        {
        }

        // 检查队友是否存活
        public bool IsAlive()
        {
            return this.Teammate != null && this.Teammate.IsActive();
        }

        // 定期执行的方法，更新编队的移动命令
        public override void TickOccasionally()
        {
            // 获取旧编队的移动命令
            if (oldformation != null)
            {
                MovementOrder aa = oldformation.GetReadonlyMovementOrderReference();   //*this.oldformation.GetReadonlyMovementOrderReference();
            }

            // 检查队友是否存活且旗手和队友角色相同
            if (this.BannerBearer != null && this.Teammate != null)
            {
                if (this.IsAlive() && this.BannerBearer.Character == this.Teammate.Character)
                {
                    // 设置编队的移动命令为跟随队友
                    base.Formation.SetMovementOrder(MovementOrder.MovementOrderFollow(this.Teammate));
                    base.CurrentOrder = MovementOrder.MovementOrderFollow(this.Teammate);
                }
            }
        }

        // 获取 AI 行为的权重
        protected override float GetAiWeight()
        {
            // 检查队友是否存活
            float result = 0f;
            if (this.IsAlive())
            {
                // 根据是否手动禁用行为返回权重
                result = (this.DisableThisBehaviorManually ? 0f : 100f);
            }
            else if (this.oldformation != null)
            {
                // 队友死亡，设置权重为 0 并选择新的队友
                result = 0f;
                this.Teammate = this.oldformation.GetUnitWithIndex(1);
            }
            return result;
        }

        // 重置行为状态
        public override void ResetBehavior()
        {
            this.DisableThisBehaviorManually = false;
        }

        // 是否手动禁用行为的标志
        public bool DisableThisBehaviorManually = false;

        // 队友代理
        public Agent? Teammate;

        // 旗手代理
        public Agent? BannerBearer;

        // 当前编队
        public Formation? formation;

        // 旧编队
        public Formation? oldformation;
    }
}