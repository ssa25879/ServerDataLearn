using System.Text;              // StringBuilder: 글자를 여러 줄 이어 붙일 때 쓴다
using TMPro;                    // TMP_Text: TextMeshPro 텍스트 컴포넌트
using UnityEngine;              // MonoBehaviour, JsonUtility
using UnityEngine.Networking;   // UnityWebRequest: HTTP 요청을 보내는 도구

// 1회차 · 2단계: 서버에 요청하고 응답을 기다린 뒤 화면에 뿌린다 (동기 방식)
// 버튼의 OnClick()에 OnClickLoad를 연결한다
public class MonsterClientSync : MonoBehaviour
{
    public string url = "http://localhost:5000/monsters";   // 요청을 보낼 주소. Inspector에서 바꿀 수 있다
    public TMP_Text resultText;                             // 결과를 보여 줄 텍스트. Inspector에서 ResultText를 끌어다 놓는다

    // 버튼을 누르면 실행된다
    public void OnClickLoad()
    {
        resultText.text = "Loading...";   // 이 글자는 화면에 보이지 않는다 — 왜인지는 2회차에서

        // GET 요청 객체를 만든다. 택배 상자에 주소를 적는 단계. using: 블록이 끝나면 자동으로 정리한다
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();   // 요청을 실제로 보낸다. 서버의 MapGet("/monsters")가 실행된다

            // 동기: 응답이 올 때까지 이 줄에서 빙글빙글 돈다. 그동안 Update()는 한 번도 불리지 않는다
            var watch = System.Diagnostics.Stopwatch.StartNew();   // 기다린 시간을 재는 시계
            while (!operation.isDone)                              // 응답이 아직 안 왔으면 계속 반복한다
            {
                if (watch.ElapsedMilliseconds > 10000) break;   // 안전장치: 10초가 넘으면 포기한다
            }

            // 서버가 꺼져 있거나 주소가 틀리면 여기서 걸린다
            if (request.result != UnityWebRequest.Result.Success)
            {
                resultText.text = "Failed: " + request.error;   // 실패 이유를 화면에 보여 준다
                return;                                       // 아래로 내려가지 않고 끝낸다
            }

            ShowMonsters(request.downloadHandler.text);   // 응답 본문(JSON 글자)을 꺼내 화면에 뿌린다
        }
    }

    // JSON 글자를 받아 몬스터 목록을 텍스트로 만든다
    void ShowMonsters(string json)
    {
        MonsterList list = JsonUtility.FromJson<MonsterList>(json);   // 역직렬화: JSON 글자 → C# 객체. 바깥 상자 MonsterList로 받는다

        var sb = new StringBuilder();                 // 여러 줄을 모을 빈 글자 상자
        foreach (Monster m in list.monsters)          // 몬스터를 하나씩 꺼내서
        {
            sb.AppendLine($"{m.id}. {m.name}   HP {m.hp}   ATK {m.attack}   SPD {m.speed}");   // 한 줄로 만들어 붙인다
        }
        resultText.text = sb.ToString();              // 모은 글자를 화면에 표시한다
    }
}