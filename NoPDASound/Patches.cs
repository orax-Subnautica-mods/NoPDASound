using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NoPDASound;

[HarmonyPatch]
public static class Patches
{
    [HarmonyPatch(typeof(uGUI_PDA), nameof(uGUI_PDA.OnOpenPDA))]
    public static class Patch_uGUI_PDA_OnOpenPDA
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            /*
            [PATCH]: br           IL_0050  // add a jump to IL_0050

            IL_002f: ldarg.0      // this
            IL_0030: call         instance bool uGUI_PDA::get_introActive()
            IL_0035: brtrue.s     IL_0050

            IL_0037: ldarg.0      // this
            IL_0038: ldfld        class ['Assembly-CSharp-firstpass']FMODAsset uGUI_PDA::soundOpen
            IL_003d: ldfld        string ['Assembly-CSharp-firstpass']FMODAsset::path
            IL_0042: ldloca.s     V_1
            IL_0044: initobj      [UnityEngine.CoreModule]UnityEngine.Vector3
            IL_004a: ldloc.1      // V_1
            IL_004b: call         void [FMODUnity]FMODUnity.RuntimeManager::PlayOneShot(string, valuetype [UnityEngine.CoreModule]UnityEngine.Vector3)

            IL_0050: ldarg.1      // tabId
            */

            CodeMatcher cm = new CodeMatcher(instructions);

            // Find:
            // if (!this.introActive) { RuntimeManager.PlayOneShot(this.soundOpen.path); }
            cm.MatchForward(false, // false = move at the start of the match, true = move at the end of the match
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(uGUI_PDA), "get_introActive")),
                    new CodeMatch(OpCodes.Brtrue));

            if (cm.IsValid)
            {
                CodeInstruction brtrue = cm.InstructionAt(2);
                CodeInstruction br = new CodeInstruction(OpCodes.Br, brtrue.operand);
                br.MoveLabelsFrom(brtrue); // copy labels if any
                cm.Insert(br);
            }
            else
            {
                Plugin.Logger.LogError("Unable to patch uGUI_PDA.OnOpenPDA().");
            }

            return cm.InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(uGUI_PDA), nameof(uGUI_PDA.OnClosePDA))]
    public static class Patch_uGUI_PDA_OnClosePDA
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            /*
            The patch removes these instructions.

            IL_0025: ldarg.0      // this
            IL_0026: ldfld        class ['Assembly-CSharp-firstpass']FMODAsset uGUI_PDA::soundClose
            IL_002b: ldfld        string ['Assembly-CSharp-firstpass']FMODAsset::path
            IL_0030: ldloca.s     V_0
            IL_0032: initobj      [UnityEngine.CoreModule]UnityEngine.Vector3
            IL_0038: ldloc.0      // V_0
            IL_0039: call         void [FMODUnity]FMODUnity.RuntimeManager::PlayOneShot(string, valuetype [UnityEngine.CoreModule]UnityEngine.Vector3)
            */
            CodeMatcher cm = new CodeMatcher(instructions);

            // Find:
            // RuntimeManager.PlayOneShot(this.soundClose.path);
            CodeMatch[] codeMatches = new[] {
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "soundClose"),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Ldloca_S),
                    new CodeMatch(OpCodes.Initobj),
                    new CodeMatch(OpCodes.Ldloc_0),
                    new CodeMatch(OpCodes.Call)};
            cm.MatchForward(false, codeMatches);

            if (cm.IsValid)
            {
                cm.RemoveInstructions(codeMatches.Length);
            }
            else
            {
                Plugin.Logger.LogError("Unable to patch uGUI_PDA.OnClosePDA().");
            }

            return cm.InstructionEnumeration();
        }
    }
}