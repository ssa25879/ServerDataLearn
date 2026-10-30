using System;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

// 2회차 · 비동기 요청: 같은 요청을 async와 await로 — 응답을 기다리는 동안 화면은 계속 돈다
public class MonsterClientAsync : MonoBehaviour
{
    public string url = "http://localhost:5000/monsters/slow?ms=3000";
    public TMP_Text resultText;

    public async void OnClickLoad()
    {
        resultText.text = "요청 중...";   // 이번에는 이 글자가 보인다
        try
        {
            MonsterList list = await FetchMonstersAsync(url);   // 끝날 때까지 이 메서드만 멈추고 메인은 돌아간다
            ShowMonsters(list);                                  // 응답이 온 프레임에 여기부터 이어진다
        }
        catch (Exception e)
        {
            resultText.text = "실패: " + e.Message;   // FetchMonstersAsync가 던진 예외가 여기로 온다
        }
    }

    // Task<T>: 02 콘솔에서 쓴 그 약속 그대로. 다른 것은 await 다음 줄이 메인 작업자에서 이어진다는 것뿐
    async Task<MonsterList> FetchMonstersAsync(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            await request.SendWebRequest();   // Unity 6: 요청 작업을 바로 await 할 수 있다. 여기서 메인으로 돌아간다

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception(request.error);   // 화면에 쓰지 않고 부른 쪽의 catch로 보낸다

            return JsonUtility.FromJson<MonsterList>(request.downloadHandler.text);   // 값을 돌려준다
        }
    }

    void ShowMonsters(MonsterList list)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"총 {list.monsters.Length}마리");
        int shown = Mathf.Min(list.monsters.Length, 20);   // 화면에는 20마리까지만 (2만 줄을 넣으면 그것만으로 멈춘다)
        for (int i = 0; i < shown; i++)
        {
            Monster m = list.monsters[i];
            sb.AppendLine($"{m.id}. {m.name}   HP {m.hp}   공격 {m.attack}   속도 {m.speed}");
        }
        resultText.text = sb.ToString();
    }
}