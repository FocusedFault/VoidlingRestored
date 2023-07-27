using RoR2.VoidRaidCrab;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using EntityStates;

namespace VoidlingRestored
{
  public class BetterReEmerge : BaseState
  {// 10 Body Spawn Spawn.playbackRate
    public float duration = 10;
    public float delay = 2;
    public string soundString = "Play_voidRaid_spawn";
    public bool playedAnim = false;
    public string animationLayerName = "Body";
    public string animationStateName = "Spawn";
    public string animationPlaybackRateParam = "Spawn.playbackRate";

    public override void OnEnter()
    {
      base.OnEnter();
      if (!NetworkServer.active)
        return;
      CentralLegController component = this.GetComponent<CentralLegController>();
      if (!(bool)(Object)component)
        return;
      component.RegenerateAllBrokenServer();
    }

    public override void FixedUpdate()
    {
      base.FixedUpdate();
      if ((double)this.fixedAge >= (double)this.delay && !playedAnim)
      {
        int num = (int)Util.PlaySound(this.soundString, this.gameObject);
        this.PlayAnimation(this.animationLayerName, this.animationStateName, this.animationPlaybackRateParam, this.duration);
        playedAnim = true;
      }
      if (!this.isAuthority || (double)this.fixedAge < (double)this.duration)
        return;
      this.outer.SetNextStateToMain();
    }

    public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
  }
}
