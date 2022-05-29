using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using UnityEngine.UI;
using System.Linq;
using System.Threading.Tasks;

public class GameController : MonoBehaviour
{
    public TextMeshProUGUI[] board;
    public DatabaseReference reference;
    public Button joinGameButton;
    public TextMeshProUGUI info;
    public string queueKey;
    public string matchKey;
    public Player player;
    public bool isFirstPlayer;
    public int playerNum
    {
        get
        {
            return isFirstPlayer ? 0 : 1;
        }
    }
    public Match match;
    public bool yourTurn
    {
        get
        {
            return match.isFirstPlayersTurn && isFirstPlayer || !match.isFirstPlayersTurn && !isFirstPlayer;
        }
    }
    public bool matchFound;
    public bool firstTurnChangeListenIgnored;
    public bool firstMatchListenIgnored;
    public bool squareClickedThisRound;

    // Start is called before the first frame update
    void Start()
    {
        reference = FirebaseDatabase.DefaultInstance.RootReference;
        reference.KeepSynced(false);
        DBUtility.SetDatabaseReference(reference);
        Debug.Log(reference);

        squareClickedThisRound = false;
        firstMatchListenIgnored = false;
        firstTurnChangeListenIgnored = false;
        matchFound = false;
        match = null;
        isFirstPlayer = false;
        queueKey = "";
        matchKey = "";
        joinGameButton.onClick.AddListener(LookForGame);
        ShowInfoText("Welcome");

        player = new Player("Poop", 0, 0, 0);
    }

    public void LookForGame()
    {
        // Check if you are already in a game or the Waiting Room
        // If so, return

        // If no,
        DataSnapshot snapshot = null;
        DBUtility.GetValueThenDoTask("waitingRoom", task =>
        {

            snapshot = task.Result;

            // Check if anyone else is in Waiting Room
            if (snapshot.ChildrenCount > 0)
            {
                // If so, don't join Waiting Room and start match with them
                DataSnapshot[] playersWaiting = snapshot.Children.ToArray();
                foreach (DataSnapshot playerWaiting in playersWaiting)
                {
                    Debug.Log(playerWaiting.Key);
                }
                string enemyKey = playersWaiting[0].Key;

                CreateGame(enemyKey);
            }
            else
            {
                // If no, join Waiting Room
                JoinWaitingRoom();
            }

        });
        //reference.Child("waitingRoom").GetValueAsync().ContinueWith(task =>
        //{

        //    snapshot = task.Result;

        //    // Check if anyone else is in Waiting Room
        //    if (snapshot.ChildrenCount > 0)
        //    {
        //        // If so, don't join Waiting Room and start match with them
        //        DataSnapshot[] playersWaiting = snapshot.Children.ToArray();
        //        foreach (DataSnapshot playerWaiting in playersWaiting)
        //        {
        //            Debug.Log(playerWaiting.Key);
        //        }
        //        string enemyKey = playersWaiting[0].Key;

        //        CreateGame(enemyKey);
        //    }
        //    else
        //    {
        //        // If no, join Waiting Room
        //        JoinWaitingRoom();
        //    }

        //}, TaskScheduler.FromCurrentSynchronizationContext());

        joinGameButton.transform.gameObject.SetActive(false);
    }

    public void ShowInfoText(string text)
    {
        info.text = text;
        Debug.Log(text);
    }

