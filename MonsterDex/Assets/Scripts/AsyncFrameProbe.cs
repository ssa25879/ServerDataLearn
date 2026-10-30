using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// 2회차 · await 앞뒤에서 프레임과 작업자가 어떻게 되는지 보는 실험. 빈 오브젝트에 붙인다
public class AsyncFrameProbe : MonoBehaviour
{
    async void Start()   // 시작만 하는 메서드라 async void
    {
        Debug.Log($"await 전 — 프레임 {Time.frameCount}, 작업자 {Thread.CurrentThread.ManagedThreadId}");
        await Task.Delay(1000);   // 여기서 메인으로 돌아갔다가 1초 뒤에 이어진다. 콘솔이면 아무 작업자, Unity는 메인
        Debug.Log($"await 후 — 프레임 {Time.frameCount}, 작업자 {Thread.CurrentThread.ManagedThreadId}");
    }

    void Update()
    {
        if (Time.frameCount <= 3)
        {
            Debug.Log($"Update — 프레임 {Time.frameCount}, 작업자 {Thread.CurrentThread.ManagedThreadId}");
        }
    }
}