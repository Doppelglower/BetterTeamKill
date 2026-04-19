using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Collections.Generic;
using System;
using UI.Utility;
using System.Linq;

namespace BetterTeamKill;


[BepInPlugin(GUID, PLUGIN_NAME, VERSION)]
[BepInDependency("Lethe", BepInDependency.DependencyFlags.HardDependency)]
public class Main : BasePlugin
{
    public const string GUID = $"{AUTHOR}.{PLUGIN_NAME}";
    public const string PLUGIN_NAME = "BetterTeamKill";
    public const string VERSION = "1.0.3";
    public const string AUTHOR = "Doppelglower";
    public static Harmony harmony = new(GUID);
    public static ManualLogSource sharedLog;
    public static List<int> slotIDs = [];
    public static List<SinActionModel> sams = [];
    public static List<UnitSinModel> usms = [];
    public static List<SinActionModel> tsas = [];
    public static bool isSelected = false;

    public override void Load()
    {
        sharedLog = Log;
        sharedLog.LogInfo($"{PLUGIN_NAME} loaded.");
        harmony.PatchAll(typeof(Main));
    }

    [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.OnRoundStart_After_Event))]
    [HarmonyPostfix]
    public static void Postfix_BattleUnitModel_OnRoundStart_After_Event(BattleUnitModel __instance)
    {
        if (!__instance.IsFaction(UNIT_FACTION.PLAYER))
        {
            return;
        }
        var battleObjectManager = BattleObjectManager.Instance;
        if (battleObjectManager == null)
        {
            return;
        }
        var battleUIRoot = BattleUI.BattleUIRoot.Instance;
        if (battleUIRoot == null)
        {
            return;
        }
        var unitUIManager = battleObjectManager.GetView(__instance).UIManager;
        if (unitUIManager == null)
        {
            return;
        }
        var battleActionModelManager = BattleActionModelManager.Instance;
        if (battleActionModelManager == null)
        {
            return;
        }

        // defense target self?

        foreach (var unitActionSlotUI in unitUIManager.UnitActionUI._actionSlotUIList)
        {
            var instanceID = unitActionSlotUI.GetInstanceID();
            if (!slotIDs.Contains(instanceID))
            {
                slotIDs.Add(instanceID);
                unitActionSlotUI._trigger.AttachEntry(UnityEngine.EventSystems.EventTriggerType.PointerEnter, new Action(() =>
                {
                    //battleUIRoot.ClearAllArrows();
                    SoundGenerator.PlayUISound(UI_SOUNDS_TYPE.BattleUI_CharacterSlot_Click);

                    if (battleUIRoot.AbUIController.IsDragginSin())
                    {
                        battleUIRoot.AbUIController._abOperationTracker._opDragginData._targetSinActionModel = unitActionSlotUI._sinAction;
                        battleUIRoot.NewOperationController.UpdateDraggingSlotFromOpSlotToSinAction();
                        UnitSinModel dragginSin = battleUIRoot.AbUIController.GetDragginSin();
                        battleUIRoot.CreateExpectedArrowByActionOver(dragginSin.GetBattleActionModel().SinAction, unitActionSlotUI._sinAction);
                        if (unitActionSlotUI._sinAction.CurrentBattleAction != null)
                        {
                            if (unitActionSlotUI._sinAction.CurrentBattleAction.GetMainTargetSinAction() == dragginSin.GetBattleActionModel().SinAction && BattleActionModel.CanDuelBoth(dragginSin.GetBattleActionModel(), unitActionSlotUI._sinAction.CurrentBattleAction))
                            {
                                battleActionModelManager.RemoveDuel(unitActionSlotUI._sinAction.CurrentBattleAction);
                                battleActionModelManager.AddDuel(unitActionSlotUI._sinAction.CurrentBattleAction, dragginSin.GetBattleActionModel());
                            }
                        }
                        battleUIRoot.ShowExpectedSkillInfoByOverAction(dragginSin, unitActionSlotUI._sinAction);
                        if (unitActionSlotUI._sinAction.currentSelectSin != null)
                        {
                            battleUIRoot.ShowSkillInfoByOperation(unitActionSlotUI._sinAction.currentSelectSin, dragginSin.GetBattleActionModel().SinAction);
                        }
                    }
                    else if (battleUIRoot.AbUIController.IsClickedOpSlot())
                    {
                        battleUIRoot.AbUIController._abOperationTracker._opDragginData._targetSinActionModel = unitActionSlotUI._sinAction;
                        UnitSinModel clickedSin = battleUIRoot.AbUIController.GetClickedSin();
                        if (unitActionSlotUI._sinAction.CurrentBattleAction != null)
                        {
                            if (unitActionSlotUI._sinAction.CurrentBattleAction.GetMainTargetSinAction() == clickedSin.GetBattleActionModel().SinAction && BattleActionModel.CanDuelBoth(clickedSin.GetBattleActionModel(), unitActionSlotUI._sinAction.CurrentBattleAction))
                            {
                                battleActionModelManager.RemoveDuel(unitActionSlotUI._sinAction.CurrentBattleAction);
                                battleActionModelManager.AddDuel(unitActionSlotUI._sinAction.CurrentBattleAction, clickedSin.GetBattleActionModel());
                            }
                        }
                        if (unitActionSlotUI._sinAction != null)
                        {
                            battleUIRoot.CreateExpectedArrowByActionOver(clickedSin.GetBattleActionModel().SinAction, unitActionSlotUI._sinAction);
                            battleUIRoot.ShowExpectedSkillInfoByOverAction(clickedSin, unitActionSlotUI._sinAction);
                        }
                        else
                        {
                            battleUIRoot.ShowExpectedSkillInfoByOverAction(clickedSin);
                        }
                        if (unitActionSlotUI._sinAction.currentSelectSin != null)
                        {
                            battleUIRoot.ShowSkillInfoByOperation(unitActionSlotUI._sinAction.currentSelectSin, clickedSin.GetBattleActionModel().SinAction);
                        }
                    }
                    else
                    {
                        // if (unitActionSlotUI._sinAction != null && unitActionSlotUI._sinAction.CurrentBattleAction != null && unitActionSlotUI._sinAction.CurrentBattleAction.GetMainTargetSinAction() != null)
                        // {
                        //     battleUIRoot.CreateExpectedArrowByActionOver(unitActionSlotUI._sinAction, unitActionSlotUI._sinAction.CurrentBattleAction.GetMainTargetSinAction());
                        // }
                        // battleUIRoot.ClearHighlightAll();
                        // BattleObjectManager.Instance.GetView(unitActionSlotUI._sinAction.UnitModel.InstanceID).ShowSkillInfoBySdOver();
                        // battleUIRoot.ShowSkillInfoByUpperSlotOver(unitActionSlotUI._sinAction);
                        // if (unitActionSlotUI._sinAction.currentSelectSin != null)
                        // {
                        //     battleUIRoot.ShowSkillInfoByOperation(unitActionSlotUI._sinAction.currentSelectSin, (unitActionSlotUI._sinAction.currentSelectSin.GetBattleActionModel().GetTargetSinActionList().Count <= 0) ? null : unitActionSlotUI._sinAction.currentSelectSin.GetBattleActionModel().GetTargetSinActionList()[0]);
                        // }
                    }
                }));

                unitActionSlotUI._trigger.AttachEntry(UnityEngine.EventSystems.EventTriggerType.PointerExit, new Action(() =>
                {
                    battleUIRoot.ClearAllArrows();
                    battleUIRoot.AbUIController.ClearDragginTargetData(updateOperation: true);

                    if (battleUIRoot.AbUIController.IsDragginSin() || battleUIRoot.AbUIController.IsClickedOpSlot())
                    {
                        battleUIRoot.ShowAbnormalityBattleDragSin();
                        if (battleUIRoot.AbUIController.IsDragginSin())
                        {
                            battleUIRoot.OffSkillInfo();
                            UnitSinModel dragginSin = battleUIRoot.AbUIController.GetDragginSin();
                            battleUIRoot.ShowSkillInfoByOperation(dragginSin, unitActionSlotUI._sinAction);
                            battleUIRoot.AbUIController._abOperationTracker._opDragginData._targetSinActionModel = null;
                        }
                        else if (battleUIRoot.AbUIController.IsClickedOpSlot())
                        {
                            battleUIRoot.OffSkillInfo();
                            UnitSinModel clickedSin = battleUIRoot.AbUIController.GetClickedSin();
                            battleUIRoot.ShowSkillInfoByOperation(clickedSin, unitActionSlotUI._sinAction);
                            battleUIRoot.AbUIController._abOperationTracker._opDragginData._targetSinActionModel = null;
                        }
                    }
                    else
                    {
                        // BattleEffectManager.Instance.SetFadeBlackBackground_BattleView(value: false, commandState: true);
                        // battleUIRoot.OffSkillInfo();
                        // battleUIRoot.ShowAllCharacterTargetArrows();
                    }
                }));
                unitActionSlotUI._trigger.AttachEntry(UnityEngine.EventSystems.EventTriggerType.PointerClick, new Action(() =>
                {
                    if (battleUIRoot.AbUIController.IsDragginSin())
                    {
                        UnitSinModel dragginSin = battleUIRoot.AbUIController.GetDragginSin();
                        battleUIRoot.OffSkillInfo();
                        battleUIRoot.NewOperationController.EndDrag(dragginSin);
                        battleUIRoot.AbUIController._abOperationTracker.EndActionClicked();
                    }
                    else if (battleUIRoot.AbUIController.IsClickedOpSlot())
                    {
                        UnitSinModel clickedSin = battleUIRoot.AbUIController.GetClickedSin();
                        battleUIRoot.OffSkillInfo();
                        SinActionModel sinAction = clickedSin.GetBattleActionModel().SinAction;
                        sinAction.SelectSin(clickedSin, unitActionSlotUI._sinAction);
                        battleUIRoot.NewOperationController.EndDrag(clickedSin);
                        battleUIRoot.AbUIController._abOperationTracker.EndActionClicked();
                    }
                }));
            }
        }
    }

    [HarmonyPatch(typeof(SinActionModel), nameof(SinActionModel.IsTargetable))]
    [HarmonyPostfix]
    public static void Postfix_SinActionModel_IsTargetable(SinActionModel __instance, BattleUnitModel attacker, ref bool __result)
    {
        if (__instance.GetFaction() == UNIT_FACTION.PLAYER && attacker.IsFaction(UNIT_FACTION.PLAYER) && !__instance.UnitModel.Is(attacker))
        {
            __result = true;
        }
    }

    [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.IsTargetable))]
    [HarmonyPostfix]
    public static void Postfix_BattleUnitModel_IsTargetable(BattleUnitModel __instance, BattleUnitModel attacker, ref bool __result)
    {
        if (__instance.IsFaction(UNIT_FACTION.PLAYER) && attacker.IsFaction(UNIT_FACTION.PLAYER) && !__instance.Is(attacker))
        {
            __result = true;
        }
    }

    [HarmonyPatch(typeof(StageModel), nameof(StageModel.Init))]
    [HarmonyPrefix]
    public static void Prefix_StageModel_Init()
    {
        slotIDs.Clear();
        sams.Clear();
        usms.Clear();
        tsas.Clear();
        isSelected = false;
    }

    [HarmonyPatch(typeof(BattleUI.Operation.NewOperationController), nameof(BattleUI.Operation.NewOperationController.EquipDefense))]
    [HarmonyPrefix]
    public static void Prefix_BattleUI_Operation_NewOperationController_EquipDefense(BattleUI.Operation.NewOperationController __instance, SinActionModel sinAction)
    {
        sams.Clear();
        usms.Clear();
        tsas.Clear();
        isSelected = false;
        var sm = SinManager.Instance;
        if (sm == null)
        {
            return;
        }
        if (sinAction.currentSelectSin != null && sinAction.currentSelectSin == sinAction.currentSinList[0])
        {
            isSelected = true;
        }
        foreach (var sam in sm.GetActionListByFactionExceptAssistant(UNIT_FACTION.PLAYER))
        {
            sams.Add(sam);
            var usm = sam.currentSelectSin;
            usms.Add(usm);
            var tsal = usm?.GetBattleActionModel()?.GetTargetSinActionList();
            SinActionModel tsa = null;
            if (tsal != null && tsal.Count > 0)
            {
                tsa = tsal[0];
            }
            tsas.Add(tsa);
            sam.DeSelectSin();
        }
    }

    [HarmonyPatch(typeof(BattleUI.Operation.NewOperationController), nameof(BattleUI.Operation.NewOperationController.EquipDefense))]
    [HarmonyPostfix]
    public static void Postfix_BattleUI_Operation_NewOperationController_EquipDefense(BattleUI.Operation.NewOperationController __instance, SinActionModel sinAction)
    {
        if (sams.Any())
        {
            for (int i = 0; i < sams.Count; i++)
            {
                var sam = sams[i];
                if (sam.currentSelectSin == null)
                {
                    var usm = usms[i];
                    if (sam.InstanceID == sinAction.InstanceID && isSelected)
                    {
                        usm = sinAction.currentSinList[0];
                    }
                    var tsa = tsas[i];
                    sam.SelectSin(usm, tsa);
                }
            }
            var asasl = __instance.GetActionableSinActionSlotList();
            foreach (var asas in asasl)
            {
                asas.UpdateStateForAb();
            }
        }
    }

    [HarmonyPatch(typeof(BattleUI.Operation.NewOperationController), nameof(BattleUI.Operation.NewOperationController.NewEquipEgo))]
    [HarmonyPrefix]
    public static void Prefix_BattleUI_Operation_NewOperationController_NewEquipEgo(BattleUI.Operation.NewOperationController __instance, SinActionModel sinActionModel)
    {
        sams.Clear();
        usms.Clear();
        tsas.Clear();
        isSelected = false;
        var sm = SinManager.Instance;
        if (sm == null)
        {
            return;
        }
        if (sinActionModel.currentSelectSin != null && sinActionModel.currentSelectSin == sinActionModel.currentSinList[0])
        {
            isSelected = true;
        }
        foreach (var sam in sm.GetActionListByFactionExceptAssistant(UNIT_FACTION.PLAYER))
        {
            sams.Add(sam);
            var usm = sam.currentSelectSin;
            usms.Add(usm);
            var tsal = usm?.GetBattleActionModel()?.GetTargetSinActionList();
            SinActionModel tsa = null;
            if (tsal != null && tsal.Count > 0)
            {
                tsa = tsal[0];
            }
            tsas.Add(tsa);
            sam.DeSelectSin();
        }
    }

    [HarmonyPatch(typeof(BattleUI.Operation.NewOperationController), nameof(BattleUI.Operation.NewOperationController.NewEquipEgo))]
    [HarmonyPostfix]
    public static void Postfix_BattleUI_Operation_NewOperationController_NewEquipEgo(BattleUI.Operation.NewOperationController __instance, SinActionModel sinActionModel)
    {
        if (sams.Any())
        {
            for (int i = 0; i < sams.Count; i++)
            {
                var sam = sams[i];
                if (sam.currentSelectSin == null)
                {
                    var usm = usms[i];
                    if (sam.InstanceID == sinActionModel.InstanceID && isSelected)
                    {
                        usm = sinActionModel.currentSinList[0];
                    }
                    var tsa = tsas[i];
                    sam.SelectSin(usm, tsa);
                }
            }
            var asasl = __instance.GetActionableSinActionSlotList();
            foreach (var asas in asasl)
            {
                asas.UpdateStateForAb();
            }
        }
    }
}

public static class BattleUnitModelExtensions
{
    public static bool Is(this BattleUnitModel self, BattleUnitModel other)
    {
        bool selfIsNull = self == null || self.Pointer == IntPtr.Zero;
        bool otherIsNull = other == null || other.Pointer == IntPtr.Zero;
        if (selfIsNull && otherIsNull) return true;
        if (selfIsNull || otherIsNull) return false;
        return self.InstanceID == other.InstanceID;
    }
}
