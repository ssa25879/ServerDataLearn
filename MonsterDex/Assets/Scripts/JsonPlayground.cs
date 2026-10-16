using UnityEngine;

// 1회차 · 0단계: JSON ↔ C# 클래스를 콘솔에서 눈으로 확인한다
public class JsonPlayground : MonoBehaviour
{
    void Start()
    {
        // 1) C# 객체 → JSON 글자 (직렬화)
        Monster zombie = new Monster { id = 1, name = "일반 좀비", hp = 100, attack = 20, speed = 2.0f };
        string json = JsonUtility.ToJson(zombie, true);   // true: 보기 좋게 줄바꿈
        Debug.Log(json);

        // 2) JSON 글자 → C# 객체 (역직렬화)
        string fromServer = "{\"id\":2,\"name\":\"빠른 좀비\",\"hp\":60,\"attack\":15,\"speed\":4.5}";
        Monster fast = JsonUtility.FromJson<Monster>(fromServer);
        Debug.Log($"{fast.name}의 속도는 {fast.speed}");

        // 3) 배열은 "바깥 상자"가 필요하다
        string listJson = "{\"monsters\":[{\"id\":1,\"name\":\"일반 좀비\",\"hp\":100,\"attack\":20,\"speed\":2.0},{\"id\":2,\"name\":\"빠른 좀비\",\"hp\":60,\"attack\":15,\"speed\":4.5}]}";
        MonsterList list = JsonUtility.FromJson<MonsterList>(listJson);
        Debug.Log($"몬스터 {list.monsters.Length}마리, 첫 번째는 {list.monsters[0].name}");
        
        // 4) 현장 과제: 캐릭터 안에 스킬 배열이 들어 있다
        string characterJson = "{\"id\":7,\"name\":\"용사\",\"level\":12,\"hp\":350,\"skills\":[{\"name\":\"베기\",\"damage\":40,\"cooldown\":1.5},{\"name\":\"회전 베기\",\"damage\":90,\"cooldown\":6.0},{\"name\":\"방패 밀치기\",\"damage\":20,\"cooldown\":3.0}]}";
        // 여기에 FromJson과 Debug.Log를 씁니다
        Character character = JsonUtility.FromJson<Character>(characterJson);
        Debug.Log($"{character.name} (Lv.{character.level}) 스킬 {character.skills.Length}");
        foreach(Skill s in character.skills)
        {
            Debug.Log($"- {s.name}:피해 {s.damage} | 쿨다운 : {s.cooldown}");
        }
        
    }
}