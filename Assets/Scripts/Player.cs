using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Player
{
    public string name;

    public int wins;
    public int draws;
    public int losses;

    public static Player PlayerFromDict(Dictionary<string, object> dict)
    {
        string name = dict["name"] as string;
        int wins = dict["wins"] as int? ?? 0;
        int draws = dict["draws"] as int? ?? 0;
        int losses = dict["losses"] as int? ?? 0;

        return new Player(name, wins, draws, losses);
    }

    public Player(string name, int wins, int draws, int losses)
    {
        this.name = name;
        this.wins = wins;
        this.draws = draws;
        this.losses = losses;
    }
}