    public void JoinWaitingRoom()
    {
        ShowInfoText("Waiting");
        queueKey = reference.Child("waitingRoom").Push().Key;
        Debug.Log(queueKey);
        reference.Child("waitingRoom").Child(queueKey).SetRawJsonValueAsync(JsonUtility.ToJson(player)).ContinueWith(task =>
        {
            // Add Listener for when new match starts
            reference.Child("matches").ValueChanged += JoinGame;
            Debug.Log("Listener added");
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void JoinGame(object sender, ValueChangedEventArgs args)
    {
        if (matchFound)
        {
            Debug.Log("Match already found");
            return;
        }

        if (!firstMatchListenIgnored)
        {
            firstMatchListenIgnored = true;
            return;
        }

        matchFound = true;
        ShowInfoText("Game found");
        matchKey = queueKey;
        Debug.Log(matchKey);
        reference.Child("matches").ValueChanged -= JoinGame;
        reference.Child("matches").Child(matchKey).GetValueAsync().ContinueWith(task2 =>
        {
            DataSnapshot snapshot2 = task2.Result;

            Dictionary<string, object> newDict = snapshot2.Value as Dictionary<string, object>;
            // THIS LINE MAKES ALL SUBSEQUENT LINES NOT EXECUTE FOR SOME REASON
            //match = Match.MatchFromDict(newDict);
            match = new Match(player, player);

            SetGameParams(false);

        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void CreateGame(string enemyKey)
    {
        ShowInfoText("Enemy found");
        DataSnapshot snapshot = null;
        reference.Child("waitingRoom").Child(enemyKey).GetValueAsync().ContinueWith(task =>
        {
            snapshot = task.Result;

            Dictionary<string, object> newDict = snapshot.Value as Dictionary<string, object>;

            Player enemyPlayer = Player.PlayerFromDict(newDict);

            match = new Match(player, enemyPlayer);

            string matchJson = JsonUtility.ToJson(match);

            matchKey = enemyKey;
            reference.Child("matches").Child(matchKey).SetRawJsonValueAsync(matchJson).ContinueWith(task2 =>
            {

                reference.Child("waitingRoom").Child(enemyKey).RemoveValueAsync();

                SetGameParams(true);

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void SetGameParams(bool isFirstPlayer)
    {
        Debug.Log("Game params set for:" + isFirstPlayer);
        this.isFirstPlayer = isFirstPlayer;
        ResetSquaresLocal();
        SetTurnChangeListener();
        SetSquareChangeListener();
        ShowInfoText(isFirstPlayer ? "Your turn" : "Enemy turn");
    }

    public void SetSquareLocal(int squareNumber, int player)
    {
        TextMeshProUGUI squareText;
        if (squareNumber < board.Length && squareNumber >= 0)
        {
            squareText = board[squareNumber];
        }
        else
        {
            Debug.Log("Square Number out of range: " + squareNumber);
            return;
        }

        if (player == -1)
        {
            match.board[squareNumber] = -1;
            squareText.text = "";
        }
        else if (player == 0)
        {
            match.board[squareNumber] = 0;
            squareText.text = "O";
        }
        else if (player == 1)
        {
            match.board[squareNumber] = 1;
            squareText.text = "X";
        }
        else
        {
            Debug.Log("Unknown value for square: " + player);
        }
    }

    public void SetSquare(int squareNumber, int player)
    {
        SetSquareLocal(squareNumber, player);
        SendSquareChange(squareNumber);

        // Check for win
    }

    public void ResetSquaresLocal()
    {
        for (int i = 0; i < 9; i++)
        {
            SetSquareLocal(i, -1);
        }
    }

    public void ClickSquare(int squareNumber)
    {
        if (squareClickedThisRound)
        {
            Debug.Log("One square click per round.");
            return;
        }

        if (match == null)
        {
            Debug.Log("Match not yet started!");
            return;
        }

        if (!yourTurn)
        {
            Debug.Log("Not your turn!");
            return;
        }

        if (match.board[squareNumber] != -1)
        {
            Debug.Log("Square already taken!");
            return;
        }

        squareClickedThisRound = true;
        SetSquare(squareNumber, playerNum);
        SendTurnChange();  
    }

    public void SetSquareChangeListener()
    {
        reference.Child("matches").Child(matchKey).Child("board").ValueChanged += ReceiveSquareChange;
    }

    public void ReceiveSquareChange(object sender, ValueChangedEventArgs args)
    {
        int[] results = (args.Snapshot.Value as List<object>).OfType<long>().Select(o => (int)o).ToArray();
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i] != match.board[i])
            {
                SetSquareLocal(i, results[i]);
            }
        }
    }

    public void SendSquareChange(int squareNumber)
    {
        reference.Child("matches").Child(matchKey).Child("board").Child("" + squareNumber).SetValueAsync(match.board[squareNumber]);
    }

    public void SetTurnChangeListener()
    {
        reference.Child("matches").Child(matchKey).Child("isFirstPlayersTurn").ValueChanged += ReceiveTurnChange; ;
    }

    public void ReceiveTurnChange(object sender, ValueChangedEventArgs args)
    {
        if (!firstTurnChangeListenIgnored)
        {
            firstTurnChangeListenIgnored = true;
            Debug.Log("First turn not yet taken.");
            return;
        }
        Debug.Log("Turn changed on network");
        match.isFirstPlayersTurn = !match.isFirstPlayersTurn;
        squareClickedThisRound = false;
        ShowInfoText(yourTurn ? "Your turn" : "Enemy turn");
    }

    public void SendTurnChange()
    {
        Debug.Log("Send turn change");
        reference.Child("matches").Child(matchKey).Child("isFirstPlayersTurn").SetValueAsync(!isFirstPlayer);
    }
}
