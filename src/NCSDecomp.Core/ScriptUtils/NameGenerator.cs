// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.ScriptNode;

namespace NCSDecomp.Core.ScriptUtils
{
    internal static class NameGenerator
    {
        private static string ActionParamTag(IAExpression input)
        {
            if (input is AConst ac)
            {
                string str = ac.ToString();
                if (str.Length > 2)
                {
                    return (char.ToUpper(str[1]) + str.Substring(2, str.Length - 3)).Replace(' ', '_');
                }
            }
            return null;
        }

        private static int ActionParamToInt(IAExpression input)
        {
            if (!(input is AConst ac))
            {
                return -1;
            }

            return int.TryParse(ac.ToString(), out int v) ? v : -1;
        }

        public static string GetNameFromAction(AActionExp actionexp)
        {
            string action = actionexp.Action();
            if (action == "GetObjectByTag")
            {
                string tag = ActionParamTag(actionexp.GetParam(0));
                return tag != null ? "o" + tag : null;
            }
            else if (action == "GetFirstPC")
            {
                return "oPC";
            }
            else if (action == "GetScriptParameter")
            {
                int i = ActionParamToInt(actionexp.GetParam(0));
                return i > 0 ? "nParam" + i.ToString() : "nParam";
            }
            else if (action == "GetScriptStringParameter")
            {
                return "sParam";
            }
            else if (action == "GetMaxHitPoints")
            {
                return "nMaxHP";
            }
            else if (action == "GetCurrentHitPoints")
            {
                return "nCurHP";
            }
            else if (action == "Random")
            {
                return "nRandom";
            }
            else if (action == "GetArea")
            {
                return "oArea";
            }
            else if (action == "GetEnteringObject")
            {
                return "oEntering";
            }
            else if (action == "GetExitingObject")
            {
                return "oExiting";
            }
            else if (action == "GetPosition")
            {
                return "vPosition";
            }
            else if (action == "GetFacing")
            {
                return "fFacing";
            }
            else if (action == "GetLastAttacker")
            {
                return "oAttacker";
            }
            else if (action == "GetNearestCreature")
            {
                return "oNearest";
            }
            else if (action == "GetDistanceToObject")
            {
                return "fDistance";
            }
            else if (action == "GetIsObjectValid")
            {
                return "nValid";
            }
            else if (action == "GetSpellTargetObject")
            {
                return "oTarget";
            }
            else if (action == "EffectAssuredHit")
            {
                return "efHit";
            }
            else if (action == "GetLastItemEquipped")
            {
                return "oLastEquipped";
            }
            else if (action == "GetCurrentForcePoints")
            {
                return "nCurFP";
            }
            else if (action == "GetMaxForcePoints")
            {
                return "nMaxFP";
            }
            else if (action == "EffectHeal")
            {
                return "efHeal";
            }
            else if (action == "EffectDamage")
            {
                return "efDamage";
            }
            else if (action == "EffectAbilityIncrease")
            {
                return "efAbilityInc";
            }
            else if (action == "EffectDamageResistance")
            {
                return "efDamageRes";
            }
            else if (action == "EffectResurrection")
            {
                return "efResurrect";
            }
            else if (action == "GetCasterLevel")
            {
                return "nCasterLevel";
            }
            else if (action == "GetFirstObjectInArea")
            {
                return "oAreaObject";
            }
            else if (action == "GetNextObjectInArea")
            {
                return "oAreaObject";
            }
            else if (action == "GetObjectType")
            {
                return "nType";
            }
            else if (action == "GetRacialType")
            {
                return "nRace";
            }
            else if (action == "EffectACIncrease")
            {
                return "efACInc";
            }
            else if (action == "EffectSavingThrowIncrease")
            {
                return "efSaveInc";
            }
            else if (action == "EffectAttackIncrease")
            {
                return "efAttackInc";
            }
            else if (action == "EffectDamageReduction")
            {
                return "efDamageDec";
            }
            else if (action == "EffectDamageIncrease")
            {
                return "efDamageInc";
            }
            else if (action == "GetGoodEvilValue")
            {
                return "nAlign";
            }
            else if (action == "GetPartyMemberCount")
            {
                return "nPartyCount";
            }
            else if (action == "GetAlignmentGoodEvil")
            {
                return "nAlign";
            }
            else if (action == "GetFirstObjectInShape")
            {
                return "oShapeObject";
            }
            else if (action == "GetNextObjectInShape")
            {
                return "oShapeObject";
            }
            else if (action == "EffectEntangle")
            {
                return "efEntangle";
            }
            else if (action == "EffectDeath")
            {
                return "efDeath";
            }
            else if (action == "EffectKnockdown")
            {
                return "efKnockdown";
            }
            else if (action == "GetAbilityScore")
            {
                int i = ActionParamToInt(actionexp.GetParam(1));
                switch (i)
                {
                    case 0:
                        return "nStrength";
                    case 1:
                        return "nDex";
                    case 2:
                        return "nConst";
                    case 3:
                        return "nInt";
                    case 4:
                        return "nWis";
                    case 5:
                        return "nChar";
                    default:
                        return "nAbility";
                }
            }
            else if (action == "EffectParalyze")
            {
                return "efParalyze";
            }
            else if (action == "EffectSpellImmunity")
            {
                return "efSpellImm";
            }
            else if (action == "GetDistanceBetween")
            {
                return "fDistance";
            }
            else if (action == "EffectForceJump")
            {
                return "efForceJump";
            }
            else if (action == "EffectSleep")
            {
                return "efSleep";
            }
            else if (action == "GetItemInSlot")
            {
                int i = ActionParamToInt(actionexp.GetParam(0));
                switch (i)
                {
                    case 0:
                        return "oHeadItem";
                    case 1:
                        return "oBodyItem";
                    case 2:
                    case 6:
                    case 11:
                    case 12:
                    case 13:
                    default:
                        return "oSlotItem";
                    case 3:
                        return "oHandsItem";
                    case 4:
                        return "oRWeapItem";
                    case 5:
                        return "oLWeapItem";
                    case 7:
                        return "oLArmItem";
                    case 8:
                        return "oRArmItem";
                    case 9:
                        return "oImplantItem";
                    case 10:
                        return "oBeltItem";
                    case 14:
                        return "oCWeapLItem";
                    case 15:
                        return "oCWeapRItem";
                    case 16:
                        return "oCWeapBItem";
                    case 17:
                        return "oCArmourItem";
                    case 18:
                        return "oRWeap2Item";
                    case 19:
                        return "oLWeap2Item";
                }
            }
            else if (action == "EffectTemporaryForcePoints")
            {
                return "efTempFP";
            }
            else if (action == "EffectConfused")
            {
                return "efConfused";
            }
            else if (action == "EffectFrightened")
            {
                return "efFright";
            }
            else if (action == "EffectChoke")
            {
                return "efChoke";
            }
            else if (action == "EffectStunned")
            {
                return "efStun";
            }
            else if (action == "EffectRegenerate")
            {
                return "efRegen";
            }
            else if (action == "EffectMovementSpeedIncrease")
            {
                return "efSpeedInc";
            }
            else if (action == "GetHitDice")
            {
                return "nLevel";
            }
            else if (action == "GetEffectType")
            {
                return "nEfType";
            }
            else if (action == "EffectAreaOfEffect")
            {
                return "efAOE";
            }
            else if (action == "EffectVisualEffect")
            {
                return "efVisual";
            }
            else if (action == "GetFactionWeakestMember")
            {
                return "oWeakest";
            }
            else if (action == "GetFactionStrongestMember")
            {
                return "oStrongest";
            }
            else if (action == "GetFactionMostDamagedMember")
            {
                return "oMostDamaged";
            }
            else if (action == "GetFactionLeastDamagedMember")
            {
                return "oLeastDamaged";
            }
            else if (action == "GetWaypointByTag")
            {
                string tag = ActionParamTag(actionexp.GetParam(0));
                return tag != null ? "o" + tag : "oWP";
            }
            else if (action == "GetTransitionTarget")
            {
                return "oTransTarget";
            }
            else if (action == "EffectBeam")
            {
                return "efBeam";
            }
            else if (action == "GetReputation")
            {
                return "nRep";
            }
            else if (action == "GetModuleFileName")
            {
                return "sModule";
            }
            else if (action == "EffectForceResistanceIncrease")
            {
                return "efForceResInc";
            }
            else if (action == "GetSpellTargetLocation")
            {
                return "locTarget";
            }
            else if (action == "EffectBodyFuel")
            {
                return "efFuel";
            }
            else if (action == "GetFacingFromLocation")
            {
                return "fFacing";
            }
            else if (action == "GetNearestCreatureToLocation")
            {
                return "oNearestCreat";
            }
            else if (action == "GetNearestObject")
            {
                return "oNearest";
            }
            else if (action == "GetNearestObjectToLocation")
            {
                return "oNearest";
            }
            else if (action == "GetNearestObjectByTag")
            {
                string tag = ActionParamTag(actionexp.GetParam(0));
                return tag != null ? "oNearest" + tag : null;
            }
            else if (action == "GetPCSpeaker")
            {
                return "oSpeaker";
            }
            else if (action == "GetModule")
            {
                return "oModule";
            }
            else if (action == "CreateObject")
            {
                string tag = ActionParamTag(actionexp.GetParam(1));
                return tag != null ? "o" + tag : null;
            }
            else if (action == "EventSpellCastAt")
            {
                return "evSpellCast";
            }
            else if (action == "GetLastSpellCaster")
            {
                return "oCaster";
            }
            else if (action == "EffectPoison")
            {
                return "efPoison";
            }
            else if (action == "EffectAssuredDeflection")
            {
                return "efDeflect";
            }
            else if (action == "GetName")
            {
                return "sName";
            }
            else if (action == "GetLastSpeaker")
            {
                return "oSpeaker";
            }
            else if (action == "GetLastPerceived")
            {
                return "oPerceived";
            }
            else if (action == "EffectForcePushTargeted")
            {
                return "efPush";
            }
            else if (action == "EffectHaste")
            {
                return "efHaste";
            }
            else if (action == "EffectImmunity")
            {
                return "efImmunity";
            }
            else if (action == "GetIsImmune")
            {
                return "nImmune";
            }
            else if (action == "EffectDamageImmunityIncrease")
            {
                return "efDamageImmInc";
            }
            else if (action == "GetDistanceBetweenLocations")
            {
                return "fDistance";
            }
            else if (action == "GetLocalNumber")
            {
                return "nLocal";
            }
            else if (action == "GetStringLength")
            {
                return "nLen";
            }
            else if (action == "GetObjectPersonalSpace")
            {
                return "fPersonalSpace";
            }
            else if (action == "d8")
            {
                return "nRandom";
            }
            else if (action == "d10")
            {
                return "nRandom";
            }
            else if (action == "GetPartyMemberByIndex")
            {
                return "oNPC";
            }
            else if (action == "GetAttackTarget")
            {
                return "oTarget";
            }
            else if (action == "GetCreatureTalentRandom")
            {
                return "talRandom";
            }
            else if (action == "GetPUPOwner")
            {
                return "oPUPOwner";
            }
            else if (action == "GetDistanceToObject2D")
            {
                return "fDistance";
            }
            else if (action == "GetCurrentAction")
            {
                return "nAction";
            }
            else if (action == "GetPartyLeader")
            {
                return "oLeader";
            }
            else if (action == "GetFirstEffect")
            {
                return "efFirst";
            }
            else if (action == "GetNextEffect")
            {
                return "efNext";
            }
            else if (action == "GetPartyAIStyle")
            {
                return "nStyle";
            }
            else if (action == "GetNPCAIStyle")
            {
                return "nNPCStyle";
            }
            else if (action == "GetLastHostileTarget")
            {
                return "oLastTarget";
            }
            else if (action == "GetLastHostileActor")
            {
                return "oLastActor";
            }
            else if (action == "GetRandomDestination")
            {
                return "vRandom";
            }
            else if (action == "Location")
            {
                return null;
            }
            else if (action == "GetHealTarget")
            {
                return "oTarget";
            }
            else if (action == "GetCreatureTalentBest")
            {
                return "talBest";
            }
            else if (action == "d4")
            {
                return "nRandom";
            }
            else if (action == "d6")
            {
                return "nRandom";
            }
            else if (action == "d100")
            {
                return "nRandom";
            }
            else if (action == "d3")
            {
                return "nRandom";
            }
            else if (action == "GetIdFromTalent")
            {
                return "nTalent";
            }
            else if (action == "GetLocalBoolean")
            {
                return "nLocalBool";
            }
            else if (action == "TalentSpell")
            {
                return "talSpell";
            }
            else if (action == "TalentFeat")
            {
                return "talFeat";
            }
            else if (action == "FloatToString")
            {
                return null;
            }
            else if (action == "GetLocation")
            {
                return null;
            }
            else if (action == "IntToString")
            {
                return null;
            }
            else if (action == "GetGlobalNumber")
            {
                return "nGlobal";
            }
            else if (action == "GetBaseItemType")
            {
                return "nItemType";
            }
            else if (action == "GetFirstItemInInventory")
            {
                return "oInvItem";
            }
            else if (action == "GetNextItemInInventory")
            {
                return "oInvItem";
            }
            else if (action == "GetSpellBaseForcePointCost")
            {
                return "nBaseFP";
            }
            else if (action == "GetLastForcePowerUsed")
            {
                return "nLastForce";
            }
            else if (action == "StringToInt")
            {
                return null;
            }
            else
            {
                SubScriptLogger.Trace("Variable Naming: consider adding " + action);
                return null;
            }
        }
    }
}
