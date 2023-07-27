using UnityEngine;
using RoR2;
using EntityStates;
using EntityStates.VoidRaidCrab;

namespace VoidlingRestored
{
  public class BetterCollapse : BaseState
  {
    public float duration = 6;
    public string soundString = "Play_voidRaid_runaway";
    public string animationLayerName = "Body";
    public string animationStateName = "Collapse";
    public string animationPlaybackRateParam = "Collapse.playbackRate";

    public override void OnEnter()
    {
      base.OnEnter();
      int num = (int)Util.PlaySound(this.soundString, this.gameObject);
      EffectManager.SpawnEffect(VoidlingRestored.spawnEffect, new EffectData() { origin = new Vector3(0, -25, 0), scale = this.characterBody.radius, rotation = Quaternion.AngleAxis(180, Vector3.forward) }, false);
      this.PlayAnimation(this.animationLayerName, this.animationStateName, this.animationPlaybackRateParam, this.duration);
    }

    public override void FixedUpdate()
    {
      base.FixedUpdate();
      if (!this.isAuthority || (double)this.fixedAge < (double)this.duration)
        return;
      EffectManager.SpawnEffect(VoidlingRestored.spawnEffect, new EffectData() { origin = new Vector3(0, -25, 0), scale = this.characterBody.radius, rotation = Quaternion.AngleAxis(180, Vector3.forward) }, false);
      this.outer.SetNextState((EntityState)new ReEmerge());
    }

    public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
  }
}
