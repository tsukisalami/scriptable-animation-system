using Ballistics;
using Unity.Collections;
using UnityEngine;

public class RawBulletDataAccess : MonoBehaviour
{
    private int used = 0;
    private NativeArray<BulletNative> bulletNative;

    private void Awake()
    {
        Core.OnBeginUpdate += BeforeBallisticUpdate;
    }

    private void OnDestroy()
    {
        Core.OnBeginUpdate -= BeforeBallisticUpdate;
        if (bulletNative.IsCreated) {
            bulletNative.Dispose();
        }
    }

    private void BeforeBallisticUpdate()
    {
        if (!enabled)
            return;
        CopyBulletData();

        // do something with bulletNative

        // use a job instead..
        // for (var i = 0; i < used; i++) {
        //     Debug.LogFormat("{0} - position: {1}", i, bulletNative[i].Position);
        // }
    }

    private void CopyBulletData()
    {
        var loop = Core.BulletUpdateLoop;
        used = loop.ActiveCount();
        if (bulletNative.Length < used || !bulletNative.IsCreated) {    // unsure buffer size
            if (bulletNative.IsCreated)
                bulletNative.Dispose();
            bulletNative = new NativeArray<BulletNative>(used, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        // TODO: move this to a job?
        var index = 0;
        foreach (var batchHandle in loop.UpdateBatchHandles) {      // copy bullet data from update batches
            var bulletData = batchHandle.BulletUpdater.BulletData;
            for (var i = bulletData.ActiveCount - 1; i >= 0; i--) {
                bulletNative[index++] = bulletData.Native[bulletData.Indices[i]];   // bullet data is not packed, use index buffer!
            }
        }
    }
}
