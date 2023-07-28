using BepInEx;
using RoR2;
using EntityStates.VoidRaidCrab;
using EntityStates.VoidRaidCrab.Weapon;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;

namespace VoidlingRestored
{
  [BepInPlugin("com.Nuxlar.VoidlingRestored", "VoidlingRestored", "1.0.0")]

  public class VoidlingRestored : BaseUnityPlugin
  {
    public static GameObject spawnEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidRaidCrab/VoidRaidCrabSpawnEffect.prefab").WaitForCompletion();
    public static CharacterSpawnCard jointCard = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/VoidRaidCrab/cscVoidRaidCrabJoint.asset").WaitForCompletion();
    GameObject portalEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidMegaCrab/VoidMegaCrabSpawnEffect.prefab").WaitForCompletion();
    GameObject voidling = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidRaidCrab/VoidRaidCrabBody.prefab").WaitForCompletion();
    GameObject miniVoidling = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidRaidCrab/MiniVoidRaidCrabBodyPhase1.prefab").WaitForCompletion();
    SpawnCard voidlingCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/VoidRaidCrab/cscVoidRaidCrab.asset").WaitForCompletion();

    public void Awake()
    {
      voidling.GetComponent<ModelLocator>().modelTransform.gameObject.AddComponent<PrintController>();
      On.RoR2.Stage.Start += Stage_Start;
      On.EntityStates.VoidRaidCrab.SpawnState.OnEnter += VoidRaidCrab_SpawnState;
      On.EntityStates.VoidRaidCrab.Collapse.OnEnter += Collapse_OnEnter;
      On.EntityStates.VoidRaidCrab.ReEmerge.OnEnter += ReEmerge_OnEnter;
      On.EntityStates.VoidRaidCrab.BaseSpinBeamAttackState.OnEnter += BaseSpinBeamAttackState_OnEnter;
      On.EntityStates.VoidRaidCrab.ChargeGauntlet.OnEnter += ChargeGauntlet_OnEnter;
      On.EntityStates.VoidRaidCrab.ChargeWardWipe.OnEnter += ChargeWardWipe_OnEnter;
      On.EntityStates.VoidRaidCrab.ChargeFinalStand.OnEnter += ChargeFinalStand_OnEnter;
      On.EntityStates.VoidRaidCrab.DeathState.OnEnter += DeathState_OnEnter;
      On.EntityStates.VoidRaidCrab.DeathState.OnExit += DeathState_OnExit;
    }

    private void BaseSpinBeamAttackState_OnEnter(On.EntityStates.VoidRaidCrab.BaseSpinBeamAttackState.orig_OnEnter orig, BaseSpinBeamAttackState self)
    {
      self.headForwardYCurve = AnimationCurve.Linear(0, 0, 10, 0);
      orig(self);
    }

    private void DeathState_OnEnter(On.EntityStates.VoidRaidCrab.DeathState.orig_OnEnter orig, DeathState self)
    {
      // Body TrueDeath TrueDeath.playbackRate 5
      self.animationStateName = "ChargeWipe";
      self.animationPlaybackRateParam = "Wipe.playbackRate";
      self.addPrintController = false;
      orig(self);
      PrintController printController = self.modelTransform.gameObject.AddComponent<PrintController>();
      printController.printTime = self.printDuration;
      printController.enabled = true;
      printController.startingPrintHeight = 200f;
      printController.maxPrintHeight = 500f;
      printController.startingPrintBias = self.startingPrintBias;
      printController.maxPrintBias = self.maxPrintBias;
      printController.disableWhenFinished = false;
      printController.printCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1f, 1f);
    }

    private void DeathState_OnExit(On.EntityStates.VoidRaidCrab.DeathState.orig_OnExit orig, DeathState self)
    {
      orig(self);
      GameObject[] joints = GameObject.FindObjectsOfType<GameObject>().Where(go => go.name == "VoidRaidCrabJointBody(Clone)" || go.name == "VoidRaidCrabJointMaster(Clone)").ToArray();
      foreach (GameObject joint in joints)
      {
        if (NetworkServer.active)
          NetworkServer.Destroy(joint);
        else
          Destroy(joint);
      }
    }

    private void ChargeGauntlet_OnEnter(On.EntityStates.VoidRaidCrab.ChargeGauntlet.orig_OnEnter orig, ChargeGauntlet self)
    {
      self.outer.SetState(new ChargeWardWipe());
    }

