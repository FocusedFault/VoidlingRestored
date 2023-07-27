using BepInEx;
using RoR2;
using EntityStates.VoidRaidCrab;
using System.Linq;
using UnityEngine;
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
      On.EntityStates.VoidRaidCrab.SpawnState.OnEnter += VoidRaidCrab_SpawnState;
      On.EntityStates.VoidRaidCrab.Collapse.OnEnter += Collapse_OnEnter;
      On.EntityStates.VoidRaidCrab.ReEmerge.OnEnter += ReEmerge_OnEnter;
      On.RoR2.Stage.Start += Stage_Start;
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
  }
}