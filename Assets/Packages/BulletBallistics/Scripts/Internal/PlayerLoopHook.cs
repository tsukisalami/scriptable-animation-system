using UnityEngine;

namespace Ballistics
{
    public struct BallisticsInitialize { }
    public struct BallisticsUpdate { }
    public struct BallisticsFinalize { }
    public struct BallisticsUpdatedDelayedCalls { }

    /// hook ballistics simulation directly into the unity player loop
    public static class PlayerLoopHook
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void InitializeBallisticsUpdateHook()
        {
            using (var edit = PlayerLoopEdit.BeginEdit()) {
                edit.Insert(new() {
                    type = typeof(BallisticsUpdatedDelayedCalls), updateDelegate = Delay.Update
                }, typeof(UnityEngine.PlayerLoop.Update.ScriptRunBehaviourUpdate));

                edit.Insert(new() {
                    type = typeof(BallisticsInitialize), updateDelegate = Core.InitializeUpdate
                }, typeof(UnityEngine.PlayerLoop.Update.ScriptRunBehaviourUpdate), PlayerLoopEdit.InsertMode.After);

                edit.Insert(new() {
                    type = typeof(BallisticsUpdate), updateDelegate = Core.Update
                }, typeof(UnityEngine.PlayerLoop.PreLateUpdate.ScriptRunBehaviourLateUpdate));

                edit.Insert(new() {
                    type = typeof(BallisticsFinalize), updateDelegate = Core.CompleteUpdates
                }, typeof(UnityEngine.PlayerLoop.PostLateUpdate.UpdateAllRenderers));
            }
        }
    }
}