    private void ChargeWardWipe_OnEnter(On.EntityStates.VoidRaidCrab.ChargeWardWipe.orig_OnEnter orig, ChargeWardWipe self)
    {
      orig(self);
      PhasedInventorySetter component1 = self.GetComponent<PhasedInventorySetter>();
      if ((bool)(Object)component1 && NetworkServer.active)
        component1.AdvancePhase();
      ChargeGauntlet gauntlet = new ChargeGauntlet();
      if (!(bool)(Object)gauntlet.nextSkillDef)
        return;
      GenericSkill skillByDef = self.skillLocator.FindSkillByDef(gauntlet.skillDefToReplaceAtStocksEmpty);
      if (!(bool)(Object)skillByDef || skillByDef.stock != 0)
        return;
      skillByDef.SetBaseSkill(gauntlet.nextSkillDef);
    }

    private void ChargeFinalStand_OnEnter(On.EntityStates.VoidRaidCrab.ChargeFinalStand.orig_OnEnter orig, ChargeFinalStand self)
    {
      Debug.LogWarning("Im in charge final stand");
      orig(self);
    }

    private void Collapse_OnEnter(On.EntityStates.VoidRaidCrab.Collapse.orig_OnEnter orig, Collapse self)
    {
      self.outer.SetState(new BetterCollapse());
    }

    private void ReEmerge_OnEnter(On.EntityStates.VoidRaidCrab.ReEmerge.orig_OnEnter orig, ReEmerge self)
    {
      self.outer.SetState(new BetterReEmerge());
    }

    private void VoidRaidCrab_SpawnState(On.EntityStates.VoidRaidCrab.SpawnState.orig_OnEnter orig, SpawnState self)
    {
      self.outer.SetState(new BetterSpawnState());
    }

    private void Stage_Start(On.RoR2.Stage.orig_Start orig, Stage self)
    {
      orig(self);
      if (self.sceneDef.cachedName == "voidraid")
      {
        GameObject phases = GameObject.Find("EncounterPhases");
        VoidRaidGauntletController controller = Object.FindObjectOfType<VoidRaidGauntletController>();
        if (controller)
        {
          Debug.LogWarning("i have been found");
          Debug.LogWarning(controller.gameObject.name);
        }
        Transform cam = GameObject.Find("RaidVoid").transform.GetChild(8);
        if (phases)
        {
          phases.transform.GetChild(1).gameObject.SetActive(false);
          phases.transform.GetChild(2).gameObject.SetActive(false);
          Transform phase1 = phases.transform.GetChild(0);
          phase1.GetChild(0).position = new Vector3(0, -300, 0);
          phase1.GetComponent<ScriptedCombatEncounter>().spawns = new ScriptedCombatEncounter.SpawnInfo[1] { new ScriptedCombatEncounter.SpawnInfo() { spawnCard = voidlingCard, explicitSpawnPosition = phase1.GetChild(0) } };
          if (cam)
          {
            Transform curve = cam.GetChild(2);
            curve.GetChild(0).position = new Vector3(-0.2f, 217.13f, -442.84f);
            curve.GetChild(1).position = new Vector3(-12.5f, 29.7f, -181.4f);
          }
        }
      }
    }

    private void SetCurveLinear(AnimationCurve curve)
    {
      for (int i = 0; i < curve.keys.Length; ++i)
      {
        float intangent = 0;
        float outtangent = 0;
        bool intangent_set = false;
        bool outtangent_set = false;
        Vector2 point1;
        Vector2 point2;
        Vector2 deltapoint;
        Keyframe key = curve[i];

        if (i == 0)
        {
          intangent = 0; intangent_set = true;
        }

        if (i == curve.keys.Length - 1)
        {
          outtangent = 0; outtangent_set = true;
        }

        if (!intangent_set)
        {
          point1.x = curve.keys[i - 1].time;
          point1.y = curve.keys[i - 1].value;
          point2.x = curve.keys[i].time;
          point2.y = curve.keys[i].value;

          deltapoint = point2 - point1;

          intangent = deltapoint.y / deltapoint.x;
        }
        if (!outtangent_set)
        {
          point1.x = curve.keys[i].time;
          point1.y = curve.keys[i].value;
          point2.x = curve.keys[i + 1].time;
          point2.y = curve.keys[i + 1].value;

          deltapoint = point2 - point1;

          outtangent = deltapoint.y / deltapoint.x;
        }

        key.inTangent = intangent;
        key.outTangent = outtangent;
        curve.MoveKey(i, key);
      }
    }
  }
}