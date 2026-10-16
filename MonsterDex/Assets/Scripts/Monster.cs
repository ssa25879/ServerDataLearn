using System;

// 서버가 보내는 JSON과 모양이 같은 C# 클래스
// JSON의 키 이름(id, name, hp, attack, speed)과 필드 이름이 정확히 같아야 JsonUtility가 채워 준다
[Serializable]
public class Monster
{
    public int id;
    public string name;
    public int hp;
    public int attack;
    public float speed;
    public bool isBoss;
}

// JsonUtility는 맨 바깥이 배열([ ... ])인 JSON을 읽지 못한다
// 그래서 서버는 { "monsters": [ ... ] } 모양으로 보내고, 그 모양에 맞는 "바깥 상자" 클래스를 둔다
// 참고로 필드 타입은 List<Monster>도 가능하다
[Serializable]
public class MonsterList
{
    public Monster[] monsters;
}