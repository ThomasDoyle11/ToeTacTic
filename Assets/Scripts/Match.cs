using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Match
{
    public Player[] players;

    public int[] board; // Left to right, then top to bottom

    public bool isFirstPlayersTurn;

    public int winner; // -2 = unfinished, -1 = draw, 0 = 0, 1 = 1

    public bool isMatchOver
    {
        get
        {
            return winner != -2;
        }
    }

    public static Match MatchFromDict(Dictionary<string, object> dict)
    {
        Player[] players = dict["players"] as Player[];

        return new Match(players[0], players[1]);
    }

    public Match(Player firstPlayer, Player secondPlayer)
    {
        players = new Player[] { firstPlayer, secondPlayer };
        board = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        isFirstPlayersTurn = true;
        winner = -2;
    }
}
