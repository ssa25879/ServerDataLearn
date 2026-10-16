using System;

[Serializable]
public class Character
{
    public int id;
    public string name;
    public int level;
    public int hp;
    public Skill[] skills;
}

[Serializable]
public class Skill
{
    public string name;
    public int damage;
    public float cooldown;
}

[Serializable]
public class CharacterList
{
    public Character[] characters;
}