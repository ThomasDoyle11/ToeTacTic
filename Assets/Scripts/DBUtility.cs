using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using System.Linq;
using System.Threading.Tasks;

public static class DBUtility
{
    public static DatabaseReference reference { get; private set; }

    public static void SetDatabaseReference(DatabaseReference reference)
    {
        DBUtility.reference = reference;
    }

    public static bool CheckDatabaseReference()
    {
        return reference != null;
    }

    public static void GetValueThenDoTask(string dataPath, Action<Task<DataSnapshot>> action)
    {
        if (!CheckDatabaseReference())
        {
            Debug.Log("Database Reference not set");
            return;
        }

        reference.Child(dataPath).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("Task cancelled at " + dataPath);
                return;
            }

            if (task.IsFaulted)
            {
                Debug.Log("Task faulted at " + dataPath);
                return;
            }

            if (!task.IsCompleted)
            {
                Debug.Log("Task incomplete at " + dataPath);
                return;
            }

            action.Invoke(task);

        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public static void SetJsonValueThenDoTask(string dataPath, string json, Action<Task> action)
    {
        if (!CheckDatabaseReference())
        {
            Debug.Log("Database Reference not set");
            return;
        }

        reference.Child(dataPath).SetRawJsonValueAsync(json).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("Task cancelled at " + dataPath);
                return;
            }

            if (task.IsFaulted)
            {
                Debug.Log("Task faulted at " + dataPath);
                return;
            }

            if (!task.IsCompleted)
            {
                Debug.Log("Task incomplete at " + dataPath);
                return;
            }

            action.Invoke(task);

        }, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
