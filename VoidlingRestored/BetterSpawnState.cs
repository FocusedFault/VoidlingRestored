using RoR2;
using RoR2.VoidRaidCrab;
using UnityEngine;
using UnityEngine.Networking;
using EntityStates;

namespace VoidlingRestored
{
  public class BetterSpawnState : BaseState
  {
    public float duration = 10f;
    public float delay = 2f;
    public string spawnSoundString = "Play_voidRaid_spawn";
    public GameObject spawnEffectPrefab = VoidlingRestored.spawnEffect;
    public string animationLayerName = "Body";
    public string animationStateName = "Spawn";
    public string animationPlaybackRateParam = "Spawn.playbackRate";
    public bool doLegs = true;
    public bool playedAnim = false;
    public CharacterSpawnCard jointSpawnCard = VoidlingRestored.jointCard;
    public string leg1Name = "FrontLegL";
    public string leg2Name = "FrontLegR";
    public string leg3Name = "MidLegL";
    public string leg4Name = "MidLegR";
    public string leg5Name = "BackLegL";
    public string leg6Name = "BackLegR";
    private CharacterModel characterModel;

    public override void OnEnter()
    {
      base.OnEnter();
      int num = (int)Util.PlaySound(this.spawnSoundString, GameObject.Find("SpawnCamera"));
      if ((bool)(Object)this.spawnEffectPrefab)
        EffectManager.SpawnEffect(this.spawnEffectPrefab, new EffectData() { origin = new Vector3(0, 0, 0), scale = 2, rotation = Quaternion.AngleAxis(180, Vector3.forward) }, false);
      if (!this.doLegs || !NetworkServer.active)
        return;
      ChildLocator modelChildLocator = this.GetModelChildLocator();
      if (!(bool)(Object)this.jointSpawnCard || !(bool)(Object)modelChildLocator)
        return;
      DirectorPlacementRule placementRule = new DirectorPlacementRule()
      {
        placementMode = DirectorPlacementRule.PlacementMode.Direct,
        spawnOnTarget = this.GetModelTransform()
      };
      this.SpawnJointBodyForLegServer(this.leg1Name, modelChildLocator, placementRule);
      this.SpawnJointBodyForLegServer(this.leg2Name, modelChildLocator, placementRule);
      this.SpawnJointBodyForLegServer(this.leg3Name, modelChildLocator, placementRule);
      this.SpawnJointBodyForLegServer(this.leg4Name, modelChildLocator, placementRule);
      this.SpawnJointBodyForLegServer(this.leg5Name, modelChildLocator, placementRule);
      this.SpawnJointBodyForLegServer(this.leg6Name, modelChildLocator, placementRule);
      this.characterModel = this.GetModelTransform().GetComponent<CharacterModel>();
      ++this.characterModel.invisibilityCount;
    }

    private void SpawnJointBodyForLegServer(
      string legName,
      ChildLocator childLocator,
      DirectorPlacementRule placementRule)
    {
      GameObject gameObject = DirectorCore.instance?.TrySpawnObject(new DirectorSpawnRequest((SpawnCard)this.jointSpawnCard, placementRule, Run.instance.stageRng)
      {
        summonerBodyObject = this.gameObject
      });
      Transform child = childLocator.FindChild(legName);
      if (!(bool)(Object)gameObject || !(bool)(Object)child)
        return;
      CharacterMaster component1 = gameObject.GetComponent<CharacterMaster>();
      if (!(bool)(Object)component1)
        return;
      LegController component2 = child.GetComponent<LegController>();
      if (!(bool)(Object)component2)
        return;
      component2.SetJointMaster(component1, child.GetComponent<ChildLocator>());
    }

    public override void FixedUpdate()
    {
      base.FixedUpdate();
      if ((double)this.fixedAge >= (double)this.delay && !playedAnim)
      {
        TeleportHelper.TeleportBody(this.characterBody, new Vector3(0, 0, 0));
        this.PlayAnimation(this.animationLayerName, this.animationStateName, this.animationPlaybackRateParam, this.duration);
        this.playedAnim = true;
        --this.characterModel.invisibilityCount;
      }
      if ((double)this.fixedAge < (double)this.duration || !this.isAuthority)
        return;
      this.outer.SetNextStateToMain();
    }

    public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
  }
}