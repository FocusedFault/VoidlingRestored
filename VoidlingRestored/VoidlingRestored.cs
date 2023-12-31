using BepInEx;
using RoR2;
using RoR2.VoidRaidCrab;
using R2API;
using EntityStates.VoidRaidCrab;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    GameEndingDef voidEnding = Addressables.LoadAssetAsync<GameEndingDef>("RoR2/Base/WeeklyRun/PrismaticTrialEnding.asset").WaitForCompletion();
    GameObject voidling = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidRaidCrab/VoidRaidCrabBody.prefab").WaitForCompletion();
    GameObject miniVoidling = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidRaidCrab/MiniVoidRaidCrabBodyPhase1.prefab").WaitForCompletion();
    SpawnCard voidlingCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/VoidRaidCrab/cscVoidRaidCrab.asset").WaitForCompletion();

    public void Awake()
    {
      voidling.GetComponent<ModelLocator>().modelTransform.gameObject.AddComponent<PrintController>();
      AddContent();
      On.RoR2.Stage.Start += Stage_Start;
      On.RoR2.CharacterMaster.OnBodyStart += CharacterMaster_OnBodyStart;
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

    private void AddContent()
    {
      ContentAddition.AddEntityState<BetterCollapse>(out _);
      ContentAddition.AddEntityState<BetterReEmerge>(out _);
      ContentAddition.AddEntityState<BetterSpawnState>(out _);
    }

    private void CharacterMaster_OnBodyStart(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
    {
      orig(self, body);
      if (body.isPlayerControlled && SceneManager.GetActiveScene().name == "voidraid" && body.HasBuff(RoR2Content.Buffs.Immune))
      {
        GameObject crab = GameObject.Find("VoidRaidCrabBody(Clone)");
        if (crab)
          crab.GetComponent<VoidRaidCrabHealthBarOverlayProvider>().OnEnable();
      }
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
      Run.instance.BeginGameOver(voidEnding);
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
      GameObject phases = GameObject.Find("EncounterPhases");
      if (phases)
        phases.transform.GetChild(0).GetChild(3).gameObject.SetActive(true);
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
        Transform cam = GameObject.Find("RaidVoid").transform.GetChild(8);
        if (phases)
        {
          GameObject p3Music = phases.transform.GetChild(2).GetChild(1).gameObject;
          p3Music.name = "MusicEnd";
          p3Music.transform.parent = phases.transform.GetChild(0);
          if (NetworkServer.active)
          {
            NetworkServer.Destroy(phases.transform.GetChild(1).gameObject);
            NetworkServer.Destroy(phases.transform.GetChild(2).gameObject);
          }
          else
          {
            Destroy(phases.transform.GetChild(1).gameObject);
            Destroy(phases.transform.GetChild(2).gameObject);
          }
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
  }
}